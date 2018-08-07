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

        Shader m_unlit;
        Shader Unlit
        {
            get
            {
                if (m_unlit == null) m_unlit = Shader.Find("Unlit/Texture");
                return m_unlit;
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
                return m_unlitTransparent;
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

        public Shader GetShader(glTFMaterial material)
        {
            if (material != null)
            {
                if (material.extensions != null && material.extensions.KHR_materials_unlit != null)
                {
                    // is unlit
                    switch (material.alphaMode)
                    {
                        case "BLEND": return UnlitTransparent;
                        case "MASK": return UnlitCutout;
                        default: return Unlit; // OPAQUE
                    }
                }
            }

            if (m_context != null && m_context.MaterialHasVertexColor(material))
            {
                return VColor;
            }

            return Default;
        }
    }
}
