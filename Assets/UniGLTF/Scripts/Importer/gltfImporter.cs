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

        public struct Context
        {
            public AssetImportContext AssetImportContext;
            public String Path;

            public Context(AssetImportContext assetImportContext)
            {
                AssetImportContext = assetImportContext;
                Path = assetImportContext.assetPath;
            }

            public Context(String path)
            {
                AssetImportContext = null;
                Path = path;
            }

            public void AddObjectToAsset(string key, UnityEngine.Object o)
            {
                if (AssetImportContext == null)
                {
                    return;
                }
                AssetImportContext.AddObjectToAsset(key, o);
            }

            public void SetMainObject(string key, UnityEngine.Object o)
            {
                if (AssetImportContext == null)
                {
                    return;
                }
                AssetImportContext.SetMainObject(key, o);
            }
        }

        public static GameObject Import(AssetImportContext ctx, string json, ArraySegment<Byte> bytes = default(ArraySegment<Byte>))
        {
            return Import(new Context(ctx), json, bytes);
        }

        public static GameObject Import(string path, string json, ArraySegment<Byte> bytes = default(ArraySegment<Byte>))
        {
            return Import(new Context(path), json, bytes);
        }

        static void TraverseTransform(Transform t, Action<Transform> pred)
        {
            pred(t);

            foreach (Transform child in t)
            {
                TraverseTransform(child, pred);
            }
        }

        public static GameObject Import(Context ctx, string json, ArraySegment<Byte> bytes)
        {
            var baseDir = Path.GetDirectoryName(ctx.Path);

            var gltf = glTF.Parse(json, baseDir, bytes);
            Debug.Log(gltf);

            // textures
            var textures = gltf.ReadTextures()
                    .Select(x =>
                    {
                        if (!x.IsAsset)
                        {
                            ctx.AddObjectToAsset(x.Texture.name, x.Texture);
                        }
                        return x.Texture;
                    })
                    .ToArray();

            // materials
            var materials = gltf.ReadMaterials(textures).ToArray();
            foreach (var material in materials)
            {
                ctx.AddObjectToAsset(material.name, material);
            }

            // meshes
            var meshes = gltf.meshes.Select((x, i) =>
            {
                var meshWithMaterials = gltf.ReadMesh(x, materials);
                var mesh = meshWithMaterials.Mesh;
                if (string.IsNullOrEmpty(mesh.name))
                {
                    mesh.name = string.Format("UniGLTF import#{0}", i);
                }

                ctx.AddObjectToAsset(mesh.name, mesh);

                return meshWithMaterials;
            }).ToArray();

            var root = new GameObject("_root_");

            // nodes
            var _nodes = gltf.nodes.Select(x => x.ToGameObject()).ToArray();

            var nodes = _nodes.Select((go, i) =>
            {
                if (string.IsNullOrEmpty(go.name))
                {
                    go.name = string.Format("node{0:000}", i);
                }

                var nodeWithSkin = new TransformWithSkin
                {
                    Transform = go.transform,
                };

                var node = gltf.nodes[i];

                //
                // build hierachy
                //
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        _nodes[child].transform.SetParent(_nodes[i].transform,
                            false // node has local transform
                            );
                    }
                }

                //
                // attach mesh
                //
                if (node.mesh != -1)
                {
                    var mesh = meshes[node.mesh];
                    if (mesh.Mesh.blendShapeCount == 0 && node.skin == -1)
                    {
                        // without blendshape and bone skinning
                        var filter = go.AddComponent<MeshFilter>();
                        filter.sharedMesh = mesh.Mesh;
                        var renderer = go.AddComponent<MeshRenderer>();
                        renderer.sharedMaterials = mesh.Materials;
                    }
                    else
                    {
                        var renderer = go.AddComponent<SkinnedMeshRenderer>();

                        if (node.skin != -1)
                        {
                            nodeWithSkin.SkinIndex = node.skin;
                        }

                        renderer.sharedMesh = mesh.Mesh;
                        renderer.sharedMaterials = mesh.Materials;
                    }
                }

                return nodeWithSkin;
            }).ToArray();

            //
            // fix node's coordinate. z-back to z-forward
            //
            var globalTransformMap = nodes.ToDictionary(x => x.Transform, x => new PosRot
            {
                Position = x.Transform.position,
                Rotation = x.Transform.rotation,
            });
            foreach (var x in gltf.rootnodes)
            {
                // fix nodes coordinate
                // reverse Z in global
                var t = nodes[x].Transform;
                //t.SetParent(root.transform, false);

                TraverseTransform(t, transform =>
                {
                    var g = globalTransformMap[transform];
                    transform.position = g.Position.ReverseZ();
                    transform.rotation = g.Rotation.ReverseZ();
                });
            }

            var animator = root.AddComponent<Animator>();

            root.AddComponent<UniHumanoid.BoneMapping>();

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

                        var skin = gltf.skins[x.SkinIndex.Value];

                        skinnedMeshRenderer.sharedMesh = null;

                        var joints = skin.joints.Select(y => nodes[y].Transform).ToArray();
                        skinnedMeshRenderer.bones = joints;
                        skinnedMeshRenderer.rootBone = nodes[0].Transform;

                        // https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html
                        var _b = joints.Select(y => y.worldToLocalMatrix * nodes[0].Transform.localToWorldMatrix).ToArray();
                        var bindePoses = gltf.GetBuffer<Matrix4x4>(skin.inverseBindMatrices).ToArray();
                        var bindePosesR = bindePoses.Select(y => y.ReverseZ()).ToArray();

                        // ...
                        mesh.bindposes = _b;
                        skinnedMeshRenderer.sharedMesh = mesh;
                    }
                }
            }

            foreach (var x in gltf.rootnodes)
            {
                // fix nodes coordinate
                // reverse Z in global
                var t = nodes[x].Transform;
                t.SetParent(root.transform, false);
            }

            ctx.SetMainObject("root", root);

            // animation
            if (gltf.animations != null)
            {
                var clip = new AnimationClip();
                clip.name = GltfAnimation.ANIMATION_NAME;
                clip.ClearCurves();

                GltfAnimation.ReadAnimation(clip, gltf.animations, nodes.Select(x => x.Transform).ToArray(), gltf);

                ctx.AddObjectToAsset(GltfAnimation.ANIMATION_NAME, clip);
            }

            Debug.LogFormat("Import completed");

            return root;
        }
    }
}
