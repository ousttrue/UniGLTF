using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;

namespace UniGLTF
{
    public class TextureItem
    {
        glTF m_gltf;
        int m_textureIndex;
        glTFImage Image
        {
            get { return m_gltf.images[m_gltf.textures[m_textureIndex].source]; }
        }

        glTFTextureSampler Sampler
        {
            get {
                var samplerIndex=m_gltf.textures[m_textureIndex].sampler;
                return m_gltf.GetSampler(samplerIndex);
            }
        }

        public Texture2D Texture;
        public bool IsAsset { get; private set; }

        public IEnumerable<Texture2D> GetTexturesForSaveAssets()
        {
            if (!IsAsset) yield return Texture;
            if (m_metallicRoughnessOcclusion != null) yield return m_metallicRoughnessOcclusion;
        }

        public TextureItem(glTF gltf, int index)
        {
            m_gltf = gltf;
            m_textureIndex = index;

            var image = Image;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(image.uri)
                && !image.uri.StartsWith("data:")
                && !string.IsNullOrEmpty(m_gltf.baseDir) 
                && m_gltf.baseDir.StartsWith("Assets/"))
            {
                IsAsset = true;
                m_textureName = !string.IsNullOrEmpty(image.name) ? image.name : Path.GetFileNameWithoutExtension(image.uri);
            }
#endif
        }

        Byte[] m_imageBytes;
        string m_textureName;
        public void GetImageBytes()
        {
            if (IsAsset) return;

            var image = Image;
            if (string.IsNullOrEmpty(image.uri))
            {
                //
                // use buffer view (GLB)
                //
                var byteSegment = m_gltf.GetViewBytes(image.bufferView);
                m_imageBytes = byteSegment.ToArray();
                m_textureName = !string.IsNullOrEmpty(image.name) ? image.name : string.Format("{0:00}#GLB", m_textureIndex);
            }
            else if (image.uri.StartsWith("data:"))
            {
                //
                // embeded Base64Encoded
                //
                m_imageBytes = UriByteBuffer.ReadEmbeded(image.uri);
                m_textureName = !string.IsNullOrEmpty(image.name) ? image.name : string.Format("{0:00}#Base64Embeded", m_textureIndex);
            }
            else
            {
                //
                // file from external folder
                //
                var path = Path.Combine(m_gltf.baseDir, image.uri);
                m_imageBytes = File.ReadAllBytes(path);
                m_textureName = !string.IsNullOrEmpty(image.name) ? image.name : Path.GetFileNameWithoutExtension(path);
            }
        }

        public void GetOrCreateTexture()
        {
            var image = Image;
#if UNITY_EDITOR
            if (IsAsset)
            {
                //
                // texture from assets
                //
                var path = Path.Combine(m_gltf.baseDir, image.uri);
                UnityEditor.AssetDatabase.ImportAsset(path.ToUnityRelativePath());
                Texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            else
#endif
            {
                //
                // texture from image(png etc) bytes
                //
                Texture = new Texture2D(2, 2);
                Texture.LoadImage(m_imageBytes);
            }
            Texture.name = m_textureName;
        }

        public void SetSampler()
        {
            SetSampler(Texture, Sampler);
        }

        static void SetSampler(Texture2D texture, glTFTextureSampler sampler)
        {
            if (texture == null)
            {
                return;
            }

            switch (sampler.wrapS)
            {
#if UNITY_2017_OR_NEWER
                case glWrap.CLAMP_TO_EDGE:
                    texture.wrapModeU = TextureWrapMode.Clamp;
                    break;

                case glWrap.REPEAT:
                    texture.wrapModeU = TextureWrapMode.Repeat;
                    break;

                case glWrap.MIRRORED_REPEAT:
                    texture.wrapModeU = TextureWrapMode.Mirror;
                    break;
#else
                case glWrap.CLAMP_TO_EDGE:
                case glWrap.MIRRORED_REPEAT:
                    texture.wrapMode = TextureWrapMode.Clamp;
                    break;

                case glWrap.REPEAT:
                    texture.wrapMode = TextureWrapMode.Repeat;
                    break;
#endif

                default:
                    throw new NotImplementedException();
            }

#if UNITY_2017_OR_NEWER
            switch (sampler.wrapT)
            {
                case glWrap.CLAMP_TO_EDGE:
                    texture.wrapModeV = TextureWrapMode.Clamp;
                    break;

                case glWrap.REPEAT:
                    texture.wrapModeV = TextureWrapMode.Repeat;
                    break;

                case glWrap.MIRRORED_REPEAT:
                    texture.wrapModeV = TextureWrapMode.Mirror;
                    break;

                default:
                    throw new NotImplementedException();
            }
#endif

            switch (sampler.magFilter)
            {
                case glFilter.NEAREST:
                    texture.filterMode = FilterMode.Point;
                    break;

                case glFilter.LINEAR:
                    texture.filterMode = FilterMode.Bilinear;
                    break;

                default:
                    throw new NotImplementedException();
            }
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
