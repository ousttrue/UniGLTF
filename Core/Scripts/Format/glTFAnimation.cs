using System;
using System.Linq;
using System.Collections.Generic;
using UniJSON;


namespace UniGLTF
{
    [Serializable]
    public class glTFAnimationTarget : IJsonSerializable
    {
        [JsonSchema(Minimum = 0)]
        public int node;

        [JsonSchema(Required = true, EnumValues = new object[] { "translation", "rotation", "scale", "weights" })]
        public string path;

        // empty schemas
        public object extensions;
        public object extras;

        public string ToJson()
        {
            var f = new GLTFJsonFormatter();
            f.BeginMap();

            f.KeyValue(() => node);
            f.KeyValue(() => path);

            f.EndMap();
            return f.ToString();
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
    public class glTFAnimationChannel : IJsonSerializable
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int sampler = -1;

        [JsonSchema(Required = true)]
        public glTFAnimationTarget target;

        // empty schemas
        public object extensions;
        public object extras;

        public string ToJson()
        {
            var f = new GLTFJsonFormatter();
            f.BeginMap();

            f.KeyValue(() => sampler);
            f.KeyValue(() => target);

            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFAnimationSampler : IJsonSerializable
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

        public string ToJson()
        {
            var f = new GLTFJsonFormatter();
            f.BeginMap();

            f.KeyValue(() => input);
            f.KeyValue(() => interpolation);
            f.KeyValue(() => output);

            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFAnimation : IJsonSerializable
    {
        public string name = "";

        [JsonSchema(Required = true, MinItems = 1)]
        public List<glTFAnimationChannel> channels = new List<glTFAnimationChannel>();

        [JsonSchema(Required = true, MinItems = 1)]
        public List<glTFAnimationSampler> samplers = new List<glTFAnimationSampler>();

        // empty schemas
        public object extensions;
        public object extras;

        public string ToJson()
        {
            var f = new GLTFJsonFormatter();
            f.BeginMap();

            f.KeyValue(() => name);
            f.KeyValue(() => channels);
            f.KeyValue(() => samplers);

            f.EndMap();
            return f.ToString();
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
