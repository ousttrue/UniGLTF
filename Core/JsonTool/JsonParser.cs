using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// reference: http://www.json.org/index.html
/// </summary>
namespace UniGLTF
{
    public class JsonParseException : FormatException
    {
        public JsonParseException(string msg) : base(msg) { }
    }
    public class JsonValueException: ArgumentException
    {
        public JsonValueException(string msg) : base(msg) { }
    }

    public enum JsonValueType
    {
        Unknown,

        String,
        Number,
        Object,
        Array,
        Boolean,

        Close, // internal use
    }

    public enum ParseMode
    {
        None,
        Recursive,
        ToEnd,
    }

    public struct JsonParser
    {
        StringSegment m_segment;
        public StringSegment Segment
        {
            get { return m_segment; }
        }

        public bool IsParsedToEnd
        {
            get;
            private set;
        }

        public void ParseToEnd()
        {
            if (JsonValueType != JsonValueType.Object && JsonValueType != JsonValueType.Array)
            {
                throw new JsonParseException("require object or arrray");
            }
            if (IsParsedToEnd)
            {
                throw new InvalidOperationException("already parsed");
            }

            var close = GetNodes(true).Last();
            if (close.JsonValueType != JsonValueType.Close)
            {
                throw new JsonParseException("close expected");
            }
            m_segment = m_segment.Take(close.Start + 1 - m_segment.Offset);
            IsParsedToEnd = true;
        }

        public int Start
        {
            get { return m_segment.Offset; }
        }

        public int End
        {
            get {
                if (!IsParsedToEnd) throw new InvalidOperationException("is not parsed to end");
                return m_segment.Offset + m_segment.Count;
            }
        }

        public JsonValueType JsonValueType
        {
            get;
            private set;
        }

        /*
        public ParserValueType ValueType
        {
            get
            {
                switch(JsonValueType)
                {
                    case JsonValueType.Array: return ParserValueType.List;
                    case JsonValueType.Object: return ParserValueType.Map;
                    case JsonValueType.Number: return ParserValueType.Double;
                    case JsonValueType.String: return ParserValueType.String;
                    case JsonValueType.Boolean: return ParserValueType.Boolean;
                }
                return ParserValueType.Unknown;
            }
        }
        */

        static StringSegment SearchTokenEnd(StringSegment segment)
        {
            // search token end
            int i = 1;
            if (segment[0] == '"')
            {
                // string
                for (; i < segment.Count; ++i)
                {
                    if (segment[i] == '\"')
                    {
                        return segment.Take(i+1);
                    }
                    else if(segment[i] == '\\')
                    {
                        switch(segment[i+1])
                        {
                            case '"': // fall through
                            case '\\': // fall through
                            case '/': // fall through
                            case 'b': // fall through
                            case 'f': // fall through
                            case 'n': // fall through
                            case 'r': // fall through
                            case 't': // fall through
                                // skip next
                                i+=1;
                                break;

                            case 'u': // unicode
                                // skip next 4
                                i += 4;
                                break;

                            default:
                                // unkonw escape
                                throw new JsonParseException("unknown escape: "+segment.Skip(i));
                        }                         
                    }
                }
                throw new JsonParseException("no close string: " + segment.Skip(i));
            }
            else
            {
                // exclude string
                for (; i < segment.Count; ++i)
                {
                    if (Char.IsWhiteSpace(segment[i])
                        || segment[i] == '}' 
                        || segment[i] == ']'
                        || segment[i] == ','
                        || segment[i] == ':'
                        )
                    {
                        break;
                    }
                }
                return segment.Take(i);
            }
        }

        public void SetBytes(ArraySegment<Byte> bytes)
        {
            var json = Encoding.UTF8.GetString(bytes.Array, bytes.Offset, bytes.Count);
            Initialize(new StringSegment(json), ParseMode.ToEnd);
        }

