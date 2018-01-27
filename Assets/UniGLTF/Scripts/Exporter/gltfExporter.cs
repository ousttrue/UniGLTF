using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;


namespace UniGLTF
{
    public static class gltfExporter
    {
        const string CONVERT_HUMANOID_KEY = "GameObject/gltf/export";
        private static readonly UnityEngine.Object json;

        [MenuItem(CONVERT_HUMANOID_KEY, true, 1)]
        private static bool ExportValidate()
        {
            return Selection.activeObject != null && Selection.activeObject is GameObject;
        }

        [MenuItem(CONVERT_HUMANOID_KEY, false, 1)]
        private static void Export()
        {
            var go = Selection.activeObject as GameObject;

            var path = EditorUtility.SaveFilePanel(
                    "Save glb",
                    "",
                    go.name + ".glb",
                    "glb");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var gltf = new glTF();
            var copy = GameObject.Instantiate(go);
            try
            {
                // Left handed to Right handed
                copy.transform.ReverseZ();

                gltf.FromGameObject(copy);
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

            var buffer = gltf.buffers[0].Storage;

            var json = gltf.ToJson();
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            using (var s = new FileStream(path, FileMode.Create))
            {
                GlbHeader.WriteTo(s);

                var pos = s.Position;
                s.Position += 4; // skip total size

                int size = 12;

                {
                    var chunk = new GlbChunk(json);
                    size += chunk.WriteTo(s);
                }
                {
                    var chunk = new GlbChunk(buffer.GetBytes());
                    size += chunk.WriteTo(s);
                }

                s.Position = pos;
                var bytes = BitConverter.GetBytes(size);
                s.Write(bytes, 0, bytes.Length);
            }

            Debug.Log(json);
        }

        #region Export
        static byte[] GetPngBytes(Texture2D texture)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(texture);
            if (String.IsNullOrEmpty(path))
            {
                return texture.EncodeToPNG();
            }
            else
            {
                Debug.Log(path);
                return File.ReadAllBytes(path);
            }
        }

        static glTFNode ExportNode(Transform x, List<Transform> nodes, List<Mesh> meshes, List<SkinnedMeshRenderer> skins)
        {
            var node = new glTFNode
            {
                name = x.name,
                children = x.transform.GetChildren().Select(y => nodes.IndexOf(y)).ToArray(),
                rotation = x.transform.localRotation.ToArray(),
                translation = x.transform.localPosition.ToArray(),
                scale = x.transform.localScale.ToArray(),
            };

            var meshFilter = x.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                node.mesh = meshes.IndexOf(meshFilter.sharedMesh);
            }

            var skinnredMeshRenderer = x.GetComponent<SkinnedMeshRenderer>();
            if (skinnredMeshRenderer != null)
            {
                node.mesh = meshes.IndexOf(skinnredMeshRenderer.sharedMesh);
                node.skin = skins.IndexOf(skinnredMeshRenderer);
            }

            return node;
        }

