using System;


namespace UniGLTF
{
    [Serializable]
    public struct gltfImage : IJsonSerializable
    {
        public string uri;
        public int bufferView;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            if (!string.IsNullOrEmpty(uri))
            {
                f.Key("uri"); f.Value(uri);
            }
            f.Key("bufferView"); f.Value(bufferView);
            f.EndMap();
            return f.ToString();
        }
    }

    public struct TextureWithImage
    {
        public gltfTexture Texture;
        public gltfTexture Image;
    }

    [Serializable]
    public struct gltfTexture : IJsonSerializable
    {
        public int sampler;
        public int source;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.Key("sampler"); f.Value(sampler);
            f.Key("source"); f.Value(source);
            f.EndMap();
            return f.ToString();
        }
    }
}
