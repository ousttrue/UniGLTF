using Osaru.Json;
using System.Text;


namespace Osaru
{
    public static partial class IParserExtensions
    {
        public static string ToJson<PARSER>(this PARSER parser)
            where PARSER : IParser<PARSER>
        {
            var formatter = new JsonFormatter();
            parser.Convert(formatter);
            var bytes = formatter.GetStore().Bytes;
            return Encoding.UTF8.GetString(bytes.Array, bytes.Offset, bytes.Count);
        }
    }
}
