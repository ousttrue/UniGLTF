#if UNITY_2017_1_OR_NEWER
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;


public static class AssetImportContextExtensions
{

#if UNITY_2017_3_OR_NEWER
    public static void SetMainObject(this AssetImportContext ctx, string key, Object o)
    {
        ctx.AddObjectToAsset(key, o);
        ctx.SetMainObject(o);
    }
#else
    public static void AddObjectToAsset(this AssetImportContext ctx, string key, Object o)
    {
        ctx.AddSubAsset(key, o);
    }

    public static void SetMainObject(this AssetImportContext ctx, string key, Object o)
    {
        ctx.SetMainAsset(key, o);
    }
#endif
}
#endif
