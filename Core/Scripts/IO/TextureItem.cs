using UnityEngine;
using System.Linq;


namespace UniGLTF
{
    public class TextureItem
    {
        public int TextureIndex = -1;
        public Texture2D Texture;
        public bool IsAsset;

        public TextureItem(Texture2D texture, int index = -1, bool isAsset = false)
        {
            Texture = texture;
            TextureIndex = index;
            IsAsset = isAsset;
        }

        Texture2D m_metallicRoughness;
        public Texture2D GetMetallicRoughnessConverted(IImporterContext ctx)
        {
            if (m_metallicRoughness == null)
            {
                //m_metallicRoughness = new Texture2D(Texture.width, Texture.height, TextureFormat.ARGB32, true);
                var texture = CopyTexture();
                texture.SetPixels32(texture.GetPixels32().Select(ConvertMetallicRoughness).ToArray());
                texture.name = this.Texture.name + ".converted";
                m_metallicRoughness = texture;
                ctx.AddObjectToAsset(texture.name, texture);
            }
            return m_metallicRoughness;
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

        public Texture2D CopyTexture()
        {
            var renderTexture = new RenderTexture(Texture.width, Texture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(Texture, renderTexture);
            var copyTexture = new Texture2D(Texture.width, Texture.height, TextureFormat.ARGB32, false);
            copyTexture.ReadPixels(new Rect(0, 0, Texture.width, Texture.height), 0, 0);
            return copyTexture;
        }
    }
}
