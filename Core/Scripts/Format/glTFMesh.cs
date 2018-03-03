using System;
using System.Collections.Generic;


namespace UniGLTF
{
    [Serializable]
    public class glTFAttributes : IJsonSerializable
    {
        public int POSITION = -1;
        public int NORMAL = -1;
        public int TANGENT = -1;
        public int TEXCOORD_0 = -1;
        public int JOINTS_0 = -1;
        public int WEIGHTS_0 = -1;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as glTFAttributes;
            if (rhs == null)
            {
                return base.Equals(obj);
            }

            return POSITION == rhs.POSITION
                && NORMAL == rhs.NORMAL
                && TANGENT == rhs.TANGENT
                && TEXCOORD_0 == rhs.TEXCOORD_0
                && JOINTS_0 == rhs.JOINTS_0
                && WEIGHTS_0 == rhs.WEIGHTS_0
                ;
        }

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            if (POSITION >= 0) { f.KeyValue(() => POSITION); }
            if (NORMAL >= 0) { f.KeyValue(() => NORMAL); }
            if (TANGENT >= 0) { f.KeyValue(() => TANGENT); }
            if (TEXCOORD_0 >= 0) { f.KeyValue(() => TEXCOORD_0); }
            if (JOINTS_0 >= 0) { f.KeyValue(() => JOINTS_0); }
            if (WEIGHTS_0 >= 0) { f.KeyValue(() => WEIGHTS_0); }
            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFPrimitives: IJsonSerializable
    {
        public int mode;
        public int indices = -1;
        public glTFAttributes attributes;
        public int material;

        public List<glTFAttributes> targets = new List<glTFAttributes>();

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.KeyValue(() => mode);
            f.KeyValue(() => indices);
            f.Key("attributes"); f.Value(attributes);
            f.KeyValue(() => material);
            if (targets != null)
            {
                f.Key("targets"); f.Value(targets);
            }
            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFMesh : IJsonSerializable
    {
        public string name;
        public List<glTFPrimitives> primitives;
        public float[] weights;

        public glTFMesh(string _name)
        {
            name = _name;
            primitives = new List<glTFPrimitives>();
        }

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.KeyValue(() => name);
            f.Key("primitives"); f.Value(primitives);
            f.EndMap();
            return f.ToString();
        }
    }
}
