using UnityEngine;


namespace UniGLTF
{
    public interface IShaderStore
    {
        Shader GetShader(ImporterContext context, int materialIndex);
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
                if (m_vcolor == null)
                {
                    m_vcolor = Shader.Find("UniGLTF/StandardVColor");
                }
                return m_vcolor;
            }
        }

        public ShaderStore() : this("Standard")
        {

        }

        public ShaderStore(string defaultShaderName)
        {
            m_defaultShaderName = defaultShaderName;
        }

        public Shader GetShader(ImporterContext context, int materialIndex)
        {
            if (context.HasVertexColor(materialIndex))
            {
                return VColor;
            }

            return Default;
        }
    }

    public delegate Material CreateMaterialFunc(ImporterContext ctx, int i);
}
