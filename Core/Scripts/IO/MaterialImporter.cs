using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniGLTF
{
    public interface IMaterialImporter
    {
        Material CreateMaterial(int i, glTFMaterial src, Func<int, TextureItem> getTexture);
    }

    public class MaterialImporter : IMaterialImporter
    {
        IShaderStore m_shaderStore;

        public MaterialImporter(IShaderStore shaderStore)
        {
            m_shaderStore = shaderStore;
        }

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
        public virtual Material CreateMaterial(int i, glTFMaterial x, Func<int, TextureItem> getTexture)
        {
            var shader = m_shaderStore.GetShader(x);
            Debug.LogFormat("[{0}]{1}", i, shader.name);
            var material = new Material(shader);
            material.name = string.IsNullOrEmpty(x.name)
                ? string.Format("material_{0:00}", i)
                : x.name
                ;

            if (x != null)
            {
                if (x.pbrMetallicRoughness != null)
                {
                    if (x.pbrMetallicRoughness.baseColorFactor != null)
                    {
                        var color = x.pbrMetallicRoughness.baseColorFactor;
                        material.color = new Color(color[0], color[1], color[2], color[3]);
                    }

                    if (x.pbrMetallicRoughness.baseColorTexture != null && x.pbrMetallicRoughness.baseColorTexture.index != -1)
                    {
                        var texture = getTexture(x.pbrMetallicRoughness.baseColorTexture.index);
                        if (texture != null)
                        {
                            material.mainTexture = texture.Texture;
                        }
                    }

                    if (x.pbrMetallicRoughness.metallicRoughnessTexture != null && x.pbrMetallicRoughness.metallicRoughnessTexture.index != -1)
                    {
                        material.EnableKeyword("_METALLICGLOSSMAP");
                        var texture = getTexture(x.pbrMetallicRoughness.metallicRoughnessTexture.index);
                        if (texture != null)
                        {
                            material.SetTexture("_MetallicGlossMap", texture.GetMetallicRoughnessOcclusionConverted());
                        }
                    }
                }

                if (x.normalTexture!=null && x.normalTexture.index != -1)
                {
                    material.EnableKeyword("_NORMALMAP");
                    var texture = getTexture(x.normalTexture.index);
                    if (texture != null)
                    {
#if UNITY_EDITOR
                        var textureAssetPath = AssetDatabase.GetAssetPath(texture.Texture);
                        if (!string.IsNullOrEmpty(textureAssetPath))
                        {
                            TextureIO.MarkTextureAssetAsNormalMap(textureAssetPath);
                        }
#endif
                        material.SetTexture("_BumpMap", texture.Texture);
                    }
                }

                if (x.occlusionTexture!=null && x.occlusionTexture.index != -1)
                {
                    var texture = getTexture(x.occlusionTexture.index);
                    if (texture != null)
                    {
                        material.SetTexture("_OcclusionMap", texture.GetMetallicRoughnessOcclusionConverted());
                    }
                }

                if (x.emissiveFactor != null
                    || (x.emissiveTexture!=null && x.emissiveTexture.index != -1))
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                    if (x.emissiveFactor != null)
                    {
                        material.SetColor("_EmissionColor", new Color(x.emissiveFactor[0], x.emissiveFactor[1], x.emissiveFactor[2]));
                    }

                    if (x.emissiveTexture.index != -1)
                    {
                        var texture = getTexture(x.emissiveTexture.index);
                        if (texture != null)
                        {
                            material.SetTexture("_EmissionMap", texture.Texture);
                        }
                    }
                }
            }

            return material;
        }
    }
}
