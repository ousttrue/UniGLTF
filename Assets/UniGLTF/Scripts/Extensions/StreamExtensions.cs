using System.IO;


namespace UniGLTF
{
    public static class StreamExtensions
    {
        public static int Write(this Stream s, GlbChunk chunk)
        {
            return chunk.WriteTo(s);
        }
    }
}
