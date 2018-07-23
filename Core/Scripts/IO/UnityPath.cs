using System.IO;
using UnityEditor;
using UnityEngine;


namespace UniGLTF
{
    /// <summary>
    /// relative path from Unity project root.
    /// For AssetDatabase.
    /// </summary>
    public struct UnityPath
    {
        #region UnityPath
        public string Value
        {
            get;
            private set;
        }

        public bool IsNullOrEmpty
        {
            get { return string.IsNullOrEmpty(Value); }
        }

        public bool IsUnderAssetsFolder
        {
            get
            {
                if (string.IsNullOrEmpty(Value))
                {
                    return false;
                }
                return Value == "Assets" || Value.StartsWith("Assets/");
            }
        }

        public string FileNameWithoutExtension
        {
            get { return Path.GetFileNameWithoutExtension(Value); }
        }

        public string Extension
        {
            get { return Path.GetExtension(Value); }
        }

        public UnityPath Parent
        {
            get
            {
                if (IsNullOrEmpty)
                {
                    return default(UnityPath);
                }

                return new UnityPath(Path.GetDirectoryName(Value));
            }
        }

        static readonly char[] EscapeChars = new char[]
        {
            '\\',
            '/',
            ':',
            '*',
            '?',
            '"',
            '<',
            '>',
            '|',
        };

        static string EscapeFilePath(string path)
        {
            foreach (var x in EscapeChars)
            {
                path = path.Replace(x, '+');
            }
            return path;
        }

        public UnityPath Child(string name)
        {
            if (IsNullOrEmpty)
            {
                return new UnityPath(EscapeFilePath(name));
            }
            else
            {
                return new UnityPath(Value + "/" + EscapeFilePath(name));
            }
        }

        /// <summary>
        /// Remove extension and add suffix
        /// </summary>
        /// <param name="prefabPath"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public UnityPath GetAssetFolder(string suffix)
        {
            return new UnityPath(
                string.Format("{0}/{1}{2}",
                Parent.Value,
                FileNameWithoutExtension,
                suffix
                ));
        }

        UnityPath(string value)
        {
            Value = value.Replace("\\", "/");
        }

        public static UnityPath FromUnityPath(string unityPath)
        {
            return new UnityPath(unityPath);
        }
        #endregion

        #region FullPath
        static string s_basePath;
        static string BaseFullPath
        {
            get
            {
                if (string.IsNullOrEmpty(s_basePath))
                {
                    s_basePath = Path.GetFullPath(Application.dataPath + "/..").Replace("\\", "/");
                }
                return s_basePath;
            }
        }

        public string FullPath
        {
            get
            {
                return Path.Combine(BaseFullPath, Value).Replace("\\", "/");
            }
        }

        public bool IsFileExists
        {
            get { return File.Exists(FullPath); }
        }

        public bool IsDirectoryExists
        {
            get { return Directory.Exists(FullPath); }
        }

        static string AssetFullPath
        {
            get
            {
                return BaseFullPath + "/Assets";
            }
        }

        public static UnityPath FromFullpath(string fullPath)
        {
            var fullpath = fullPath.Replace("\\", "/");

            if (fullpath == BaseFullPath || fullpath.StartsWith(BaseFullPath + "/"))
            {
                return new UnityPath(fullPath.Substring(BaseFullPath.Length + 1));
            }
            else
            {
                return default(UnityPath);
            }
        }

        public static bool IsUnderAssetFolder(string fullPath)
        {
            return fullPath.Replace("\\", "/").StartsWith(AssetFullPath);
        }
        #endregion

#if UNITY_EDITOR
        public T GetImporter<T>() where T : AssetImporter
        {
            return AssetImporter.GetAtPath(Value) as T;
        }

        public void MarkTextureAssetAsNormalMap()
        {
            if (IsNullOrEmpty)
            {
                return;
            }

            var textureImporter = GetImporter<TextureImporter>();
            if (null == textureImporter)
            {
                return;
            }

            //Debug.LogFormat("[MarkTextureAssetAsNormalMap] {0}", assetPath);
            textureImporter.textureType = TextureImporterType.NormalMap;
            textureImporter.SaveAndReimport();
        }

        public static UnityPath FromAsset(UnityEngine.Object asset)
        {
            return new UnityPath(AssetDatabase.GetAssetPath(asset));
        }

        public void ImportAsset()
        {
            AssetDatabase.ImportAsset(Value);
        }

        public void EnsureFolder()
        {
            if (!IsDirectoryExists)
            {
                var parent = Parent;
                // ensure parent
                parent.ImportAsset();
                // create
                AssetDatabase.CreateFolder(
                    parent.Value,
                    Path.GetFileName(Value)
                    );
                ImportAsset();
            }
        }

        public UnityEngine.Object[] GetSubAssets()
        {
            return AssetDatabase.LoadAllAssetsAtPath(Value);
        }

        public void CreateAsset(UnityEngine.Object o)
        {
            AssetDatabase.CreateAsset(o, Value);
        }

        public void AddObjectToAsset(UnityEngine.Object o)
        {
            AssetDatabase.AddObjectToAsset(o, Value);
        }

        public T LoadAsset<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(Value);
        }

        public UnityPath GenerateUniqueAssetPath()
        {
            return new UnityPath(AssetDatabase.GenerateUniqueAssetPath(Value));
        }
#endif
    }
}
