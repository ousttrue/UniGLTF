using System;
using UniJSON;

namespace UniGLTF
{
    public enum ProjectionType
    {
        Perspective,
        Orthographic
    }

    [Serializable]
    public class glTFOrthographic : glTFProperty
    {
        [JsonSchema(Required = true)]
        public float xmag;
        [JsonSchema(Required = true)]
        public float ymag;
        [JsonSchema(Required = true, Minimum = 0.0f, ExclusiveMinimum = true)]
        public float zfar;
        [JsonSchema(Required = true, Minimum = 0.0f)]
        public float znear;
    }

    [Serializable]
    public class glTFPerspective : glTFProperty
    {
        [JsonSchema(Minimum = 0.0f, ExclusiveMinimum = true)]
        public float aspectRatio;
        [JsonSchema(Required = true, Minimum = 0.0f, ExclusiveMinimum = true)]
        public float yfov;
        [JsonSchema(Minimum = 0.0f, ExclusiveMinimum = true)]
        public float zfar;
        [JsonSchema(Required = true, Minimum = 0.0f, ExclusiveMinimum = true)]
        public float znear;
    }

    [Serializable]
    public class glTFCamera : glTFChildOfRootProperty
    {
        public glTFOrthographic orthographic;
        public glTFPerspective perspective;

        [JsonSchema(Required = true, EnumSerializationType = EnumSerializationType.AsLowerString)]
        public ProjectionType type;
    }
}
