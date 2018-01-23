using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace UniGLTF
{
    [Serializable]
    public class GltfTextureRef
    {
        public int index = -1;
        public int texCoord;
        public float scale;
        public float streangth;
    }

    [Serializable]
    public class GltfPbrMetallicRoughness
    {
        public GltfTextureRef baseColorTexture = null;
        public float[] baseColorFactor;
        public GltfTextureRef metallicRoghnessTexture = null;
        public float metallicFactor;
        public float roughnessFactor;
    }

    [Serializable]
    public class GltfMaterial
    {
        public string name;
        public GltfPbrMetallicRoughness pbrMetallicRoughness;
        public GltfTextureRef normalTexture = null;
        public GltfTextureRef occlusionTexture = null;
        public GltfTextureRef emissiveTexture = null;
        public float[] emissiveFactor;

        public static GltfMaterial Create(Material m, List<Texture2D> textures)
        {
            var material= new GltfMaterial
            {
                name=m.name,
                pbrMetallicRoughness=new GltfPbrMetallicRoughness
                {
                    baseColorFactor=m.color.ToArray(),
                }
            };

            if (m.mainTexture!=null)
            {
                material.pbrMetallicRoughness.baseColorTexture = new GltfTextureRef
                {
                    index=textures.IndexOf((Texture2D)m.mainTexture),
                };
            }

            return material;
        }
    }
}
