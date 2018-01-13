using Osaru;
using Osaru.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;


namespace UniGLTF
{
    struct TransformWithSkin
    {
        public Transform Transform;
        public int? SkinIndex;
    }

    [Serializable]
    struct AnimationTarget
    {
        public int node;
        public string path;
    }

    [Serializable]
    struct Channel
    {
        public int sampler;
        public AnimationTarget target;
    }

    [Serializable]
    struct Sampler
    {
        public int input;
        public string interpolation;
        public int output;
    }

    [Serializable]
    struct Animation
    {
        public Channel[] channels;
        public Sampler[] samplers;
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
                    Transform = go.transform,
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
                    foreach (var child in node["children"].ListItems)
                    {
                        // node has local transform
                        list[child.GetInt32()].Transform.SetParent(list[i].Transform, false);
                    }
                }

                ++i;
            }

            return list;
        }

        IEnumerable<Material> ReadMaterials(JsonParser materialsJson, Texture2D[] textures)
        {
            foreach (var x in materialsJson.ListItems)
            {
                var shader = Shader.Find("Standard");

                var material = new Material(shader);
                material.name = x["name"].GetString();

                if (x.HasKey("pbrMetallicRoughness"))
                {
                    var pbr = x["pbrMetallicRoughness"];
                    if (pbr.HasKey("baseColorTexture"))
                    {
                        var textureIndex = pbr["baseColorTexture"]["index"].GetInt32();
                        material.mainTexture = textures[textureIndex];
                    }
                }

                yield return material;
            }
        }

        static string ANIMATION_NAME = "animation";

        T GetOrCreate<T>(UnityEngine.Object[] assets, string name, Func<T> create) where T : UnityEngine.Object
        {
            var found = assets.FirstOrDefault(x => x.name == name);
            if (found != null)
            {
                return found as T;
            }
            return create();
        }

        static void ReadAnimation(AnimationClip clip, Animation[] animations, List<TransformWithSkin> nodes, GltfBuffer buffer)
        {
            foreach (var x in animations)
            {
                foreach (var y in x.channels)
                {
                    var node = nodes[y.target.node];
                    switch (y.target.path)
                    {
                        case "translation":
                            {
                                var curveX = new AnimationCurve();
                                var curveY = new AnimationCurve();
                                var curveZ = new AnimationCurve();

                                var sampler = x.samplers[y.sampler];
                                var input = buffer.GetBuffer<float>(sampler.input);
                                var output = buffer.GetBuffer<Vector3>(sampler.output);
                                for (int i = 0; i < input.Length; ++i)
                                {
                                    var time = input[i];
                                    var pos = output[i].ReverseZ();
                                    curveX.AddKey(time, pos.x);
                                    curveY.AddKey(time, pos.y);
                                    curveZ.AddKey(time, pos.z);
                                }

                                var relativePath = node.Transform.RelativePathFrom(nodes[0].Transform);
                                clip.SetCurve(relativePath, typeof(Transform), "localPosition.x", curveX);
                                clip.SetCurve(relativePath, typeof(Transform), "localPosition.y", curveY);
                                clip.SetCurve(relativePath, typeof(Transform), "localPosition.z", curveZ);
                            }
                            break;

                        case "rotation":
                            {
                                var curveX = new AnimationCurve();
                                var curveY = new AnimationCurve();
                                var curveZ = new AnimationCurve();
                                var curveW = new AnimationCurve();

                                var sampler = x.samplers[y.sampler];
                                var input = buffer.GetBuffer<float>(sampler.input);
                                var output = buffer.GetBuffer<Quaternion>(sampler.output);
                                for (int i = 0; i < input.Length; ++i)
                                {
                                    var time = input[i];
                                    var rot = output[i].ReverseZ();
                                    curveX.AddKey(time, rot.x);
                                    curveY.AddKey(time, rot.y);
                                    curveZ.AddKey(time, rot.z);
                                    curveW.AddKey(time, rot.w);
                                }

                                var relativePath = node.Transform.RelativePathFrom(nodes[0].Transform);
                                clip.SetCurve(relativePath, typeof(Transform), "localRotation.x", curveX);
                                clip.SetCurve(relativePath, typeof(Transform), "localRotation.y", curveY);
                                clip.SetCurve(relativePath, typeof(Transform), "localRotation.z", curveZ);
                                clip.SetCurve(relativePath, typeof(Transform), "localRotation.w", curveW);
                            }
                            break;

                        case "scale":
                            {
                                var curveX = new AnimationCurve();
                                var curveY = new AnimationCurve();
                                var curveZ = new AnimationCurve();

                                var sampler = x.samplers[y.sampler];
                                var input = buffer.GetBuffer<float>(sampler.input);
                                var output = buffer.GetBuffer<Vector3>(sampler.output);
                                for (int i = 0; i < input.Length; ++i)
                                {
                                    var time = input[i];
                                    var scale = output[i];
                                    curveX.AddKey(time, scale.x);
                                    curveY.AddKey(time, scale.y);
                                    curveZ.AddKey(time, scale.z);
                                }

                                var relativePath = node.Transform.RelativePathFrom(nodes[0].Transform);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.x", curveX);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.y", curveY);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.z", curveZ);
                            }
                            break;
                    }
                }

                nodes[0].Transform.gameObject.AddComponent<Animator>();
            }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## GLTFImporter ##: {0}", ctx.assetPath);

            var assets = AssetDatabase.LoadAllAssetsAtPath(ctx.assetPath);

            var baseDir = Path.GetDirectoryName(ctx.assetPath);
            var parsed = File.ReadAllText(ctx.assetPath, Encoding.UTF8).ParseAsJson();

            // buffer
            var buffer = new GltfBuffer(parsed, baseDir);

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
                materials = ReadMaterials(parsed["materials"], textures).ToArray();
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
            ctx.SetMainObject("root", root);

            // animation
            if (parsed.HasKey("animations"))
            {
                var animations = parsed["animations"].DeserializeList<Animation>();

                var clip = GetOrCreate(assets, ANIMATION_NAME, () => new AnimationClip());
                clip.name = ANIMATION_NAME;
                clip.ClearCurves();

                ReadAnimation(clip, animations, nodes, buffer);

                ctx.AddObjectToAsset(ANIMATION_NAME, clip);
            }

            Debug.LogFormat("Import completed");
        }
    }
}
