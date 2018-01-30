using System;


namespace UniGLTF
{
    [Serializable]
    public class glTFTextureSampler : IJsonSerializable
    {
        public glFilter magFilter = glFilter.NEAREST;
        public glFilter minFilter = glFilter.NEAREST;
        public glWrap wrapS = glWrap.REPEAT;
        public glWrap wrapT = glWrap.REPEAT;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.Key("magFilter"); f.Value((int)magFilter);
            f.Key("minFilter"); f.Value((int)minFilter);
            f.Key("wrapS"); f.Value((int)wrapS);
            f.Key("wrapT"); f.Value((int)wrapT);
            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFImage : IJsonSerializable
    {
        public string uri;

        public int bufferView;
        public string mimeType;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            if (!string.IsNullOrEmpty(uri))
            {
                f.KeyValue(() => uri);
            }
            else
            {
                f.KeyValue(() => bufferView);
                f.KeyValue(() => mimeType);
            }
            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFTexture : IJsonSerializable
    {
        public int sampler;
        public int source;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.KeyValue(() => sampler);
            f.KeyValue(() => source);
            f.EndMap();
            return f.ToString();
        }
    }
}
