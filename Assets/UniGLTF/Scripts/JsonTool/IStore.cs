using System;


namespace UniGLTF
{
    public interface IStore
    {
        void Clear();
        ArraySegment<Byte> Bytes { get; }

        void Write(Byte value);
        void Write(SByte value);

        // network
        void WriteBigEndian(UInt16 value);
        void WriteBigEndian(UInt32 value);
        void WriteBigEndian(UInt64 value);
        void WriteBigEndian(Int16 value);
        void WriteBigEndian(Int32 value);
        void WriteBigEndian(Int64 value);
        void WriteBigEndian(Single value);
        void WriteBigEndian(Double value);

        // intel cpu
        void WriteLittleEndian(UInt16 value);
        void WriteLittleEndian(UInt32 value);
        void WriteLittleEndian(UInt64 value);
        void WriteLittleEndian(Int16 value);
        void WriteLittleEndian(Int32 value);
        void WriteLittleEndian(Int64 value);
        void WriteLittleEndian(Single value);
        void WriteLittleEndian(Double value);

        void Write(ArraySegment<Byte> bytes);

        void Write(string src);
        void Write(char c);
    }

    public static class IStoreExtensions
    {
        public static void Write(this IStore s, Byte[] bytes)
        {
            s.Write(new ArraySegment<Byte>(bytes));
        }
    }
}
