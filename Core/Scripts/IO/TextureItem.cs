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
        private Texture2D m_converted;

        public Texture2D Texture
        {
            get { return m_texture; }
        }

        public Texture2D Converted
        {
            get { return m_converted; }
            set { m_converted = value; }
        }

        UnityPath m_assetPath;
        public bool IsAsset
        {
            get
            {
                return m_assetPath.IsUnderAssetsFolder;
            }
        }

        public IEnumerable<Texture2D> GetTexturesForSaveAssets()
        {
            if (!IsAsset) yield return m_texture;
            if (m_converted != null) yield return m_converted;
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
                return bytes.Array.Skip(bytes.Offset).Take(bytes.Count).ToArray();
            }
        }

        public TextureItem(glTF gltf, int index, UnityPath textureBase = default(UnityPath))
        {
            m_textureIndex = index;

            var image = gltf.GetImageFromTextureIndex(m_textureIndex);
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(image.uri)
                && !image.uri.StartsWith("data:")
                && textureBase.IsUnderAssetsFolder)
            {
                m_assetPath = textureBase.Child(image.uri);
                m_textureName = !string.IsNullOrEmpty(image.name) ? image.name : Path.GetFileNameWithoutExtension(image.uri);
            }
#endif
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

            var image = gltf.GetImageFromTextureIndex(m_textureIndex);
            if (string.IsNullOrEmpty(image.uri))
            {
                //
                // use buffer view (GLB)
                //
                var byteSegment = gltf.GetViewBytes(image.bufferView);
                m_imageBytes = ToArray(byteSegment);
                m_textureName = !string.IsNullOrEmpty(image.name) ? image.name : string.Format("{0:00}#GLB", m_textureIndex);
            }
            else
            {
                m_imageBytes = ToArray(storage.Get(image.uri));
                if (image.uri.StartsWith("data:"))
                {
                    m_textureName = !string.IsNullOrEmpty(image.name) ? image.name : string.Format("{0:00}#Base64Embeded", m_textureIndex);
                }
                else
                {
                    m_textureName = !string.IsNullOrEmpty(image.name) ? image.name : Path.GetFileNameWithoutExtension(image.uri);
                }
            }
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
                importer.sRGBTexture = !isLinear;
                importer.SaveAndReimport();
                m_texture = m_assetPath.LoadAsset<Texture2D>();
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