        public static void FromGameObject(this glTF gltf, GameObject go)
        {
            var bytesBuffer = new ArrayByteBuffer();
            var bufferIndex = gltf.AddBuffer(bytesBuffer);

            var unityNodes = go.transform.Traverse()
                .Skip(1) // exclude root object for the symmetry with the importer
                .ToList();

            #region Material
            var unityMaterials = unityNodes.SelectMany(x => x.GetSharedMaterials()).Where(x => x != null).Distinct().ToList();
            var unityTextures = unityMaterials.Select(x => (Texture2D)x.mainTexture).Where(x => x != null).Distinct().ToList();

            for (int i = 0; i < unityTextures.Count; ++i)
            {
                var texture = unityTextures[i];

                var bytes = GetPngBytes(texture); ;

                // add view
                var view = gltf.buffers[bufferIndex].Storage.Extend(bytes, glBufferTarget.NONE);
                var viewIndex = gltf.AddBufferView(view);

                // add image
                var imageIndex = gltf.images.Count;
                gltf.images.Add(new gltfImage
                {
                    bufferView = viewIndex,
                });

                // add texture
                gltf.textures.Add(new gltfTexture
                {
                    //sampler = -1, ToDo
                    source = imageIndex,
                });
            }

            gltf.materials = unityMaterials.Select(x => GltfMaterial.Create(x, unityTextures)).ToList();
            #endregion

            #region Meshes
            var unityMeshes = unityNodes.Select(x => x.GetSharedMesh()).Where(x => x != null).ToList();
            for (int i = 0; i < unityMeshes.Count; ++i)
            {
                var x = unityMeshes[i];

                var positions = x.vertices.Select(y => y.ReverseZ()).ToArray();
                var positionAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, positions, glBufferTarget.ARRAY_BUFFER);
                gltf.accessors[positionAccessorIndex].min = positions.Aggregate(positions[0], (a, b) => new Vector3(Mathf.Min(a.x, b.x), Math.Min(a.y, b.y), Mathf.Min(a.z, b.z))).ToArray();
                gltf.accessors[positionAccessorIndex].max = positions.Aggregate(positions[0], (a, b) => new Vector3(Mathf.Max(a.x, b.x), Math.Max(a.y, b.y), Mathf.Max(a.z, b.z))).ToArray();

                var normalAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, x.normals.Select(y => y.ReverseZ()).ToArray(), glBufferTarget.ARRAY_BUFFER);
                var uvAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, x.uv.Select(y => y.ReverseY()).ToArray(), glBufferTarget.ARRAY_BUFFER);
                var tangentAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, x.tangents, glBufferTarget.ARRAY_BUFFER);

                var boneweights = x.boneWeights;
                var weightAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, boneweights.Select(y => new Vector4(y.weight0, y.weight1, y.weight2, y.weight3)).ToArray(), glBufferTarget.ARRAY_BUFFER);
                var jointsAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, boneweights.Select(y => new UShort4((ushort)y.boneIndex0, (ushort)y.boneIndex1, (ushort)y.boneIndex2, (ushort)y.boneIndex3)).ToArray(), glBufferTarget.ARRAY_BUFFER);

                var attributes = new glTFAttributes
                {
                    POSITION = positionAccessorIndex,
                };
                if (normalAccessorIndex != -1)
                {
                    attributes.NORMAL = normalAccessorIndex;
                }
                if (uvAccessorIndex != -1)
                {
                    attributes.TEXCOORD_0 = uvAccessorIndex;
                }
                if (weightAccessorIndex != -1)
                {
                    attributes.WEIGHTS_0 = weightAccessorIndex;
                }
                if (jointsAccessorIndex != -1)
                {
                    attributes.JOINTS_0 = jointsAccessorIndex;
                }

                gltf.meshes.Add(new glTFMesh(x.name));

                for (int j = 0; j < x.subMeshCount; ++j)
                {
                    var indices = TriangleUtil.FlipTriangle(x.GetIndices(j)).Select(y => (uint)y).ToArray();
                    var indicesAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, indices, glBufferTarget.ELEMENT_ARRAY_BUFFER);

                    gltf.meshes.Last().primitives.Add(new glTFPrimitives
                    {
                        attributes = attributes,
                        indices = indicesAccessorIndex,
                        mode = 4 // triangels ?
                    });
                }
            }
            #endregion

            var unitySkins = unityNodes.Select(x => x.GetComponent<SkinnedMeshRenderer>()).Where(x => x != null).ToList();
            gltf.nodes = unityNodes.Select(x => ExportNode(x, unityNodes, unityMeshes, unitySkins)).ToList();
            gltf.scenes = new List<gltfScene>
            {
                new gltfScene
                {
                    nodes = go.transform.GetChildren().Select(x => unityNodes.IndexOf(x)).ToArray(),
                }
            };

            foreach (var x in unitySkins)
            {
                var matrices = x.sharedMesh.bindposes.Select(y => y.ReverseZ()).ToArray();
                var accessor = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, matrices, glBufferTarget.NONE);

                var skin = new glTFSkin
                {
                    inverseBindMatrices = accessor,
                    joints = x.bones.Select(y => unityNodes.IndexOf(y)).ToArray(),
                };
                var skinIndex = gltf.skins.Count;
                gltf.skins.Add(skin);

                foreach(var z in unityNodes.Where(y => y.Has(x)))
                {
                    var nodeIndex = unityNodes.IndexOf(z);
                    gltf.nodes[nodeIndex].skin = skinIndex;
                }
            }

            // glb buffer
            gltf.buffers[bufferIndex].UpdateByteLength();
        }
        #endregion
    }
}
