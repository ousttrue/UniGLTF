using System;
using UniJSON;

namespace UniGLTF
{
    [Serializable]
    public class glTFTextureInfo : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int index = -1;

        [JsonSchema(Minimum = 0)]
        public int texCoord;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => index);
            f.KeyValue(() => texCoord);
        }
    }


    [Serializable]
    public class glTFMaterialNormalTextureInfo : glTFTextureInfo
    {
        public float scale;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => scale);
            base.SerializeMembers(f);
        }
    }

    [Serializable]
    public class glTFMaterialOcclusionTextureInfo : glTFTextureInfo
    {
        [JsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float strength;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => strength);
            base.SerializeMembers(f);
        }
    }

    [Serializable]
    public class glTFPbrMetallicRoughness : JsonSerializableBase
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

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
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
        }
    }

    [Serializable]
    public class glTFMaterial : JsonSerializableBase
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

        [JsonSchema(SkipSchemaComparison = true)]
        public glTFMaterial_extensions extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
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

            f.KeyValue(() => doubleSided);
        }
    }
}
