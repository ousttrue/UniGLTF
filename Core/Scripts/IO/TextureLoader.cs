using System;
using System.Collections;
using UnityEditor;
using UnityEngine;


namespace UniGLTF
{
    public interface ITextureLoader : IDisposable
    {
        Texture2D Texture { get; }
        void ProcessOnAnyThread(glTF gltf, IStorage storage);
        IEnumerator ProcessOnMainThread(bool isLinear);
    }

#if UNITY_EDITOR
    public class AssetTextureLoader : ITextureLoader
    {
        public Texture2D Texture
        {
            private set;
            get;
        }

        private string m_textureName;
        UnityPath m_assetPath;

        public AssetTextureLoader(UnityPath assetPath, string textureName)
        {
            m_assetPath = assetPath;
            m_textureName = textureName;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void ProcessOnAnyThread(glTF gltf, IStorage storage)
        {
            throw new NotImplementedException();
        }

        public IEnumerator ProcessOnMainThread(bool isLinear)
        {
            //
            // texture from assets
            //
            m_assetPath.ImportAsset();
            var importer = m_assetPath.GetImporter<TextureImporter>();
            if (importer == null)
            {
                Debug.LogWarningFormat("fail to get TextureImporter: {0}", m_assetPath);
            }
            importer.sRGBTexture = !isLinear;
            importer.SaveAndReimport();

            Texture = m_assetPath.LoadAsset<Texture2D>();
            //Texture.name = m_textureName;
            if (Texture == null)
            {
                Debug.LogWarningFormat("fail to Load Texture2D: {0}", m_assetPath);
            }

            yield break;
        }
    }
#endif

    public class TextureLoader : ITextureLoader
    {
        int m_textureIndex;
        public TextureLoader(int textureIndex)
        {
            m_textureIndex = textureIndex;
        }

        public Texture2D Texture
        {
            private set;
            get;
        }

        public void Dispose()
        {
        }

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

        Byte[] m_imageBytes;
        string m_textureName;
        public void ProcessOnAnyThread(glTF gltf, IStorage storage)
        {
            var imageIndex = gltf.GetImageIndexFromTextureIndex(m_textureIndex);
            m_imageBytes = ToArray(gltf.GetImageBytes(storage, imageIndex, out m_textureName));
        }

        public IEnumerator ProcessOnMainThread(bool isLinear)
        {
            //
            // texture from image(png etc) bytes
            //
            Texture = new Texture2D(2, 2, TextureFormat.ARGB32, false, isLinear);
            Texture.name = m_textureName;
            if (m_imageBytes != null)
            {
                Texture.LoadImage(m_imageBytes);
            }
            yield break;
        }
    }
}
