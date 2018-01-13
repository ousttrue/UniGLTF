using Osaru.Json;
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

        public IEnumerable<Texture2D> GetTextures(string dir)
        {
            foreach (var x in m_textures)
            {
                var path = Path.Combine(dir, m_images[x.source].uri);
                Debug.LogFormat("load texture: {0}", path);

                /*
                var bytes = File.ReadAllBytes(path);

                var texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                */
                var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                yield return texture;
            }
        }

        public static Texture2D[] ReadTextures(JsonParser parsed, string dir)
        {
            var texture = new GltfTexture(parsed);
            return texture.GetTextures(dir).ToArray();
        }
    }
}
