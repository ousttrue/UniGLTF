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

            return material;
        }
    }
}
