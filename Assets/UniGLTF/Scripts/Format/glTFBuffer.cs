using System;
using System.IO;


namespace UniGLTF
{
    [Serializable]
    public struct glTFBuffer : IJsonSerializable
    {
        public string uri;
        public int byteLength;

        const string DataPrefix = "data:application/octet-stream;base64,";

        public Byte[] GetBytes(string baseDir)
        {
            if (uri.StartsWith(DataPrefix))
            {
                // embeded
                return Convert.FromBase64String(uri.Substring(DataPrefix.Length));
            }
            else
            {
                // as local file path
                return File.ReadAllBytes(Path.Combine(baseDir, uri));
            }
        }

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            if (!string.IsNullOrEmpty(uri))
            {
                f.Key("uri"); f.Value(uri);
            }
            f.Key("byteLength"); f.Value(byteLength);
            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public struct glTFBufferView : IJsonSerializable
    {
        public int buffer;
        public int byteOffset;
        public int byteLength;
        public int byteStride;
        public glBufferTarget target;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.Key("buffer"); f.Value(buffer);
            f.Key("byteOffset"); f.Value(byteOffset);
            f.Key("byteLength"); f.Value(byteLength);
            f.Key("byteStride"); f.Value(byteStride);
            f.Key("target"); f.Value((int)target);
            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFAccessor : IJsonSerializable
    {
        public int bufferView;
        public int byteOffset;
        public string type;
        public glComponentType componentType;
        public int count;
        public float[] max;
        public float[] min;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.KeyValue(() => bufferView);
            f.KeyValue(() => byteOffset);
            f.KeyValue(() => type);
            f.Key("componentType"); f.Value((int)componentType);
            f.KeyValue(() => count);
            f.KeyValue(() => max);
            f.KeyValue(() => min);
            f.EndMap();
            return f.ToString();
        }
    }
}
