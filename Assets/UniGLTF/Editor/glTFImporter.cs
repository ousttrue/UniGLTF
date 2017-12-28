using Osaru;
using Osaru.Json;
using System;
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

            public Byte[] GetBytes(string baseDir)
            {
                return File.ReadAllBytes(Path.Combine(baseDir, uri));
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

        static int[] GetIndices(Accessor accessor, ArraySegment<byte> bytes)
        {
            switch(accessor.componentType)
            {
                case 5123: // GL_UNSIGNED_SHORT:
                    {
                        var indices = GetAttrib<UInt16>(accessor, bytes);
                        return indices.Select(x => (int)x).ToArray();
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

        //Byte[] GetBuffers(Osaru.IParser<Json>)

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## GLTFImporter ##: {0}", ctx.assetPath);

            var json = File.ReadAllText(ctx.assetPath, Encoding.UTF8);
            try
            {
                var parsed=json.ParseAsJson();

                // asset
                var asset = parsed["asset"];
                var generator = asset["generator"].GetString();
                var version = float.Parse(asset["version"].GetString());
                Debug.LogFormat("{0}: glTF-{1}", generator, version);

                // scene;
                var scene = parsed["scenes"][parsed["scene"].GetInt32()];

                var root = new GameObject("_root_");

                int i = 0;
                foreach(var n in scene["nodes"].ListItems.Select(x => x.GetInt32()))
                {
                    //Debug.LogFormat("nodes: {0}", String.Join(", ", nodes.Select(x => x.ToString()).ToArray()));
                    var node = parsed["nodes"][n];
                    var meshJson = parsed["meshes"][node["mesh"].GetInt32()];

                    var go = new GameObject(string.Format("node{0}", i++));
                    go.transform.SetParent(root.transform, false);

                    //Debug.Log(prims.ToJson());
                    foreach(var prim in meshJson["primitives"].ListItems)
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

                        var buffers =  DeserializeJsonList<Buffer>(parsed["buffers"]);
                        var bufferViews = DeserializeJsonList<BufferView>(parsed["bufferViews"]);
                        var accessors = DeserializeJsonList<Accessor>(parsed["accessors"]);

                        var bytesList = buffers.Select(x => x.GetBytes(Path.GetDirectoryName(ctx.assetPath))).ToArray();

                        var mesh = new Mesh();
                        mesh.name = "UniGLTF import";

                        var vertexAccessor = accessors[attribs["POSITION"]];
                        var vertexBytes = GetBytes(bytesList, bufferViews[vertexAccessor.bufferView]);
                        mesh.vertices = GetAttrib<Vector3>(vertexAccessor, vertexBytes);

                        var normalAccessor = accessors[attribs["NORMAL"]];
                        var normalBytes = GetBytes(bytesList, bufferViews[normalAccessor.bufferView]);
                        mesh.normals = GetAttrib<Vector3>(normalAccessor, normalBytes);

                        var indexAccessor = accessors[indexBuffer];
                        var indexBytes = GetBytes(bytesList, bufferViews[indexAccessor.bufferView]);
                        var indices = GetIndices(indexAccessor, indexBytes);
                        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

                        ctx.AddObjectToAsset("mesh", mesh);

                        var shader = Shader.Find("Standard");
                        var material = new Material(shader);
                        ctx.AddObjectToAsset("material", material);

                        var renderer = go.AddComponent<MeshRenderer>();
                        renderer.sharedMaterials = new[] { material };

                        var filter = go.AddComponent<MeshFilter>();
                        filter.sharedMesh = mesh;
                    }
                }

                ctx.SetMainObject("root", root);

                Debug.Log("imported");

            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}
