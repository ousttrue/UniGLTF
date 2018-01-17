using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


namespace UniGLTF
{
    [Serializable]
    public struct Buffer
    {
        public string uri;
        public int byteLength;

        const string DataPrefix = "data:application/octet-stream;base64,";

        public Byte[] GetBytes(string baseDir)
        {
            if (uri.StartsWith(DataPrefix))
            {
                // embeded
                return Convert.FromBase64String(uri.Substring(DataPrefix.Length));
            }
            else
            {
                // as local file path
                return File.ReadAllBytes(Path.Combine(baseDir, uri));
            }
        }
    }

    [Serializable]
    public struct BufferView
    {
        public int buffer;
        public int byteOffset;
        public int byteLength;
        public int byteStride;
        public int target; // ARRAY_BUFFER
    }

    [Serializable]
    public struct Accessor
    {
        public int bufferView;
        public int byteOffset;
        public string type;
        public int componentType;
        public int count;
        public float[] max;
        public float[] min;
    }

    public struct MeshWithMaterials
    {
        public Mesh Mesh;
        public Material[] Materials;
    }

    [Serializable]
    public struct Skin
    {
        public int inverseBindMatrices;
        public int[] joints;
    }

    public class GltfBuffer
    {
        ArraySegment<Byte>[] m_bytesList;
        Buffer[] m_buffers;
        BufferView[] m_bufferViews;
        Accessor[] m_accessors;

        public GltfBuffer(JsonParser parsed, string dir, ArraySegment<byte> glbDataBytes)
        {
            // asset
            var asset = parsed["asset"];
            var generator = "unknown";
            if (parsed.HasKey("generator"))
            {
                generator = parsed["generator"].GetString();
            }
            var version = float.Parse(asset["version"].GetString());
            if (version != 2.0f)
            {
                throw new NotImplementedException(string.Format("unknown version: {0}", version));
            }
            Debug.LogFormat("{0}: glTF-{1}", generator, version);

            m_buffers = parsed["buffers"].DeserializeList<Buffer>();
            m_bufferViews = parsed["bufferViews"].DeserializeList<BufferView>();
            m_accessors = parsed["accessors"].DeserializeList<Accessor>();

            if (glbDataBytes.Count > 0)
            {
                m_bytesList = new ArraySegment<Byte>[]
                {
                    glbDataBytes
                };
            }
            else
            {
                m_bytesList = m_buffers.Select(x => new ArraySegment<Byte>(x.GetBytes(dir))).ToArray();
            }
        }

        T[] GetAttrib<T>(Accessor accessor, BufferView view) where T : struct
        {
            var attrib = new T[accessor.count];
            //
            var bytes = new ArraySegment<Byte>(m_bytesList[view.buffer].Array, m_bytesList[view.buffer].Offset+view.byteOffset + accessor.byteOffset, accessor.count * view.byteStride);
            bytes.MarshalCoyTo(attrib);
            return attrib;
        }

        public int[] GetIndices(int index)
        {
            var accessor = m_accessors[index];
            var view = m_bufferViews[accessor.bufferView];
            switch ((glComponentType)accessor.componentType)
            {
                case glComponentType.UNSIGNED_BYTE:
                    {
                        var indices = GetAttrib<Byte>(accessor, view);
                        return FlipTriangle(indices).ToArray();
                    }

                case glComponentType.UNSIGNED_SHORT:
                    {
                        var indices = GetAttrib<UInt16>(accessor, view);
                        return FlipTriangle(indices).ToArray();
                    }
            }

            throw new NotImplementedException("GetIndices: unknown componenttype: " + accessor.componentType);
        }

        public T[] GetBuffer<T>(int index) where T : struct
        {
            var vertexAccessor = m_accessors[index];
            var view = m_bufferViews[vertexAccessor.bufferView];
            return GetAttrib<T>(vertexAccessor, view);
        }