        void Initialize(StringSegment segment, ParseMode mode)
        {
            switch (segment[0])
            {
                case '{': JsonValueType = JsonValueType.Object; break;
                case '[': JsonValueType = JsonValueType.Array; break;
                case '"': JsonValueType = JsonValueType.String; break;
                case 't': JsonValueType = JsonValueType.Boolean; break;
                case 'f': JsonValueType = JsonValueType.Boolean; break;
                case 'n': JsonValueType = JsonValueType.Unknown; break;

                case '}': // fall through
                case ']': // fall through
                    JsonValueType = JsonValueType.Close; break;

                case '-': // fall through
                case '0': // fall through
                case '1': // fall through
                case '2': // fall through
                case '3': // fall through
                case '4': // fall through
                case '5': // fall through
                case '6': // fall through
                case '7': // fall through
                case '8': // fall through
                case '9': // fall through
                    JsonValueType = JsonValueType.Number; break;

                default:
                    JsonValueType = JsonValueType.Unknown;
                    throw new JsonParseException(segment.ToString() + " is not json");
            }

            switch (JsonValueType)
            {
                case JsonValueType.Array: // fall through
                case JsonValueType.Object: // fall through
                    m_segment = segment;
                    IsParsedToEnd = false;
                    // parse child objects ?
                    switch(mode)
                    {
                        case ParseMode.None:
                            break;

                        case ParseMode.Recursive:                   
                            ParseToEnd();
                            break;

                        case ParseMode.ToEnd:
                            IsParsedToEnd = true;
                            break;
                    }
                    break;

                default:
                    m_segment = SearchTokenEnd(segment);
                    IsParsedToEnd = true;
                    break;
            }
        }

        public static JsonParser Parse(ArraySegment<Byte> bytes, ParseMode mode = ParseMode.None)
        {
            var json = Encoding.UTF8.GetString(bytes.Array, bytes.Offset, bytes.Count);
            return Parse(new StringSegment(json), mode);
        }

        public static JsonParser Parse(string json, ParseMode mode=ParseMode.None)
        {
            return Parse(new StringSegment(json), mode);
        }

        public static JsonParser Parse(StringSegment json, ParseMode mode=ParseMode.None)
        {
            // search non whitespace
            int pos;
            if(!json.TrySearch(x => !Char.IsWhiteSpace(x), out pos))
            {
                throw new JsonParseException("[" + json.ToString() + "] is only whitespace");
            }
            var parser = new JsonParser();
            parser.Initialize(json.Skip(pos), mode);
            return parser;
        }

        #region PrimitiveType
        public bool IsNull
        {
            get
            {
                return m_segment.IsMatch("null");
            }
        }

        public bool GetBoolean()
        {
            if (JsonValueType != JsonValueType.Boolean) throw new JsonValueException("is not boolean: "+m_segment);
            var s = m_segment.ToString();
            switch (s)
            {
                case "true": return true;
                case "false": return false;
                default: throw new JsonParseException(s + " is not boolean");
            }
        }

