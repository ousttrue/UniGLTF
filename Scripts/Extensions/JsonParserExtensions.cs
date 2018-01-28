using UniGLTF;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UniGLTF
{
    public static class JsonParserExtensions
    {
        public static List<T> DeserializeList<T>(this JsonParser jsonList)
        {
            return jsonList.ListItems.Select(x => {

                if (!x.IsParsedToEnd)
                {
                    x.ParseToEnd();
                }

                return JsonUtility.FromJson<T>(x.Segment.ToString());
                
            }).ToList();
        }

        public static bool HasKey(this JsonParser parsed, string key)
        {
            return parsed.ObjectItems.Any(x => x.Key == key);
        }
    }
}
