using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;


namespace UniGLTF
{
    public static class TextureSamplerUtil
    {
        #region WrapMode
        public enum TextureWrapType
        {
            All,
#if UNITY_2017_1_OR_NEWER
            U,
            V,
            W,
#endif
        }

        public static KeyValuePair<TextureWrapType, TextureWrapMode> TypeWithMode(TextureWrapType type, TextureWrapMode mode)
        {
            return new KeyValuePair<TextureWrapType, TextureWrapMode>(type, mode);
        }

        public static IEnumerable<KeyValuePair<TextureWrapType, TextureWrapMode>> GetUnityWrapMode(glTFTextureSampler sampler)
        {
#if UNITY_2017_1_OR_NEWER
            if (sampler.wrapS == sampler.wrapT)
            {
                switch (sampler.wrapS)
                {
                    case glWrap.NONE: // default
                        yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Repeat);
                        break;

                    case glWrap.CLAMP_TO_EDGE:
                        yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Clamp);
                        break;

                    case glWrap.REPEAT:
                        yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Repeat);
                        break;

                    case glWrap.MIRRORED_REPEAT:
                        yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Mirror);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (sampler.wrapS)
                {
                    case glWrap.NONE: // default
                        yield return TypeWithMode(TextureWrapType.U, TextureWrapMode.Repeat);
                        break;

                    case glWrap.CLAMP_TO_EDGE:
                        yield return TypeWithMode(TextureWrapType.U, TextureWrapMode.Clamp);
                        break;

                    case glWrap.REPEAT:
                        yield return TypeWithMode(TextureWrapType.U, TextureWrapMode.Repeat);
                        break;

                    case glWrap.MIRRORED_REPEAT:
                        yield return TypeWithMode(TextureWrapType.U, TextureWrapMode.Mirror);
                        break;

                    default:
                        throw new NotImplementedException();
                }
                switch (sampler.wrapT)
                {
                    case glWrap.NONE: // default
                        yield return TypeWithMode(TextureWrapType.V, TextureWrapMode.Repeat);
                        break;

                    case glWrap.CLAMP_TO_EDGE:
                        yield return TypeWithMode(TextureWrapType.V, TextureWrapMode.Clamp);
                        break;

                    case glWrap.REPEAT:
                        yield return TypeWithMode(TextureWrapType.V, TextureWrapMode.Repeat);
                        break;

                    case glWrap.MIRRORED_REPEAT:
                        yield return TypeWithMode(TextureWrapType.V, TextureWrapMode.Mirror);
                        break;

                    default:
                        throw new NotImplementedException();
                }
#else
            // Unity2017.1より前
            // * wrapSとwrapTの区別が無くてwrapしかない
            // * Mirrorが無い

            switch (sampler.wrapS)
            {
                case glWrap.NONE: // default
                    yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Repeat);
                    break;

                case glWrap.CLAMP_TO_EDGE:
                case glWrap.MIRRORED_REPEAT:
                    yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Clamp);
                    break;

                case glWrap.REPEAT:
                    yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Repeat);
                    break;

                default:
                    throw new NotImplementedException();
