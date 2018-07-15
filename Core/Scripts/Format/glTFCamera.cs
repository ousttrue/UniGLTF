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
        public float xmag;
        public float ymag;
        [JsonSchema(Minimum = 0.0f, ExclusiveMinimum = true)]
        public float zfar;
        [JsonSchema(Minimum = 0.0f)]
        public float znear;
    }

    [Serializable]
    public class glTFPerspective : glTFProperty
    {
        [JsonSchema(Minimum = 0.0f, ExclusiveMinimum = true)]
        public float aspectRatio;
        [JsonSchema(Minimum = 0.0f, ExclusiveMinimum = true)]
        public float yfov;
        [JsonSchema(Minimum = 0.0f, ExclusiveMinimum = true)]
        public float zfar;
        [JsonSchema(Minimum = 0.0f, ExclusiveMinimum = true)]
        public float znear;
    }

    [Serializable]
    public class glTFCamera : glTFChildOfRootProperty
    {
        public glTFOrthographic orthographic;
        public glTFPerspective perspective;

        [JsonSchema(EnumSerializationType = EnumSerializationType.AsLowerString)]
        public ProjectionType type;
    }
}
