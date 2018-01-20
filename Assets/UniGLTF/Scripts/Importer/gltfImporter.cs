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

        struct PosRot
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        static Transform GetLeftLeg(Transform[] joints)
        {
            Transform t = joints[0];
            for(int i=1; i<joints.Length; ++i)
            {
                if(joints[i].transform.localPosition.x<t.localPosition.x)
                {
                    t = joints[i];
                }
            }
            return t;
        }

        static Transform GetRightLeg(Transform[] joints)
        {
            Transform t = joints[0];
            for (int i = 1; i < joints.Length; ++i)
            {
                if (joints[i].transform.localPosition.x > t.localPosition.x)
                {
                    t = joints[i];
                }
            }
            return t;
        }

        static Transform GetSpine(Transform[] joints)
        {
            Transform t = joints[0];
            for (int i = 1; i < joints.Length; ++i)
            {
                if (joints[i].transform.position.y > t.position.y)
                {
                    t = joints[i];
                }
            }
            return t;
        }

        static Transform GetChest(Transform spine)
        {
            var current = spine;
            while (current!=null)
            {
                if (current.childCount >= 3)
                {
                    return current;
                }

                current = spine.GetChild(0);
            }
            return null;
        }

        static Transform GetLeftArm(Transform chest, Transform[] joints, Vector3 leftDir)
        {
            var values = joints.Select(x => Vector3.Dot((x.position - chest.position).normalized, leftDir)).ToArray();

            var current = joints[0];
            var value = values[0];
            for(int i=1; i<joints.Length; ++i)
            {
                if (values[i] > value)
                {
                    value = values[i];
                    current = joints[i];
                }
            }
            return current;
        }

        static Transform GetRightArm(Transform chest, Transform[] joints, Vector3 rightDir)
        {
            var values = joints.Select(x => Vector3.Dot((x.position - chest.position).normalized, rightDir)).ToArray();

            var current = joints[0];
            var value = values[0];
            for (int i = 1; i < joints.Length; ++i)
            {
                if (values[i] > value)
                {
                    value = values[i];
                    current = joints[i];
                }
            }
            return current;
        }

        static Transform GetNeck(Transform[] joints)
        {
            Transform t = joints[0];
            for (int i = 1; i < joints.Length; ++i)
            {
                if (joints[i].transform.position.y > t.position.y)
                {
                    t = joints[i];
                }
            }
            return t;
        }

        static IEnumerable<KeyValuePair<HumanBodyBones, Transform>> TraverseSkeleton(Transform root, Transform[] joints)
        {
            var rootJoints = joints.Where(x => !joints.Contains(x.parent)).ToArray();

            if (rootJoints.Length != 1)
            {
                yield break;
            }

            var hips = rootJoints[0];
            if (hips.childCount < 3)
            {
                yield break;
            }

            var hipsChildren = hips.GetChildren().ToArray();

            var leftLeg = GetLeftLeg(hipsChildren);
            var leftLowerLeg = leftLeg.GetChild(0);
            var leftFoot = leftLowerLeg.GetChild(0);

            var rightLeg = GetRightLeg(hipsChildren);
            var rightLowerLeg = rightLeg.GetChild(0);
            var rightFoot = rightLowerLeg.GetChild(0);

            var spine = GetSpine(hipsChildren);

            var chest = GetChest(spine);
            var chestChildren = chest.GetChildren().ToArray();

            var rightDir = (rightLeg.position - leftLeg.position).normalized;

            var leftArm = GetLeftArm(chest, chestChildren, -rightDir);
            var leftLowerArm = leftArm.GetChild(0);
            var leftHand = leftLowerArm.GetChild(0);

            var rightArm = GetRightArm(chest, chestChildren, rightDir);
            var rightLowerArm = rightArm.GetChild(0);
            var rightHand = rightLowerArm.GetChild(0);

            var neck = GetNeck(chestChildren);
            var head = neck.GetChild(0);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Hips, hips);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftUpperLeg, leftLeg);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftLowerLeg, leftLowerLeg);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftFoot, leftFoot);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightUpperLeg, rightLeg);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightLowerLeg, rightLowerLeg);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightFoot, rightFoot);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Spine, spine);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Chest, chest);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Neck, neck);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Head, head);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftUpperArm, leftArm);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftLowerArm, leftLowerArm);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftHand, leftHand);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightUpperArm, rightArm);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightLowerArm, rightLowerArm);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightHand, rightHand);
        }

        static String ToHumanBoneName(HumanBodyBones b)
        {
            foreach (var x in HumanTrait.BoneName)
            {
                if (x.Replace(" ", "") == b.ToString())
                {
                    return x;
                }
            }

            throw new KeyNotFoundException();
        }

        static Avatar CreateAvatar(Transform root, Transform[] joints)
        {
            var map = TraverseSkeleton(root, joints).ToDictionary(x => x.Key, x => x.Value);

            var description = new HumanDescription
            {
                human = map.Select(x =>
                {
                    var hb = new HumanBone
                    {
                        boneName = x.Value.name,
                        humanName = ToHumanBoneName(x.Key)

                    };
                    hb.limit.useDefaultValues = true;
                    return hb;
                }).ToArray(),
                skeleton = root.Traverse().Select(x => x.ToSkeletonBone()).ToArray(),
                lowerArmTwist = 0.5f,
                upperArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0.0f,
            };

            return AvatarBuilder.BuildHumanAvatar(root.gameObject, description);
        }

        public static GameObject Import(Context ctx, string json, ArraySegment<Byte> bytes)
        {
            var baseDir = Path.GetDirectoryName(ctx.Path);
            var parsed = json.ParseAsJson();

            // buffer
            var buffer = new GltfBuffer(parsed, baseDir, bytes);

            // textures
            Texture2D[] textures = null;
            if (parsed.HasKey("textures"))
            {
                textures = GltfTexture.ReadTextures(parsed, baseDir, buffer)
                    .Select(x =>
                    {
                        if (!x.IsAsset)
                        {
                            ctx.AddObjectToAsset(x.Texture.name, x.Texture);
                        }
                        return x.Texture;
                    })
                    .ToArray();
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
            var globalTransformMap = nodes.ToDictionary(x => x.Transform, x => new PosRot
            {
                Position = x.Transform.position,
                Rotation = x.Transform.rotation,
            });
            // hierachy
            var nodeJsonList = scene["nodes"].ListItems.ToArray();
            foreach (var x in nodeJsonList)
            {
                // fix nodes coordinate
                // reverse Z in global
                var t = nodes[x.GetInt32()].Transform;
                //t.SetParent(root.transform, false);

                TraverseTransform(t, transform =>
                {
                    var g = globalTransformMap[transform];
                    transform.position = g.Position.ReverseZ();
                    transform.rotation = g.Rotation.ReverseZ();
                });
            }

            var animator=nodes[0].Transform.gameObject.AddComponent<Animator>();

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

                        // make humanoid avatar
                        var avatar = CreateAvatar(nodes[0].Transform, joints);
                        if (avatar != null)
                        {
                            Debug.LogFormat("isValid: {0}, isHuman: {1}", avatar.isValid, avatar.isHuman, avatar);
                            avatar.name = "avatar";
                            ctx.AddObjectToAsset("avatar", avatar);
                            animator.avatar = avatar;
                        }
                    }
                }
            }

            foreach (var x in nodeJsonList)
            {
                // fix nodes coordinate
                // reverse Z in global
                var t = nodes[x.GetInt32()].Transform;
                t.SetParent(root.transform, false);
            }

            // reset nodes[0] rotation
            var nodes0map = nodes[0].Transform.GetChildren().ToDictionary(x =>x, x=> new PosRot
            {
                Position=x.position,
                Rotation =x.rotation,
            });
            nodes[0].Transform.rotation = Quaternion.identity;
            foreach(Transform x in nodes[0].Transform)
            {
                x.position = nodes0map[x].Position;
                x.rotation = nodes0map[x].Rotation;
            }

            // rename nodes0
            //nodes[0].Transform.name = Path.GetFileNameWithoutExtension(ctx.Path) + "0";

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

            return root;
        }
    }
}
