using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UniGLTF
{
    public class gltfExporter : IDisposable
    {
        const string CONVERT_HUMANOID_KEY = "GameObject/gltf/export";

#if UNITY_EDITOR
        [MenuItem(CONVERT_HUMANOID_KEY, true, 1)]
        private static bool ExportValidate()
        {
            return Selection.activeObject != null && Selection.activeObject is GameObject;
        }

        [MenuItem(CONVERT_HUMANOID_KEY, false, 1)]
        private static void ExportFromMenu()
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

            using (var exporter = new gltfExporter(new glTF()))
            {
                exporter.Prepare(go);
                exporter.Export();
                exporter.WriteTo(path);
            }
        }
#endif

        glTF glTF;

        public GameObject Copy
        {
            get;
            protected set;
        }

        public struct MeshWithRenderer
        {
            public Mesh Mesh;
            public Renderer Rendererer;
        }

        public List<Mesh> Meshes
        {
            get;
            private set;
        }

        public List<Transform> Nodes
        {
            get;
            private set;
        }

        public List<Material> Materials
        {
            get;
            private set;
        }

        public List<Texture2D> Textures
        {
            get;
            private set;
        }

        public gltfExporter(glTF gltf)
        {
            glTF = gltf;

            glTF.asset=new glTFAssets
            {
                generator = "UniGLTF-"+UniGLTFVersion.VERSION,
                version = "2.0",
            };
        }

        public virtual void Prepare(GameObject go)
        {
            Copy = GameObject.Instantiate(go);

            // Left handed to Right handed
            Copy.transform.ReverseZ();
        }

        public virtual void Export()
        {
            var exported = FromGameObject(glTF, Copy);
            Meshes = exported.Meshes.Select(x => x.Mesh).ToList();
            Nodes = exported.Nodes;
            Materials = exported.Materials;
            Textures = exported.Textures;
        }

        public void Dispose()
        {
            if (Application.isEditor)
            {
                GameObject.DestroyImmediate(Copy);
            }
            else
            {
                GameObject.Destroy(Copy);
            }
        }

        public void WriteTo(string path)
        {
            var buffer = glTF.buffers[0].Storage;

            var json = glTF.ToJson();

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
            public string Mime;

            public BytesWithPath(Texture2D texture)
            {
                //var path = UnityEditor.AssetDatabase.GetAssetPath(texture);
                /*
                if (!String.IsNullOrEmpty(path))
                {
                    Bytes = File.ReadAllBytes(path);
                    Path = path;
                    var ext = System.IO.Path.GetExtension(Path).ToLower();
                    switch (ext)
                    {
                        case ".png":
                            Mime = "image/png";
                            break;

                        case ".jpg":
                            Mime = "image/jpeg";
                            break;

                        case ".tga":
                            Mime = "image/tga";
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                */
                {
                    Path = "";
                    Bytes = new TextureItem(texture).CopyTexture().EncodeToPNG();
                    Mime = "image/png";
                }
            }
        }

        public static glTFMaterial ExportMaterial(Material m, List<Texture2D> textures)
        {
            var material = new glTFMaterial
            {
                name = m.name,
                pbrMetallicRoughness = new GltfPbrMetallicRoughness(),
            };

            if (m.HasProperty("_Color"))
            {
                material.pbrMetallicRoughness.baseColorFactor = m.color.ToArray();
            }

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
                if (skinnredMeshRenderer.rootBone != null)
                {
                    node.extras.skinRootBone = nodes.IndexOf(skinnredMeshRenderer.rootBone);
                }
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
                return glTFAnimationTarget.NOT_IMPLEMENTED;
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

#if UNITY_EDITOR
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
                if (target == glTFAnimationTarget.NOT_IMPLEMENTED) {
                    continue;
                }
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
                    if (binding.propertyName == "m_LocalPosition.z" ||
                        binding.propertyName == "m_LocalRotation.z" ||
                        binding.propertyName == "m_LocalRotation.w")
                    {
                        values.Output[j] = -keys[i].value;
                    } else {
                        values.Output[j] = keys[i].value;
                    }
                }
            }

            return animation;
        }
