using System;
using System.IO;


namespace UniGLTF
{
    public enum GlbChunkType : UInt32
    {
        JSON = 0x4E4F534A,
        BIN = 0x004E4942,
    }

    public struct GlbHeader
    {
        public static void WriteTo(Stream s)
        {
            s.WriteByte((Byte)'g');
            s.WriteByte((Byte)'l');
            s.WriteByte((Byte)'T');
            s.WriteByte((Byte)'F');
            var bytes = BitConverter.GetBytes(2.0f);
            s.Write(bytes, 0, bytes.Length);
        }
    }

    public struct GlbChunk
    {
        public GlbChunkType ChunkType;
        public ArraySegment<Byte> Bytes;

        public GlbChunk(GlbChunkType type, ArraySegment<Byte> bytes)
        {
            ChunkType = type;
            Bytes = bytes;
        }

        public int WriteTo(Stream s)
        {
            var bytes = BitConverter.GetBytes((int)ChunkType);
            s.Write(bytes, 0, bytes.Length);
            s.Write(Bytes.Array, Bytes.Offset, Bytes.Count);
            return 4 + Bytes.Count;
        }
    }
}
