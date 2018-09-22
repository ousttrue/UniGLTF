using UnityEngine;


namespace UniGLTF
{
    public interface IShaderStore
    {
        Shader GetShader(glTFMaterial material);
    }

    public class ShaderStore : IShaderStore
    {
        string m_defaultShaderName;
        Shader m_default;
        Shader Default
        {
            get
            {
                if (m_default == null)
                {
                    m_default = Shader.Find(m_defaultShaderName);
                }
                return m_default;
            }
        }

        Shader m_vcolor;
        Shader VColor
        {
            get
            {
                if (m_vcolor == null) m_vcolor = Shader.Find("UniGLTF/StandardVColor");
                return m_vcolor;
            }
        }

        Shader m_unlitTexture;
        Shader UnlitTexture
        {
            get
            {
                if (m_unlitTexture == null) m_unlitTexture = Shader.Find("Unlit/Texture");
                return m_unlitTexture;
            }
        }

        Shader m_unlitColor;
        Shader UnlitColor
        {
            get
            {
                if (m_unlitColor == null) m_unlitColor = Shader.Find("Unlit/Color");
                return m_unlitColor;
            }
        }

        Shader m_unlitTransparent;
        Shader UnlitTransparent
        {
            get
            {
                if (m_unlitTransparent == null) m_unlitTransparent = Shader.Find("Unlit/Transparent");
                return m_unlitTransparent;
            }
        }

        Shader m_unlitCoutout;
        Shader UnlitCutout
        {
            get
            {
                if (m_unlitCoutout == null) m_unlitCoutout = Shader.Find("Unlit/Transparent Cutout");
                return m_unlitCoutout;
            }
        }

        ImporterContext m_context;

        public ShaderStore(ImporterContext context) : this(context, "Standard")
        {
        }

        public ShaderStore(ImporterContext context, string defaultShaderName)
        {
            m_context = context;
            m_defaultShaderName = defaultShaderName;
        }

        static bool IsWhite(float[] color)
        {
            if (color == null) return false;
            if(color.Length!=4)return false;
            if(color[0]!=1
                || color[1]!=1
                || color[2]!=1
                || color[3] != 1)
            {
                return false;
            }
            return true;
        }

        public Shader GetShader(glTFMaterial material)
        {
            if (material == null)
            {
                return Default;
            }

            if (material.extensions != null && material.extensions.KHR_materials_unlit != null)
            {
                var isWhite = material.pbrMetallicRoughness != null && IsWhite(material.pbrMetallicRoughness.baseColorFactor);
                var hasTexture = material.pbrMetallicRoughness != null && material.pbrMetallicRoughness.baseColorTexture != null;

                // is unlit
                switch (material.alphaMode)
                {
                    case "BLEND":
                        {
                            if (hasTexture)
                            {
                                if (isWhite)
                                {
                                    return UnlitTransparent;
                                }
                                else
                                {
                                    Debug.LogWarningFormat("{0}: shader has no color property", UnlitTexture.name);
                                    return UnlitTransparent;
                                }
                            }
                            else
                            {
                                Debug.LogWarningFormat("{0}: shader is opaque", UnlitColor.name);
                                return UnlitColor;
                            }
                        }

                    case "MASK":
                        {
                            if (hasTexture)
                            {
                                if (isWhite)
                                {
                                    return UnlitCutout;
                                }
                                else
                                {
                                    Debug.LogWarningFormat("{0}: shader has no color property", UnlitCutout.name);
                                    return UnlitCutout;
                                }
                            }
                            else
                            {
                                Debug.LogErrorFormat("{0}: alphaMode='MASK' but no texture", UnlitTexture.name);
                                return UnlitCutout;
                            }
                        }

                    default:
                        {

                            if (hasTexture)
                            {
                                if (isWhite)
                                {
                                    return UnlitTexture;
                                }
                                else
                                {
                                    Debug.LogWarningFormat("{0}: shader has no color property", UnlitTexture.name);
                                    return UnlitTexture;
                                }
                            }
                            else
                            {
                                return UnlitColor;
                            }
                        }
                }
            }

            // custom shader for vertex color
            if (m_context != null && m_context.GLTF.MaterialHasVertexColor(material))
            {
                return VColor;
            }

            // standard
            return Default;
        }
    }
}
