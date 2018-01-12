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
    struct TransformWithSkin
    {
        public Transform Transform;
        public int? SkinIndex;
    }

    [ScriptedImporter(1, "gltf")]
    public class GLTFImporter : ScriptedImporter
    {
        List<TransformWithSkin> ReadNodes(JsonParser nodesJson, MeshWithMaterials[] meshes)
        {
            var list = new List<TransformWithSkin>();
            int i = 0;
            foreach (var node in nodesJson.ListItems)
            {
                var go = new GameObject(string.Format("node{0}", i));
                var nodeWithSkin = new TransformWithSkin
                {
                    Transform=go.transform,
                };

                // transform
                if (node.HasKey("translation"))
                {
                    var values = node["translation"].ListItems.Select(x => x.GetSingle()).ToArray();
                    go.transform.localPosition = new Vector3(values[0], values[1], values[2]);
                }
                if (node.HasKey("rotation"))
                {
                    var values = node["rotation"].ListItems.Select(x => x.GetSingle()).ToArray();
                    go.transform.localRotation = new Quaternion(values[0], values[1], values[2], values[3]);
                }
                if (node.HasKey("scale"))
                {
                    var values = node["scale"].ListItems.Select(x => x.GetSingle()).ToArray();
                    go.transform.localScale = new Vector3(values[0], values[1], values[2]);
                }
                if (node.HasKey("matrix"))
                {
                    var values = node["matrix"].ListItems.Select(x => x.GetSingle()).ToArray();
                    var col0 = new Vector4(values[0], values[1], values[2], values[3]);
                    var col1 = new Vector4(values[4], values[5], values[6], values[7]);
                    var col2 = new Vector4(values[8], values[9], values[10], values[11]);
                    var col3 = new Vector4(values[12], values[13], values[14], values[15]);
                    var m = new Matrix4x4(col0, col1, col2, col3);
                    go.transform.localRotation = m.rotation;
                    go.transform.localPosition = m.GetColumn(3);
                }

                // mesh
                if (node.HasKey("mesh"))
                {
                    var mesh = meshes[node["mesh"].GetInt32()];
                    Renderer renderer = null;
                    var hasSkin = node.HasKey("skin");
                    if (mesh.Mesh.blendShapeCount == 0 && !hasSkin)
                    {
                        // without blendshape and bone skinning
                        var filter = go.AddComponent<MeshFilter>();
                        filter.sharedMesh = mesh.Mesh;

                        renderer = go.AddComponent<MeshRenderer>();
                    }
                    else
                    {
                        var _renderer = go.AddComponent<SkinnedMeshRenderer>();

                        if (hasSkin)
                        {
                            nodeWithSkin.SkinIndex = node["skin"].GetInt32();
                        }

                        _renderer.sharedMesh = mesh.Mesh;

                        renderer = _renderer;
                    }

                    renderer.sharedMaterials = mesh.Materials;
                }

                list.Add(nodeWithSkin);

                ++i;
            }

            i = 0;
            foreach (var node in nodesJson.ListItems)
            {
                // children
                if (node.HasKey("children"))
                {
                    foreach(var child in node["children"].ListItems)
                    {
                        list[child.GetInt32()].Transform.SetParent(list[i].Transform, false);
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

        struct Matrix4
        {
            public float _00;
            public float _01;
            public float _02;
            public float _03;

            public float _04;
            public float _05;
            public float _06;
            public float _07;

            public float _08;
            public float _09;
            public float _10;
            public float _11;

            public float _12;
            public float _13;
            public float _14;
            public float _15;
        }

        IEnumerable<Matrix4x4> ToMatrix(Matrix4[] matrices)
        {
            foreach(var m in matrices)
            {
                var v0 = new Vector4(m._00, m._01, m._02, m._03);
                var v1 = new Vector4(m._04, m._05, m._06, m._07);
                var v2 = new Vector4(m._08, m._09, m._10, m._11);
                var v3 = new Vector4(m._12, m._13, m._14, m._15);
                yield return new Matrix4x4(v0, v1, v2, v3);
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
                foreach (var mesh in meshes.Select(x => x.Mesh))
                {
                    ctx.AddObjectToAsset(mesh.name, mesh);
                }

                var root = new GameObject("_root_");

                // nodes
                var nodes = ReadNodes(parsed["nodes"], meshes);

                // skins
                Skin[] skins = null;
                if (parsed.HasKey("skins"))
                {
                    skins = parsed["skins"].DeserializeList<Skin>();
                }

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
                // hierachy
                var nodeJsonList = scene["nodes"].ListItems.ToArray();
                foreach (var x in nodeJsonList)
                {
                    nodes[x.GetInt32()].Transform.SetParent(root.transform, false);
                }
                // reverse Z in global
                foreach (var x in nodes)
                {
                    x.Transform.localPosition = x.Transform.localPosition.ReverseZ();
                    x.Transform.localRotation = x.Transform.localRotation.ReverseZ();
                }
                // skinning
                foreach (var x in nodes)
                {
                    var skinnedMeshRenderer = x.Transform.GetComponent<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null)
                    {
                        var mesh = skinnedMeshRenderer.sharedMesh;
                        if (x.SkinIndex.HasValue)
                        {
                            if (mesh == null) throw new Exception();
                            if (skinnedMeshRenderer == null) throw new Exception();

                            var skin = skins[x.SkinIndex.Value];

                            skinnedMeshRenderer.sharedMesh = null;

                            var joints = skin.joints.Select(y => nodes[y].Transform).ToArray();
                            skinnedMeshRenderer.bones = joints;
                            skinnedMeshRenderer.rootBone = nodes[0].Transform;

                            // https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html
                            var _b = joints.Select(y => y.worldToLocalMatrix * nodes[0].Transform.localToWorldMatrix).ToArray();
                            var bindePoses = buffer.GetBuffer<Matrix4x4>(skin.inverseBindMatrices).ToArray();
                            var bindePosesR = bindePoses.Select(y => y.ReverseZ()).ToArray();

                            // ...
                            mesh.bindposes = _b;
                            skinnedMeshRenderer.sharedMesh = mesh;
                        }
                    }
                }
                ctx.SetMainObject("root", root);

                Debug.LogFormat("Import completed");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}
