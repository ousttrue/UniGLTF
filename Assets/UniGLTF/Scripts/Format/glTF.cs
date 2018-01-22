using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UniGLTF
{
    public class glTF : MonoBehaviour
    {
        public string baseDir;

        public glTFAssets asset;

        public GltfBuffer buffer;

        public GltfTexture texture;

        public IEnumerable<GltfTexture.TextureWithIsAsset> ReadTextures()
        {
            if (texture == null)
            {
                return new GltfTexture.TextureWithIsAsset[] { };
            }
            else
            {
                return texture.GetTextures(baseDir, buffer);
            }
        }

        public GltfMaterial[] materials;

        public IEnumerable<Material> ReadMaterials(Texture2D[] textures)
        {
            var shader = Shader.Find("Standard");
            if (materials == null)
            {
                var material = new Material(shader);
                return new Material[] { material };
            }
            else
            {
                return materials.Select(x =>
                {
                    var material = new Material(shader);

                    material.name = x.name;

                    if (x.pbrMetallicRoughness != null)
                    {
                        if (x.pbrMetallicRoughness.baseColorFactor != null)
                        {
                            var color = x.pbrMetallicRoughness.baseColorFactor;
                            material.color = new Color(color[0], color[1], color[2], color[3]);
                        }

                        if (x.pbrMetallicRoughness.baseColorTexture.index != -1)
                        {
                            material.mainTexture = textures[x.pbrMetallicRoughness.baseColorTexture.index];
                        }
                    }

                    return material;
                });
            }
        }

        public glTFMesh[] meshes;

        public gltfNode[] nodes;

        public gltfSkin[] skins;

        public int scene;

        [Serializable]
        public struct gltfScene
        {
            public int[] nodes;
        }

        public gltfScene[] scenes;

        public int[] rootnodes
        {
            get
            {
                return scenes[scene].nodes;
            }
        }

        public GltfAnimation[] animations;

        public override string ToString()
        {
            return string.Format("{0}", asset);
        }

        public static glTF Parse(string json, string baseDir, ArraySegment<Byte> bytes)
        {
            var parsed = json.ParseAsJson();

            var gltf = new glTF
            {
                baseDir = baseDir,
            };

            // asset
            gltf.asset = JsonUtility.FromJson<glTFAssets>(parsed["asset"].Segment.ToString());
            if (gltf.asset.version != 2.0f)
            {
                throw new NotImplementedException(string.Format("unknown version: {0}", gltf.asset.version));
            }

            // buffer
            gltf.buffer = new GltfBuffer(parsed, baseDir, bytes);

            // texture
            if (parsed.HasKey("textures"))
            {
                gltf.texture = new GltfTexture(parsed);
            }

            // material
            if (parsed.HasKey("materials"))
            {
                gltf.materials = parsed["materials"].DeserializeList<GltfMaterial>();
            }

            // mesh
            if (parsed.HasKey("meshes"))
            {
                gltf.meshes = parsed["meshes"].DeserializeList<glTFMesh>();
            }

            // nodes
            gltf.nodes = parsed["nodes"].DeserializeList<gltfNode>();

            // skins
            if (parsed.HasKey("skins"))
            {
                gltf.skins = parsed["skins"].DeserializeList<gltfSkin>();
            }

            // scene;
            if (parsed.HasKey("scene"))
            {
                gltf.scene=parsed["scene"].GetInt32();
            }
            gltf.scenes = parsed["scenes"].DeserializeList<gltfScene>();

            // animations
            if (parsed.HasKey("animations"))
            {
                gltf.animations = parsed["animations"].DeserializeList<GltfAnimation>();
            }

            return gltf;
        }
    }
}
