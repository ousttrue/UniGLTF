using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniGLTF
{
    public interface IMaterialImporter
    {
        Material CreateMaterial(int i, glTFMaterial src);
    }

    public class MaterialImporter : IMaterialImporter
    {
        IShaderStore m_shaderStore;

        ImporterContext m_context;
        protected ImporterContext Context
        {
            get { return m_context; }
        }

        public MaterialImporter(IShaderStore shaderStore, ImporterContext context)
        {
            m_shaderStore = shaderStore;
            m_context = context;
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
        public virtual Material CreateMaterial(int i, glTFMaterial x)
        {
            var shader = m_shaderStore.GetShader(x);
            Debug.LogFormat("[{0}]{1}", i, shader.name);
            var material = new Material(shader);
            material.name = (x==null || string.IsNullOrEmpty(x.name))
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
                        var texture = m_context.GetTexture(x.pbrMetallicRoughness.baseColorTexture.index);
                        if (texture != null)
                        {
                            material.mainTexture = texture.Texture;
                        }
                    }

                    if (x.pbrMetallicRoughness.metallicRoughnessTexture != null && x.pbrMetallicRoughness.metallicRoughnessTexture.index != -1)
                    {
                        material.EnableKeyword("_METALLICGLOSSMAP");
                        var texture = Context.GetTexture(x.pbrMetallicRoughness.metallicRoughnessTexture.index);
                        if (texture != null)
                        {
                            material.SetTexture("_MetallicGlossMap", texture.GetMetallicRoughnessOcclusionConverted());
                        }
                    }
                }

                if (x.normalTexture!=null && x.normalTexture.index != -1)
                {
                    material.EnableKeyword("_NORMALMAP");
                    var texture = Context.GetTexture(x.normalTexture.index);
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
                    var texture = Context.GetTexture(x.occlusionTexture.index);
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
                        var texture = Context.GetTexture(x.emissiveTexture.index);
                        if (texture != null)
                        {
                            material.SetTexture("_EmissionMap", texture.Texture);
                        }
                    }
                }

                // https://forum.unity.com/threads/standard-material-shader-ignoring-setfloat-property-_mode.344557/#post-2229980
                switch (x.alphaMode)
                {
                    case "BLEND":
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000;
                        break;

                    case "MASK":
                        material.SetOverrideTag("RenderType", "TransparentCutout");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.EnableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 2450;
                        break;

                    default: // OPAQUE
                        material.SetOverrideTag("RenderType", "");
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = -1;
                        break;
                }
            }

            return material;
        }
    }
}
