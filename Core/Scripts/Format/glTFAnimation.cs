using System;
using System.Linq;
using System.Collections.Generic;
using UniJSON;


namespace UniGLTF
{
    [Serializable]
    public class glTFAnimationTarget : JsonSerializableBase
    {
        [JsonSchema(Minimum = 0)]
        public int node;

        [JsonSchema(Required = true, EnumValues = new object[] { "translation", "rotation", "scale", "weights" })]
        public string path;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => node);
            if (!string.IsNullOrEmpty(path))
            {
                f.KeyValue(() => path);
            }
        }

        public const string PATH_TRANSLATION = "translation";
        public const string PATH_ROTATION = "rotation";
        public const string PATH_SCALE = "scale";
        public const string PATH_WEIGHT = "weights";
        public const string NOT_IMPLEMENTED = "NotImplemented";

        public static int GetElementCount(string target)
        {
            switch (target)
            {
                case PATH_TRANSLATION: return 3;
                case PATH_ROTATION: return 4;
                case PATH_SCALE: return 3;
                default: throw new NotImplementedException();
            }
        }
    }

    [Serializable]
    public class glTFAnimationChannel : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int sampler = -1;

        [JsonSchema(Required = true)]
        public glTFAnimationTarget target;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => sampler);
            f.KeyValue(() => target);
        }
    }

    [Serializable]
    public class glTFAnimationSampler : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int input = -1;

        [JsonSchema(EnumValues = new object[] { "LINEAR", "STEP", "CUBICSPLINE" })]
        public string interpolation;

        [JsonSchema(Required = true, Minimum = 0)]
        public int output = -1;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => input);
            if (!string.IsNullOrEmpty(interpolation))
            {
                f.KeyValue(() => interpolation);
            }
            f.KeyValue(() => output);
        }
    }

    [Serializable]
    public class glTFAnimation : JsonSerializableBase
    {
        public string name = "";

        [JsonSchema(Required = true, MinItems = 1)]
        public List<glTFAnimationChannel> channels = new List<glTFAnimationChannel>();

        [JsonSchema(Required = true, MinItems = 1)]
        public List<glTFAnimationSampler> samplers = new List<glTFAnimationSampler>();

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            if (!string.IsNullOrEmpty(name))
            {
                f.KeyValue(() => name);
            }

            f.KeyValue(() => channels);
            f.KeyValue(() => samplers);
        }

        public int AddChannelAndGetSampler(int nodeIndex, string path)
        {
            // find channel
            var channel = channels.FirstOrDefault(x => x.target.node == nodeIndex && x.target.path == path);
            if (channel != null)
            {
                return channel.sampler;
            }

            // not found. create new
            var samplerIndex = samplers.Count;
            var sampler = new glTFAnimationSampler();
            samplers.Add(sampler);

            channel = new glTFAnimationChannel
            {
                sampler = samplerIndex,
                target = new glTFAnimationTarget
                {
                    node = nodeIndex,
                    path = path,
                },
            };
            channels.Add(channel);

            return samplerIndex;
        }
    }
}
