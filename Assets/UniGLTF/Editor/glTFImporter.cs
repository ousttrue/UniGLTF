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
    public class gltfImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## Importer ##: {0}", ctx.assetPath);

            var json = File.ReadAllText(ctx.assetPath, Encoding.UTF8);

            Import(ctx, json, new ArraySegment<byte>());
        }

        public static void Import(AssetImportContext ctx, string json, ArraySegment<Byte> bytes)
        {
            var baseDir = Path.GetDirectoryName(ctx.assetPath);
            var parsed = json.ParseAsJson();

            // buffer
            var buffer = new GltfBuffer(parsed, baseDir, bytes);

            // textures
            Texture2D[] textures = null;
            if (parsed.HasKey("textures"))
            {
                textures = GltfTexture.ReadTextures(parsed, baseDir);
            }

            // materials
            Material[] materials = null;
            if (parsed.HasKey("materials"))
            {
                materials = GltfMaterial.ReadMaterials(parsed["materials"], textures).ToArray();
                foreach (var material in materials)
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
            var nodes = TransformWithSkin.ReadNodes(parsed["nodes"], meshes);

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
            // fix nodes coordinate
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

            // rename nodes0
            nodes[0].Transform.name = Path.GetFileNameWithoutExtension(ctx.assetPath) + "0";

            ctx.SetMainObject("root", root);

            // animation
            if (parsed.HasKey("animations"))
            {
                var animations = parsed["animations"].DeserializeList<GltfAnimation>();

                var clip = new AnimationClip();
                clip.name = GltfAnimation.ANIMATION_NAME;
                clip.ClearCurves();

                GltfAnimation.ReadAnimation(clip, animations, nodes.Select(x => x.Transform).ToArray(), buffer);

                ctx.AddObjectToAsset(GltfAnimation.ANIMATION_NAME, clip);
            }

            Debug.LogFormat("Import completed");
        }
    }
}