#endif

        public struct Exported
        {
            public List<MeshWithRenderer> Meshes;
            public List<Transform> Nodes;
            public List<Material> Materials;
            public List<Texture2D> Textures;
        }

        public static int ExportTexture(glTF gltf, int bufferIndex, Texture2D texture)
        {
            var bytesWithPath = new BytesWithPath(texture); ;

            // add view
            var view = gltf.buffers[bufferIndex].Storage.Extend(bytesWithPath.Bytes, glBufferTarget.NONE);
            var viewIndex = gltf.AddBufferView(view);

            // add image
            var imageIndex = gltf.images.Count;
            gltf.images.Add(new glTFImage
            {
                name = texture.name,
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

            return imageIndex;
        }

        public static Exported FromGameObject(glTF gltf, GameObject go)
        {
            var bytesBuffer = new ArrayByteBuffer();
            var bufferIndex = gltf.AddBuffer(bytesBuffer);

            if (go.transform.childCount == 0)
            {
                throw new UniGLTFException("root node required");
            }

            var unityNodes = go.transform.Traverse()
                .Skip(1) // exclude root object for the symmetry with the importer
                .ToList();

            #region Material
            var unityMaterials = unityNodes.SelectMany(x => x.GetSharedMaterials()).Where(x => x != null).Distinct().ToList();
            var unityTextures = unityMaterials.SelectMany(x => x.GetTextures()).Where(x => x != null).Distinct().ToList();

            for (int i = 0; i < unityTextures.Count; ++i)
            {
                var texture = unityTextures[i];
                ExportTexture(gltf, bufferIndex, texture);            
            }
           
            gltf.materials = unityMaterials.Select(x => ExportMaterial(x, unityTextures)).ToList();
            #endregion

            #region Meshes
            var unityMeshes = unityNodes
                .Select(x => new MeshWithRenderer
                {
                    Mesh = x.GetSharedMesh(),
                    Rendererer = x.GetComponent<Renderer>(),
                })
                .Where(x => x.Mesh != null)
                .ToList();
            for (int i = 0; i < unityMeshes.Count; ++i)
            {
                var x = unityMeshes[i];
                var mesh = x.Mesh;
                var materials = x.Rendererer.sharedMaterials;

                var positions = mesh.vertices.Select(y => y.ReverseZ()).ToArray();
                var positionAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, positions, glBufferTarget.ARRAY_BUFFER);
                gltf.accessors[positionAccessorIndex].min = positions.Aggregate(positions[0], (a, b) => new Vector3(Mathf.Min(a.x, b.x), Math.Min(a.y, b.y), Mathf.Min(a.z, b.z))).ToArray();
                gltf.accessors[positionAccessorIndex].max = positions.Aggregate(positions[0], (a, b) => new Vector3(Mathf.Max(a.x, b.x), Math.Max(a.y, b.y), Mathf.Max(a.z, b.z))).ToArray();

                var normalAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, mesh.normals.Select(y => y.ReverseZ()).ToArray(), glBufferTarget.ARRAY_BUFFER);
                var tangentAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, mesh.tangents.Select(y => y.ReverseZ()).ToArray(), glBufferTarget.ARRAY_BUFFER);
                var uvAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, mesh.uv.Select(y => y.ReverseY()).ToArray(), glBufferTarget.ARRAY_BUFFER);
                var colorAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, mesh.colors, glBufferTarget.ARRAY_BUFFER);

                var boneweights = mesh.boneWeights;
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
                if (tangentAccessorIndex != -1)
                {
                    attributes.TANGENT = tangentAccessorIndex;
                }
                if (uvAccessorIndex != -1)
                {
                    attributes.TEXCOORD_0 = uvAccessorIndex;
                }
                if (colorAccessorIndex != -1)
                {
                    attributes.COLOR_0 = colorAccessorIndex;
                }
                if (weightAccessorIndex != -1)
                {
                    attributes.WEIGHTS_0 = weightAccessorIndex;
                }
                if (jointsAccessorIndex != -1)
                {
                    attributes.JOINTS_0 = jointsAccessorIndex;
                }

                gltf.meshes.Add(new glTFMesh(mesh.name));

                for (int j = 0; j < mesh.subMeshCount; ++j)
                {
                    var indices = TriangleUtil.FlipTriangle(mesh.GetIndices(j)).Select(y => (uint)y).ToArray();
                    var indicesAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, indices, glBufferTarget.ELEMENT_ARRAY_BUFFER);

                    gltf.meshes.Last().primitives.Add(new glTFPrimitives
                    {
                        attributes = attributes,
                        indices = indicesAccessorIndex,
                        mode = 4, // triangels ?
                        material = unityMaterials.IndexOf(materials[j])
                    });
                }

                if (mesh.blendShapeCount > 0)
                {
                    for (int j = 0; j < mesh.blendShapeCount; ++j)
                    {
                        var blendShapeVertices = mesh.vertices;
                        var blendShpaeNormals = mesh.normals;
                        var blendShapeTangents = mesh.tangents.Select(y => (Vector3)y).ToArray();
                        var k = mesh.GetBlendShapeFrameCount(j);
                        mesh.GetBlendShapeFrameVertices(j, k - 1, blendShapeVertices, blendShpaeNormals, null);
                        blendShapeVertices = blendShapeVertices.Select(y => y.ReverseZ()).ToArray();

                        var blendShapePositionAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, blendShapeVertices
                            , glBufferTarget.ARRAY_BUFFER);
                        gltf.accessors[blendShapePositionAccessorIndex].min = blendShapeVertices.Aggregate(blendShapeVertices[0], (a, b) => new Vector3(Mathf.Min(a.x, b.x), Math.Min(a.y, b.y), Mathf.Min(a.z, b.z))).ToArray();
                        gltf.accessors[blendShapePositionAccessorIndex].max = blendShapeVertices.Aggregate(blendShapeVertices[0], (a, b) => new Vector3(Mathf.Max(a.x, b.x), Math.Max(a.y, b.y), Mathf.Max(a.z, b.z))).ToArray();

                        var blendShapeNormalAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex,
                            blendShpaeNormals.Select(y => y.ReverseZ()).ToArray(), glBufferTarget.ARRAY_BUFFER);
                        var blendShapeTangentAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex,
                            blendShapeTangents.Select(y => y.ReverseZ()).ToArray(), glBufferTarget.ARRAY_BUFFER);
                        //
                        // first primitive has whole blendShape
                        //
                        var primitive = gltf.meshes.Last().primitives[0];
                        primitive.targets.Add(new gltfMorphTarget
                        {
                            POSITION = blendShapePositionAccessorIndex,
                            NORMAL = blendShapeNormalAccessorIndex,
                            TANGENT = blendShapeTangentAccessorIndex,
                        });
                        primitive.extras.targetNames.Add(mesh.GetBlendShapeName(j));
                    }
                }
            }
            #endregion

            #region Skins
            var unitySkins = unityNodes
                .Select(x => x.GetComponent<SkinnedMeshRenderer>()).Where(x => x != null)
                .ToList();
            gltf.nodes = unityNodes.Select(x => ExportNode(x, unityNodes, unityMeshes.Select(y => y.Mesh).ToList(), unitySkins)).ToList();
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
                    var node = gltf.nodes[nodeIndex];
                    node.skin = skinIndex;
                    node.extras.skinRootBone = unityNodes.IndexOf(x.rootBone);
                }
            }
            #endregion

#if UNITY_EDITOR
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
#endif

            // glb buffer
            gltf.buffers[bufferIndex].UpdateByteLength();

            return new Exported
            {
                Meshes = unityMeshes,
                Nodes = unityNodes.Select(x => x.transform).ToList(),
                Materials = unityMaterials,
                Textures = unityTextures,
            };
        }
        #endregion
    }
}
