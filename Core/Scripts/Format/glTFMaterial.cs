using System;
using System.Collections.Generic;


namespace UniGLTF
{
    [Serializable]
    public class GltfTextureRef: IJsonSerializable
    {
        public int index = -1;
        public int texCoord;
        public float scale;
        public float strength;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.Key("index"); f.Value(index);
            f.Key("texCoord"); f.Value(texCoord);
            //f.Key("scale"); f.Value(scale);
            //f.Key("strength"); f.Value(strength);
            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class GltfPbrMetallicRoughness: IJsonSerializable
    {
        public GltfTextureRef baseColorTexture = null;
        public float[] baseColorFactor;
        public GltfTextureRef metallicRoughnessTexture = null;
        public float metallicFactor;
        public float roughnessFactor;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            if (baseColorTexture != null)
            {
                f.KeyValue(() => baseColorTexture);
            }
            if (baseColorFactor != null)
            {
                f.KeyValue(() => baseColorFactor);
            }
            if (metallicRoughnessTexture != null)
            {
                f.KeyValue(() => metallicRoughnessTexture);
            }
            f.KeyValue(() => metallicFactor);
            f.KeyValue(() => roughnessFactor);
            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFMaterial: IJsonSerializable
    {
        public string name;
        public GltfPbrMetallicRoughness pbrMetallicRoughness;
        public GltfTextureRef normalTexture = null;
        public GltfTextureRef occlusionTexture = null;
        public GltfTextureRef emissiveTexture = null;
        public float[] emissiveFactor;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            if (!String.IsNullOrEmpty(name))
            {
                f.Key("name"); f.Value(name);
            }
            if (pbrMetallicRoughness != null)
            {
                f.Key("pbrMetallicRoughness"); f.Value(pbrMetallicRoughness);
            }
            if (normalTexture != null)
            {
                f.Key("normalTexture"); f.Value(normalTexture);
            }
            if (occlusionTexture != null)
            {
                f.Key("occlusionTexture"); f.Value(occlusionTexture);
            }
            if (emissiveTexture != null)
            {
                f.Key("emissiveTexture"); f.Value(emissiveTexture);
            }
            if (emissiveFactor != null)
            {
                f.Key("emissiveFactor"); f.Value(emissiveFactor);
            }
            f.EndMap();
            return f.ToString();
        }
    }
}
