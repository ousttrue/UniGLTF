using System;
using System.Linq;


namespace UniGLTF
{
    [Serializable]
    public class glTFNode : IJsonSerializable
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

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            if (!string.IsNullOrEmpty(name))
            {
                f.KeyValue(() => name);
            }
            if (children != null && children.Any())
            {
                f.Key("children"); f.BeginList();
                foreach (var child in children)
                {
                    f.Value(child);
                }
                f.EndList();
            }
            if (matrix != null)
            {
                f.KeyValue(() => matrix);
            }
            if (translation != null)
            {
                f.KeyValue(() => translation);
            }
            if (rotation != null)
            {
                f.KeyValue(() => rotation);
            }
            if (scale != null)
            {
                f.KeyValue(() => scale);
            }

            if (mesh >= 0)
            {
                f.KeyValue(() => mesh);
            }
            if (skin >= 0)
            {
                f.KeyValue(() => skin);
            }
            if (camera >= 0)
            {
                f.KeyValue(() => camera);
            }
            f.EndMap();
            return f.ToString();
        }
    }
}