        public SByte GetSByte()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return SByte.Parse(m_segment.ToString());
        }
        public Int16 GetInt16()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return Int16.Parse(m_segment.ToString());
        }
        public Int32 GetInt32()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return Int32.Parse(m_segment.ToString());
        }
        public Int64 GetInt64()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return Int64.Parse(m_segment.ToString());
        }
        public Byte GetByte()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return Byte.Parse(m_segment.ToString());
        }
        public UInt16 GetUInt16()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return UInt16.Parse(m_segment.ToString());
        }
        public UInt32 GetUInt32()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return UInt32.Parse(m_segment.ToString());
        }
        public UInt64 GetUInt64()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return UInt64.Parse(m_segment.ToString());
        }
        public float GetSingle()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return float.Parse(m_segment.ToString());
        }
        public double GetDouble()
        {
            if (JsonValueType != JsonValueType.Number) throw new JsonValueException("is not number: " + m_segment);
            return double.Parse(m_segment.ToString());
        }
        public string GetString()
        {
            if (JsonValueType != JsonValueType.String) throw new JsonValueException("is not string: "+m_segment);
            return JsonString.Unquote(m_segment.ToString());
        }
        #endregion

        #region CollectionType
        public JsonParser this[string key]
        {
            get
            {
                foreach(var kv in ObjectItems)
                {
                    if (kv.Key == key)
                    {
                        return kv.Value;
                    }
                }
                throw new KeyNotFoundException();
            }
        }
        public bool HasKey(string key)
        {
            return ObjectItems.Any(x => x.Key == key);
        }

        public JsonParser this[int index]
        {
            get
            {
                var it = ListItems.GetEnumerator();
                for(int i=0; it.MoveNext(); ++i)
                {
                    if (i == index)
                    {
                        return it.Current;
                    }
                }
                throw new ArgumentOutOfRangeException();
            }
        }

        public IEnumerable<KeyValuePair<String, JsonParser>> ObjectItems
        {
            get
            {
                if (JsonValueType != JsonValueType.Object) throw new JsonValueException("is not object");
                var it = GetNodes(false).GetEnumerator();
                while (it.MoveNext())
                {
                    var key = it.Current.GetString();

                    it.MoveNext();
                    yield return new KeyValuePair<string, JsonParser>(key, it.Current);
                }
            }
        }

        public IEnumerable<JsonParser> ListItems
        {
            get
            {
                if (JsonValueType != JsonValueType.Array) throw new JsonValueException("is not array");
                return GetNodes(false).Cast<JsonParser>();
            }
        }

        IEnumerable<JsonParser> GetNodes(bool useCloseNode)
        {
            if(JsonValueType!=JsonValueType.Array
                && JsonValueType!=JsonValueType.Object)
            {
                yield break;
            }

            var closeChar = JsonValueType == JsonValueType.Array ? ']' : '}';
            bool isFirst = true;
            var current = m_segment.Skip(1);
            while (true)
            {
                {
                    // skip white space
                    int nextToken;
                    if (!current.TrySearch(x => !Char.IsWhiteSpace(x), out nextToken))
                    {
                        throw new JsonParseException("no white space expected");
                    }
                    current = current.Skip(nextToken);
                }

                {
                    if (current[0]==closeChar)
                    {
                        // end
                        if (useCloseNode) {
                            var parser = new JsonParser();
                            parser.Initialize(current, ParseMode.Recursive);
                            yield return parser;
                        }
                        break;
                    }
                }

                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    // search ',' or closeChar
                    int keyPos;
                    if (!current.TrySearch(x => x == ',', out keyPos))
                    {
                        throw new JsonParseException("',' expected");
                    }
                    current = current.Skip(keyPos + 1);
                }

                {
                    // skip white space
                    int nextToken;
                    if (!current.TrySearch(x => !Char.IsWhiteSpace(x), out nextToken))
                    {
                        throw new JsonParseException("not whitespace expected");
                    }
                    current = current.Skip(nextToken);
                }

                // key
                var key = Parse(current, ParseMode.Recursive);
                if (JsonValueType==JsonValueType.Object && key.JsonValueType != JsonValueType.String)
                {
                    throw new JsonParseException("no string key is not allowed: " + key.Segment);
                }
                current = current.Skip(key.Segment.Count);
                yield return key;

                if (JsonValueType == JsonValueType.Object)
                {
                    // search ':'
                    int valuePos;
                    if (!current.TrySearch(x => x == ':', out valuePos))
                    {
                        throw new JsonParseException(": is not found");
                    }
                    current = current.Skip(valuePos + 1);

                    {
                        // skip white space
                        int nextToken;
                        if (!current.TrySearch(x => !Char.IsWhiteSpace(x), out nextToken))
                        {
                            throw new JsonParseException("not whitespace expected");
                        }
                        current = current.Skip(nextToken);
                    }

                    // value
                    var value = Parse(current, ParseMode.Recursive);
                    current = current.Skip(value.Segment.Count);
                    yield return value;
                }
            }
        }
        #endregion

        public ArraySegment<Byte> GetBytes()
        {
            var str = GetString();
            var decoded = Convert.FromBase64String(str);
            return new ArraySegment<byte>(decoded); 
        }

        public void Dump(JsonFormatter f)
        {
            f.Dump(Dump());
        }

        public ArraySegment<Byte> Dump()
        {
            var bytes = Encoding.UTF8.GetBytes(m_segment.Value.ToCharArray(), m_segment.Offset, m_segment.Count);
            return new ArraySegment<byte>(bytes);
        }
    }
}
