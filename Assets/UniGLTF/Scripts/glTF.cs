using Osaru;
using Osaru.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace UniGLTF
{
    [Serializable]
    public struct Image
    {
        public string uri;
    }

    [Serializable]
    public struct Texture
    {
        public int sampler;
        public int source;
    }

    public class GltfTexture
    {
        Texture[] m_textures;
        Image[] m_images;

        public GltfTexture(JsonParser parsed)
        {
            if (parsed.HasKey("textures"))
            {
                m_textures = parsed["textures"].DeserializeList<Texture>();
            }
            if (parsed.HasKey("images"))
            {
                m_images = parsed["images"].DeserializeList<Image>();
            }
        }

        public IEnumerable<Texture2D> GetTextures(string dir)
        {
            foreach(var x in m_textures)
            {
                var path = Path.Combine(dir, m_images[x.source].uri);
                Debug.LogFormat("load texture: {0}", path);

                /*
                var bytes = File.ReadAllBytes(path);

                var texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                */
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                yield return texture;
            }
        }

        public static Texture2D[] ReadTextures(JsonParser parsed, string dir)
        {
            var texture = new GltfTexture(parsed);
            return texture.GetTextures(dir).ToArray();
        }
    }

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

    /*
    [Serializable]
    struct IndexView
    {
        public int bufferView;
        public int componentType;
    }

    [Serializable]
    struct Values
    {
        public int bufferView;
    }

    [Serializable]
    struct Sparse
    {
        public int count;
        public Values values;
        public IndexView indices;
    }
    */

    [Serializable]
    public struct Accessor
    {
        public int bufferView;
        public int byteOffset;
        public string type;
        public int componentType;
        public int count;
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
        Byte[][] m_bytesList;
        Buffer[] m_buffers;
        BufferView[] m_bufferViews;
        Accessor[] m_accessors;

        public GltfBuffer(JsonParser parsed, string dir)
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
            m_bytesList = m_buffers.Select(x => x.GetBytes(dir)).ToArray();
        }

        T[] GetAttrib<T>(Accessor accessor, BufferView view) where T : struct
        {
            var attrib = new T[accessor.count];
            var bytes = new ArraySegment<Byte>(m_bytesList[view.buffer], view.byteOffset + accessor.byteOffset, accessor.count * view.byteStride);
            bytes.MarshalCoyTo(attrib);
            return attrib;
        }

        public int[] GetIndices(int index)
        {
            var accessor = m_accessors[index];
            var view = m_bufferViews[accessor.bufferView];
            switch (accessor.componentType)
            {
                case 5123: // GL_UNSIGNED_SHORT:
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

        static IEnumerable<int> FlipTriangle(IEnumerable<UInt16> src)
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

        MeshWithMaterials ReadMesh(JsonParser meshJson, int i, Material[] materials)
        {
            //Debug.Log(prims.ToJson());
            var mesh = new Mesh();
            var materialIndices = new List<int>();
            if (meshJson.HasKey("name"))
            {
                mesh.name = meshJson["name"].GetString();
            }
            else
            {
                mesh.name = string.Format("UniGLTF import#{0}", i);
            }

            if (meshJson["primitives"].ListItems.Count() > 1)
            {
                throw new NotImplementedException("multi primitives");
            }

            var result = new MeshWithMaterials
            {
                Mesh = mesh,
            };
            
            foreach (var prim in meshJson["primitives"].ListItems)
            {
                var indexBuffer = prim["indices"].GetInt32();
                var attribs = prim["attributes"].ObjectItems.ToDictionary(x => x.Key, x => x.Value.GetInt32());

                // positions
                mesh.vertices = GetBuffer<Vector3>(attribs["POSITION"]).Select(x => x.ReverseZ()).ToArray();

                // indices
                mesh.SetIndices(GetIndices(indexBuffer), MeshTopology.Triangles, 0);

                // normal
                if (attribs.ContainsKey("NORMAL"))
                {
                    mesh.normals = GetBuffer<Vector3>(attribs["NORMAL"]).Select(x => x.ReverseZ()).ToArray();
                }
                else
                {
                    mesh.RecalculateNormals();
                }

                // uv
                if(attribs.ContainsKey("TEXCOORD_0"))
                {
                    mesh.uv = GetBuffer<Vector2>(attribs["TEXCOORD_0"]).Select(x => x.ReverseY()).ToArray();
                }

                // skin
                if (attribs.ContainsKey("JOINTS_0") && attribs.ContainsKey("WEIGHTS_0"))
                {
                    var joints0 = GetBuffer<int>(attribs["JOINTS_0"]);
                    var weights0 = GetBuffer<float>(attribs["WEIGHTS_0"]);

                    var boneWeights = new BoneWeight[joints0.Length];
                    for (int j=0; j<joints0.Length; ++j)
                    {
                        var bw = new BoneWeight();
                        bw.boneIndex0 = joints0[j];
                        bw.weight0 = weights0[j];
                    }
                    mesh.boneWeights = boneWeights;
                }

                // material
                if (prim.HasKey("material"))
                {
                    materialIndices.Add(prim["material"].GetInt32());
                }

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

                break;
            }

            if (!materialIndices.Any())
            {
                materialIndices.Add(0);
            }
            result.Materials = materialIndices.Select(x => materials[x]).ToArray();

            return result;
        }

        public MeshWithMaterials[] ReadMeshes(JsonParser meshesJson, Material[] materials)
        {
            return meshesJson.ListItems.Select((x, j) => ReadMesh(x, j, materials)).ToArray();
        }
    }
}
