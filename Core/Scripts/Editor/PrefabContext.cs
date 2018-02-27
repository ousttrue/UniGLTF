using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace UniGLTF
{
    class PrefabContext : IImporterContext
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

        string m_prefabPath;

        IEnumerable<UnityEngine.Object> GetSubAssets()
        {
            return AssetDatabase.LoadAllAssetsAtPath(m_prefabPath);
        }

        public PrefabContext(String path, bool overwrite = true)
        {
            Path = path;

            var dir = System.IO.Path.GetDirectoryName(Path);
            var name = System.IO.Path.GetFileNameWithoutExtension(Path);

            m_prefabPath = string.Format("{0}/{1}.prefab", dir, name);
            if (!overwrite)
            {
                m_prefabPath = AssetDatabase.GenerateUniqueAssetPath(m_prefabPath);
            }

            if (File.Exists(m_prefabPath))
            {
                //Debug.LogFormat("Exist: {0}", m_prefabPath);

                // clear subassets
                foreach (var x in GetSubAssets())
                {
                    if (x is Transform
                        || x is GameObject)
                    {
                        continue;
                    }
                    GameObject.DestroyImmediate(x, true);
                }
            }
        }

        public void SetMainGameObject(string key, GameObject go)
        {
            MainGameObject = go;
        }

        public void AddObjectToAsset(string key, UnityEngine.Object o)
        {
            AssetDatabase.AddObjectToAsset(o, m_prefabPath);
        }

        public void Dispose()
        {
            if (MainGameObject == null)
            {
                return;
            }

            ///
            /// create prefab, after subasset AssetDatabase.AddObjectToAsset
            ///
            if (File.Exists(m_prefabPath))
            {
                //Debug.LogFormat("ReplacePrefab: {0}", m_prefabPath);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_prefabPath);
                PrefabUtility.ReplacePrefab(MainGameObject, prefab, ReplacePrefabOptions.ReplaceNameBased);
            }
            else
            {
                //Debug.LogFormat("CreatePrefab: {0}", m_prefabPath);
                PrefabUtility.CreatePrefab(m_prefabPath, MainGameObject, ReplacePrefabOptions.ConnectToPrefab);
            }

            GameObject.DestroyImmediate(MainGameObject);
        }
    }
}
