#if UNITY_2017_3_OR_NEWER
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UniGLTF
{
    class ScriptedImporterContext : IImporterContext
    {
        public string Path
        {
            get;
            private set;
        }

        public GameObject MainGameObject
        {
            get;
            private set;
        }

        public void Dispose()
        {
        }

        public AssetImportContext AssetImportContext;

        public ScriptedImporterContext(AssetImportContext assetImportContext)
        {
            AssetImportContext = assetImportContext;
            Path = assetImportContext.assetPath;
        }

        public void AddObjectToAsset(string key, UnityEngine.Object o)
        {
            AssetImportContext.AddObjectToAsset(key, o);
        }

        public void SetMainGameObject(string key, UnityEngine.GameObject go)
        {
            MainGameObject = go;
            AssetImportContext.SetMainObject(key, go);
        }
    }
}
#endif