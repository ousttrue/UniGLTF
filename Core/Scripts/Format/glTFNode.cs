using System;
using System.Linq;


namespace UniGLTF
{
    [Serializable]
    public class glTFNode_extra_rootBone: JsonSerializableBase
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
        public int[] children;
        public float[] matrix;
        public float[] translation;
        public float[] rotation;
        public float[] scale;
        public int mesh = -1;
        public int skin = -1;
        public int camera = -1;

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
