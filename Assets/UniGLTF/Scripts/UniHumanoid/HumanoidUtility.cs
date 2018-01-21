using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UniHumanoid
{
    public static class Extensions
    {
        public static IEnumerable<Transform> GetChildren(this Transform parent)
        {
            foreach (Transform child in parent)
            {
                yield return child;
            }
        }

        public static IEnumerable<Transform> Traverse(this Transform parent)
        {
            yield return parent;

            foreach (Transform child in parent)
            {
                foreach (Transform descendant in Traverse(child))
                {
                    yield return descendant;
                }
            }
        }

        public static SkeletonBone ToSkeletonBone(this Transform t)
        {
            var sb = new SkeletonBone();
            sb.name = t.name;
            sb.position = t.localPosition;
            sb.rotation = t.localRotation;
            sb.scale = t.localScale;
            return sb;
        }
    }


    public static class HumanoidUtility
    {
        static Transform GetLeftLeg(Transform[] joints)
        {
            Transform t = joints[0];
            for (int i = 1; i < joints.Length; ++i)
            {
                if (joints[i].transform.position.x < t.position.x)
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
                if (joints[i].transform.position.x > t.position.x)
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
            while (current != null)
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

        public static IEnumerable<KeyValuePair<HumanBodyBones, Transform>> TraverseSkeleton(Transform root, Transform[] joints)
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

            var rightArm = GetRightArm(chest, chestChildren, rightDir);
            var rightLowerArm = rightArm.GetChild(0);

            var neck = GetNeck(chestChildren);
            Transform head = null;
            if (neck.childCount == 0)
            {
                head = neck;
                neck = null;
            }
            else
            {
                head = neck.GetChild(0);
            }

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Hips, hips);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftUpperLeg, leftLeg);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftLowerLeg, leftLowerLeg);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftFoot, leftFoot);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightUpperLeg, rightLeg);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightLowerLeg, rightLowerLeg);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightFoot, rightFoot);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Spine, spine);
            if (chest != spine)
            {
                yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Chest, chest);
            }
            if (neck != null)
            {
                yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Neck, neck);
            }
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.Head, head);

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftUpperArm, leftArm);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftLowerArm, leftLowerArm);
            if (leftLowerArm.childCount > 0)
            {
                var leftHand = leftLowerArm.GetChild(0);
                yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.LeftHand, leftHand);
            }

            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightUpperArm, rightArm);
            yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightLowerArm, rightLowerArm);
            if (rightLowerArm.childCount > 0)
            {
                var rightHand = rightLowerArm.GetChild(0);
                yield return new KeyValuePair<HumanBodyBones, Transform>(HumanBodyBones.RightHand, rightHand);
            }
        }

        public static String ToHumanBoneName(HumanBodyBones b)
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
    }
}
