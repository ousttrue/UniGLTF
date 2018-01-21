using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace UniHumanoid
{
    [CustomEditor(typeof(BoneMapping))]
    public class BoneMappingEditor : Editor
    {
        BoneMapping m_target;

        void OnEnable()
        {
            m_target = (BoneMapping)target;
        }

        static GameObject ObjectField(GameObject obj)
        {
            return (GameObject)EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
        }

        static GameObject ObjectField(string label, GameObject obj)
        {
            return (GameObject)EditorGUILayout.ObjectField(label, obj, typeof(GameObject), true);
        }

        const int LABEL_WIDTH = 100;

        static void BoneField(HumanBodyBones bone, GameObject[] bones)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(bone.ToString(), GUILayout.Width(LABEL_WIDTH));
            bones[(int)bone] = ObjectField(bones[(int)bone]);
            EditorGUILayout.EndHorizontal();
        }

        static void BoneField(HumanBodyBones left, HumanBodyBones right, GameObject[] bones)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(left.ToString().Substring(4), GUILayout.Width(LABEL_WIDTH)); // skip left
            bones[(int)left] = ObjectField(bones[(int)left]);
            bones[(int)right] = ObjectField(bones[(int)right]);
            EditorGUILayout.EndHorizontal();
        }

        bool m_handFoldout;

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Guess bone mapping"))
            {
                m_target.GuessBoneMapping();
            }

            if (GUILayout.Button("Ensure T-Pose"))
            {
                m_target.EnsureTPose();
            }

            if (GUILayout.Button("Create avatar"))
            {
                var avatar = m_target.CreateAvatar();
                if (avatar != null)
                {
                    avatar.name = "avatar";
                    var path = "Assets/avtar.asset";
                    AssetDatabase.CreateAsset(avatar, path);
                    Debug.LogFormat("Create avatar {0}", path);
                }
                else
                {
                    Debug.LogWarning("fail to CreateAvatar");
                }
            }

            var bones = m_target.Bones;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Arm", EditorStyles.boldLabel, GUILayout.Width(LABEL_WIDTH));
            EditorGUILayout.LabelField("Left", EditorStyles.boldLabel, GUILayout.Width(40));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Right", EditorStyles.boldLabel, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            BoneField(HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder, bones);
            BoneField(HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm, bones);
            BoneField(HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, bones);
            BoneField(HumanBodyBones.LeftHand, HumanBodyBones.RightHand, bones);

            EditorGUILayout.LabelField("Body", EditorStyles.boldLabel);
            BoneField(HumanBodyBones.Hips, bones);
            BoneField(HumanBodyBones.Spine, bones);
            BoneField(HumanBodyBones.Chest, bones);
            BoneField(HumanBodyBones.UpperChest, bones);
            BoneField(HumanBodyBones.Neck, bones);
            BoneField(HumanBodyBones.Head, bones);
            BoneField(HumanBodyBones.Jaw, bones);
            BoneField(HumanBodyBones.LeftEye, HumanBodyBones.RightEye, bones);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Leg", EditorStyles.boldLabel, GUILayout.Width(LABEL_WIDTH));
            EditorGUILayout.LabelField("Left", EditorStyles.boldLabel, GUILayout.Width(40));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Right", EditorStyles.boldLabel, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            BoneField(HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, bones);
            BoneField(HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, bones);
            BoneField(HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, bones);
            BoneField(HumanBodyBones.LeftToes, HumanBodyBones.RightToes, bones);

            m_handFoldout = EditorGUILayout.Foldout(m_handFoldout, "Hand");
            if (m_handFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Thumb", EditorStyles.boldLabel, GUILayout.Width(LABEL_WIDTH));
                EditorGUILayout.LabelField("Left", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Right", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
                BoneField(HumanBodyBones.LeftThumbProximal, HumanBodyBones.RightThumbProximal, bones);
                BoneField(HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.RightThumbIntermediate, bones);
                BoneField(HumanBodyBones.LeftThumbDistal, HumanBodyBones.RightThumbDistal, bones);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Index", EditorStyles.boldLabel, GUILayout.Width(LABEL_WIDTH));
                EditorGUILayout.LabelField("Left", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Right", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
                BoneField(HumanBodyBones.LeftIndexProximal, HumanBodyBones.RightIndexProximal, bones);
                BoneField(HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.RightIndexIntermediate, bones);
                BoneField(HumanBodyBones.LeftIndexDistal, HumanBodyBones.RightIndexDistal, bones);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Middle", EditorStyles.boldLabel, GUILayout.Width(LABEL_WIDTH));
                EditorGUILayout.LabelField("Left", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Right", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
                BoneField(HumanBodyBones.LeftMiddleProximal, HumanBodyBones.RightMiddleProximal, bones);
                BoneField(HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.RightMiddleIntermediate, bones);
                BoneField(HumanBodyBones.LeftMiddleDistal, HumanBodyBones.RightMiddleDistal, bones);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Ring", EditorStyles.boldLabel, GUILayout.Width(LABEL_WIDTH));
                EditorGUILayout.LabelField("Left", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Right", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
                BoneField(HumanBodyBones.LeftRingProximal, HumanBodyBones.RightRingProximal, bones);
                BoneField(HumanBodyBones.LeftRingIntermediate, HumanBodyBones.RightRingIntermediate, bones);
                BoneField(HumanBodyBones.LeftRingDistal, HumanBodyBones.RightRingDistal, bones);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Little", EditorStyles.boldLabel, GUILayout.Width(LABEL_WIDTH));
                EditorGUILayout.LabelField("Left", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Right", EditorStyles.boldLabel, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
                BoneField(HumanBodyBones.LeftLittleProximal, HumanBodyBones.RightLittleProximal, bones);
                BoneField(HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.RightLittleIntermediate, bones);
                BoneField(HumanBodyBones.LeftLittleDistal, HumanBodyBones.RightLittleDistal, bones);
            }
        }

        void DrawBone(HumanBodyBones bone, GameObject go)
        {
            if (go == null)
            {
                return;
            }

            Handles.Label(go.transform.position,
                go.name + "\n(" + bone.ToString() + ")");
        }

        private void OnSceneGUI()
        {
            var bones = m_target.Bones;
            for (int i = 0; i < bones.Length; ++i)
            {
                DrawBone((HumanBodyBones)i, bones[i]);
            }
        }
    }
}