        static IEnumerable<int> FlipTriangle(IEnumerable<Byte> src)
        {
            return FlipTriangle(src.Select(x => (Int32)x));
        }

        static IEnumerable<int> FlipTriangle(IEnumerable<UInt16> src)
        {
            return FlipTriangle(src.Select(x => (Int32)x));
        }

        static IEnumerable<int> FlipTriangle(IEnumerable<Int32> src)
        {
            var it = src.GetEnumerator();
            while (true)
            {
                if (!it.MoveNext())
                {
                    yield break;
                }
                var i0 = it.Current;

                if (!it.MoveNext())
                {
                    yield break;
                }
                var i1 = it.Current;

                if (!it.MoveNext())
                {
                    yield break;
                }
                var i2 = it.Current;

                yield return i2;
                yield return i1;
                yield return i0;
            }
        }

        struct BlendShape
        {
            public Vector3[] Positions;
            public Vector3[] Normals;
            public Vector3[] Tangents;
        }

        BlendShape ReadBlendShape(JsonParser targetJson)
        {
            var blendShape = new BlendShape();
            if (targetJson.HasKey("POSITION"))
            {
                blendShape.Positions = GetBuffer<Vector3>(targetJson["POSITION"].GetInt32()).Select(x => x.ReverseZ()).ToArray();
            }
            if (targetJson.HasKey("NORMAL"))
            {
                blendShape.Normals = GetBuffer<Vector3>(targetJson["NORMAL"].GetInt32()).Select(x => x.ReverseZ()).ToArray();
            }
            if (targetJson.HasKey("TANGENT"))
            {
                blendShape.Tangents = GetBuffer<Vector3>(targetJson["TANGENT"].GetInt32())/*.Select(ReverseZ).ToArray()*/;
            }
            return blendShape;
        }

