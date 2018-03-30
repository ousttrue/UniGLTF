using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniGLTF
{
    public delegate Material CreateMaterialFunc(ImporterContext ctx, int i);

    public class ImporterContext
    {
        #region Source
        String m_path;

        /// <summary>
        /// GLTF or GLB path
        /// </summary>
        public String Path
        {
            get { return m_path; }
            set
            {
                if (m_path == value) return;
                m_path = value;
            }
        }

        /// <summary>
        /// JSON source
        /// </summary>
        public String Json;
        /// <summary>
        /// GLTF parsed from JSON
        /// </summary>
        public glTF GLTF; // parsed
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        public ArraySegment<Byte> ParseGlb<T>(Byte[] bytes) where T: glTF
        {
            var chunks = glbImporter.ParseGlbChanks(bytes);

            if (chunks.Count != 2)
            {
                throw new Exception("unknown chunk count: " + chunks.Count);
            }

            if (chunks[0].ChunkType != GlbChunkType.JSON)
            {
                throw new Exception("chunk 0 is not JSON");
            }

            if (chunks[1].ChunkType != GlbChunkType.BIN)
            {
                throw new Exception("chunk 1 is not BIN");
            }

            var jsonBytes = chunks[0].Bytes;
            Json = Encoding.UTF8.GetString(jsonBytes.Array, jsonBytes.Offset, jsonBytes.Count);

            GLTF = JsonUtility.FromJson<T>(Json);
            if (GLTF.asset.version != "2.0")
            {
                throw new UniGLTFException("unknown gltf version {0}", GLTF.asset.version);
            }

            return chunks[1].Bytes;
        }

        public CreateMaterialFunc CreateMaterial;

        #region Imported
        public GameObject Root;
        public List<Transform> Nodes = new List<Transform>();
        public List<TextureItem> Textures = new List<TextureItem>();
        public List<Material> Materials = new List<Material>();
        public List<MeshWithMaterials> Meshes = new List<MeshWithMaterials>();
        public void ShowMeshes()
        {
            foreach (var x in Meshes)
            {
                if (x.Renderer != null)
                {
                    x.Renderer.enabled = true;
                }
            }
        }
        public AnimationClip Animation;
        #endregion

#if UNITY_EDITOR
        #region PrefabPath
        string m_prefabPath;
        string PrefabPath
        {
            get
            {
                if (string.IsNullOrEmpty(m_prefabPath))
                {
                    m_prefabPath = GetPrefabPath();
                }
                return m_prefabPath;
            }
        }
        protected virtual string GetPrefabPath()
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            var name = System.IO.Path.GetFileNameWithoutExtension(Path);
            var prefabPath = string.Format("{0}/{1}.prefab", dir, name);
#if false
            if (!Application.isPlaying && File.Exists(prefabPath))
            {
                // already exists
                if (IsOwn(prefabPath))
                {
                    //Debug.LogFormat("already exist. own: {0}", prefabPath);
                }
                else
                {
                    // but unknown prefab
                    var unique = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
                    //Debug.LogFormat("already exist: {0} => {1}", prefabPath, unique);
                    prefabPath = unique;
                }
            }
#endif
            return prefabPath.Replace("\\", "/");
        }
        string GetAssetFolder(string suffix)
        {
            var path = String.Format("{0}/{1}{2}",
                System.IO.Path.GetDirectoryName(PrefabPath),
                System.IO.Path.GetFileNameWithoutExtension(PrefabPath),
                suffix
                )
                ;
            return path;
        }
        #endregion

        #region Assets
        IEnumerable<UnityEngine.Object> GetSubAssets(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path);
        }

        protected virtual bool IsOwn(string path)
        {
            foreach (var x in GetSubAssets(path))
            {
                //if (x is Transform) continue;
                if (x is GameObject) continue;
                if (x is Component) continue;
                if (AssetDatabase.IsSubAsset(x))
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual IEnumerable<UnityEngine.Object> ObjectsForSubAsset()
        {
            HashSet<Texture2D> textures = new HashSet<Texture2D>();
            foreach (var x in Textures.SelectMany(y => y.GetTexturesForSaveAssets()))
            {
                if (!textures.Contains(x))
                {
                    textures.Add(x);
                }
            }
            foreach (var x in textures) { yield return x; }
            foreach (var x in Materials) { yield return x; }
            foreach (var x in Meshes) { yield return x.Mesh; }
            if (Animation != null) yield return Animation;
        }

        void EnsureFolder(string assetPath)
        {
            var fullPath = assetPath.AssetPathToFullPath();
            if (!Directory.Exists(fullPath))
            {
                AssetDatabase.CreateFolder(
                    System.IO.Path.GetDirectoryName(assetPath),
                    System.IO.Path.GetFileName(assetPath)
                    );
            }
        }

        public void SaveAsAsset()
        {
            ShowMeshes();

            var prefabPath = PrefabPath;
            if (File.Exists(prefabPath))
            {
                // clear SubAssets
                foreach (var x in GetSubAssets(prefabPath).Where(x => !(x is GameObject) && !(x is Component)))
                {
                    GameObject.DestroyImmediate(x, true);
                }
            }

            // Add SubAsset
            var materialDir = GetAssetFolder(".Materials");
            EnsureFolder(materialDir);
            var textureDir = GetAssetFolder(".Textures");
            EnsureFolder(textureDir);

            var paths = new List<string>(){
                prefabPath
            };
            foreach (var o in ObjectsForSubAsset())
            {
                if (o is Material)
                {
                    var materialPath = string.Format("{0}/{1}.asset",
                        materialDir,
                        o.name.EscapeFilePath()
                        );
                    AssetDatabase.CreateAsset(o, materialPath);
                    paths.Add(materialPath);
                }
                else if(o is Texture2D)
                {
                    var texturePath = string.Format("{0}/{1}.asset",
                        textureDir,
                        o.name.EscapeFilePath()
                        );
                    AssetDatabase.CreateAsset(o, texturePath);
                    paths.Add(texturePath);
                }
                else
                {
                    // save as subasset
                    AssetDatabase.AddObjectToAsset(o, prefabPath);
                }
            }

            // Create or upate Main Asset
            if (File.Exists(prefabPath))
            {
                Debug.LogFormat("replace prefab: {0}", prefabPath);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                PrefabUtility.ReplacePrefab(Root, prefab, ReplacePrefabOptions.ReplaceNameBased);
            }
            else
            {
                Debug.LogFormat("create prefab: {0}", prefabPath);
                PrefabUtility.CreatePrefab(prefabPath, Root);
            }
            foreach (var x in paths)
            {
                AssetDatabase.ImportAsset(x);
            }
        }
        #endregion
#endif

        public void Destroy(bool destroySubAssets)
        {
            if (Root != null) GameObject.DestroyImmediate(Root);
            if (destroySubAssets)
            {
#if UNITY_EDITOR
                foreach (var o in ObjectsForSubAsset())
                {
                    UnityEngine.Object.DestroyImmediate(o, true);
                }
#endif
            }
        }
    }
}
