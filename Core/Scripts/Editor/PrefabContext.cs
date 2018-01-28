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

        string m_prefabPath;

        IEnumerable<UnityEngine.Object> GetSubAssets()
        {
            return AssetDatabase.LoadAllAssetsAtPath(m_prefabPath);
        }

        public PrefabContext(String path)
        {
            Path = path;

            var dir = System.IO.Path.GetDirectoryName(Path);
            var name = System.IO.Path.GetFileNameWithoutExtension(Path);
            m_prefabPath = string.Format("{0}/{1}.prefab", dir, name);

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

        GameObject m_go;
        public void SetMainGameObject(string key, GameObject go)
        {
            m_go = go;
        }

        public void AddObjectToAsset(string key, UnityEngine.Object o)
        {
            AssetDatabase.AddObjectToAsset(o, m_prefabPath);
        }

        public void Dispose()
        {
            if (m_go == null)
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
                PrefabUtility.ReplacePrefab(m_go, prefab, ReplacePrefabOptions.ConnectToPrefab);
            }
            else
            {
                //Debug.LogFormat("CreatePrefab: {0}", m_prefabPath);
                PrefabUtility.CreatePrefab(m_prefabPath, m_go, ReplacePrefabOptions.ConnectToPrefab);
            }

            GameObject.DestroyImmediate(m_go);
        }
    }
}