#endif
            }
        }
        #endregion

        public static FilterMode ImportFilterMode(glFilter filterMode)
        {
            switch (filterMode)
            {
                case glFilter.NEAREST:
                case glFilter.NEAREST_MIPMAP_LINEAR:
                case glFilter.NEAREST_MIPMAP_NEAREST:
                    return FilterMode.Point;

                case glFilter.NONE:
                case glFilter.LINEAR:
                case glFilter.LINEAR_MIPMAP_NEAREST:
                    return FilterMode.Bilinear;

                case glFilter.LINEAR_MIPMAP_LINEAR:
                    return FilterMode.Trilinear;

                default:
                    throw new NotImplementedException();

            }
        }

        public static void SetSampler(Texture2D texture, glTFTextureSampler sampler)
        {
            if (texture == null)
            {
                return;
            }

            foreach (var kv in GetUnityWrapMode(sampler))
            {
                switch (kv.Key)
                {
                    case TextureWrapType.All:
                        texture.wrapMode = kv.Value;
                        break;

#if UNITY_2017_1_OR_NEWER
                    case TextureWrapType.U:
                        texture.wrapModeU = kv.Value;
                        break;

                    case TextureWrapType.V:
                        texture.wrapModeV = kv.Value;
                        break;

                    case TextureWrapType.W:
                        texture.wrapModeW = kv.Value;
                        break;
#endif

                    default:
                        throw new NotImplementedException();
                }
            }

            texture.filterMode = ImportFilterMode(sampler.minFilter);
        }

        #region Export
        public static glFilter ExportFilter(Texture texture)
        {
            switch (texture.filterMode)
            {
                case FilterMode.Point:
                    return glFilter.NEAREST;

                case FilterMode.Bilinear:
                    return glFilter.LINEAR;

                case FilterMode.Trilinear:
                    return glFilter.LINEAR_MIPMAP_LINEAR;

                default:
                    throw new NotImplementedException();
            }
        }

        public static TextureWrapMode GetWrapS(Texture texture)
        {
#if UNITY_2017_1_OR_NEWER
            return texture.wrapModeU;
#else
            return texture.wrapMode;
#endif
        }

        public static TextureWrapMode GetWrapT(Texture texture)
        {
#if UNITY_2017_1_OR_NEWER
            return texture.wrapModeV;
#else
            return texture.wrapMode;
#endif
        }

        public static glWrap ExportWrapMode(TextureWrapMode wrapMode)
        {
            switch (wrapMode)
            {
                case TextureWrapMode.Clamp:
                    return glWrap.CLAMP_TO_EDGE;

                case TextureWrapMode.Repeat:
                    return glWrap.REPEAT;

#if UNITY_2017_1_OR_NEWER
                case TextureWrapMode.Mirror:
                case TextureWrapMode.MirrorOnce:
                    return glWrap.MIRRORED_REPEAT;
#endif

                default:
                    throw new NotImplementedException();
            }
        }

        public static glTFTextureSampler Export(Texture texture)
        {
            var filter = ExportFilter(texture);
            var wrapS = ExportWrapMode(GetWrapS(texture));
            var wrapT = ExportWrapMode(GetWrapT(texture));
            return new glTFTextureSampler
            {
                magFilter = filter,
                minFilter = filter,
                wrapS = wrapS,
                wrapT = wrapT,
            };
        }
        #endregion
    }

    public class TextureItem
    {
        int m_textureIndex;

        public Texture2D Texture;
        UnityPath m_assetPath;
        public bool IsAsset
        {
            get
            {
                return m_assetPath.IsUnderAssetsFolder;
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
                return bytes.Array.Skip(bytes.Offset).Take(bytes.Count).ToArray();
            }
        }

        string m_textureName;

        public IEnumerable<Texture2D> GetTexturesForSaveAssets()
        {
            if (!IsAsset) yield return Texture;
            if (m_metallicRoughnessOcclusion != null) yield return m_metallicRoughnessOcclusion;
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
            GetOrCreateTexture();
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

        public void GetOrCreateTexture()
        {
#if UNITY_EDITOR
            if (IsAsset)
            {
                //
                // texture from assets
                //
                m_assetPath.ImportAsset();
                Texture = m_assetPath.LoadAsset<Texture2D>();
            }
            else
#endif
            {
                //
                // texture from image(png etc) bytes
                //
                Texture = new Texture2D(2, 2);
                if (m_imageBytes != null)
                {
                    Texture.LoadImage(m_imageBytes);
                }
            }
            Texture.name = m_textureName;
        }

        public void SetSampler(glTF gltf)
        {
            TextureSamplerUtil.SetSampler(Texture, gltf.GetSamplerFromTextureIndex(m_textureIndex));
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
            bool m_sRGBWrite;
            public sRGBScope(bool sRGBWrite)
            {
                m_sRGBWrite = GL.sRGBWrite;
                GL.sRGBWrite = sRGBWrite;
            }

            public void Dispose()
            {
                GL.sRGBWrite = m_sRGBWrite;
            }
        }

        static Texture2D DTXnm2RGBA(Texture2D tex)
        {
            Color[] colors = tex.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                Color c = colors[i];
                c.r = c.a * 2 - 1;  //red<-alpha (x<-w)
                c.g = c.g * 2 - 1; //green is always the same (y)
                Vector2 xy = new Vector2(c.r, c.g); //this is the xy vector
                c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(xy, xy))); //recalculate the blue channel (z)
                colors[i] = new Color(c.r * 0.5f + 0.5f, c.g * 0.5f + 0.5f, c.b * 0.5f + 0.5f); //back to 0-1 range
            }
            tex.SetPixels(colors); //apply pixels to the texture
            tex.Apply();
            return tex;
        }

        static bool IsDxt5(Texture src)
        {
            var srcAsTexture2D = src as Texture2D;
            if (srcAsTexture2D != null)
            {
                Debug.LogFormat("{0} format {1}", srcAsTexture2D.name, srcAsTexture2D.format);
                return srcAsTexture2D.format == TextureFormat.DXT5;
            }
            return false;
        }

        static Material s_dxt5decode;
        static Material GetDecodeDxt5()
        {
            if (s_dxt5decode == null)
            {
                s_dxt5decode = new Material(Shader.Find("UniGLTF/Dxt5Decoder"));
            }
            return s_dxt5decode;
        }

        public static Texture2D CopyTexture(Texture src)
        {
            Texture2D dst = null;

            var renderTexture = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            using (var scope = new sRGBScope(true))
            {
                if (IsDxt5(src))
                {
                    var mat = GetDecodeDxt5();
                    Graphics.Blit(src, renderTexture, mat);
                }
                else
                {
                    Graphics.Blit(src, renderTexture);
                }
            }
            dst = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false, false);
            dst.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);

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
