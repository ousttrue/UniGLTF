using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

namespace UniGLTF
{
    public class TextureItem
    {
        public int TextureIndex = -1;
        public Texture2D Texture;
        public bool IsAsset;

        public IEnumerable<Texture2D> GetTexturesForSaveAssets()
        {
            if (!IsAsset) yield return Texture;
            if (m_metallicRoughnessOcclusion != null) yield return m_metallicRoughnessOcclusion;
        }

        public TextureItem(Texture2D texture, int index = -1, bool isAsset = false)
        {
            Texture = texture;
            TextureIndex = index;
            IsAsset = isAsset;
        }

        Texture2D m_metallicRoughnessOcclusion;
        public Texture2D GetMetallicRoughnessOcclusionConverted()
        {
            if (m_metallicRoughnessOcclusion == null)
            {
                var texture = CopyTexture(Texture);
                texture.SetPixels32(texture.GetPixels32().Select(ConvertMetallicRoughnessOcclusion).ToArray());
                texture.name = this.Texture.name + ".metallicRoughnessOcclusion";
                m_metallicRoughnessOcclusion = texture;
            }
            return m_metallicRoughnessOcclusion;
        }

        static Color32 ConvertMetallicRoughness(Color32 src)
        {
            return new Color32
            {
                r = src.b,
                g = src.b,
                b = src.b,
                a = (byte)(255 - src.g),
            };
        }

        static Color32 ConvertMetallicRoughnessOcclusion(Color32 src)
        {
            return new Color32
            {
                r = src.b, // metallic
                g = src.r, // occlusion
                b = 0,
                a = (byte)(255 - src.g), // smoothness
            };
        }

        static Color32 ConvertOcclusion(Color32 src)
        {
            return new Color32
            {
                r = src.r,
                g = src.r,
                b = src.r,
                a = 255,
            };
        }


        class sRGBScope : IDisposable
        {
            bool sRGBWrite;
            public sRGBScope()
            {
                sRGBWrite = GL.sRGBWrite;
                GL.sRGBWrite = true;
            }

            public void Dispose()
            {
                GL.sRGBWrite = sRGBWrite;
            }
        }


        public static Texture2D CopyTexture(Texture2D src)
        {
            Texture2D dst = null;
            var renderTexture = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            using (var scope = new sRGBScope())
            {
                Graphics.Blit(src, renderTexture);
                dst = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false, false);
                dst.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            }
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
