using System;
using UniJSON;

namespace UniGLTF
{
    [Serializable]
    public class glTFTextureInfo : IJsonSerializable
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int index = -1;

        [JsonSchema(Minimum = 0)]
        public int texCoord;
        //public float strength;

        // empty schemas
        public object extensions;
        public object extras;

        public string ToJson()
        {
            var f = new GLTFJsonFormatter();
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
    public class glTFMaterialNormalTextureInfo : glTFTextureInfo
    {
        public float scale;
    }

    [Serializable]
    public class glTFMaterialOcclusionTextureInfo : glTFTextureInfo
    {
        [JsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float strength;
    }

    [Serializable]
    public class glTFPbrMetallicRoughness : IJsonSerializable
    {
        public glTFTextureInfo baseColorTexture = null;

        [JsonSchema(MinItems = 4, MaxItems = 4)]
        [ItemJsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float[] baseColorFactor;

        public glTFTextureInfo metallicRoughnessTexture = null;

        [JsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float metallicFactor;

        [JsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float roughnessFactor;

        // empty schemas
        public object extensions;
        public object extras;

        public string ToJson()
        {
            var f = new GLTFJsonFormatter();
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
    public class glTFMaterial : IJsonSerializable
    {
        public string name;
        public glTFPbrMetallicRoughness pbrMetallicRoughness;
        public glTFMaterialNormalTextureInfo normalTexture = null;

        public glTFMaterialOcclusionTextureInfo occlusionTexture = null;

        public glTFTextureInfo emissiveTexture = null;

        [JsonSchema(MinItems = 3, MaxItems = 3)]
        [ItemJsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float[] emissiveFactor;

        [JsonSchema(EnumValues = new object[] { "OPAQUE", "MASK", "BLEND" })]
        public string alphaMode;

        [JsonSchema(Dependencies = new string[] { "alphaMode" }, Minimum = 0.0)]
        public float alphaCutoff = 0.5f;

        public bool doubleSided;

        // empty schemas
        public object extensions;
        public object extras;

        public string ToJson()
        {
            var f = new GLTFJsonFormatter();
            f.BeginMap();
            if (!String.IsNullOrEmpty(name))
            {
                f.Key("name"); f.Value(name);
            }
            if (pbrMetallicRoughness != null)
            {
                f.Key("pbrMetallicRoughness"); f.GLTFValue(pbrMetallicRoughness);
            }
            if (normalTexture != null)
            {
                f.Key("normalTexture"); f.GLTFValue(normalTexture);
            }
            if (occlusionTexture != null)
            {
                f.Key("occlusionTexture"); f.GLTFValue(occlusionTexture);
            }
            if (emissiveTexture != null)
            {
                f.Key("emissiveTexture"); f.GLTFValue(emissiveTexture);
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
