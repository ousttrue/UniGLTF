using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;
using DepthFirstScheduler;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if (NET_4_6 && UNITY_2017_1_OR_NEWER)
using System.Threading.Tasks;
#endif


namespace UniGLTF
{
    /// <summary>
    /// GLTF importer
    /// </summary>
    public class ImporterContext
    {
        #region MeasureTime
        public struct KeyElapsed
        {
            public string Key;
            public TimeSpan Elapsed;
            public KeyElapsed(string key, TimeSpan elapsed)
            {
                Key = key;
                Elapsed = elapsed;
            }
        }

        public struct MeasureScope : IDisposable
        {
            Action m_onDispose;
            public MeasureScope(Action onDispose)
            {
                m_onDispose = onDispose;
            }
            public void Dispose()
            {
                m_onDispose();
            }
        }

        public List<KeyElapsed> m_speedReports = new List<KeyElapsed>();

        public IDisposable MeasureTime(string key)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            return new MeasureScope(() =>
            {
                m_speedReports.Add(new KeyElapsed(key, sw.Elapsed));
            });
        }

        public string GetSpeedLog()
        {
            var total = TimeSpan.Zero;

            var sb = new StringBuilder();
            sb.AppendLine("【SpeedLog】");
            foreach (var kv in m_speedReports)
            {
                sb.AppendLine(string.Format("{0}: {1}ms", kv.Key, (int)kv.Elapsed.TotalMilliseconds));
                total += kv.Elapsed;
            }
            sb.AppendLine(string.Format("total: {0}ms", (int)total.TotalMilliseconds));

            return sb.ToString();
        }
        #endregion

        IShaderStore m_shaderStore;
        public IShaderStore ShaderStore
        {
            get
            {
                if (m_shaderStore == null)
                {
                    m_shaderStore = new ShaderStore(this);
                }
                return m_shaderStore;
            }
        }

        IMaterialImporter m_materialImporter;
        public IMaterialImporter MaterialImporter
        {
            get
            {
                if (m_materialImporter == null)
                {
                    m_materialImporter = new MaterialImporter(ShaderStore, this);
                }
                return m_materialImporter;
            }
        }

        public ImporterContext(IShaderStore shaderStore)
        {
            m_shaderStore = shaderStore;
        }

        public ImporterContext(IMaterialImporter materialImporter)
        {
            m_materialImporter = materialImporter;
        }

