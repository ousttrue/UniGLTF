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
    public class ImporterContext
    {
        public string TextureBaseDir
        {
            get; private set;
        }
        public ImporterContext(string assetPath = null)
        {
            if (!string.IsNullOrEmpty(assetPath))
            {
                TextureBaseDir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            }
        }
        #region Source

        /// <summary>
        /// JSON source
        /// </summary>
        public String Json;

        /// <summary>
        /// GLTF parsed from JSON
        /// </summary>
        public glTF GLTF; // parsed

        /// <summary>
        /// URI access
        /// </summary>
        public IStorage Storage;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        public void ParseGlb(Byte[] bytes)
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
            ParseJson(Encoding.UTF8.GetString(jsonBytes.Array, jsonBytes.Offset, jsonBytes.Count),
                new SimpleStorage(chunks[1].Bytes));
        }

        public void ParseJson(string json, IStorage storage)
        {
            Json = json;
            Storage = storage;

            GLTF = JsonUtility.FromJson<glTF>(Json);
            if (GLTF.asset.version != "2.0")
            {
                throw new UniGLTFException("unknown gltf version {0}", GLTF.asset.version);
            }

            // Version Compatibility
            RestoreOlderVersionValues();

            // parepare byte buffer
            //GLTF.baseDir = System.IO.Path.GetDirectoryName(Path);
            foreach (var buffer in GLTF.buffers)
            {
                buffer.OpenStorage(storage);
            }
        }

        void RestoreOlderVersionValues()
        {
            var parsed = UniJSON.JSON.Parse(Json);
            for (int i = 0; i < GLTF.images.Count; ++i)
            {
                if (string.IsNullOrEmpty(GLTF.images[i].name))
                {
                    try
                    {
                        var extraName = parsed["images"][i]["extra"]["name"].Value;
                        if (!string.IsNullOrEmpty(extraName))
                        {
                            //Debug.LogFormat("restore texturename: {0}", extraName);
                            GLTF.images[i].name = extraName;
                        }
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                }
            }
            for (int i = 0; i < GLTF.meshes.Count; ++i)
            {
                var mesh = GLTF.meshes[i];
                try
                {
                    for (int j = 0; j < mesh.primitives.Count; ++j)
                    {
                        var primitive = mesh.primitives[j];
                        for (int k = 0; k < primitive.targets.Count; ++k)
                        {
                            var extraName = parsed["meshes"][i]["primitives"][j]["targets"][k]["extra"]["name"].Value;
                            //Debug.LogFormat("restore morphName: {0}", extraName);
                            primitive.extras.targetNames.Add(extraName);
                        }
                    }
                }
                catch (Exception)
                {
                    // do nothing
                }
            }
#if false
            for (int i = 0; i < GLTF.nodes.Count; ++i)
            {
                var node = GLTF.nodes[i];
                try
                {
                    var extra = parsed["nodes"][i]["extra"]["skinRootBone"].AsInt;
                    //Debug.LogFormat("restore extra: {0}", extra);
                    //node.extras.skinRootBone = extra;
                }
                catch (Exception)
                {
                    // do nothing
                }
            }
#endif
        }

        public CreateMaterialFunc CreateMaterial;

        public bool HasVertexColor(int materialIndex)
        {
            if (materialIndex < 0 || materialIndex >= GLTF.materials.Count)
            {
                return false;
            }

            var hasVertexColor = GLTF.meshes.SelectMany(x => x.primitives).Any(x => x.material == materialIndex && x.HasVertexColor);
            return hasVertexColor;
        }

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
        #region Assets
        public string GetAssetFolder(string prefabPath, string suffix)
        {
            var path = String.Format("{0}/{1}{2}",
                System.IO.Path.GetDirectoryName(prefabPath),
                System.IO.Path.GetFileNameWithoutExtension(prefabPath),
                suffix
                )
                ;
            return path;
        }

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
                var assetDir = System.IO.Path.GetDirectoryName(assetPath).Replace("\\", "/");
                AssetDatabase.ImportAsset(assetDir);
                AssetDatabase.CreateFolder(
                    System.IO.Path.GetDirectoryName(assetPath),
                    System.IO.Path.GetFileName(assetPath)
                    );
                AssetDatabase.ImportAsset(assetPath);
            }
        }

        public bool MeshAsSubAsset = false;

        public void SaveAsAsset(string prefabPath)
        {
            ShowMeshes();

            //var prefabPath = PrefabPath;
            if (File.Exists(prefabPath))
            {
                // clear SubAssets
                foreach (var x in GetSubAssets(prefabPath).Where(x => !(x is GameObject) && !(x is Component)))
                {
                    GameObject.DestroyImmediate(x, true);
                }
            }

            // Add SubAsset
            var materialDir = GetAssetFolder(prefabPath, ".Materials");
            EnsureFolder(materialDir);
            var textureDir = GetAssetFolder(prefabPath, ".Textures");
            EnsureFolder(textureDir);


            var meshDir = GetAssetFolder(prefabPath, ".Meshes");
            if (!MeshAsSubAsset)
            {
                EnsureFolder(meshDir);
            }

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
                else if (o is Texture2D)
                {
                    var texturePath = string.Format("{0}/{1}.asset",
                        textureDir,
                        o.name.EscapeFilePath()
                        );
                    AssetDatabase.CreateAsset(o, texturePath);
                    paths.Add(texturePath);
                }
                else if (o is Mesh && !MeshAsSubAsset)
                {
                    var meshPath = string.Format("{0}/{1}.asset",
                        meshDir,
                        o.name.EscapeFilePath()
                        );
                    AssetDatabase.CreateAsset(o, meshPath);
                    paths.Add(meshPath);
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

        public void SaveTexturesAsPng(string prefabPath)
        {
            var prefabFolder = Path.GetDirectoryName(prefabPath).Replace("\\", "/");
            AssetDatabase.ImportAsset(prefabFolder);
            TextureBaseDir = prefabFolder;

            //
            // https://answers.unity.com/questions/647615/how-to-update-import-settings-for-newly-created-as.html
            //
            for (int i = 0; i < GLTF.textures.Count; ++i)
            {
                var x = GLTF.textures[i];
                var image = GLTF.images[x.source];
                if (string.IsNullOrEmpty(image.uri))
                {
                    // glb buffer
                    var folder = GetAssetFolder(prefabPath, ".Textures");
                    if (!Directory.Exists(folder.AssetPathToFullPath()))
                    {
                        AssetDatabase.CreateFolder(Path.GetDirectoryName(folder), Path.GetFileName(folder));
                    }

                    // name & bytes
                    var textureName = !string.IsNullOrEmpty(image.name) ? image.name : string.Format("{0:00}#GLB", i);
                    var byteSegment = GLTF.GetViewBytes(image.bufferView);

                    // path
                    var png = Path.Combine(folder, textureName + ".png").Replace("\\", "/");
                    File.WriteAllBytes(png.AssetPathToFullPath(), byteSegment.ToArray());

                    AssetDatabase.ImportAsset(png);
                    image.uri = png.Substring(prefabFolder.Length + 1);
                    Debug.LogFormat("image.uri: {0}", image.uri);
                }
            }
            UnityEditor.AssetDatabase.Refresh();
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
