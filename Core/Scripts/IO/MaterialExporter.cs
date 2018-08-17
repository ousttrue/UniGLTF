using System.Collections.Generic;
using UnityEngine;


namespace UniGLTF
{
    public interface IMaterialExporter
    {
        glTFMaterial ExportMaterial(Material m, List<Texture> textures);
    }

    public class MaterialExporter : IMaterialExporter
    {
        public virtual glTFMaterial ExportMaterial(Material m, List<Texture> textures)
        {
            switch (m.shader.name)
            {
                case "Unlit/Color":
                    return Export_UnlitColor(m, textures);

                case "Unlit/Texture":
                    return Export_UnlitTexture(m, textures);

                case "Unlit/Transparent":
                    return Export_UnlitTransparent(m, textures);

                case "Unlit/Transparent Cutout":
                    return Export_UnlitCutout(m, textures);

                default:
                    return Export_Standard(m, textures);
            }
        }

        glTFMaterial Export_UnlitColor(Material m, List<Texture> textures)
        {
            var material = glTF_KHR_materials_unlit.CreateDefault();
            material.alphaMode = "OPAQUE";
            return material;
        }

        glTFMaterial Export_UnlitTexture(Material m, List<Texture> textures)
        {
            var material = glTF_KHR_materials_unlit.CreateDefault();
            material.alphaMode = "OPAQUE";
            return material;
        }

        glTFMaterial Export_UnlitTransparent(Material m, List<Texture> textures)
        {
            var material = glTF_KHR_materials_unlit.CreateDefault();
            material.alphaMode = "BLEND";
            return material;
        }

        glTFMaterial Export_UnlitCutout(Material m, List<Texture> textures)
        {
            var material = glTF_KHR_materials_unlit.CreateDefault();
            material.alphaMode = "MASK";
            return material;
        }

        glTFMaterial Export_Standard(Material m, List<Texture> textures)
        {
            var material = new glTFMaterial
            {
                name = m.name,
                pbrMetallicRoughness = new glTFPbrMetallicRoughness(),
            };

            if (m.HasProperty("_Color"))
            {
                material.pbrMetallicRoughness.baseColorFactor = m.color.ToArray();
            }

            if (m.mainTexture != null)
            {
                material.pbrMetallicRoughness.baseColorTexture = new glTFTextureInfo
                {
                    index = textures.IndexOf(m.mainTexture),
                };
            }

            switch(m.GetTag("RenderType", true))
            {
                case "Transparent":
                    material.alphaMode = "BLEND";
                    break;

                case "TransparentCutout":
                    material.alphaMode = "MASK";
                    break;

                default:
                    material.alphaMode = "OPAQUE";
                    break;
            }

            return material;
        }
    }
}
