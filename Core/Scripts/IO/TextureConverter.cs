using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniGLTF
{
    interface ITextureConverter
    {
        Texture2D GetImportTexture(Texture2D texture);
        Texture2D GetExportTexture(Texture2D texture);
    }

    public static class TextureConverter
    {
        public delegate Color32 ColorConversion(Color32 color);

        public static Texture2D Convert(Texture2D texture, glTFTextureTypes textureType, ColorConversion colorConversion, Material convertMaterial, string extension)
        {
            var copyTexture = TextureItem.CopyTexture(texture, TextureIO.GetColorSpace(textureType), convertMaterial);
            if (colorConversion != null)
            {
                copyTexture.SetPixels32(copyTexture.GetPixels32().Select(x => colorConversion(x)).ToArray());
                copyTexture.Apply();
            }

            if(string.IsNullOrEmpty(copyTexture.name) || !texture.name.EndsWith(extension))
                copyTexture.name = texture.name + extension;
            return copyTexture;
        }
    }

    class MetallicRoughnessConverter : ITextureConverter
    {
        private const string m_extension = ".metallicRoughness";

        public Texture2D GetImportTexture(Texture2D texture)
        {
            return TextureConverter.Convert(texture, glTFTextureTypes.Metallic, Import, null, m_extension);
        }

        public Texture2D GetExportTexture(Texture2D texture)
        {
            return TextureConverter.Convert(texture, glTFTextureTypes.Metallic, Export, null, m_extension);
        }

        public Color32 Import(Color32 src)
        {
            return new Color32
            {
                r = src.b, // metallic
                g = 0,
                b = 0,
                a = (byte)(255 - src.g), // smoothness
            };
        }

        public Color32 Export(Color32 src)
        {
            return new Color32
            {
                r = 0,
                g = (byte)(255 - src.a),
                b = src.r,
                a = 1,
            };
        }
    }

    class NormalConverter : ITextureConverter
    {
        private const string m_extension = ".normal";

        private Material m_dxt5decode;
        private Material GetDecodeDxt5()
        {
            if (m_dxt5decode == null)
            {
                m_dxt5decode = new Material(Shader.Find("UniGLTF/Dxt5Decoder"));
            }
            return m_dxt5decode;
        }

        public Texture2D GetImportTexture(Texture2D texture)
        {
#if UNITY_EDITOR
            return texture;
#endif
            return TextureConverter.Convert(texture, glTFTextureTypes.Normal, Import, null, m_extension);

        }

        public Texture2D GetExportTexture(Texture2D texture)
        {
            var mat = GetDecodeDxt5();
            return TextureConverter.Convert(texture, glTFTextureTypes.Normal, null, mat, m_extension);
        }

        public Color32 Import(Color32 src)
        {
            return new Color32
            {
                r = 0,
                g = src.g,
                b = 0,
                a = src.r,
            };
        }
    }

    class OcclusionConverter : ITextureConverter
    {
        private const string m_extension = ".occlusion";

        public Texture2D GetImportTexture(Texture2D texture)
        {
            return TextureConverter.Convert(texture, glTFTextureTypes.Occlusion, Import, null, m_extension);
        }

        public Texture2D GetExportTexture(Texture2D texture)
        {
            return TextureConverter.Convert(texture, glTFTextureTypes.Occlusion, Export, null, m_extension);
        }

        public Color32 Import(Color32 src)
        {
            return new Color32
            {
                r = 0,
                g = src.r,
                b = 0,
                a = 1,
            };
        }

        public Color32 Export(Color32 src)
        {
            return new Color32
            {
                r = src.g,
                g = 0,
                b = 0,
                a = 1,
            };
        }
    }
}