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
            catch(Exception ex)
            {
                Debug.LogError(ex);
            }

            if (Application.isEditor)
            {
                GameObject.DestroyImmediate(copy);
            }
            else
            {
                GameObject.Destroy(copy);
            }

            var buffer = gltf.buffers[0].Storage;

            var json = gltf.ToJson();

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
        struct BytesWithPath
        {
            public Byte[] Bytes;
            public string Path;

            public string Mime
            {
                get
                {
                    if (Path.ToLower().EndsWith(".png"))
                    {
                        return "image/png";
                    }
                    if (Path.ToLower().EndsWith(".jpg"))
                    {
                        return "image/jpeg";
                    }

                    throw new NotImplementedException();
                }
            }
        }
        static BytesWithPath GetPngBytes(Texture2D texture)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(texture);
            if (String.IsNullOrEmpty(path))
            {
                return new BytesWithPath
                {
                    Bytes = texture.EncodeToPNG(),
                };

            }
            else
            {
                return new BytesWithPath
                {
                    Bytes = File.ReadAllBytes(path),
                    Path = path,
                };
            }
        }

        public static glTFMaterial ExportMaterial(Material m, List<Texture2D> textures)
        {
            var material = new glTFMaterial
            {
                name = m.name,
                pbrMetallicRoughness = new GltfPbrMetallicRoughness
                {
                    baseColorFactor = m.color.ToArray(),
                }
            };

            if (m.mainTexture != null)
            {
                material.pbrMetallicRoughness.baseColorTexture = new GltfTextureRef
                {
                    index = textures.IndexOf((Texture2D)m.mainTexture),
                };
            }

            return material;
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

        static int GetNodeIndex(Transform root, List<Transform> nodes, string path)
        {
            var descendant = root.GetFromPath(path);
            return nodes.IndexOf(descendant);
        }

        static string PropertyToTarget(string property)
        {
            if (property.StartsWith("m_LocalPosition."))
            {
                return glTFAnimationTarget.PATH_TRANSLATION;
            }
            else if (property.StartsWith("m_LocalRotation."))
            {
                return glTFAnimationTarget.PATH_ROTATION;
            }
            else if (property.StartsWith("m_LocalScale."))
            {
                return glTFAnimationTarget.PATH_SCALE;
            }
            else
            {
                throw new NotImplementedException(property);
            }
        }

        static int GetElementOffset(string property)
        {
            if (property.EndsWith(".x"))
            {
                return 0;
            }
            if (property.EndsWith(".y"))
            {
                return 1;
            }
            if (property.EndsWith(".z"))
            {
                return 2;
            }
            if (property.EndsWith(".w"))
            {
                return 3;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        class InputOutputValues
        {
            public float[] Input;
            public float[] Output;
        }

        class AnimationWithSampleCurves
        {
            public glTFAnimation Animation;
            public Dictionary<int, InputOutputValues> SamplerMap = new Dictionary<int, InputOutputValues>();
        }

        static AnimationWithSampleCurves ExportAnimation(AnimationClip clip, Transform root, List<Transform> nodes)
        {
            var animation = new AnimationWithSampleCurves
            {
                Animation = new glTFAnimation(),
            };

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);

                var nodeIndex = GetNodeIndex(root, nodes, binding.path);
                var target = PropertyToTarget(binding.propertyName);
                var samplerIndex = animation.Animation.AddChannelAndGetSampler(nodeIndex, target);
                var sampler = animation.Animation.samplers[samplerIndex];

                var keys = curve.keys;
                var elementCount = glTFAnimationTarget.GetElementCount(target);
                var values = default(InputOutputValues);
                if (!animation.SamplerMap.TryGetValue(samplerIndex, out values))
                {
                    values = new InputOutputValues();
                    values.Input = new float[keys.Length];
                    values.Output = new float[keys.Length * elementCount];
                    animation.SamplerMap[samplerIndex] = values;
                }

                var j = GetElementOffset(binding.propertyName);
                for (int i = 0; i < keys.Length; ++i, j += elementCount)
                {
                    values.Input[i] = keys[i].time;
                    values.Output[j] = keys[i].value;
                }
            }

            return animation;
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

                var bytesWithPath = GetPngBytes(texture); ;

                // add view
                var view = gltf.buffers[bufferIndex].Storage.Extend(bytesWithPath.Bytes, glBufferTarget.NONE);
                var viewIndex = gltf.AddBufferView(view);

                // add image
                var imageIndex = gltf.images.Count;
                gltf.images.Add(new glTFImage
                {
                    bufferView = viewIndex,
                    mimeType = bytesWithPath.Mime,
                });

                // add sampler
                var filter = default(glFilter);
                switch (texture.filterMode)
                {
                    case FilterMode.Point:
                        filter = glFilter.NEAREST;
                        break;

                    default:
                        filter = glFilter.LINEAR;
                        break;
                }
                var wrap = default(glWrap);

                switch (texture.wrapMode)
                {
                    case TextureWrapMode.Clamp:
                        wrap = glWrap.CLAMP_TO_EDGE;
                        break;

                    case TextureWrapMode.Repeat:
                        wrap = glWrap.REPEAT;
                        break;

#if UNITY_2017_OR_NEWER
                    case TextureWrapMode.Mirror:
                        wrap = glWrap.MIRRORED_REPEAT;
                        break;
#endif

                    default:
                        throw new NotImplementedException();
                }

                var samplerIndex = gltf.samplers.Count;
                gltf.samplers.Add(new glTFTextureSampler
                {
                    magFilter = filter,
                    minFilter = filter,
                    wrapS = wrap,
                    wrapT = wrap,

                });

                // add texture
                gltf.textures.Add(new glTFTexture
                {
                    sampler = samplerIndex,
                    source = imageIndex,
                });
            }

            gltf.materials = unityMaterials.Select(x => ExportMaterial(x, unityTextures)).ToList();
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

#region Skins
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
                    skeleton = unityNodes.IndexOf(x.rootBone),
                };
                var skinIndex = gltf.skins.Count;
                gltf.skins.Add(skin);

                foreach (var z in unityNodes.Where(y => y.Has(x)))
                {
                    var nodeIndex = unityNodes.IndexOf(z);
                    gltf.nodes[nodeIndex].skin = skinIndex;
                }
            }
#endregion

#region Animations
            var animation = go.GetComponent<Animation>();
            if (animation != null)
            {
                foreach (AnimationState state in animation)
                {
                    var animationWithCurve = ExportAnimation(state.clip, go.transform, unityNodes);

                    foreach (var kv in animationWithCurve.SamplerMap)
                    {
                        var sampler = animationWithCurve.Animation.samplers[kv.Key];

                        var inputAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, kv.Value.Input);
                        sampler.input = inputAccessorIndex;

                        var outputAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, kv.Value.Output);
                        sampler.output = outputAccessorIndex;

                        // modify accessors
                        var outputAccessor = gltf.accessors[outputAccessorIndex];
                        var channel = animationWithCurve.Animation.channels.First(x => x.sampler == kv.Key);
                        switch (glTFAnimationTarget.GetElementCount(channel.target.path))
                        {
                            case 3:
                                outputAccessor.type = "VEC3";
                                outputAccessor.count /= 3;
                                break;

                            case 4:
                                outputAccessor.type = "VEC4";
                                outputAccessor.count /= 4;
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                    }

                    gltf.animations.Add(animationWithCurve.Animation);
                }
            }
#endregion

            // glb buffer
            gltf.buffers[bufferIndex].UpdateByteLength();
        }
#endregion
    }
}
