using UniGLTF;
using System.Linq;
using UnityEngine;


namespace UniGLTF
{
    public static class JsonParserExtensions
    {
        public static T[] DeserializeList<T>(this JsonParser jsonList)
        {
            return jsonList.ListItems.Select(x => {

                if (!x.IsParsedToEnd)
                {
                    x.ParseToEnd();
                }

                return JsonUtility.FromJson<T>(x.Segment.ToString());
                
            }).ToArray();
        }

        public static bool HasKey(this JsonParser parsed, string key)
        {
            return parsed.ObjectItems.Any(x => x.Key == key);
        }
    }
}
