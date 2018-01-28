using System;
using System.Linq;


namespace UniGLTF
{
    [Serializable]
    public class glTFBuffer : IJsonSerializable
    {
        public IBytesBuffer Storage
        {
            get;
            private set;
        }
        public void OpenStorage(string baseDir, ArraySegment<Byte> glbDataBytes)
        {
            if (string.IsNullOrEmpty(uri))
            {
                Storage = new ArraySegmentByteBuffer(glbDataBytes);
            }
            else
            {
                Storage = new UriByteBuffer(baseDir, uri);
            }
        }

        public glTFBuffer(IBytesBuffer storage)
        {
            Storage = storage;
        }

        public string uri;
        public int byteLength;
        public void UpdateByteLength()
        {
            byteLength = Storage.GetBytes().Count;
        }

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            if (!string.IsNullOrEmpty(uri))
            {
                f.KeyValue(() => uri);
            }
            f.KeyValue(() => byteLength);
            f.EndMap();
            return f.ToString();
        }
    }

    [Serializable]
    public class glTFBufferView : IJsonSerializable
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
            f.KeyValue(() => buffer);
            f.KeyValue(() => byteOffset);
            f.KeyValue(() => byteLength);
            if (target != glBufferTarget.NONE)
            {
                f.Key("target"); f.Value((int)target);
            }
            if (target == glBufferTarget.ARRAY_BUFFER)
            {
                f.KeyValue(() => byteStride);
            }
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
            if (max != null && max.Any())
            {
                f.KeyValue(() => max);
            }
            if (min != null && min.Any())
            {
                f.KeyValue(() => min);
            }
            f.EndMap();
            return f.ToString();
        }
    }
}
