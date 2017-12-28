using System;
using System.Linq;


namespace Osaru
{
    public static partial class IParserExtensions
    {
        public static void Convert<PARSER>(this PARSER parser, IFormatter f)
            where PARSER: IParser<PARSER>
        {
            if (parser.IsNull)
            {
                f.Null();
                return;
            }

            switch(parser.ValueType)
            {
                case ParserValueType.Map:
                    {
                        f.BeginMap(parser.ObjectItems.Count());
                        foreach(var kv in parser.ObjectItems)
                        {
                            f.Key(kv.Key);
                            kv.Value.Convert(f);
                        }
                        f.EndMap();
                    }
                    break;

                case ParserValueType.List:
                    {
                        f.BeginList(parser.ListItems.Count());
                        foreach(var i in parser.ListItems)
                        {
                            i.Convert(f);
                        }
                        f.EndList();
                    }
                    break;

                case ParserValueType.Boolean:
                    f.Value(parser.GetBoolean());
                    break;

                case ParserValueType.Integer:
                    f.Value(parser.GetInt64());
                    break;

                case ParserValueType.Float:
                    f.Value(parser.GetSingle());
                    break;

                case ParserValueType.Double:
                    f.Value(parser.GetDouble());
                    break;

                case ParserValueType.String:
                    f.Value(parser.GetString());
                    break;
                case ParserValueType.Bytes:
                    f.Bytes(parser.GetBytes());
                    break;

                default:
                    throw new Exception("unknown type");
            }
        }
    }
}
