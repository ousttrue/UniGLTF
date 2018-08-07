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
        public UnityPath TextureBaseDir
        {
            get; private set;
        }

        public ImporterContext(UnityPath gltfPath = default(UnityPath))
        {
            TextureBaseDir = gltfPath.Parent;
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

        public IMaterialImporter MaterialImporter;

        public bool MaterialHasVertexColor(glTFMaterial material)
        {
            if (material == null)
            {
                return false;
            }

            var materialIndex = GLTF.materials.IndexOf(material);
            if (materialIndex == -1)
            {
                return false;
            }

            return MaterialHasVertexColor(materialIndex);
        }

        public bool MaterialHasVertexColor(int materialIndex)
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
        List<Material> m_materials = new List<Material>();
        public void AddMaterial(Material material)
        {
            var originalName = material.name;
            int j = 2;
            while (m_materials.Any(x => x.name == material.name))
            {
                material.name = string.Format("{0}({1})", originalName, j++);
            }
            m_materials.Add(material);
        }
        public IList<Material> GetMaterials()
        {
            return m_materials;
        }

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
            foreach (var x in m_materials) { yield return x; }
            foreach (var x in Meshes) { yield return x.Mesh; }
            if (Animation != null) yield return Animation;
        }

        public bool MeshAsSubAsset = false;

        public void SaveAsAsset(UnityPath prefabPath)
        {
            ShowMeshes();

            //var prefabPath = PrefabPath;
            if (prefabPath.IsFileExists)
            {
                // clear SubAssets
                foreach (var x in prefabPath.GetSubAssets().Where(x => !(x is GameObject) && !(x is Component)))
                {
                    GameObject.DestroyImmediate(x, true);
                }
            }

            // Add SubAsset
            var materialDir = prefabPath.GetAssetFolder(".Materials");
            materialDir.EnsureFolder();
            var textureDir = prefabPath.GetAssetFolder(".Textures");
            textureDir.EnsureFolder();
            var meshDir = prefabPath.GetAssetFolder(".Meshes");
            if (!MeshAsSubAsset)
            {
                meshDir.EnsureFolder();
            }

            var paths = new List<UnityPath>(){
                prefabPath
            };
            foreach (var o in ObjectsForSubAsset())
            {
                if (o is Material)
                {
                    var materialPath = materialDir.Child(o.name.EscapeFilePath() + ".asset");
                    materialPath.CreateAsset(o);
                    paths.Add(materialPath);
                }
                else if (o is Texture2D)
                {
                    var texturePath = textureDir.Child(o.name.EscapeFilePath() + ".asset");
                    texturePath.CreateAsset(o);
                    paths.Add(texturePath);
                }
                else if (o is Mesh && !MeshAsSubAsset)
                {
                    var meshPath = meshDir.Child(o.name.EscapeFilePath() + ".asset");
                    meshPath.CreateAsset(o);
                    paths.Add(meshPath);
                }
                else
                {
                    // save as subasset
                    prefabPath.AddObjectToAsset(o);
                }
            }

            // Create or upate Main Asset
            if (prefabPath.IsFileExists)
            {
                Debug.LogFormat("replace prefab: {0}", prefabPath);
                var prefab = prefabPath.LoadAsset<GameObject>();
                PrefabUtility.ReplacePrefab(Root, prefab, ReplacePrefabOptions.ReplaceNameBased);
            }
            else
            {
                Debug.LogFormat("create prefab: {0}", prefabPath);
                PrefabUtility.CreatePrefab(prefabPath.Value, Root);
            }
            foreach (var x in paths)
            {
                x.ImportAsset();
            }
        }

        public void SaveTexturesAsPng(UnityPath prefabPath)
        {
            TextureBaseDir = prefabPath.Parent;
            TextureBaseDir.ImportAsset();

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
                    var folder = prefabPath.GetAssetFolder(".Textures");
                    folder.EnsureFolder();

                    // name & bytes
                    var textureName = !string.IsNullOrEmpty(image.name) ? image.name : string.Format("{0:00}#GLB", i);
                    var byteSegment = GLTF.GetViewBytes(image.bufferView);

                    // path
                    var png = folder.Child(textureName + ".png");
                    File.WriteAllBytes(png.FullPath, byteSegment.ToArray());
                    png.ImportAsset();

                    image.uri = png.Value.Substring(TextureBaseDir.Value.Length + 1);
                    //Debug.LogFormat("image.uri: {0}", image.uri);
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
