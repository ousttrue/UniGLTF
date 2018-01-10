using Osaru;
using Osaru.Json;
using System.Linq;
using UnityEngine;


namespace UniGLTF
{
    public static class JsonParserExtensions
    {
        public static T[] DeserializeList<T>(this JsonParser jsonList)
        {
            return jsonList.ListItems.Select(x => JsonUtility.FromJson<T>(x.ToJson())).ToArray();
        }

        public static bool HasKey(this JsonParser parsed, string key)
        {
            return parsed.ObjectItems.Any(x => x.Key == key);
        }
    }
}
