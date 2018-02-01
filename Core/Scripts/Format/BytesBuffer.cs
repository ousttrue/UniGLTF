using System;
using System.IO;
using System.Runtime.InteropServices;


namespace UniGLTF
{
    public interface IBytesBuffer
    {
        string Uri { get; }
        ArraySegment<Byte> GetBytes();
        glTFBufferView Extend<T>(ArraySegment<T> array, glBufferTarget target) where T : struct;
    }

    public static class IBytesBufferExtensions
    {
        public static glTFBufferView Extend<T>(this IBytesBuffer buffer, T[] array, glBufferTarget target) where T : struct
        {
            return buffer.Extend(new ArraySegment<T>(array), target);
        }
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

        const string DataPrefix2 = "data:application/gltf-buffer;base64,";
        
        Byte[] ReadFromUri(string baseDir, string uri)
        {
            if (uri.StartsWith(DataPrefix))
            {
                // embeded
                return Convert.FromBase64String(uri.Substring(DataPrefix.Length));
            }
            else if (uri.StartsWith(DataPrefix2))
            {
                // embeded
                return Convert.FromBase64String(uri.Substring(DataPrefix2.Length));
            }
            else
            {
                // as local file path
                return File.ReadAllBytes(Path.Combine(baseDir, uri));
            }
        }

        public glTFBufferView Extend<T>(ArraySegment<T> array, glBufferTarget target) where T : struct
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

        public glTFBufferView Extend<T>(ArraySegment<T> array, glBufferTarget target) where T : struct
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

        public glTFBufferView Extend<T>(ArraySegment<T> array, glBufferTarget target) where T : struct
        {
            using (var pin = Pin.Create(array))
            {
                var elementSize = Marshal.SizeOf(typeof(T));
                var view=Extend(pin.Ptr, array.Count * elementSize, elementSize, target);
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
                // alignment
                var padding = tmp.Length % stride == 0 ? 0 : stride - tmp.Length % stride;
                m_bytes = new Byte[m_bytes.Length + padding + bytesLength];
                Buffer.BlockCopy(tmp, 0, m_bytes, 0, tmp.Length);
                if(tmp.Length + padding + bytesLength > m_bytes.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                Marshal.Copy(p, m_bytes, tmp.Length+padding, bytesLength);
                return new glTFBufferView
                {
                    buffer = 0,
                    byteLength = bytesLength,
                    byteOffset = tmp.Length+padding,
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
