using System;
using System.Runtime.InteropServices;


namespace UniGLTF
{
    public interface IBytesBuffer
    {
        ArraySegment<Byte> GetBytes();
    }

    public class ArraySegmentByteBuffer : IBytesBuffer
    {
        ArraySegment<Byte> m_bytes;

        public ArraySegmentByteBuffer(ArraySegment<Byte> bytes)
        {
            m_bytes = bytes;
        }

        public ArraySegment<byte> GetBytes()
        {
            return m_bytes;
        }
    }

    public class ArrayByteBuffer : IBytesBuffer
    {
        Byte[] m_bytes;

        public ArrayByteBuffer(Byte[] bytes = null)
        {
            m_bytes = bytes;
        }

        public glTFBufferView Add<T>(T[] array) where T : struct
        {
            using (var pin = Pin.Create(array))
            {
                var elementSize = Marshal.SizeOf(typeof(T));
                return Add(pin.Ptr, array.Length * elementSize, elementSize);
            }
        }

        public glTFBufferView Add(IntPtr p, int bytesLength, int stride)
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
                };
            }
        }

        public ArraySegment<byte> GetBytes()
        {
            return new ArraySegment<byte>(m_bytes);
        }
    }
}
