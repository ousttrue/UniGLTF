using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniGLTF
{
    public class TextureItem
    {
        private int m_textureIndex;
        public Texture2D Texture
        {
            get {
                return m_textureLoader.Texture;
            }
        }

        #region Texture converter
        private Dictionary<string, Texture2D> m_converts = new Dictionary<string, Texture2D>();
        public Dictionary<string, Texture2D> Converts
        {
            get { return m_converts; }
        }

        public Texture2D ConvertTexture(string prop)
        {
            var convertedTexture = Converts.FirstOrDefault(x => x.Key == prop);
            if (convertedTexture.Value != null)
                return convertedTexture.Value;

            if (prop == "_BumpMap")
            {
                if (Application.isPlaying)
                {
                    var converted = new NormalConverter().GetImportTexture(Texture);
                    m_converts.Add(prop, converted);
                    return converted;
                }
                else
                {
#if UNITY_EDITOR
                    var textureAssetPath = AssetDatabase.GetAssetPath(Texture);
                    if (!string.IsNullOrEmpty(textureAssetPath))
                    {
                        TextureIO.MarkTextureAssetAsNormalMap(textureAssetPath);
                    }
                    else
                    {
                        Debug.LogWarningFormat("no asset for {0}", Texture);
                    }
#endif
                    return Texture;
                }
            }

            if (prop == "_MetallicGlossMap")
            {
                var converted = new MetallicRoughnessConverter().GetImportTexture(Texture);
                m_converts.Add(prop, converted);
                return converted;
            }

            if (prop == "_OcclusionMap")
            {
                var converted = new OcclusionConverter().GetImportTexture(Texture);
                m_converts.Add(prop, converted);
                return converted;
            }

            return null;
        }
        #endregion

        public bool IsAsset
        {
            private set;
            get;
        }

        public IEnumerable<Texture2D> GetTexturesForSaveAssets()
        {
            if (!IsAsset)
            {
                yield return Texture;
            }
            if (m_converts.Any())
            {
                foreach (var texture in m_converts)
                {
                    yield return texture.Value;
                }
            }
        }

        /// <summary>
        /// Texture from buffer
        /// </summary>
        /// <param name="index"></param>
        public TextureItem(int index)
        {
            m_textureIndex = index;
#if UNIGLTF_USE_WEBREQUEST_TEXTURELOADER
            m_textureLoader = new UnityWebRequestTextureLoader(m_textureIndex);
#else
            m_textureLoader = new TextureLoader(m_textureIndex);
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Texture from asset
        /// </summary>
        /// <param name="index"></param>
        /// <param name="assetPath"></param>
        /// <param name="textureName"></param>
        public TextureItem(int index, UnityPath assetPath, string textureName)
        {
            m_textureIndex = index;
            IsAsset = true;
            m_textureLoader = new AssetTextureLoader(assetPath, textureName);
        }
#endif

#region Process
        ITextureLoader m_textureLoader;


        public void Process(glTF gltf, IStorage storage)
        {
            ProcessOnAnyThread(gltf, storage);
            var it = ProcessOnMainThreadCoroutine(gltf);
            while (it.MoveNext())
            {

            }
        }

        public IEnumerator ProcessCoroutine(glTF gltf, IStorage storage)
        {
            ProcessOnAnyThread(gltf, storage);
            yield return ProcessOnMainThreadCoroutine(gltf);
        }

        public void ProcessOnAnyThread(glTF gltf, IStorage storage)
        {
            m_textureLoader.ProcessOnAnyThread(gltf, storage);
        }

        public IEnumerator ProcessOnMainThreadCoroutine(glTF gltf)
        {
            using (m_textureLoader)
            {
                var textureType = TextureIO.GetglTFTextureType(gltf, m_textureIndex);
                var colorSpace = TextureIO.GetColorSpace(textureType);
                var isLinear = colorSpace == RenderTextureReadWrite.Linear;
                yield return m_textureLoader.ProcessOnMainThread(isLinear);
                TextureSamplerUtil.SetSampler(Texture, gltf.GetSamplerFromTextureIndex(m_textureIndex));
            }
        }
#endregion

        public static Texture2D CopyTexture(Texture src, RenderTextureReadWrite colorSpace, Material material)
        {
            Texture2D dst = null;

            var renderTexture = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32, colorSpace);

            bool sRGBWrite = GL.sRGBWrite;
            switch (colorSpace)
            {
                case RenderTextureReadWrite.sRGB:
                    GL.sRGBWrite = true;
                    break;
                case RenderTextureReadWrite.Linear:
                    GL.sRGBWrite = false;
                    break;
                default:
                    GL.sRGBWrite = true;
                    break;
            }

            if (material != null)
            {
                Graphics.Blit(src, renderTexture, material);
            }
            else
            {
                Graphics.Blit(src, renderTexture);
            }

            if (colorSpace == RenderTextureReadWrite.sRGB)
            {
                GL.sRGBWrite = sRGBWrite;
            }

            dst = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false, colorSpace == RenderTextureReadWrite.Linear);
            dst.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            dst.name = src.name;
            dst.Apply();

            RenderTexture.active = null;
            if (Application.isEditor)
            {
                GameObject.DestroyImmediate(renderTexture);
            }
            else
            {
                GameObject.Destroy(renderTexture);
            }
            return dst;
        }
    }
}
