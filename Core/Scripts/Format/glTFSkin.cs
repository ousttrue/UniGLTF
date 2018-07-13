using System;


namespace UniGLTF
{
    [Serializable]
    public class glTFSkin : IJsonSerializable
    {
        public int inverseBindMatrices = -1;
        public int[] joints;
        public int skeleton;

        // empty schemas
        public object extensions;
        public object extras;
        public object name;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.KeyValue(() => inverseBindMatrices);
            f.KeyValue(() => joints);
            f.EndMap();
            return f.ToString();
        }
    }
}
