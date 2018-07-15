using System;
using System.Linq;
using UniJSON;

namespace UniGLTF
{
    [Serializable]
    public class glTFNode_extra_rootBone : JsonSerializableBase
    {
        public int skinRootBone = -1; // for Unity's SkinnedMeshRenderer

        protected override void SerializeMembers(JsonFormatter f)
        {
            f.KeyValue(() => skinRootBone);
        }
    }

    [Serializable]
    public class glTFNode : JsonSerializableBase
    {
        public string name = "";

        [JsonSchema(MinItems = 1)]
        [ItemJsonSchema(Minimum = 0)]
        public int[] children;

        [JsonSchema(MinItems = 16, MaxItems = 16)]
        public float[] matrix;

        [JsonSchema(MinItems = 3, MaxItems = 3)]
        public float[] translation;

        [JsonSchema(MinItems = 4, MaxItems = 4)]
        [ItemJsonSchema(Minimum = -1.0, Maximum = 1.0)]
        public float[] rotation;

        [JsonSchema(MinItems = 3, MaxItems = 3)]
        public float[] scale;

        [JsonSchema(Minimum = 0)]
        public int mesh = -1;

        [JsonSchema(Minimum = 0)]
        public int skin = -1;

        [JsonSchema(Minimum = 0)]
        public int camera = -1;

        [JsonSchema(MinItems = 1)]
        public float[] weights;

        // empty schemas
        public object extensions;
        public glTFNode_extra_rootBone extras = new glTFNode_extra_rootBone();

        protected override void SerializeMembers(JsonFormatter f)
        {
            if (children != null && children.Any())
            {
                f.Key("children"); f.BeginList();
                foreach (var child in children)
                {
                    f.Value(child);
                }
                f.EndList();
            }

            if (!string.IsNullOrEmpty(name)) f.KeyValue(() => name);
            if (matrix != null) f.KeyValue(() => matrix);
            if (translation != null) f.KeyValue(() => translation);
            if (rotation != null) f.KeyValue(() => rotation);
            if (scale != null) f.KeyValue(() => scale);

            if (mesh >= 0) f.KeyValue(() => mesh);
            if (camera >= 0) f.KeyValue(() => camera);
            if (skin >= 0)
            {
                f.KeyValue(() => skin);
                f.KeyValue(() => extras);
            }
        }
    }
}
