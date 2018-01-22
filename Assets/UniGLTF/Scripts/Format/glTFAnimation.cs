using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniGLTF
{
    [Serializable]
    public struct AnimationTarget
    {
        public int node;
        public string path;
    }

    [Serializable]
    public struct Channel
    {
        public int sampler;
        public AnimationTarget target;
    }

    [Serializable]
    public struct Sampler
    {
        public int input;
        public string interpolation;
        public int output;
    }

    [Serializable]
    public struct GltfAnimation
    {
        public Channel[] channels;
        public Sampler[] samplers;

        public static string ANIMATION_NAME = "animation";

        public static T GetOrCreate<T>(UnityEngine.Object[] assets, string name, Func<T> create) where T : UnityEngine.Object
        {
            var found = assets.FirstOrDefault(x => x.name == name);
            if (found != null)
            {
                return found as T;
            }
            return create();
        }

        public static void ReadAnimation(AnimationClip clip, GltfAnimation[] animations, Transform[] nodes, GltfBuffer buffer)
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

                                var relativePath = node.RelativePathFrom(nodes[0]);
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

                                var relativePath = node.RelativePathFrom(nodes[0]);
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

                                var relativePath = node.RelativePathFrom(nodes[0]);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.x", curveX);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.y", curveY);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.z", curveZ);
                            }
                            break;
                    }
                }
            }
        }
    }
}