using System;
using System.IO;
using System.Runtime.InteropServices;


namespace UniGLTF
{
    public interface IBytesBuffer
    {
        string Uri { get; }
        ArraySegment<Byte> GetBytes();
        glTFBufferView Extend<T>(T[] array, glBufferTarget target, bool isVertex) where T : struct;
    }

    /// <summary>
    /// for buffer with uri read
    /// </summary>
    public class UriByteBuffer: IBytesBuffer
    {
        public string Uri
        {
            get;
            private set;
        }

        Byte[] m_bytes;
        public ArraySegment<byte> GetBytes()
        {
            return new ArraySegment<byte>(m_bytes);
        }

        public UriByteBuffer(string baseDir, string uri)
        {
            Uri = uri;
            m_bytes = ReadFromUri(baseDir, uri);
        }

        const string DataPrefix = "data:application/octet-stream;base64,";

        Byte[] ReadFromUri(string baseDir, string uri)
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

        public glTFBufferView Extend<T>(T[] array, glBufferTarget target, bool isVertex) where T : struct
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// for glb chunk buffer read
    /// </summary>
    public class ArraySegmentByteBuffer : IBytesBuffer
    {
        ArraySegment<Byte> m_bytes;

        public ArraySegmentByteBuffer(ArraySegment<Byte> bytes)
        {
            m_bytes = bytes;
        }

        public string Uri
        {
            get;
            private set;
        }

        public glTFBufferView Extend<T>(T[] array, glBufferTarget target, bool isVertex) where T : struct
        {
            throw new NotImplementedException();
        }

        public ArraySegment<byte> GetBytes()
        {
            return m_bytes;
        }
    }

    /// <summary>
    /// for exporter
    /// </summary>
    public class ArrayByteBuffer : IBytesBuffer
    {
        public string Uri
        {
            get;
            private set;
        }

        Byte[] m_bytes;

        public ArrayByteBuffer(Byte[] bytes = null)
        {
            Uri = "";
            m_bytes = bytes;
        }

        public glTFBufferView Extend<T>(T[] array, glBufferTarget target, bool isVertex) where T : struct
        {
            using (var pin = Pin.Create(array))
            {
                var elementSize = Marshal.SizeOf(typeof(T));
                var view=Extend(pin.Ptr, array.Length * elementSize, elementSize, target);
                if (!isVertex)
                {
                    view.byteStride = 0;
                }
                return view;
            }
        }

        public glTFBufferView Extend(IntPtr p, int bytesLength, int stride, glBufferTarget target)
        {
            if (m_bytes == null)
            {
                m_bytes = new byte[bytesLength];
                Marshal.Copy(p, m_bytes, 0, bytesLength);
                return new glTFBufferView
                {
                    buffer = 0,
                    byteLength = bytesLength,
                    byteOffset = 0,
                    byteStride = stride,
                    target = target,
                };
            }
            else
            {
                var tmp = m_bytes;
                m_bytes = new Byte[m_bytes.Length + bytesLength];
                Buffer.BlockCopy(tmp, 0, m_bytes, 0, tmp.Length);
                Marshal.Copy(p, m_bytes, tmp.Length, bytesLength);
                return new glTFBufferView
                {
                    buffer = 0,
                    byteLength = bytesLength,
                    byteOffset = tmp.Length,
                    byteStride = stride,
                    target = target,
                };
            }
        }

        public ArraySegment<byte> GetBytes()
        {
            return new ArraySegment<byte>(m_bytes);
        }
    }
}
