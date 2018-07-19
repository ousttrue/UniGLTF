using System.Linq;
using UnityEngine;
using System.Collections.Generic;


namespace UniJSON
{
    public static class JsonParserExtensions
    {
        public static List<T> DeserializeList<T>(this JsonNode jsonList)
        {
            return jsonList.ArrayItems.Select(x => {

                return JsonUtility.FromJson<T>(x.Value.Segment.ToString());
                
            }).ToList();
        }
    }
}
