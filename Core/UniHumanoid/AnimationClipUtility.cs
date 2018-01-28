using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniHumanoid
{
    public class AnimationClipUtility : MonoBehaviour
    {
        const string CONVERT_HUMANOID_KEY = "Assets/AnimationClip/ConvertHumanoid";

#if UNITY_EDITOR
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
                    if (HumanTrait.MuscleName.Contains(binding.propertyName))
                    {
                        // muscle
                    }
                    else
                    {
                        // not muscle
                        Debug.LogFormat("{0}:{1}:{2}", binding.path, binding.type, binding.propertyName);
                    }
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

            //var muscles = HumanTrait.MuscleName.Select((x, i) => string.Format("[{0}]{1}", i, x)).ToArray();
            //var str = string.Join("\n", muscles);

            var dst = new AnimationClip();
            {
                var curve = new AnimationCurve(new Keyframe[]
                {
                new Keyframe(0, 0),
                });
                var muscle = "RootT.x";
                dst.SetCurve(null, typeof(Animator), muscle, curve);
            }
            {
                var curve = new AnimationCurve(new Keyframe[]
                {
                new Keyframe(0, 0.8f),
                });
                var muscle = "RootT.y";
                dst.SetCurve(null, typeof(Animator), muscle, curve);
            }
            {
                var curve = new AnimationCurve(new Keyframe[]
                {
                new Keyframe(0, 0),
                });
                var muscle = "RootT.z";
                dst.SetCurve(null, typeof(Animator), muscle, curve);
            }
            {
                var curve = new AnimationCurve(new Keyframe[]
                {
                new Keyframe(0, -1),
                new Keyframe(1, 1),
                });
                var muscle = "Spine Front-Back";
                dst.SetCurve(null, typeof(Animator), muscle, curve);
            }

            var dstPath = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + ".anim");
            Debug.LogFormat("create: {0}", dstPath);
            AssetDatabase.CreateAsset(dst, dstPath);
            AssetDatabase.Refresh();
        }

        [MenuItem(CONVERT_HUMANOID_KEY, true)]
        private static bool ConvertHumanoidValidation()
        {
            return Selection.activeObject is AnimationClip;
        }
#endif
    }
}
