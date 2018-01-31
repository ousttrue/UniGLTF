using System;
using UnityEngine;


namespace UniGLTF
{
    public interface IImporterContext : IDisposable
    {
        GameObject MainGameObject { get; }
        String Path { get; }
        void SetMainGameObject(string key, GameObject go);
        void AddObjectToAsset(string key, UnityEngine.Object o);
    }

    public class RuntimeContext : IImporterContext
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

        public RuntimeContext(String path)
        {
            Path = path;
        }

        public void AddObjectToAsset(string key, UnityEngine.Object o)
        {
        }

        public void SetMainGameObject(string key, GameObject go)
        {
            MainGameObject = go;
        }
    }
}
