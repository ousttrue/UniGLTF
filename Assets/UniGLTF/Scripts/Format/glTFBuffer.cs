using System;
using System.IO;


namespace UniGLTF
{
    [Serializable]
    public struct glTFBuffer
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
    }

    [Serializable]
    public struct glTFBufferView
    {
        public int buffer;
        public int byteOffset;
        public int byteLength;
        public int byteStride;
        public glBufferTarget target;
    }

    [Serializable]
    public struct glTFAccessor
    {
        public int bufferView;
        public int byteOffset;
        public string type;
        public glComponentType componentType;
        public int count;
        public float[] max;
        public float[] min;
    }
}
