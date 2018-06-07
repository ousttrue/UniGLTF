using System;
using System.Linq;


namespace UniGLTF
{
    [Serializable]
    public class glTFBuffer : IJsonSerializable
    {
        IBytesBuffer Storage;

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

        public glTFBufferView Append<T>(T[] array, glBufferTarget target) where T : struct
        {
            return Append(new ArraySegment<T>(array), target);
        }
        public glTFBufferView Append<T>(ArraySegment<T> segment, glBufferTarget target) where T : struct
        {
            var view = Storage.Extend(segment, target);
            byteLength = Storage.GetBytes().Count;
            return view;
        }

        public ArraySegment<Byte> GetBytes()
        {
            return Storage.GetBytes();
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
    public class glTFSparseIndices : JsonSerializableBase
    {
        public int bufferView = -1;
        public int byteOffset;
        public glComponentType componentType;

        protected override void SerializeMembers(JsonFormatter f)
        {
            f.KeyValue(() => bufferView);
            f.KeyValue(() => byteOffset);
            f.Key("componentType"); f.Value((int)componentType);
        }
    }

    [Serializable]
    public class glTFSparseValues : JsonSerializableBase
    {
        public int bufferView = -1;
        public int byteOffset;

        protected override void SerializeMembers(JsonFormatter f)
        {
            f.KeyValue(() => bufferView);
            f.KeyValue(() => byteOffset);
        }
    }

    [Serializable]
    public class glTFSparse : JsonSerializableBase
    {
        public int count;
        public glTFSparseIndices indices;
        public glTFSparseValues values;

        protected override void SerializeMembers(JsonFormatter f)
        {
            f.KeyValue(() => count);
            f.KeyValue(() => indices);
            f.KeyValue(() => values);
        }
    }

    [Serializable]
    public class glTFAccessor : JsonSerializableBase
    {
        public int bufferView = -1;
        public int byteOffset;
        public string type;
        public glComponentType componentType;
        public int count;
        public float[] max;
        public float[] min;

        public glTFSparse sparse;

        protected override void SerializeMembers(JsonFormatter f)
        {
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

            if (sparse != null && sparse.count > 0)
            {
                f.KeyValue(() => sparse);
            }
        }
    }
}
