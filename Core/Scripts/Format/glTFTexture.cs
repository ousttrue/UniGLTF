using System;


namespace UniGLTF
{
    [Serializable]
    public class glTFTextureSampler : JsonSerializableBase
    {
        public glFilter magFilter = glFilter.NEAREST;
        public glFilter minFilter = glFilter.NEAREST;
        public glWrap wrapS = glWrap.REPEAT;
        public glWrap wrapT = glWrap.REPEAT;

        // empty schemas
        public object extensions;
        public object extras;
        public object name;

        protected override void SerializeMembers(JsonFormatter f)
        {
            f.Key("magFilter"); f.Value((int)magFilter);
            f.Key("minFilter"); f.Value((int)minFilter);
            f.Key("wrapS"); f.Value((int)wrapS);
            f.Key("wrapT"); f.Value((int)wrapT);
        }
    }

    [Serializable]
    public class glTFImage : JsonSerializableBase
    {
        public string name;
        public string uri;

        public int bufferView;
        public string mimeType;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(JsonFormatter f)
        {
            f.KeyValue(() => name);
            if (!string.IsNullOrEmpty(uri))
            {
                f.KeyValue(() => uri);
            }
            else
            {
                f.KeyValue(() => bufferView);
                f.KeyValue(() => mimeType);
            }
        }
    }

    [Serializable]
    public class glTFTexture : JsonSerializableBase
    {
        public int sampler;
        public int source;

        // empty schemas
        public object extensions;
        public object extras;
        public object name;

        protected override void SerializeMembers(JsonFormatter f)
        {
            f.KeyValue(() => sampler);
            f.KeyValue(() => source);
        }
    }
}
