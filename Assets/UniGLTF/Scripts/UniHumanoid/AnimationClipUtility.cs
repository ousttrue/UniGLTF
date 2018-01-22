using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace UniHumanoid
{
    public class AnimationClipUtility : MonoBehaviour
    {
        const string CONVERT_HUMANOID_KEY = "Assets/AnimationClip/ConvertHumanoid";
        [MenuItem(CONVERT_HUMANOID_KEY)]
        private static void ConvertHumanoid()
        {
            var src = Selection.activeObject as AnimationClip;
            //Debug.LogFormat("isHumanoidMotion: {0}", src.isHumanMotion);

            var path = AssetDatabase.GetAssetPath(src);
            //Debug.LogFormat("path: {0}", path);

            foreach (var binding in AnimationUtility.GetCurveBindings(src))
            {
                if (string.IsNullOrEmpty(binding.path))
                {
                    Debug.LogFormat("{0}:{1}:{2}", binding.path, binding.type, binding.propertyName);
                }
                /*
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (binding.propertyName.StartsWith("m_Offset.") || binding.propertyName.StartsWith("m_Size."))
                {
                    for (int j = 0; j < curve.keys.Length; j++)
                    {
                        var key = curve.keys[j];
                        key.value = ((int)(key.value * 1000.0f)) * 0.001f;
                        curve.MoveKey(j, key);
                    }
                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                }
                */
            }

            var dst = new AnimationClip();
            var dstPath = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + ".anim");

            var dstBinding = new EditorCurveBinding
            {
                propertyName = "Spine Front - Back",
            };

            Debug.LogFormat("create: {0}", dstPath);
            AssetDatabase.CreateAsset(dst, dstPath);
            AssetDatabase.Refresh();
        }

        [MenuItem(CONVERT_HUMANOID_KEY, true)]
        private static bool ConvertHumanoidValidation()
        {
            return Selection.activeObject is AnimationClip;
        }
    }
}
