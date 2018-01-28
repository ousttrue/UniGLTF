using System;
using System.Linq;
using System.Collections.Generic;


namespace UniGLTF
{
    [Serializable]
    public class glTFAnimationTarget: IJsonSerializable
    {
        public int node;
        public string path;

        public string ToJson()
        {
            var f = new JsonFormatter();
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
    public class glTFAnimationChannel: IJsonSerializable
    {
        public int sampler = -1;
        public glTFAnimationTarget target;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();

            f.KeyValue(() => sampler);
            f.KeyValue(() => target);

            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFAnimationSampler: IJsonSerializable
    {
        public int input = -1;
        public string interpolation;
        public int output = -1;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();

            f.KeyValue(() => input);
            f.KeyValue(() => interpolation);
            f.KeyValue(() => output);

            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFAnimation: IJsonSerializable
    {
        public string name = "";
        public List<glTFAnimationChannel> channels = new List<glTFAnimationChannel>();
        public List<glTFAnimationSampler> samplers = new List<glTFAnimationSampler>();

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();

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