        [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct UShort4
        {
            public ushort x;
            public ushort y;
            public ushort z;
            public ushort w;
        }
        [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Float4
        {
            public float x;
            public float y;
            public float z;
            public float w;

            public Float4 One()
            {
                var sum = x + y + z + w;
                var f = 1.0f / sum;
                return new Float4
                {
                    x = x * f,
                    y = y * f,
                    z = z * f,
                    w = w * f,
                };
            }
        }

        MeshWithMaterials ReadMesh(JsonParser meshJson, int meshIndex, Material[] materials)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var boneWeights = new List<BoneWeight>();
            var subMeshes = new List<int[]>();
            var materialIndices = new List<int>();
            foreach (var prim in meshJson["primitives"].ListItems)
            {
                var indexOffset = positions.Count;
                var indexBuffer = prim["indices"].GetInt32();
                var attribs = prim["attributes"].ObjectItems.ToDictionary(x => x.Key, x => x.Value.GetInt32());

                positions.AddRange(GetBuffer<Vector3>(attribs["POSITION"]).Select(x => x.ReverseZ()));

                // normal
                if (attribs.ContainsKey("NORMAL"))
                {
                    normals.AddRange(GetBuffer<Vector3>(attribs["NORMAL"]).Select(x => x.ReverseZ()));
                }
                // uv
                if (attribs.ContainsKey("TEXCOORD_0"))
                {
                    uv.AddRange(GetBuffer<Vector2>(attribs["TEXCOORD_0"]).Select(x => x.ReverseY()).ToArray());
                }

                // skin
                if (attribs.ContainsKey("JOINTS_0") && attribs.ContainsKey("WEIGHTS_0"))
                {
                    var jointsAccessor = m_accessors[attribs["JOINTS_0"]];
                    var joints0 = GetBuffer<UShort4>(attribs["JOINTS_0"]); // uint4
                    var weightsAccessor = m_accessors[attribs["WEIGHTS_0"]];
                    var weights0 = GetBuffer<Float4>(attribs["WEIGHTS_0"]).Select(x => x.One()).ToArray();

                    var weightNorms = weights0.Select(x => x.x + x.y + x.z + x.w).ToArray();

                    //var _boneWeights = new BoneWeight[joints0.Length];
                    for (int j = 0; j < joints0.Length; ++j)
                    {
                        var bw = new BoneWeight();

#if false
                        bw.boneIndex0 = (int)Mathf.Clamp(joints0[j].x, jointsAccessor.min[0], jointsAccessor.max[0]);
                        bw.weight0 = Mathf.Clamp(weights0[j].x, weightsAccessor.min[0], weightsAccessor.max[0]);

                        bw.boneIndex1 = (int)Mathf.Clamp(joints0[j].y, jointsAccessor.min[1], jointsAccessor.max[1]);
                        bw.weight1 = Mathf.Clamp(weights0[j].y, weightsAccessor.min[1], weightsAccessor.max[1]);

                        bw.boneIndex2 = (int)Mathf.Clamp(joints0[j].z, jointsAccessor.min[2], jointsAccessor.max[2]);
                        bw.weight2 = Mathf.Clamp(weights0[j].z, weightsAccessor.min[2], weightsAccessor.max[2]);

                        bw.boneIndex3 = (int)Mathf.Clamp(joints0[j].w, jointsAccessor.min[3], jointsAccessor.max[3]);
                        bw.weight3 = Mathf.Clamp(weights0[j].w, weightsAccessor.min[3], weightsAccessor.max[3]);
#else
                        bw.boneIndex0 = joints0[j].x;
                        bw.weight0 = weights0[j].x;

                        bw.boneIndex1 = joints0[j].y;
                        bw.weight1 = weights0[j].y;

                        bw.boneIndex2 = joints0[j].z;
                        bw.weight2 = weights0[j].z;

                        bw.boneIndex3 = joints0[j].w;
                        bw.weight3 = weights0[j].w;
#endif

                        boneWeights.Add(bw);
                    }
                }

                subMeshes.Add(GetIndices(indexBuffer).Select(x => x + indexOffset).ToArray());

                // material
                if (prim.HasKey("material"))
                {
                    materialIndices.Add(prim["material"].GetInt32());
                }

                /*
                // blendshape
                if (prim.HasKey("targets"))
                {
                    int j = 0;
                    foreach (var x in prim["targets"].ListItems)
                    {
                        var blendShape = ReadBlendShape(x);

                        var name = string.Format("target{0}", j++);

                        mesh.AddBlendShapeFrame(name, 1.0f, blendShape.Positions, blendShape.Normals, blendShape.Tangents);
                    }
                }
                */
            }
            if (!materialIndices.Any())
            {
                materialIndices.Add(0);
            }

            //Debug.Log(prims.ToJson());
            var mesh = new Mesh();
            if (meshJson.HasKey("name"))
            {
                mesh.name = meshJson["name"].GetString();
            }
            else
            {
                mesh.name = string.Format("UniGLTF import#{0}", meshIndex);
            }

            mesh.vertices = positions.ToArray();
            if (normals.Any())
            {
                mesh.normals = normals.ToArray();
            }
            else
            {
                mesh.RecalculateNormals();
            }
            if (uv.Any())
            {
                mesh.uv = uv.ToArray();
            }
            if (boneWeights.Any())
            {
                mesh.boneWeights = boneWeights.ToArray();
            }
            mesh.subMeshCount = subMeshes.Count;
            for (int i = 0; i < subMeshes.Count; ++i)
            {
                mesh.SetTriangles(subMeshes[i], i);
            }
            mesh.RecalculateNormals();
            var result = new MeshWithMaterials
            {
                Mesh = mesh,
                Materials = materialIndices.Select(x => materials[x]).ToArray()
            };

            return result;
        }

        public MeshWithMaterials[] ReadMeshes(JsonParser meshesJson, Material[] materials)
        {
            return meshesJson.ListItems.Select((x, j) => ReadMesh(x, j, materials)).ToArray();
        }
    }
}