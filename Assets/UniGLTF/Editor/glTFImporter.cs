using Osaru;
using Osaru.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;


namespace UniGLTF
{
    [ScriptedImporter(1, "gltf")]
    public class GLTFImporter : ScriptedImporter
    {
        [Serializable]
        struct Buffer
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
        struct BufferView
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
        struct Accessor
        {
            public int bufferView;
            public int bufferOffset;
            public string type;
            public int componentType;
            public int count;
        }

        static T[] DeserializeJsonList<T>(JsonParser jsonList)
        {
            return jsonList.ListItems.Select(x => JsonUtility.FromJson<T>(x.ToJson())).ToArray();
        }

        static int SizeOfComponentType(int compoenentType)
        {
            switch (compoenentType)
            {
                case 5123: // GL_UNSIGNED_SHORT
                    return 2;
                case 5126: // GL_FLOAT
                    return 4;
            }

            throw new NotImplementedException("SizeOfComponentType: unknown componenttype: " + compoenentType);
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

        static int[] GetIndices(Accessor accessor, ArraySegment<byte> bytes)
        {
            switch(accessor.componentType)
            {
                case 5123: // GL_UNSIGNED_SHORT:
                    {
                        var indices = GetAttrib<UInt16>(accessor, bytes);
                        return FlipTriangle(indices).ToArray();
                    }
            }

            throw new NotImplementedException("GetIndices: unknown componenttype: " + accessor.componentType);
        }

        static T[] GetAttrib<T>(Accessor accessor, ArraySegment<byte> bytes) where T: struct
        {
            var attrib = new T[accessor.count];
            bytes.MarshalCoyTo(attrib);
            return attrib;
        }

        static ArraySegment<Byte> GetBytes(Byte[][] bytesList, BufferView view)
        {
            return new ArraySegment<Byte>(bytesList[view.buffer], view.byteOffset, view.byteLength);
        }

        static Mesh ToMesh(byte[][] bytesList, 
            Accessor[] accessors, 
            BufferView[] bufferViews, 
            JsonParser meshJson, int i)
        {
            //Debug.Log(prims.ToJson());
            var mesh = new Mesh();
            mesh.name = string.Format("UniGLTF import#{0}", i);

            if (meshJson["primitives"].ListItems.Count()>1)
            {
                throw new NotImplementedException("multi primitives");
            }

            foreach (var prim in meshJson["primitives"].ListItems)
            {
                var indexBuffer = prim["indices"].GetInt32();
                //Debug.LogFormat("indices => {0}", indexBuffer);
                var attribs = prim["attributes"].ObjectItems.ToDictionary(x => x.Key, x => x.Value.GetInt32());
                /*
                foreach (var kv in attribs)
                {
                    Debug.LogFormat("{0} => {1}", kv.Key, kv.Value);
                }
                */

                // positions
                var vertexAccessor = accessors[attribs["POSITION"]];
                var vertexBytes = GetBytes(bytesList, bufferViews[vertexAccessor.bufferView]);
                mesh.vertices = GetAttrib<Vector3>(vertexAccessor, vertexBytes);

                // indices
                var indexAccessor = accessors[indexBuffer];
                var indexBytes = GetBytes(bytesList, bufferViews[indexAccessor.bufferView]);
                var indices = GetIndices(indexAccessor, indexBytes);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);

                if (attribs.ContainsKey("NORMAL"))
                {
                    var normalAccessor = accessors[attribs["NORMAL"]];
                    var normalBytes = GetBytes(bytesList, bufferViews[normalAccessor.bufferView]);
                    mesh.normals = GetAttrib<Vector3>(normalAccessor, normalBytes);
                }
                else
                {
                    mesh.RecalculateNormals();
                }

                break;
            }

            return mesh;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## GLTFImporter ##: {0}", ctx.assetPath);

            try
            {
                var json = File.ReadAllText(ctx.assetPath, Encoding.UTF8);
                var parsed = json.ParseAsJson();

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

                // buffer
                var buffers = DeserializeJsonList<Buffer>(parsed["buffers"]);
                var bufferViews = DeserializeJsonList<BufferView>(parsed["bufferViews"]);
                var accessors = DeserializeJsonList<Accessor>(parsed["accessors"]);
                var bytesList = buffers.Select(x => x.GetBytes(Path.GetDirectoryName(ctx.assetPath))).ToArray();

                // meshes
                var meshes = parsed["meshes"].ListItems.Select((x, j) => ToMesh(bytesList, accessors, bufferViews, x, j)).ToArray();
                foreach(var x in meshes)
                {
                    ctx.AddObjectToAsset(x.name, x);
                }

                // materials
                var shader = Shader.Find("Standard");
                var material = new Material(shader);
                ctx.AddObjectToAsset(material.name, material);

                var root = new GameObject("_root_");
                ctx.SetMainObject("root", root);

                // scene;
                var scene = default(JsonParser);
                if (parsed.ObjectItems.Any(x => x.Key == "scene"))
                {
                    scene = parsed["scenes"][parsed["scene"].GetInt32()];
                }
                else
                {
                    scene = parsed["scenes"][0];
                }

                // nodes
                int i = 0;
                foreach (var n in scene["nodes"].ListItems.Select(x => x.GetInt32()))
                {
                    //Debug.LogFormat("nodes: {0}", String.Join(", ", nodes.Select(x => x.ToString()).ToArray()));
                    var node = parsed["nodes"][n];

                    var go = new GameObject(string.Format("node{0}", i++));

                    // transform
                    go.transform.SetParent(root.transform, false);
                    if (node.ObjectItems.Any(x => x.Key == "translation")) {
                        var values = node["translation"].ListItems.Select(x => x.GetSingle()).ToArray();
                        go.transform.localPosition = new Vector3(values[0], values[1], values[2]);
                    }

                    // set mesh
                    var filter = go.AddComponent<MeshFilter>();
                    filter.sharedMesh = meshes[node["mesh"].GetInt32()];

                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = new[] { material };

                }

                Debug.Log("imported");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}
