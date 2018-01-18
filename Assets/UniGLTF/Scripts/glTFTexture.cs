using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace UniGLTF
{
    [Serializable]
    public struct Image
    {
        public string uri;
        public int bufferView;
    }

    [Serializable]
    public struct Texture
    {
        public int sampler;
        public int source;
    }

    public class GltfTexture
    {
        Texture[] m_textures;
        Image[] m_images;

        public GltfTexture(JsonParser parsed)
        {
            if (parsed.HasKey("textures"))
            {
                m_textures = parsed["textures"].DeserializeList<Texture>();
            }
            if (parsed.HasKey("images"))
            {
                m_images = parsed["images"].DeserializeList<Image>();
            }
        }

        public struct TextureWithIsAsset
        {
            public Texture2D Texture;
            public bool IsAsset;
        }

        public IEnumerable<TextureWithIsAsset> GetTextures(string dir, GltfBuffer buffer)
        {
            int i = 0;
            foreach (var x in m_textures)
            {
                var image = m_images[x.source];
                if (string.IsNullOrEmpty(image.uri))
                {
                    // use buffer view
                    var texture = new Texture2D(2, 2);
                    texture.name = string.Format("texture#{0:00}", i++);
                    var bytes = buffer.GetViewBytes(image.bufferView);
                    texture.LoadImage(bytes.Array.Skip(bytes.Offset).Take(bytes.Count).ToArray());
                    yield return  new TextureWithIsAsset{ Texture=texture, IsAsset=false };
                }
                else
                {
                    var path = Path.Combine(dir, m_images[x.source].uri);
                    Debug.LogFormat("load texture: {0}", path);

                    /*
                    var bytes = File.ReadAllBytes(path);

                    var texture = new Texture2D(2, 2);
                    texture.LoadImage(bytes);
                    */
                    var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    yield return new TextureWithIsAsset { Texture = texture, IsAsset = true };
                }
            }
        }

        public static TextureWithIsAsset[] ReadTextures(JsonParser parsed, string dir, GltfBuffer buffer)
        {
            var texture = new GltfTexture(parsed);
            return texture.GetTextures(dir, buffer).ToArray();
        }
    }
}
