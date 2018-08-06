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
        const string CONVERT_HUMANOID_KEY = UniGLTFVersion.UNIGLTF_VERSION + "/Export";

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

            var gltf = new glTF();
            using (var exporter = new gltfExporter(gltf))
            {
                exporter.Prepare(go);
                exporter.Export();
            }
            var bytes = gltf.ToGlbBytes();
            File.WriteAllBytes(path, bytes);

            if (path.StartsWithUnityAssetPath())
            {
                AssetDatabase.ImportAsset(path.ToUnityRelativePath());
                AssetDatabase.Refresh();
            }
        }
#endif

        glTF glTF;

        public bool UseSparseAccessorForBlendShape
        {
            get;
            set;
        }

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

        public List<Texture> Textures
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

        public static glTF Export(GameObject go)
        {
            var gltf = new glTF();
            using (var exporter = new gltfExporter(gltf))
            {
                exporter.Prepare(go);
                exporter.Export();
            }
            return gltf;
        }

        public virtual void Prepare(GameObject go)
        {
            Copy = GameObject.Instantiate(go);

            // Left handed to Right handed
            Copy.transform.ReverseZ();
        }

        public virtual void Export()
        {
            var exported = FromGameObject(glTF, Copy, UseSparseAccessorForBlendShape);
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

        #region Export
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
            public List<Texture> Textures;
        }

        static glTFMesh ExportPrimitives(glTF gltf, int bufferIndex, 
            string rendererName,
            Mesh mesh, Material[] materials, 
            List<Material> unityMaterials)
        {
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

            var gltfMesh = new glTFMesh(mesh.name);
            for (int j = 0; j < mesh.subMeshCount; ++j)
            {
                var indices = TriangleUtil.FlipTriangle(mesh.GetIndices(j)).Select(y => (uint)y).ToArray();
                var indicesAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, indices, glBufferTarget.ELEMENT_ARRAY_BUFFER);

                if (j >= materials.Length)
                {
                    Debug.LogWarningFormat("{0}.materials is not enough", rendererName);
                    break;
                }

                gltfMesh.primitives.Add(new glTFPrimitives
                {
                    attributes = attributes,
                    indices = indicesAccessorIndex,
                    mode = 4, // triangels ?
                    material = unityMaterials.IndexOf(materials[j])
                });
            }
            return gltfMesh;
        }

        static gltfMorphTarget ExportMorphTarget(glTF gltf, int bufferIndex,
            Mesh mesh, int j,
            bool useSparseAccessorForMorphTarget)
        {
            var blendShapeVertices = mesh.vertices;
            var usePosition = blendShapeVertices != null && blendShapeVertices.Length > 0;

            var blendShpaeNormals = mesh.normals;
            var useNormal = usePosition && blendShpaeNormals != null && blendShpaeNormals.Length == blendShapeVertices.Length;

            var blendShapeTangents = mesh.tangents.Select(y => (Vector3)y).ToArray();
            var useTangent = usePosition && blendShapeTangents != null && blendShapeTangents.Length == blendShapeVertices.Length;

            var frameCount = mesh.GetBlendShapeFrameCount(j);
            mesh.GetBlendShapeFrameVertices(j, frameCount - 1, blendShapeVertices, blendShpaeNormals, null);

            var blendShapePositionAccessorIndex = -1;
            var blendShapeNormalAccessorIndex = -1;
            var blendShapeTangentAccessorIndex = -1;
            if (useSparseAccessorForMorphTarget)
            {
                var accessorCount = blendShapeVertices.Length;
                var sparseIndices = Enumerable.Range(0, blendShapeVertices.Length)
                    .Where(x =>
                    {
                        return
                        (usePosition && blendShapeVertices[x] != Vector3.zero)
                        || (useNormal && blendShpaeNormals[x] != Vector3.zero)
                        || (useTangent && blendShapeTangents[x] != Vector3.zero)
                        ;
                    })
                    .ToArray()
                    ;

                if (sparseIndices.Length == 0)
                {
                    usePosition = false;
                    useNormal = false;
                    useTangent = false;
                }
                /*
                var vertexSize = 12;
                if (useNormal) vertexSize += 12;
                if (useTangent) vertexSize += 24;
                var sparseBytes = (4 + vertexSize) * sparseIndices.Length;
                var fullBytes = (vertexSize) * blendShapeVertices.Length;
                Debug.LogFormat("Export sparse: {0}/{1}bytes({2}%)",
                    sparseBytes, fullBytes, (int)((float)sparseBytes / fullBytes)
                    );
                    */

                var sparseIndicesViewIndex = -1;
                if (usePosition)
                {
                    sparseIndicesViewIndex = gltf.ExtendBufferAndGetViewIndex(bufferIndex, sparseIndices);

                    blendShapeVertices = sparseIndices.Select(x => blendShapeVertices[x].ReverseZ()).ToArray();
                    blendShapePositionAccessorIndex = gltf.ExtendSparseBufferAndGetAccessorIndex(bufferIndex, accessorCount,
                        blendShapeVertices,
                        sparseIndices, sparseIndicesViewIndex,
                        glBufferTarget.ARRAY_BUFFER);
                }

                if (useNormal)
                {
                    blendShpaeNormals = sparseIndices.Select(x => blendShpaeNormals[x].ReverseZ()).ToArray();
                    blendShapeNormalAccessorIndex = gltf.ExtendSparseBufferAndGetAccessorIndex(bufferIndex, accessorCount,
                        blendShpaeNormals,
                        sparseIndices, sparseIndicesViewIndex,
                        glBufferTarget.ARRAY_BUFFER);
                }

                if (useTangent)
                {
                    blendShapeTangents = sparseIndices.Select(x => blendShapeTangents[x].ReverseZ()).ToArray();
                    blendShapeTangentAccessorIndex = gltf.ExtendSparseBufferAndGetAccessorIndex(bufferIndex, accessorCount,
                        blendShapeTangents, sparseIndices, sparseIndicesViewIndex,
                        glBufferTarget.ARRAY_BUFFER);
                }
            }
            else
            {
                for (int i = 0; i < blendShapeVertices.Length; ++i) blendShapeVertices[i] = blendShapeVertices[i].ReverseZ();
                if (usePosition)
                {
                    blendShapePositionAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex,
                        blendShapeVertices,
                        glBufferTarget.ARRAY_BUFFER);
                }

                if (useNormal)
                {
                    for (int i = 0; i < blendShpaeNormals.Length; ++i) blendShpaeNormals[i] = blendShpaeNormals[i].ReverseZ();
                    blendShapeNormalAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex,
                        blendShpaeNormals, 
                        glBufferTarget.ARRAY_BUFFER);
                }

                if (useTangent)
                {
                    for (int i = 0; i < blendShapeTangents.Length; ++i) blendShapeTangents[i] = blendShapeTangents[i].ReverseZ();
                    blendShapeTangentAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex,
                        blendShapeTangents, 
                        glBufferTarget.ARRAY_BUFFER);
                }
            }

            if(blendShapePositionAccessorIndex!=-1)
            {
                gltf.accessors[blendShapePositionAccessorIndex].min = blendShapeVertices.Aggregate(blendShapeVertices[0], (a, b) => new Vector3(Mathf.Min(a.x, b.x), Math.Min(a.y, b.y), Mathf.Min(a.z, b.z))).ToArray();
                gltf.accessors[blendShapePositionAccessorIndex].max = blendShapeVertices.Aggregate(blendShapeVertices[0], (a, b) => new Vector3(Mathf.Max(a.x, b.x), Math.Max(a.y, b.y), Mathf.Max(a.z, b.z))).ToArray();
            }

            return new gltfMorphTarget
            {
                POSITION = blendShapePositionAccessorIndex,
                NORMAL = blendShapeNormalAccessorIndex,
                TANGENT = blendShapeTangentAccessorIndex,
            };
        }

        static void ExportMeshes(glTF gltf, int bufferIndex,
            List<MeshWithRenderer> unityMeshes, List<Material> unityMaterials,
            bool useSparseAccessorForMorphTarget)
        {
            for (int i = 0; i < unityMeshes.Count; ++i)
            {
                var x = unityMeshes[i];
                var mesh = x.Mesh;
                var materials = x.Rendererer.sharedMaterials;

                var gltfMesh = ExportPrimitives(gltf, bufferIndex,
                    x.Rendererer.name,
                    mesh, materials, unityMaterials);

                for (int j = 0; j < mesh.blendShapeCount; ++j)
                {
                    var morphTarget = ExportMorphTarget(gltf, bufferIndex,
                        mesh, j,
                        useSparseAccessorForMorphTarget);

                    //
                    // first primitive has whole blendShape
                    //
                    var firstPrimitive = gltfMesh.primitives[0];
                    firstPrimitive.targets.Add(morphTarget);

                    firstPrimitive.extras.targetNames.Add(mesh.GetBlendShapeName(j));
                }

                gltf.meshes.Add(gltfMesh);
            }
        }

        public static Exported FromGameObject(glTF gltf, GameObject go, bool useSparseAccessorForMorphTarget=false)
        {
            var bytesBuffer = new ArrayByteBuffer(new byte[50 * 1024 * 1024]);
            var bufferIndex = gltf.AddBuffer(bytesBuffer);

            if (go.transform.childCount == 0)
            {
                throw new UniGLTFException("empty root GameObject required");
            }

            var unityNodes = go.transform.Traverse()
                .Skip(1) // exclude root object for the symmetry with the importer
                .ToList();

            #region Materials and Textures
            var unityMaterials = unityNodes.SelectMany(x => x.GetSharedMaterials()).Where(x => x != null).Distinct().ToList();
            var unityTextures = unityMaterials.SelectMany(x => x.GetTextures()).Where(x => x != null).Distinct().ToList();

            for (int i = 0; i < unityTextures.Count; ++i)
            {
                var texture = unityTextures[i];
                TextureIO.ExportTexture(gltf, bufferIndex, texture);
            }
           
            gltf.materials = unityMaterials.Select(x => MaterialIO.ExportMaterial(x, unityTextures)).ToList();
            #endregion

            #region Meshes
            var unityMeshes = unityNodes
                .Select(x => new MeshWithRenderer
                {
                    Mesh = x.GetSharedMesh(),
                    Rendererer = x.GetComponent<Renderer>(),
                })
                .Where(x =>
                {
                    if(x.Mesh == null)
                    {
                        return false;
                    }
                    if(x.Rendererer.sharedMaterials==null 
                    || x.Rendererer.sharedMaterials.Length==0)
                    {
                        return false;
                    }

                    return true;
                })
                .ToList();
            ExportMeshes(gltf, bufferIndex, unityMeshes, unityMaterials, useSparseAccessorForMorphTarget);
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
