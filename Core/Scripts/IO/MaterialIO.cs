using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniGLTF
{
    public static class MaterialIO
    {
        public delegate Material CreateMaterialFunc(ImporterContext ctx, int i);

        /// StandardShader vaiables
        /// 
        /// _Color
        /// _MainTex
        /// _Cutoff
        /// _Glossiness
        /// _Metallic
        /// _MetallicGlossMap
        /// _BumpScale
        /// _BumpMap
        /// _Parallax
        /// _ParallaxMap
        /// _OcclusionStrength
        /// _OcclusionMap
        /// _EmissionColor
        /// _EmissionMap
        /// _DetailMask
        /// _DetailAlbedoMap
        /// _DetailNormalMapScale
        /// _DetailNormalMap
        /// _UVSec
        /// _EmissionScaleUI
        /// _EmissionColorUI
        /// _Mode
        /// _SrcBlend
        /// _DstBlend
        /// _ZWrite
        public static CreateMaterialFunc CreateMaterialFuncFromShader(IShaderStore shaderStore)
        {
            if (shaderStore == null) return null;

            return (ctx, i) =>
            {
                var shader = shaderStore.GetShader(ctx, i);
                Debug.LogFormat("[{0}]{1}", i, shader.name);
                var material = new Material(shader);
                material.name = string.Format("material_{0:00}", i);

                if (i >= 0 && i < ctx.GLTF.materials.Count)
                {
                    var x = ctx.GLTF.materials[i];
                    if (x != null)
                    {
                        if (!string.IsNullOrEmpty(x.name))
                        {
                            material.name = ctx.GLTF.GetUniqueMaterialName(i);
                        }
                        //Debug.LogFormat("{0}: {1}", i, material.name);

                        if (x.pbrMetallicRoughness != null)
                        {
                            if (x.pbrMetallicRoughness.baseColorFactor != null)
                            {
                                var color = x.pbrMetallicRoughness.baseColorFactor;
                                material.color = new Color(color[0], color[1], color[2], color[3]);
                            }

                            if (x.pbrMetallicRoughness.baseColorTexture != null && x.pbrMetallicRoughness.baseColorTexture.index != -1)
                            {
                                var texture = ctx.Textures[x.pbrMetallicRoughness.baseColorTexture.index];
                                material.mainTexture = texture.Texture;
                            }

                            if (x.pbrMetallicRoughness.metallicRoughnessTexture != null && x.pbrMetallicRoughness.metallicRoughnessTexture.index != -1)
                            {
                                material.EnableKeyword("_METALLICGLOSSMAP");
                                var texture = ctx.Textures[x.pbrMetallicRoughness.metallicRoughnessTexture.index];
                                material.SetTexture("_MetallicGlossMap", texture.GetMetallicRoughnessOcclusionConverted());
                            }
                        }

                        if (x.normalTexture.index != -1)
                        {
                            material.EnableKeyword("_NORMALMAP");
                            var texture = ctx.Textures[x.normalTexture.index];
#if UNITY_EDITOR
                            var textureAssetPath = AssetDatabase.GetAssetPath(texture.Texture);
                            if (!string.IsNullOrEmpty(textureAssetPath))
                            {
                                TextureIO.MarkTextureAssetAsNormalMap(textureAssetPath);
                            }
#endif
                            material.SetTexture("_BumpMap", texture.Texture);
                        }

                        if (x.occlusionTexture.index != -1)
                        {
                            var texture = ctx.Textures[x.occlusionTexture.index];
                            material.SetTexture("_OcclusionMap", texture.GetMetallicRoughnessOcclusionConverted());
                        }

                        if (x.emissiveFactor != null
                            || x.emissiveTexture.index != -1)
                        {
                            material.EnableKeyword("_EMISSION");
                            material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                            if (x.emissiveFactor != null)
                            {
                                material.SetColor("_EmissionColor", new Color(x.emissiveFactor[0], x.emissiveFactor[1], x.emissiveFactor[2]));
                            }

                            if (x.emissiveTexture.index != -1)
                            {
                                var texture = ctx.Textures[x.emissiveTexture.index];
                                material.SetTexture("_EmissionMap", texture.Texture);
                            }
                        }
                    }
                }

                return material;
            };
        }

        public static glTFMaterial ExportMaterial(Material m, List<Texture> textures)
        {
            var material = new glTFMaterial
            {
                name = m.name,
                pbrMetallicRoughness = new glTFPbrMetallicRoughness(),
            };

            if (m.HasProperty("_Color"))
            {
                material.pbrMetallicRoughness.baseColorFactor = m.color.ToArray();
            }

            if (m.mainTexture != null)
            {
                material.pbrMetallicRoughness.baseColorTexture = new glTFTextureInfo
                {
                    index = textures.IndexOf(m.mainTexture),
                };
            }

            return material;
        }
    }
}
