using Osaru;
using Osaru.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public class GltfBuffer
    {
        Byte[][] m_bytesList;
        Buffer[] m_buffers;
        BufferView[] m_bufferViews;
        Accessor[] m_accessors;

        static T[] DeserializeJsonList<T>(JsonParser jsonList)
        {
            return jsonList.ListItems.Select(x => JsonUtility.FromJson<T>(x.ToJson())).ToArray();
        }

        public GltfBuffer(JsonParser parsed, string dir)
        {
            // asset
            var asset = parsed["asset"];
            var generator = "unknown";
            if (parsed.ObjectItems.Any(x => x.Key == "generator"))
            {
                generator = parsed["generator"].GetString();
            }
            var version = float.Parse(asset["version"].GetString());
            if (version != 2.0f)
            {
                throw new NotImplementedException(string.Format("unknown version: {0}", version));
            }
            Debug.LogFormat("{0}: glTF-{1}", generator, version);

            m_buffers = DeserializeJsonList<Buffer>(parsed["buffers"]);
            m_bufferViews = DeserializeJsonList<BufferView>(parsed["bufferViews"]);
            m_accessors = DeserializeJsonList<Accessor>(parsed["accessors"]);
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

        public struct BlendShape
        {
            public Vector3[] Positions;
            public Vector3[] Normals;
            public Vector3[] Tangents;
        }

        public BlendShape ReadBlendShape(JsonParser targetJson)
        {
            var blendShape = new BlendShape();
            if (targetJson.ObjectItems.Any(x => x.Key == "POSITION"))
            {
                blendShape.Positions = GetBuffer<Vector3>(targetJson["POSITION"].GetInt32()).Select(x => x.ReverseZ()).ToArray();
            }
            if (targetJson.ObjectItems.Any(x => x.Key == "NORMAL"))
            {
                blendShape.Normals = GetBuffer<Vector3>(targetJson["NORMAL"].GetInt32()).Select(x => x.ReverseZ()).ToArray();
            }
            if (targetJson.ObjectItems.Any(x => x.Key == "TANGENT"))
            {
                blendShape.Tangents = GetBuffer<Vector3>(targetJson["TANGENT"].GetInt32())/*.Select(ReverseZ).ToArray()*/;
            }
            return blendShape;
        }

        public Mesh ReadMesh(JsonParser meshJson, int i)
        {
            //Debug.Log(prims.ToJson());
            var mesh = new Mesh();
            if (meshJson.ObjectItems.Any(x => x.Key == "name"))
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

            foreach (var prim in meshJson["primitives"].ListItems)
            {
                var indexBuffer = prim["indices"].GetInt32();
                var attribs = prim["attributes"].ObjectItems.ToDictionary(x => x.Key, x => x.Value.GetInt32());

                // positions
                mesh.vertices = GetBuffer<Vector3>(attribs["POSITION"]).Select(x => x.ReverseZ()).ToArray();

                // indices
                mesh.SetIndices(GetIndices(indexBuffer), MeshTopology.Triangles, 0);

                if (attribs.ContainsKey("NORMAL"))
                {
                    mesh.normals = GetBuffer<Vector3>(attribs["NORMAL"]).Select(x => x.ReverseZ()).ToArray();
                }
                else
                {
                    mesh.RecalculateNormals();
                }

                // TEXCOORD_0

                // blendshape
                if (prim.ObjectItems.Any(x => x.Key == "targets"))
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

            return mesh;
        }

        public Mesh[] ReadMeshes(JsonParser meshesJson)
        {
            return meshesJson.ListItems.Select((x, j) => ReadMesh(x, j)).ToArray();
        }
    }
}
