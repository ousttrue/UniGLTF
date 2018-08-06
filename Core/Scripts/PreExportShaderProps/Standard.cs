using System.Collections.Generic;


namespace UniGLTF.ShaderPropExporter
{
    public static partial class PreShaderPropExporter
    {
        [PreExportShader]
        static KeyValuePair<string, ShaderProps> Standard 
        {
            get 
            {
                return new KeyValuePair<string, ShaderProps>(
                    "Standard",
                    new ShaderProps
                    {
                        Properties = new ShaderProperty[]{
new ShaderProperty("_Color", ShaderPropertyType.Color, false)
,new ShaderProperty("_MainTex", ShaderPropertyType.TexEnv, false)
,new ShaderProperty("_Cutoff", ShaderPropertyType.Range, false)
,new ShaderProperty("_Glossiness", ShaderPropertyType.Range, false)
,new ShaderProperty("_GlossMapScale", ShaderPropertyType.Range, false)
,new ShaderProperty("_SmoothnessTextureChannel", ShaderPropertyType.Float, false)
,new ShaderProperty("_Metallic", ShaderPropertyType.Range, false)
,new ShaderProperty("_MetallicGlossMap", ShaderPropertyType.TexEnv, false)
,new ShaderProperty("_SpecularHighlights", ShaderPropertyType.Float, false)
,new ShaderProperty("_GlossyReflections", ShaderPropertyType.Float, false)
,new ShaderProperty("_BumpScale", ShaderPropertyType.Float, false)
,new ShaderProperty("_BumpMap", ShaderPropertyType.TexEnv, true)
,new ShaderProperty("_Parallax", ShaderPropertyType.Range, false)
,new ShaderProperty("_ParallaxMap", ShaderPropertyType.TexEnv, false)
,new ShaderProperty("_OcclusionStrength", ShaderPropertyType.Range, false)
,new ShaderProperty("_OcclusionMap", ShaderPropertyType.TexEnv, false)
,new ShaderProperty("_EmissionColor", ShaderPropertyType.Color, false)
,new ShaderProperty("_EmissionMap", ShaderPropertyType.TexEnv, false)
,new ShaderProperty("_DetailMask", ShaderPropertyType.TexEnv, false)
,new ShaderProperty("_DetailAlbedoMap", ShaderPropertyType.TexEnv, false)
,new ShaderProperty("_DetailNormalMapScale", ShaderPropertyType.Float, false)
,new ShaderProperty("_DetailNormalMap", ShaderPropertyType.TexEnv, false)
,new ShaderProperty("_UVSec", ShaderPropertyType.Float, false)
,new ShaderProperty("_Mode", ShaderPropertyType.Float, false)
,new ShaderProperty("_SrcBlend", ShaderPropertyType.Float, false)
,new ShaderProperty("_DstBlend", ShaderPropertyType.Float, false)
,new ShaderProperty("_ZWrite", ShaderPropertyType.Float, false)

                        }
                    }
                );
            }
        }
    }
}
