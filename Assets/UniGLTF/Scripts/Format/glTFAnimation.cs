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
    }
}