        public ImporterContext()
        {
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

        public static bool IsGeneratedUniGLTFAndOlderThan(string generatorVersion, int major, int minor)
        {
            if (string.IsNullOrEmpty(generatorVersion)) return false;
            if (generatorVersion == "UniGLTF") return true;
            if (!generatorVersion.StartsWith("UniGLTF-")) return false;

            try
            {
                var index = generatorVersion.IndexOf('.');
                var generatorMajor = int.Parse(generatorVersion.Substring(8, index - 8));
                var generatorMinor = int.Parse(generatorVersion.Substring(index + 1));

                if (generatorMajor < major)
                {
                    return true;
                }
                else
                {
                    if (generatorMinor >= minor)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("{0}: {1}", generatorVersion, ex);
                return false;
            }
        }

        public bool IsGeneratedUniGLTFAndOlder(int major, int minor)
        {
            if (GLTF == null) return false;
            if (GLTF.asset == null) return false;
            return IsGeneratedUniGLTFAndOlderThan(GLTF.asset.generator, major, minor);
        }

        /// <summary>
        /// URI access
        /// </summary>
        public IStorage Storage;
        #endregion

        #region Parse
        public void Parse(string path)
        {
            Parse(path, File.ReadAllBytes(path));
        }

        /// <summary>
        /// Parse gltf json or Parse json chunk of glb
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bytes"></param>
        public virtual void Parse(string path, Byte[] bytes)
        {
            var ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".gltf":
                    ParseJson(Encoding.UTF8.GetString(bytes), new FileSystemStorage(Path.GetDirectoryName(path)));
                    break;

                case ".zip":
                    {
                        var zipArchive = Zip.ZipArchiveStorage.Parse(bytes);
                        var gltf = zipArchive.Entries.FirstOrDefault(x => x.FileName.ToLower().EndsWith(".gltf"));
                        if (gltf == null)
                        {
                            throw new Exception("no gltf in archive");
                        }
                        var jsonBytes = zipArchive.Extract(gltf);
                        var json = Encoding.UTF8.GetString(jsonBytes);
                        ParseJson(json, zipArchive);
                    }
                    break;

                case ".glb":
                    ParseGlb(bytes);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

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
        #endregion

        #region Load. Build unity objects
        /// <summary>
        /// ReadAllBytes, Parse, Create GameObject
        /// </summary>
        /// <param name="path">allbytes</param>
        public void Load(string path)
        {
            var bytes = File.ReadAllBytes(path);
            Load(path, bytes);
        }

        /// <summary>
        /// Parse, Create GameObject
        /// </summary>
        /// <param name="path">gltf or glb path</param>
        /// <param name="bytes">allbytes</param>
        public void Load(string path, byte[] bytes)
        {
            Parse(path, bytes);
            Load();
            Root.name = Path.GetFileNameWithoutExtension(path);
        }

        public void CreateTextureItems(UnityPath imageBaseDir = default(UnityPath))
        {
            if (m_textures.Any())
            {
                return;
            }

            for (int i = 0; i < GLTF.textures.Count; ++i)
            {
                var item = new TextureItem(i);

#if UNITY_EDITOR
                if (imageBaseDir.IsUnderAssetsFolder)
                {
                    var image = GLTF.GetImageFromTextureIndex(i);
                    if (!string.IsNullOrEmpty(image.uri)
                        && !image.uri.StartsWith("data:")
                        && imageBaseDir.IsUnderAssetsFolder)
                    {
                        ///
                        /// required SaveTexturesAsPng or SetTextureBaseDir
                        ///
                        var assetPath = imageBaseDir.Child(image.uri);
                        var textureName = !string.IsNullOrEmpty(image.name) ? image.name : Path.GetFileNameWithoutExtension(image.uri);
                        item.SetAssetInfo(assetPath, textureName);
                    }
                }
#endif

                AddTexture(item);
            }
        }

        /// <summary>
        /// Build unity objects from parsed gltf
        /// </summary>
        public virtual void Load()
        {
            // textures
            if (GLTF.textures != null)
            {
                CreateTextureItems();
            }

            foreach (var x in GetTextures())
            {
                x.Process(GLTF, Storage);
            }

            // materials
            if (GLTF.materials == null || !GLTF.materials.Any())
            {
                // no material
                AddMaterial(MaterialImporter.CreateMaterial(0, null));
            }
            else
            {
                for (int i = 0; i < GLTF.materials.Count; ++i)
                {
                    var index = i;
                    var material = MaterialImporter.CreateMaterial(index, GLTF.materials[i]);
                    AddMaterial(material);
                }
            }

            // meshes
            if (GLTF.meshes
                .SelectMany(x => x.primitives)
                .Any(x => x.extensions.KHR_draco_mesh_compression != null))
            {
                throw new UniGLTFNotSupportedException("draco is not supported");
            }

            var meshImporter = new MeshImporter();
            for (int i = 0; i < GLTF.meshes.Count; ++i)
            {
                var meshContext = meshImporter.ReadMesh(this, i);
                var meshWithMaterials = MeshImporter.BuildMesh(this, meshContext);

                var mesh = meshWithMaterials.Mesh;

                // mesh name
                if (string.IsNullOrEmpty(mesh.name))
                {
                    mesh.name = string.Format("UniGLTF import#{0}", i);
                }
                var originalName = mesh.name;
                for (int j = 1; Meshes.Any(x => x.Mesh.name == mesh.name); ++j)
                {
                    mesh.name = string.Format("{0}({1})", originalName, j);
                }

                Meshes.Add(meshWithMaterials);
            }

            // nodes
            Nodes.AddRange(GLTF.nodes.Select(x => NodeImporter.ImportNode(x).transform));

            var nodes = Nodes.Select((x, i) => NodeImporter.BuildHierarchy(this, i)).ToList();

            NodeImporter.FixCoordinate(this, nodes);

            // skinning
            for (int i = 0; i < nodes.Count; ++i)
            {
                NodeImporter.SetupSkinning(this, nodes, i);
            }

            // connect root
            Root = new GameObject("_root_");
            foreach (var x in GLTF.rootnodes)
            {
                var t = nodes[x].Transform;
                t.SetParent(Root.transform, false);
            }

            AnimationImporter.ImportAnimation(this);

            //Debug.LogFormat("Import {0}", Path);
        }
        #endregion

        #region Load async
        public void LoadAsync(Action<GameObject> onLoaded, Action<Exception> onError = null, bool show = true)
        {
            if (onError == null)
            {
                onError = Debug.LogError;
            }

            LoadAsync(show)
                .Subscribe(Scheduler.MainThread,
                onLoaded,
                onError
                );
        }

#if (NET_4_6 && UNITY_2017_1_OR_NEWER)
        public Task<GameObject> LoadAsyncTask(bool show = true)
        {
            return LoadAsync(show).ToTask();
        }
#endif

        protected virtual Schedulable<GameObject> LoadAsync(bool show = true)
        {
            return Schedulable.Create()
                .OnExecute(Scheduler.ThreadPool, parent =>
                {
                    // textures
                    for (int i = 0; i < GLTF.textures.Count; ++i)
                    {
                        var index = i;
                        parent.AddTask(Scheduler.MainThread,
                                () =>
                                {
                                    using (MeasureTime("texture.Process"))
                                    {
                                        var texture = new TextureItem(index);
                                        texture.Process(GLTF, Storage);
                                        return texture;
                                    }
                                })
                            .ContinueWith(Scheduler.ThreadPool, x => AddTexture(x));
                    }
                })
                .ContinueWithCoroutine(Scheduler.MainThread, () => LoadMaterials())
                .OnExecute(Scheduler.ThreadPool, parent =>
                {
                    // meshes
                    var meshImporter = new MeshImporter();
                    for (int i = 0; i < GLTF.meshes.Count; ++i)
                    {
                        var index = i;
                        parent.AddTask(Scheduler.ThreadPool,
                                () =>
                                {
                                    using (MeasureTime("ReadMesh"))
                                    {
                                        return meshImporter.ReadMesh(this, index);
                                    }
                                })
                        .ContinueWith(Scheduler.MainThread, x =>
                        {
                            using (MeasureTime("BuildMesh"))
                            {
                                return MeshImporter.BuildMesh(this, x);
                            }
                        })
                        .ContinueWith(Scheduler.ThreadPool, x => Meshes.Add(x))
                        ;
                    }
                })
                .ContinueWithCoroutine(Scheduler.MainThread, () =>
                {
                    using (MeasureTime("LoadNodes"))
                    {
                        return LoadNodes();
                    }
                })
                .ContinueWithCoroutine(Scheduler.MainThread, () =>
                {
                    using (MeasureTime("BuildHierarchy"))
                    {
                        return BuildHierarchy();
                    }
                })
                .ContinueWith(Scheduler.CurrentThread,
                    _ =>
                    {
                        Root.name = "GLTF";
                        Debug.Log(GetSpeedLog());

                        if (show)
                        {
                            ShowMeshes();
                        }

                        return Root;
                    });
        }

        protected IEnumerator LoadTextures(IStorage storage)
        {
            for (int i = 0; i < GLTF.textures.Count; ++i)
            {
                var x = new TextureItem(i);
                x.Process(GLTF, storage);
                AddTexture(x);
                yield return null;
            }
        }

        protected IEnumerator LoadMaterials()
        {
            if (GLTF.materials == null || !GLTF.materials.Any())
            {
                AddMaterial(MaterialImporter.CreateMaterial(0, null));
            }
            else
            {
                for (int i = 0; i < GLTF.materials.Count; ++i)
                {
                    AddMaterial(MaterialImporter.CreateMaterial(i, GLTF.materials[i]));
                    yield return null;
                }
            }
        }

        protected IEnumerator LoadMeshes()
        {
            var meshImporter = new MeshImporter();
            for (int i = 0; i < GLTF.meshes.Count; ++i)
            {
                var meshContext = meshImporter.ReadMesh(this, i);
                var meshWithMaterials = MeshImporter.BuildMesh(this, meshContext);
                var mesh = meshWithMaterials.Mesh;
                if (string.IsNullOrEmpty(mesh.name))
                {
                    mesh.name = string.Format("UniGLTF import#{0}", i);
                }
                Meshes.Add(meshWithMaterials);

                yield return null;
            }
        }

        protected IEnumerator LoadNodes()
        {
            foreach (var x in GLTF.nodes)
            {
                Nodes.Add(NodeImporter.ImportNode(x).transform);
            }

            yield return null;
        }

        protected IEnumerator BuildHierarchy()
        {
            var nodes = new List<NodeImporter.TransformWithSkin>();
            for (int i = 0; i < Nodes.Count; ++i)
            {
                nodes.Add(NodeImporter.BuildHierarchy(this, i));
            }

            NodeImporter.FixCoordinate(this, nodes);

            // skinning
            for (int i = 0; i < nodes.Count; ++i)
            {
                NodeImporter.SetupSkinning(this, nodes, i);
            }

            // connect root
            Root = new GameObject("_root_");
            foreach (var x in GLTF.rootnodes)
            {
                var t = nodes[x].Transform;
                t.SetParent(Root.transform, false);
            }

            yield return null;
        }
        #endregion

        #region Imported
        public GameObject Root;
        public List<Transform> Nodes = new List<Transform>();

        List<TextureItem> m_textures = new List<TextureItem>();
        public IList<TextureItem> GetTextures()
        {
            return m_textures;
        }
        public TextureItem GetTexture(int i)
        {
            if (i < 0 || i >= m_textures.Count)
            {
                return null;
            }
            return m_textures[i];
        }
        public void AddTexture(TextureItem item)
        {
            m_textures.Add(item);
        }

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
        public Material GetMaterial(int index)
        {
            if (index < 0) return null;
            if (index >= m_materials.Count) return null;
            return m_materials[index];
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

        public void EnableUpdateWhenOffscreen()
        {
            foreach (var x in Meshes)
            {
                var skinnedMeshRenderer = x.Renderer as SkinnedMeshRenderer;
                if (skinnedMeshRenderer != null)
                {
                    skinnedMeshRenderer.updateWhenOffscreen = true;
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
            foreach (var x in m_textures.SelectMany(y => y.GetTexturesForSaveAssets()))
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

        protected virtual UnityPath GetAssetPath(UnityPath prefabPath, UnityEngine.Object o)
        {
            if (o is Material)
            {
                var materialDir = prefabPath.GetAssetFolder(".Materials");
                var materialPath = materialDir.Child(o.name.EscapeFilePath() + ".asset");
                return materialPath;
            }
            else if (o is Texture2D)
            {
                var textureDir = prefabPath.GetAssetFolder(".Textures");
                var texturePath = textureDir.Child(o.name.EscapeFilePath() + ".asset");
                return texturePath;
            }
            else if (o is Mesh && !MeshAsSubAsset)
            {
                var meshDir = prefabPath.GetAssetFolder(".Meshes");
                var meshPath = meshDir.Child(o.name.EscapeFilePath() + ".asset");
                return meshPath;
            }
            else
            {
                return default(UnityPath);
            }
        }

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

            //
            // save sub assets
            //
            var paths = new List<UnityPath>(){
                prefabPath
            };
            foreach (var o in ObjectsForSubAsset())
            {
                var assetPath = GetAssetPath(prefabPath, o);
                if (!assetPath.IsNull)
                {
                    assetPath.Parent.EnsureFolder();
                    assetPath.CreateAsset(o);
                    paths.Add(assetPath);
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

        /// <summary>
        /// Extract images from glb or gltf out of Assets folder.
        /// </summary>
        /// <param name="prefabPath"></param>
        public void ExtranctImages(UnityPath prefabPath)
        {
            var prefabParentDir = prefabPath.Parent;

            // glb buffer
            var folder = prefabPath.GetAssetFolder(".Textures");

            //
            // https://answers.unity.com/questions/647615/how-to-update-import-settings-for-newly-created-as.html
            //
            int created = 0;
            //for (int i = 0; i < GLTF.textures.Count; ++i)
            for (int i = 0; i < GLTF.images.Count; ++i)
            {
                folder.EnsureFolder();

                //var x = GLTF.textures[i];
                var image = GLTF.images[i];
                var src = Storage.GetPath(image.uri);
                if (UnityPath.FromFullpath(src).IsUnderAssetsFolder)
                {
                    // asset is exists.
                }
                else
                {
                    string textureName;
                    var byteSegment = GLTF.GetImageBytes(Storage, i, out textureName);

                    // path
                    var dst = folder.Child(textureName + image.GetExt());
                    File.WriteAllBytes(dst.FullPath, byteSegment.ToArray());
                    dst.ImportAsset();

                    // make relative path from PrefabParentDir
                    image.uri = dst.Value.Substring(prefabParentDir.Value.Length + 1);
                    ++created;
                }
            }

            if (created > 0)
            {
                AssetDatabase.Refresh();
            }

            CreateTextureItems(prefabParentDir);
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
