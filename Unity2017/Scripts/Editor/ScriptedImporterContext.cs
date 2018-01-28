using UnityEditor.Experimental.AssetImporters;


namespace UniGLTF
{
    class ScriptedImporterContext : IImporterContext
    {
        public string Path
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
            if (AssetImportContext == null)
            {
                return;
            }
            AssetImportContext.AddObjectToAsset(key, o);
        }

        public void SetMainGameObject(string key, UnityEngine.GameObject go)
        {
            if (AssetImportContext == null)
            {
                return;
            }
            AssetImportContext.SetMainObject(key, go);
        }
    }
}
