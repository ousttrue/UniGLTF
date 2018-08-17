using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniGLTF
{
    public static class TextureIO
    {
#if UNITY_EDITOR
        public static void MarkTextureAssetAsNormalMap(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (null == textureImporter)
            {
                return;
            }

            //Debug.LogFormat("[MarkTextureAssetAsNormalMap] {0}", assetPath);
            textureImporter.textureType = TextureImporterType.NormalMap;
            textureImporter.SaveAndReimport();
        }
#endif

        public struct TextureExportItem
        {
            public Texture Texture;
            public bool IsNormalMap;

            public TextureExportItem(Texture texture, bool isNormalMap)
            {
                Texture = texture;
                IsNormalMap = isNormalMap;
            }

            public TextureExportItem(Texture texture) : this(texture, false)
            {
            }
        }

        public static IEnumerable<TextureExportItem> GetTextures(Material m)
        {
            var props = ShaderPropExporter.PreShaderPropExporter.GetPropsForSupportedShader(m.shader.name);
            if (props == null)
            {
                yield return new TextureExportItem(m.mainTexture);
            }

            foreach (var prop in props.Properties)
            {
                if (prop.ShaderPropertyType == ShaderPropExporter.ShaderPropertyType.TexEnv)
                {
                    yield return new TextureExportItem(m.GetTexture(prop.Key), prop.IsNormalMap);
                }
            }
        }

        struct BytesWithPath
        {
            public Byte[] Bytes;
            //public string Path;
            public string Mime;

            public BytesWithPath(Texture texture)
            {
                //Path = "";
                Bytes = TextureItem.CopyTexture(texture).EncodeToPNG();
                Mime = "image/png";
            }
        }

        public static int ExportTexture(glTF gltf, int bufferIndex, Texture texture, bool isNormalMap)
        {
            var bytesWithPath = new BytesWithPath(texture); ;

            // add view
            var view = gltf.buffers[bufferIndex].Append(bytesWithPath.Bytes, glBufferTarget.NONE);
            var viewIndex = gltf.AddBufferView(view);

            // add image
            var imageIndex = gltf.images.Count;
            gltf.images.Add(new glTFImage
            {
                name = texture.name,
                bufferView = viewIndex,
                mimeType = bytesWithPath.Mime,
            });

            // add sampler
            var samplerIndex = gltf.samplers.Count;
            var sampler = TextureSamplerUtil.Export(texture);
            gltf.samplers.Add(sampler);

            // add texture
            gltf.textures.Add(new glTFTexture
            {
                sampler = samplerIndex,
                source = imageIndex,
            });

            return imageIndex;
        }
    }
}
