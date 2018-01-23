using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace UniGLTF
{
    public struct PosRot
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public static PosRot FromGlobalTransform(Transform t)
        {
            return new PosRot
            {
                Position = t.position,
                Rotation = t.rotation,
            };
        }
    }

    public class glTF
    {
        public string baseDir
        {
            get;
            set;
        }

        public glTFAssets asset;

        //public GltfBuffer buffer;
        glTFBuffer[] m_buffers;
        glTFBufferView[] m_bufferViews;
        glTFAccessor[] m_accessors;
        ArraySegment<Byte>[] m_bytesList;
        #region Importer
        T[] GetAttrib<T>(glTFAccessor accessor, glTFBufferView view) where T : struct
        {
            var attrib = new T[accessor.count];
            //
            var bytes = new ArraySegment<Byte>(m_bytesList[view.buffer].Array, m_bytesList[view.buffer].Offset + view.byteOffset + accessor.byteOffset, accessor.count * view.byteStride);
            bytes.MarshalCoyTo(attrib);
            return attrib;
        }

        public ArraySegment<Byte> GetViewBytes(int bufferView)
        {
            var view = m_bufferViews[bufferView];
            return new ArraySegment<byte>(m_bytesList[view.buffer].Array, m_bytesList[view.buffer].Offset + view.byteOffset, view.byteLength);
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

        public struct MeshWithMaterials
        {
            public Mesh Mesh;
            public Material[] Materials;
        }

        public MeshWithMaterials ReadMesh(glTFMesh gltfMesh, Material[] materials)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var boneWeights = new List<BoneWeight>();
            var subMeshes = new List<int[]>();
            var materialIndices = new List<int>();
            foreach (var prim in gltfMesh.primitives)
            {
                var indexOffset = positions.Count;
                var indexBuffer = prim.indices;

                positions.AddRange(GetBuffer<Vector3>(prim.attributes.POSITION).Select(x => x.ReverseZ()));

                // normal
                if (prim.attributes.NORMAL != -1)
                {
                    normals.AddRange(GetBuffer<Vector3>(prim.attributes.NORMAL).Select(x => x.ReverseZ()));
                }
                // uv
                if (prim.attributes.TEXCOORD_0 != -1)
                {
                    uv.AddRange(GetBuffer<Vector2>(prim.attributes.TEXCOORD_0).Select(x => x.ReverseY()));
                }

                // skin
                if (prim.attributes.JOINTS_0 != -1 && prim.attributes.WEIGHTS_0 != -1)
                {
                    var joints0 = GetBuffer<UShort4>(prim.attributes.JOINTS_0); // uint4
                    var weights0 = GetBuffer<Float4>(prim.attributes.WEIGHTS_0).Select(x => x.One()).ToArray();

                    var weightNorms = weights0.Select(x => x.x + x.y + x.z + x.w).ToArray();

                    for (int j = 0; j < joints0.Length; ++j)
                    {
                        var bw = new BoneWeight();

                        bw.boneIndex0 = joints0[j].x;
                        bw.weight0 = weights0[j].x;

                        bw.boneIndex1 = joints0[j].y;
                        bw.weight1 = weights0[j].y;

                        bw.boneIndex2 = joints0[j].z;
                        bw.weight2 = weights0[j].z;

                        bw.boneIndex3 = joints0[j].w;
                        bw.weight3 = weights0[j].w;

                        boneWeights.Add(bw);
                    }
                }

                subMeshes.Add(GetIndices(indexBuffer).Select(x => x + indexOffset).ToArray());

                // material
                materialIndices.Add(prim.material);

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
            mesh.name = gltfMesh.name;

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
        #endregion

        #region Exporter
        public int AddBuffer(Byte[] bytes)
        {
            throw new NotImplementedException();
        }
        #endregion

        public gltfTexture[] textures;
        public gltfImage[] images;

        public IEnumerable<TextureWithIsAsset> ReadTextures()
        {
            if (textures == null)
            {
                return new TextureWithIsAsset[] { };
            }
            else
            {
                return textures.Select(x => x.GetTexture(baseDir, this, images));
            }
        }

        public GltfMaterial[] materials;

        public IEnumerable<Material> ReadMaterials(Texture2D[] textures)
        {
            var shader = Shader.Find("Standard");
            if (materials == null)
            {
                var material = new Material(shader);
                return new Material[] { material };
            }
            else
            {
                return materials.Select(x =>
                {
                    var material = new Material(shader);

                    material.name = x.name;

                    if (x.pbrMetallicRoughness != null)
                    {
                        if (x.pbrMetallicRoughness.baseColorFactor != null)
                        {
                            var color = x.pbrMetallicRoughness.baseColorFactor;
                            material.color = new Color(color[0], color[1], color[2], color[3]);
                        }

                        if (x.pbrMetallicRoughness.baseColorTexture.index != -1)
                        {
                            material.mainTexture = textures[x.pbrMetallicRoughness.baseColorTexture.index];
                        }
                    }

                    return material;
                });
            }
        }

        public glTFMesh[] meshes;

        public gltfNode[] nodes;

        public gltfSkin[] skins;

        public int scene;

        [Serializable]
        public struct gltfScene
        {
            public int[] nodes;
        }

        public gltfScene[] scenes;

        public int[] rootnodes
        {
            get
            {
                return scenes[scene].nodes;
            }
        }

        public GltfAnimation[] animations;

        public static glTF FromGameObject(GameObject go)
        {
            var gltf = new glTF();

            gltf.asset = new glTFAssets
            {
                generator="UniGLTF",
                version=2.0f,
            };

            var copy = GameObject.Instantiate(go);
            try
            {
                // Left handed to Right handed
                go.transform.ReverseZ();
            }
            finally
            {
                if (Application.isEditor)
                {
                    GameObject.DestroyImmediate(copy);
                }
                else
                {
                    GameObject.Destroy(copy);
                }
            }

            //var buffer = new GltfBuffer();

            var nodes = go.transform.Traverse().Skip(1).ToList();

            var meshes = nodes.Select(x => x.GetSharedMesh()).Where(x => x != null).ToList();
            var skins = nodes.Select(x => x.GetComponent<SkinnedMeshRenderer>()).Where(x => x!= null).ToList();

            gltf.nodes = nodes.Select(x => gltfNode.Create(x, nodes, meshes, skins)).ToArray();

            var materials = nodes.SelectMany(x => x.GetSharedMaterials()).Distinct().ToArray();
            var textures = materials.Select(x => x.mainTexture).Distinct().ToArray();

            /*
            var textureWithImages = materials.Select(x => (Texture2D)x.mainTexture).Where(x => x != null).Select(x => gltfTexture.Create(x, buffer)).ToArray();

            gltf.materials = materials.Select(x => GltfMaterial.Create(x)).ToArray();

            gltf.meshes = meshes.Select(x => glTFMesh.Create(x, materials)).ToArray();
            */

            return gltf;
        }

        public override string ToString()
        {
            return string.Format("{0}", asset);
        }

        public ArraySegment<Byte> ToJson()
        {
            var formatter = new JsonFormatter();
            formatter.BeginMap();



            formatter.EndMap();
            return formatter.GetStore().Bytes;
        }

        public static glTF Parse(string json, string baseDir, ArraySegment<Byte> glbDataBytes)
        {
            var parsed = json.ParseAsJson();

            var gltf = new glTF
            {
                baseDir = baseDir,
            };

            // asset
            gltf.asset = JsonUtility.FromJson<glTFAssets>(parsed["asset"].Segment.ToString());
            if (gltf.asset.version != 2.0f)
            {
                throw new NotImplementedException(string.Format("unknown version: {0}", gltf.asset.version));
            }

            // buffer
            //gltf.buffer = new GltfBuffer(parsed, baseDir, bytes);
            gltf.m_buffers = parsed["buffers"].DeserializeList<glTFBuffer>();
            gltf.m_bufferViews = parsed["bufferViews"].DeserializeList<glTFBufferView>();
            gltf.m_accessors = parsed["accessors"].DeserializeList<glTFAccessor>();
            if (glbDataBytes.Count > 0)
            {
                gltf.m_bytesList = new ArraySegment<Byte>[]
                {
                    glbDataBytes
                };
            }
            else
            {
                gltf.m_bytesList = gltf.m_buffers.Select(x => new ArraySegment<Byte>(x.GetBytes(baseDir))).ToArray();
            }

            // texture
            if (parsed.HasKey("textures"))
            {
                gltf.textures = parsed["textures"].DeserializeList<gltfTexture>();
            }

            if (parsed.HasKey("images"))
            {
                gltf.images = parsed["images"].DeserializeList<gltfImage>();
            }

            // material
            if (parsed.HasKey("materials"))
            {
                gltf.materials = parsed["materials"].DeserializeList<GltfMaterial>();
            }

            // mesh
            if (parsed.HasKey("meshes"))
            {
                gltf.meshes = parsed["meshes"].DeserializeList<glTFMesh>();
            }

            // nodes
            gltf.nodes = parsed["nodes"].DeserializeList<gltfNode>();

            // skins
            if (parsed.HasKey("skins"))
            {
                gltf.skins = parsed["skins"].DeserializeList<gltfSkin>();
            }

            // scene;
            if (parsed.HasKey("scene"))
            {
                gltf.scene=parsed["scene"].GetInt32();
            }
            gltf.scenes = parsed["scenes"].DeserializeList<gltfScene>();

            // animations
            if (parsed.HasKey("animations"))
            {
                gltf.animations = parsed["animations"].DeserializeList<GltfAnimation>();
            }

            return gltf;
        }
    }
}
