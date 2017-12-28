using System;
using System.Collections.Generic;


namespace Osaru
{
    public enum ParserValueType
    {
        Unknown, // for Null

        List,
        Map,

        Boolean,
        Integer,
        Float,
        Double,
        String,
        Bytes,
    }

    public interface IParser<T>
        where T: IParser<T>
    {
        void SetBytes(ArraySegment<Byte> bytes);

        ParserValueType ValueType { get; }
        Boolean IsNull { get; }

        Boolean GetBoolean();

        String GetString();

        Byte GetByte();
        UInt16 GetUInt16();
        UInt32 GetUInt32();
        UInt64 GetUInt64();

        SByte GetSByte();
        Int16 GetInt16();
        Int32 GetInt32();
        Int64 GetInt64();

        Single GetSingle();
        Double GetDouble();

        IEnumerable<T> ListItems { get; }
        T this[int index] { get; }

        IEnumerable<KeyValuePair<String, T>> ObjectItems { get; }
        T this[string key] { get; }

        ArraySegment<Byte> GetBytes();

        void Dump(IFormatter f);
        ArraySegment<Byte> Dump();
    }
}
