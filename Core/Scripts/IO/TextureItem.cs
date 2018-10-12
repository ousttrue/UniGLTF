using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniGLTF
{
    public class TextureItem
    {
        private int m_textureIndex;
        private string m_textureName;

        private Texture2D m_texture;
        public Texture2D Texture
        {
            get { return m_texture; }
        }

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
                        Debug.LogWarningFormat("no asset for {0}", m_texture);
                    }
#endif
                    return m_texture;
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


#if UNITY_EDITOR
        UnityPath m_assetPath;
        public void SetAssetInfo(UnityPath assetPath, string textureName)
        {
            m_assetPath = assetPath;
            m_textureName = textureName;
        }

        public bool IsAsset
        {
            get
            {
                return m_assetPath.IsUnderAssetsFolder;
            }
        }
#else
        public bool IsAsset
        {
            get
            {
                return false;
            }
        }
#endif

        public IEnumerable<Texture2D> GetTexturesForSaveAssets()
        {
            if (!IsAsset) yield return m_texture;
            if (m_converts.Any())
            {
                foreach (var texture in m_converts)
                {
                    yield return texture.Value;
                }
            }
        }

        Byte[] m_imageBytes;
        static Byte[] ToArray(ArraySegment<byte> bytes)
        {
            if (bytes.Array == null)
            {
                return new byte[] { };
            }
            else if (bytes.Offset == 0 && bytes.Count == bytes.Array.Length)
            {
                return bytes.Array;
            }
            else
            {
                Byte[] result = new byte[bytes.Count];
                Buffer.BlockCopy(bytes.Array, bytes.Offset, result, 0, result.Length);
                return result;
            }
        }

        public TextureItem(int index)
        {
            m_textureIndex = index;
        }

        public void Process(glTF gltf, IStorage storage)
        {
            ProcessOnAnyThread(gltf, storage);
            ProcessOnMainThread(gltf);
        }

        public void ProcessOnAnyThread(glTF gltf, IStorage storage)
        {
            if (!IsAsset)
            {
                GetImageBytes(gltf, storage);
            }
        }

        public void ProcessOnMainThread(glTF gltf)
        {
            var textureType = TextureIO.GetglTFTextureType(gltf, m_textureIndex);
            var colorSpace = TextureIO.GetColorSpace(textureType);
            GetOrCreateTexture(colorSpace == RenderTextureReadWrite.Linear);
            SetSampler(gltf);
        }

        public void GetImageBytes(glTF gltf, IStorage storage)
        {
            if (IsAsset) return;

            var imageIndex = gltf.GetImageIndexFromTextureIndex(m_textureIndex);
            m_imageBytes = ToArray(gltf.GetImageBytes(storage, imageIndex, out m_textureName));
        }

        public void GetOrCreateTexture(bool isLinear)
        {
#if UNITY_EDITOR
            if (IsAsset)
            {
                //
                // texture from assets
                //
                m_assetPath.ImportAsset();
                TextureImporter importer = m_assetPath.GetImporter<TextureImporter>();
                if (importer == null)
                {
                    Debug.LogWarningFormat("fail to get TextureImporter: {0}", m_assetPath);
                }
                importer.sRGBTexture = !isLinear;
                importer.SaveAndReimport();

                m_texture = m_assetPath.LoadAsset<Texture2D>();
                if (m_texture == null)
                {
                    Debug.LogWarningFormat("fail to Load Texture2D: {0}", m_assetPath);
                }
            }
            else
#endif
            {
                //
                // texture from image(png etc) bytes
                //
                m_texture = new Texture2D(2, 2, TextureFormat.ARGB32, false, isLinear);
                if (m_imageBytes != null)
                {
                    m_texture.LoadImage(m_imageBytes);
                }
            }
            m_texture.name = m_textureName;
        }

        public void SetSampler(glTF gltf)
        {
            TextureSamplerUtil.SetSampler(m_texture, gltf.GetSamplerFromTextureIndex(m_textureIndex));
        }

        public static Texture2D CopyTexture(Texture src, RenderTextureReadWrite colorSpace, Material material)
        {
            Texture2D dst = null;

            var renderTexture = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32, colorSpace);

            if (material != null)
            {
                Graphics.Blit(src, renderTexture, material);
            }
            else
            {
                Graphics.Blit(src, renderTexture);
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
