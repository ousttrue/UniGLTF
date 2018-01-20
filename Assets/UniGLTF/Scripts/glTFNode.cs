using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UniGLTF
{
    public struct TransformWithSkin
    {
        public Transform Transform;
        public int? SkinIndex;

        public static List<TransformWithSkin> ReadNodes(JsonParser nodesJson, MeshWithMaterials[] meshes)
        {
            var list = new List<TransformWithSkin>();
            int i = 0;
            foreach (var node in nodesJson.ListItems)
            {
                var go = new GameObject();
                if (node.HasKey("name"))
                {
                    go.name = node["name"].GetString();
                }
                else
                {
                    go.name = string.Format("node{0:000}", i);
                }

                var nodeWithSkin = new TransformWithSkin
                {
                    Transform = go.transform,
                };

                // transform
                if (node.HasKey("translation"))
                {
                    var values = node["translation"].ListItems.Select(x => x.GetSingle()).ToArray();
                    go.transform.localPosition = new Vector3(values[0], values[1], values[2]);
                }
                if (node.HasKey("rotation"))
                {
                    var values = node["rotation"].ListItems.Select(x => x.GetSingle()).ToArray();
                    go.transform.localRotation = new Quaternion(values[0], values[1], values[2], values[3]);
                }
                if (node.HasKey("scale"))
                {
                    var values = node["scale"].ListItems.Select(x => x.GetSingle()).ToArray();
                    go.transform.localScale = new Vector3(values[0], values[1], values[2]);
                }
                if (node.HasKey("matrix"))
                {
                    var values = node["matrix"].ListItems.Select(x => x.GetSingle()).ToArray();
                    var col0 = new Vector4(values[0], values[1], values[2], values[3]);
                    var col1 = new Vector4(values[4], values[5], values[6], values[7]);
                    var col2 = new Vector4(values[8], values[9], values[10], values[11]);
                    var col3 = new Vector4(values[12], values[13], values[14], values[15]);
                    var m = new Matrix4x4(col0, col1, col2, col3);
                    go.transform.localRotation = m.rotation;
                    go.transform.localPosition = m.GetColumn(3);
                }

                // mesh
                if (node.HasKey("mesh"))
                {
                    var mesh = meshes[node["mesh"].GetInt32()];
                    Renderer renderer = null;
                    var hasSkin = node.HasKey("skin");
                    if (mesh.Mesh.blendShapeCount == 0 && !hasSkin)
                    {
                        // without blendshape and bone skinning
                        var filter = go.AddComponent<MeshFilter>();
                        filter.sharedMesh = mesh.Mesh;

                        renderer = go.AddComponent<MeshRenderer>();
                    }
                    else
                    {
                        var _renderer = go.AddComponent<SkinnedMeshRenderer>();

                        if (hasSkin)
                        {
                            nodeWithSkin.SkinIndex = node["skin"].GetInt32();
                        }

                        _renderer.sharedMesh = mesh.Mesh;

                        renderer = _renderer;
                    }

                    renderer.sharedMaterials = mesh.Materials;
                }

                list.Add(nodeWithSkin);

                ++i;
            }

            i = 0;
            foreach (var node in nodesJson.ListItems)
            {
                // children
                if (node.HasKey("children"))
                {
                    foreach (var child in node["children"].ListItems)
                    {
                        // node has local transform
                        list[child.GetInt32()].Transform.SetParent(list[i].Transform, false);
                    }
                }

                ++i;
            }

            return list;
        }
    }
}