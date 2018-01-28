using System;


namespace UniGLTF
{
    public static partial class ArraySegmentExtensions
    {
        public static JsonParser ParseAsJson(this ArraySegment<Byte> bytes)
        {
            return JsonParser.Parse(bytes);
        }
        public static JsonParser ParseAsJson(this Byte[] src)
        {
            return JsonParser.Parse(new ArraySegment<byte>(src));
        }
        public static JsonParser ParseAsJson(this string src)
        {
            return JsonParser.Parse(src);
        }
    }
}
