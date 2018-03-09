using System;
using System.Collections.Generic;


namespace UniGLTF
{
    #region Draco
    [Serializable]
    public class glTF_KHR_draco_mesh_compression : JsonSerializableBase
    {
        public int bufferView;
        public glTFAttributes attributes;

        protected override void SerializeMembers(JsonFormatter f)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class glTFPrimitivesExtensions : JsonSerializableBase
    {
        public glTF_KHR_draco_mesh_compression KHR_draco_mesh_compression;

        protected override void SerializeMembers(JsonFormatter f)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    [Serializable]
    public class glTFAttributes : JsonSerializableBase
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

        protected override void SerializeMembers(JsonFormatter f)
        {
            f.KeyValue(() => POSITION);
            f.KeyValue(() => NORMAL);
            f.KeyValue(() => TANGENT);
            f.KeyValue(() => TEXCOORD_0);
            f.KeyValue(() => JOINTS_0);
            f.KeyValue(() => WEIGHTS_0);
        }
    }

    [Serializable]
    public class gltfMorphTarget : glTFAttributes
    {
        public extraName extra = new extraName();

        protected override void SerializeMembers(JsonFormatter f)
        {
            f.KeyValue(() => extra);
            base.SerializeMembers(f);
        }
    }

    [Serializable]
    public class glTFPrimitives: IJsonSerializable
    {
        public int mode;
        public int indices = -1;
        public glTFAttributes attributes;
        public int material;

        public List<gltfMorphTarget> targets = new List<gltfMorphTarget>();

        public glTFPrimitivesExtensions extensions;

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
