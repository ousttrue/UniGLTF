using System;
using UniJSON;

namespace UniGLTF
{
    [Serializable]
    public class glTFSkin : IJsonSerializable
    {
        [JsonSchema(Minimum = 0)]
        public int inverseBindMatrices = -1;

        [JsonSchema(Required = true, MinItems = 1)]
        [ItemJsonSchema(Minimum = 0)]
        public int[] joints;

        [JsonSchema(Minimum = 0)]
        public int skeleton;

        // empty schemas
        public object extensions;
        public object extras;
        public string name;

        public string ToJson()
        {
            var f = new GLTFJsonFormatter();
            f.BeginMap();
            f.KeyValue(() => inverseBindMatrices);
            f.KeyValue(() => joints);
            f.KeyValue(() => skeleton);
            f.EndMap();
            return f.ToString();
        }
    }
}
