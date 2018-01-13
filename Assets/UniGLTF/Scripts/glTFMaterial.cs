using Osaru.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UniGLTF
{
    public static class GltfMaterial
    {
        public static IEnumerable<Material> ReadMaterials(JsonParser materialsJson, Texture2D[] textures)
        {
            foreach (var x in materialsJson.ListItems)
            {
                var shader = Shader.Find("Standard");

                var material = new Material(shader);
                material.name = x["name"].GetString();

                if (x.HasKey("pbrMetallicRoughness"))
                {
                    var pbr = x["pbrMetallicRoughness"];
                    if (pbr.HasKey("baseColorTexture"))
                    {
                        var textureIndex = pbr["baseColorTexture"]["index"].GetInt32();
                        material.mainTexture = textures[textureIndex];
                    }
                    if (pbr.HasKey("baseColorFactor"))
                    {
                        var color = pbr["baseColorFactor"].ListItems.Select(y => y.GetSingle()).ToArray();
                        material.color = new Color(color[0], color[1], color[2], color[3]);
                    }
                }

                yield return material;
            }
        }
    }
}
