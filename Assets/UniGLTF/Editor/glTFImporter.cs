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
        List<GameObject> ReadNodes(JsonParser nodesJson, MeshWithMaterials[] meshes)
        {
            var list = new List<GameObject>();
            int i = 0;
            foreach (var node in nodesJson.ListItems)
            {
                var go = new GameObject(string.Format("node{0}", i));

                // transform
                if (node.HasKey("translation"))
                {
                    var values = node["translation"].ListItems.Select(x => x.GetSingle()).ToArray();
                    go.transform.localPosition = new Vector3(values[0], values[1], values[2]).ReverseZ();
                }
                if (node.HasKey("rotation"))
                {
                    var values = node["rotation"].ListItems.Select(x => x.GetSingle()).ToArray();
                    go.transform.localRotation = new Quaternion(values[0], values[1], values[2], values[3]).ReverseZ();
                }
                if (node.HasKey("scale"))
                {
                    var values = node["scale"].ListItems.Select(x => x.GetSingle()).ToArray();
                    go.transform.localScale = new Vector3(values[0], values[1], values[2]).ReverseZ();
                }
                if (node.HasKey("matrix"))
                {
                    var values = node["matrix"].ListItems.Select(x => x.GetSingle()).ToArray();
                    var col0 = new Vector4(values[0], values[1], values[2], values[3]);
                    var col1 = new Vector4(values[4], values[5], values[6], values[7]);
                    var col2 = new Vector4(values[8], values[9], values[10], values[11]);
                    var col3 = new Vector4(values[12], values[13], values[14], values[15]);
                    var m = new Matrix4x4(col0, col1, col2, col3);
                    go.transform.localRotation = m.rotation.ReverseZ();
                    Vector3 position= m.GetColumn(3);
                    go.transform.localPosition = position.ReverseZ();
                }

                // mesh
                if (node.HasKey("mesh"))
                {
                    var mesh = meshes[node["mesh"].GetInt32()];
                    Renderer renderer = null;
                    if (mesh.Mesh.blendShapeCount == 0)
                    {
                        var filter = go.AddComponent<MeshFilter>();
                        filter.sharedMesh = mesh.Mesh;

                        renderer = go.AddComponent<MeshRenderer>();
                    }
                    else
                    {
                        var _renderer = go.AddComponent<SkinnedMeshRenderer>();
                        _renderer.sharedMesh = mesh.Mesh;

                        renderer = _renderer;
                    }


                    renderer.sharedMaterials = mesh.Materials;
                }

                list.Add(go);

                ++i;
            }

            i = 0;
            foreach (var node in nodesJson.ListItems)
            {
                // children
                if (node.HasKey("children"))
                {
                    var children = node["children"].ListItems.Select(x => x.GetInt32()).ToArray();
                    foreach(var x in children)
                    {
                        list[x].transform.SetParent(list[i].transform, false);
                    }
                }

                ++i;
            }

            return list;
        }

        IEnumerable<Material> ReadMaterials(JsonParser materialsJson, Texture2D[] textures)
        {
            foreach(var x in materialsJson.ListItems)
            {
                var shader = Shader.Find("Standard");

                var material = new Material(shader);
                material.name = x["name"].GetString();

                if(x.HasKey("pbrMetallicRoughness"))
                {
                    var pbr = x["pbrMetallicRoughness"];
                    if(pbr.HasKey("baseColorTexture"))
                    {
                        var textureIndex = pbr["baseColorTexture"]["index"].GetInt32();
                        material.mainTexture = textures[textureIndex];
                    }
                }

                yield return material;
            }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## GLTFImporter ##: {0}", ctx.assetPath);

            try
            {
                var baseDir = Path.GetDirectoryName(ctx.assetPath);
                var parsed = File.ReadAllText(ctx.assetPath, Encoding.UTF8).ParseAsJson();

                // buffer
                var buffer = new GltfBuffer(parsed, baseDir);

                // textures
                Texture2D[] textures = null;
                if(parsed.HasKey("textures"))
                {
                    textures = GltfTexture.ReadTextures(parsed, baseDir);
                }

                // materials
                Material[] materials = null;
                if (parsed.HasKey("materials"))
                {
                    materials = ReadMaterials(parsed["materials"], textures).ToArray();
                    foreach(var material in materials)
                    {
                        ctx.AddObjectToAsset(material.name, material);
                    }
                }
                else
                {
                    var shader = Shader.Find("Standard");
                    var material = new Material(shader);
                    ctx.AddObjectToAsset(material.name, material);
                    materials = new Material[] { material };
                }

                // meshes
                var meshes = buffer.ReadMeshes(parsed["meshes"], materials);
                foreach(var x in meshes)
                {
                    ctx.AddObjectToAsset(x.Mesh.name, x.Mesh);
                }

                var root = new GameObject("_root_");
                ctx.SetMainObject("root", root);

                // nodes
                var nodes = ReadNodes(parsed["nodes"], meshes);

                // scene;
                var scene = default(JsonParser);
                if (parsed.HasKey("scene"))
                {
                    scene = parsed["scenes"][parsed["scene"].GetInt32()];
                }
                else
                {
                    scene = parsed["scenes"][0];
                }
                foreach (var n in scene["nodes"].ListItems.Select(x => x.GetInt32()))
                {
                    //Debug.LogFormat("nodes: {0}", String.Join(", ", nodes.Select(x => x.ToString()).ToArray()));
                    nodes[n].transform.SetParent(root.transform, false);
                }

                Debug.LogFormat("Import completed");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}
