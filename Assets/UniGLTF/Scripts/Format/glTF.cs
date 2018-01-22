using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UniGLTF
{
    public struct PosRot
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public static PosRot FromGlobalTransform(Transform t)
        {
            return new PosRot
            {
                Position = t.position,
                Rotation = t.rotation,
            };
        }
    }

    public class glTF
    {
        public string baseDir
        {
            get;
            set;
        }

        public glTFAssets asset;

        public GltfBuffer buffer;

        public gltfTexture[] textures;
        public gltfImage[] images;

        public IEnumerable<TextureWithIsAsset> ReadTextures()
        {
            if (textures == null)
            {
                return new TextureWithIsAsset[] { };
            }
            else
            {
                return textures.Select(x => x.GetTexture(baseDir, buffer, images));
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

        public static glTF FromGameObject(GameObject go)
        {
            var gltf = new glTF();

            gltf.asset = new glTFAssets
            {
                generator="UniGLTF",
                version=2.0f,
            };

            var copy = GameObject.Instantiate(go);
            try
            {
                // Left handed to Right handed
                go.transform.ReverseZ();
            }
            finally
            {
                if (Application.isEditor)
                {
                    GameObject.DestroyImmediate(copy);
                }
                else
                {
                    GameObject.Destroy(copy);
                }
            }

            var buffer = new GltfBuffer();

            var nodes = go.transform.Traverse().Skip(1).ToList();

            var meshes = nodes.Select(x => x.GetSharedMesh()).Where(x => x != null).ToList();
            var skins = nodes.Select(x => x.GetComponent<SkinnedMeshRenderer>()).Where(x => x!= null).ToList();

            gltf.nodes = nodes.Select(x => gltfNode.Create(x, nodes, meshes, skins)).ToArray();

            var materials = nodes.SelectMany(x => x.GetSharedMaterials()).Distinct().ToArray();
            var textures = materials.Select(x => x.mainTexture).Distinct().ToArray();

            /*
            var textureWithImages = materials.Select(x => (Texture2D)x.mainTexture).Where(x => x != null).Select(x => gltfTexture.Create(x, buffer)).ToArray();

            gltf.materials = materials.Select(x => GltfMaterial.Create(x)).ToArray();

            gltf.meshes = meshes.Select(x => glTFMesh.Create(x, materials)).ToArray();
            */

            return gltf;
        }

        public override string ToString()
        {
            return string.Format("{0}", asset);
        }

        public ArraySegment<Byte> ToJson()
        {
            var formatter = new JsonFormatter();
            formatter.BeginMap();



            formatter.EndMap();
            return formatter.GetStore().Bytes;
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
                gltf.textures = parsed["textures"].DeserializeList<gltfTexture>();
            }

            if (parsed.HasKey("images"))
            {
                gltf.images = parsed["images"].DeserializeList<gltfImage>();
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
