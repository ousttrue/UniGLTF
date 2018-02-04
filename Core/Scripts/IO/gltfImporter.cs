using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


namespace UniGLTF
{
    public static class gltfImporter
    {
        const float FRAME_WEIGHT = 100.0f;

        public delegate void OnLoadCallback(IImporterContext ctx, string json, 
            Transform[] nodes,
            List<Mesh> meshes);

        static void SetSampler(Texture2D texture, glTFTextureSampler sampler)
        {
            if (texture == null)
            {
                return;
            }

            switch (sampler.wrapS)
            {
#if UNITY_2017_OR_NEWER
                case glWrap.CLAMP_TO_EDGE:
                    texture.wrapModeU = TextureWrapMode.Clamp;
                    break;

                case glWrap.REPEAT:
                    texture.wrapModeU = TextureWrapMode.Repeat;
                    break;

                case glWrap.MIRRORED_REPEAT:
                    texture.wrapModeU = TextureWrapMode.Mirror;
                    break;
#else
                case glWrap.CLAMP_TO_EDGE:
                case glWrap.MIRRORED_REPEAT:
                    texture.wrapMode = TextureWrapMode.Clamp;
                    break;

                case glWrap.REPEAT:
                    texture.wrapMode = TextureWrapMode.Repeat;
                    break;
#endif

                default:
                    throw new NotImplementedException();
            }

#if UNITY_2017_OR_NEWER
            switch (sampler.wrapT)
            {
                case glWrap.CLAMP_TO_EDGE:
                    texture.wrapModeV = TextureWrapMode.Clamp;
                    break;

                case glWrap.REPEAT:
                    texture.wrapModeV = TextureWrapMode.Repeat;
                    break;

                case glWrap.MIRRORED_REPEAT:
                    texture.wrapModeV = TextureWrapMode.Mirror;
                    break;

                default:
                    throw new NotImplementedException();
            }
#endif

            switch (sampler.magFilter)
            {
                case glFilter.NEAREST:
                    texture.filterMode = FilterMode.Point;
                    break;

                case glFilter.LINEAR:
                    texture.filterMode = FilterMode.Bilinear;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public struct TransformWithSkin
        {
            public Transform Transform;
            public int? SkinIndex;
        }

        public static GameObject Import(IImporterContext ctx, string json, ArraySegment<Byte> glbBinChunk
            , OnLoadCallback callback=null)
        {
            // exclude not gltf-2.0
            var parsed = json.ParseAsJson();
            try
            {
                if (parsed["asset"]["version"].GetString() != "2.0")
                {
                    Debug.LogWarningFormat("is not gltf-2.0: {0}", ctx.Path);
                    return null;
                }
            }
            catch (Exception)
            {
                Debug.LogWarningFormat("{0}: fail to parse json", ctx.Path);
                return null;
            }

            // parse json
            glTF gltf = null;
            try
            {
                gltf = JsonUtility.FromJson<glTF>(json);
            }
            catch (Exception)
            {
                Debug.LogWarningFormat("{0}: fail to parse json", ctx.Path);
                return null;
            }
            if (gltf == null)
            {
                Debug.LogWarningFormat("{0}: fail to parse json", ctx.Path);
                return null;
            }

            if (gltf.asset.version != "2.0")
            {
                Debug.LogWarningFormat("unknown gltf version {0}", gltf.asset.version);
                return null;
            }

            gltf.baseDir = Path.GetDirectoryName(ctx.Path);
            Debug.LogFormat("{0}: {1}", ctx.Path, gltf);

            foreach (var buffer in gltf.buffers)
            {
                buffer.OpenStorage(gltf.baseDir, glbBinChunk);
            }

            // textures
            var textures = ImportTextures(gltf)
                    .Select(x =>
                    {
                        var samplerIndex = gltf.textures[x.TextureIndex].sampler;
                        var sampler = gltf.samplers[samplerIndex];

                        if (x.Texture == null)
                        {
                            Debug.LogWarningFormat("May be import order, not yet texture is not imported. Later, manualy reimport {0}", ctx.Path);
                        }
                        else
                        {
                            SetSampler(x.Texture, sampler);

                            if (!x.IsAsset)
                            {
                                ctx.AddObjectToAsset(x.Texture.name, x.Texture);
                            }
                        }
                        return x.Texture;
                    })
                    .ToArray();

            // materials
            var materials = ImportMaterials(gltf, textures).ToArray();
            foreach (var material in materials)
            {
                ctx.AddObjectToAsset(material.name, material);
            }

            // meshes
            var meshes = gltf.meshes.Select((x, i) =>
            {
                var meshWithMaterials = ImportMesh(gltf, x, materials);
                var mesh = meshWithMaterials.Mesh;
                if (string.IsNullOrEmpty(mesh.name))
                {
                    mesh.name = string.Format("UniGLTF import#{0}", i);
                }

                ctx.AddObjectToAsset(mesh.name, mesh);

                return meshWithMaterials;
            }).ToArray();

            // nodes
            var _nodes = gltf.nodes.Select(x => ImportNode(x)).ToArray();

            var nodes = _nodes.Select((go, i) =>
            {
                if (string.IsNullOrEmpty(go.name))
                {
                    go.name = string.Format("node{0:000}", i);
                }

                var nodeWithSkin = new TransformWithSkin
                {
                    Transform = go.transform,
                };

                var node = gltf.nodes[i];

                //
                // build hierachy
                //
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        _nodes[child].transform.SetParent(_nodes[i].transform,
                            false // node has local transform
                            );
                    }
                }

                //
                // attach mesh
                //
                if (node.mesh != -1)
                {
                    var mesh = meshes[node.mesh];
                    if (mesh.Mesh.blendShapeCount == 0 && node.skin == -1)
                    {
                        // without blendshape and bone skinning
                        var filter = go.AddComponent<MeshFilter>();
                        filter.sharedMesh = mesh.Mesh;
                        var renderer = go.AddComponent<MeshRenderer>();
                        renderer.sharedMaterials = mesh.Materials;
                    }
                    else
                    {
                        var renderer = go.AddComponent<SkinnedMeshRenderer>();

                        if (node.skin != -1)
                        {
                            nodeWithSkin.SkinIndex = node.skin;
                        }

                        renderer.sharedMesh = mesh.Mesh;
                        renderer.sharedMaterials = mesh.Materials;
                    }
                }

                return nodeWithSkin;
            }).ToArray();

            //
            // fix node's coordinate. z-back to z-forward
            //
            var globalTransformMap = nodes.ToDictionary(x => x.Transform, x => new PosRot
            {
                Position = x.Transform.position,
                Rotation = x.Transform.rotation,
            });
            foreach (var x in gltf.rootnodes)
            {
                // fix nodes coordinate
                // reverse Z in global
                var t = nodes[x].Transform;
                //t.SetParent(root.transform, false);

                foreach (var transform in t.Traverse())
                {
                    var g = globalTransformMap[transform];
                    transform.position = g.Position.ReverseZ();
                    transform.rotation = g.Rotation.ReverseZ();
                }
            }

            // skinning
            foreach (var x in nodes)
            {
                var skinnedMeshRenderer = x.Transform.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    var mesh = skinnedMeshRenderer.sharedMesh;
                    if (x.SkinIndex.HasValue)
                    {
                        if (mesh == null) throw new Exception();
                        if (skinnedMeshRenderer == null) throw new Exception();

                        if (x.SkinIndex.Value < gltf.skins.Count)
                        {
                            var skin = gltf.skins[x.SkinIndex.Value];

                            skinnedMeshRenderer.sharedMesh = null;

                            var joints = skin.joints.Select(y => nodes[y].Transform).ToArray();
                            skinnedMeshRenderer.bones = joints;
                            //skinnedMeshRenderer.rootBone = nodes[skin.skeleton].Transform;

#if false
                            // https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html
                            var hipsParent = nodes[0].Transform;
                            var calculatedBindPoses = joints.Select(y => y.worldToLocalMatrix * hipsParent.localToWorldMatrix).ToArray();
                            mesh.bindposes = calculatedBindPoses;
#else
                            var bindPoses = gltf.GetArrayFromAccessor<Matrix4x4>(skin.inverseBindMatrices)
                                .Select(y => y.ReverseZ())
                                .ToArray()
                                ;
                            mesh.bindposes = bindPoses;
#endif

                            skinnedMeshRenderer.sharedMesh = mesh;
                        }
                    }
                }
            }

            var root = new GameObject("_root_");
            ctx.SetMainGameObject("root", root);

            foreach (var x in gltf.rootnodes)
            {
                // fix nodes coordinate
                // reverse Z in global
                var t = nodes[x].Transform;
                t.SetParent(root.transform, false);
            }

            // animation
            if (gltf.animations != null && gltf.animations.Any())
            {
                var clip = new AnimationClip();
                clip.name = ANIMATION_NAME;
                clip.ClearCurves();

                ImportAnimation(root.transform, clip, gltf.animations, nodes.Select(x => x.Transform).ToArray(), gltf);

                clip.legacy = true;
                clip.name = "legacy";
                clip.wrapMode = WrapMode.Loop;
                var animation = root.AddComponent<Animation>();
                animation.clip = clip;

                ctx.AddObjectToAsset(ANIMATION_NAME, clip);
            }

            if (callback != null)
            {
                callback(ctx, json, 
                    nodes.Select(x => x.Transform).ToArray(),
                    meshes.Select(x => x.Mesh).ToList()
                    );
            }

            Debug.LogFormat("Import completed");

            return root;
        }

        #region Import
        static IEnumerable<TextureWithIsAsset> ImportTextures(glTF gltf)
        {
            if (gltf.textures == null)
            {
                return new TextureWithIsAsset[] { };
            }
            else
            {
                return gltf.textures.Select(x => ImportTexture(gltf, x.source));
            }
        }

        public struct TextureWithIsAsset
        {
            public int TextureIndex;
            public Texture2D Texture;
            public bool IsAsset;
        }

        static TextureWithIsAsset ImportTexture(glTF gltf, int index)
        {
            var image = gltf.images[index];
            if (string.IsNullOrEmpty(image.uri))
            {
                // use buffer view
                var texture = new Texture2D(2, 2);
                //texture.name = string.Format("texture#{0:00}", i++);
                var byteSegment = gltf.GetViewBytes(image.bufferView);
                var bytes = byteSegment.Array.Skip(byteSegment.Offset).Take(byteSegment.Count).ToArray();
                texture.LoadImage(bytes, true);
                return new TextureWithIsAsset { TextureIndex = index, Texture = texture, IsAsset = false };
            }
#if UNITY_EDITOR
            else if (gltf.baseDir.StartsWith("Assets/"))
            {
                // local folder
                var path = Path.Combine(gltf.baseDir, image.uri);
                //Debug.LogFormat("load texture: {0}", path);

                var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                return new TextureWithIsAsset { TextureIndex = index, Texture = texture, IsAsset = true };
            }
#endif
            else
            {
                // external
                var path = Path.Combine(gltf.baseDir, image.uri);
                var bytes = File.ReadAllBytes(path);

                var texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                return new TextureWithIsAsset { TextureIndex = index, Texture = texture, IsAsset = true };
            }
        }

        /// <summary>
        /// StandardShader vaiables
        /// 
        /// _Color
        /// _MainTex
        /// _Cutoff
        /// _Glossiness
        /// _Metallic
        /// _MetallicGlossMap
        /// _BumpScale
        /// _BumpMap
        /// _Parallax
        /// _ParallaxMap
        /// _OcclusionStrength
        /// _OcclusionMap
        /// _EmissionColor
        /// _EmissionMap
        /// _DetailMask
        /// _DetailAlbedoMap
        /// _DetailNormalMapScale
        /// _DetailNormalMap
        /// _UVSec
        /// _EmissionScaleUI
        /// _EmissionColorUI
        /// _Mode
        /// _SrcBlend
        /// _DstBlend
        /// _ZWrite
        /// </summary>
        /// <param name="gltf"></param>
        /// <param name="textures"></param>
        /// <returns></returns>
        static IEnumerable<Material> ImportMaterials(glTF gltf, Texture2D[] textures)
        {
            var shader = Shader.Find("Standard");
            if (gltf.materials == null || !gltf.materials.Any())
            {
                var material = new Material(shader);
                return new Material[] { material };
            }
            else
            {
                return gltf.materials.Select((x, i) =>
                {
                    var material = new Material(shader);

                    if (string.IsNullOrEmpty(x.name))
                    {
                        material.name = string.Format("material:{0:00}", i);
                    }
                    else
                    {
                        material.name = x.name;
                    }

                    if (x.pbrMetallicRoughness != null)
                    {
                        if (x.pbrMetallicRoughness.baseColorFactor != null)
                        {
                            var color = x.pbrMetallicRoughness.baseColorFactor;
                            material.color = new Color(color[0], color[1], color[2], color[3]);
                        }

                        if (x.pbrMetallicRoughness.baseColorTexture.index != -1)
                        {
                            var texture = textures[x.pbrMetallicRoughness.baseColorTexture.index];
                            material.mainTexture = texture;
                        }

                        if (x.pbrMetallicRoughness.metallicRoughnessTexture.index != -1)
                        {
                            var texture = textures[x.pbrMetallicRoughness.metallicRoughnessTexture.index];
                            material.SetTexture("_MetallicGlossMap", texture);
                        }
                    }

                    if (x.normalTexture.index != -1)
                    {
                        var texture = textures[x.normalTexture.index];
                        material.SetTexture("_BumpMap", texture);
                    }

                    if (x.occlusionTexture.index != -1)
                    {
                        var texture = textures[x.occlusionTexture.index];
                        material.SetTexture("_OcclusionMap", texture);
                    }

                    if (x.emissiveFactor != null)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", new Color(x.emissiveFactor[0], x.emissiveFactor[1], x.emissiveFactor[2]));
                    }

                    if (x.emissiveTexture.index != -1)
                    {
                        material.EnableKeyword("_EMISSION");
                        var texture = textures[x.emissiveTexture.index];
                        material.SetTexture("_EmissionMap", texture);
                    }

                    return material;
                });
            }
        }

        [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Float4
        {
            public float x;
            public float y;
            public float z;
            public float w;

            public Float4 One()
            {
                var sum = x + y + z + w;
                var f = 1.0f / sum;
                return new Float4
                {
                    x = x * f,
                    y = y * f,
                    z = z * f,
                    w = w * f,
                };
            }
        }

        static MeshWithMaterials ImportMesh(glTF gltf, glTFMesh gltfMesh, Material[] materials)
        {
            glTFAttributes lastAttributes = null;
            var sharedAttributes = true;
            foreach (var prim in gltfMesh.primitives)
            {
                if(lastAttributes!=null && !prim.attributes.Equals(lastAttributes))
                {
                    sharedAttributes = false;
                    break;
                }
                lastAttributes = prim.attributes;
            }

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var boneWeights = new List<BoneWeight>();
            var subMeshes = new List<int[]>();
            var materialIndices = new List<int>();
            BlendShape[] blendShapes = null;

            if (sharedAttributes)
            {
                {
                    var prim = gltfMesh.primitives.First();
                    positions.AddRange(gltf.GetArrayFromAccessor<Vector3>(prim.attributes.POSITION).Select(x => x.ReverseZ()));

                    // normal
                    if (prim.attributes.NORMAL != -1)
                    {
                        normals.AddRange(gltf.GetArrayFromAccessor<Vector3>(prim.attributes.NORMAL).Select(x => x.ReverseZ()));
                    }

                    // uv
                    if (prim.attributes.TEXCOORD_0 != -1)
                    {
                        uv.AddRange(gltf.GetArrayFromAccessor<Vector2>(prim.attributes.TEXCOORD_0).Select(x => x.ReverseY()));
                    }
                    else
                    {
                        // for inconsistent attributes in primitives
                        uv.AddRange(new Vector2[positions.Count]);
                    }

                    // skin
                    if (prim.attributes.JOINTS_0 != -1 && prim.attributes.WEIGHTS_0 != -1)
                    {
                        var joints0 = gltf.GetArrayFromAccessor<UShort4>(prim.attributes.JOINTS_0); // uint4
                        var weights0 = gltf.GetArrayFromAccessor<Float4>(prim.attributes.WEIGHTS_0).Select(x => x.One()).ToArray();

                        for (int j = 0; j < joints0.Length; ++j)
                        {
                            var bw = new BoneWeight();

                            bw.boneIndex0 = joints0[j].x;
                            bw.weight0 = weights0[j].x;

                            bw.boneIndex1 = joints0[j].y;
                            bw.weight1 = weights0[j].y;

                            bw.boneIndex2 = joints0[j].z;
                            bw.weight2 = weights0[j].z;

                            bw.boneIndex3 = joints0[j].w;
                            bw.weight3 = weights0[j].w;

                            boneWeights.Add(bw);
                        }
                    }

                    // blendshape
                    if (prim.targets != null && prim.targets.Count > 0)
                    {
                        if (blendShapes == null)
                        {
                            blendShapes = prim.targets.Select((x, i) => new BlendShape(i.ToString())).ToArray();
                        }
                        for (int i = 0; i < prim.targets.Count; ++i)
                        {
                            //var name = string.Format("target{0}", i++);
                            var primTarget = prim.targets[i];
                            var blendShape = blendShapes[i];

                            if (primTarget.POSITION != -1)
                            {
                                blendShape.Positions.AddRange(
                                    gltf.GetArrayFromAccessor<Vector3>(primTarget.POSITION).Select(x => x.ReverseZ()).ToArray());
                            }
                            if (primTarget.NORMAL != -1)
                            {
                                blendShape.Normals.AddRange(
                                    gltf.GetArrayFromAccessor<Vector3>(primTarget.NORMAL).Select(x => x.ReverseZ()).ToArray());
                            }
#if false
                        if (primTarget.TANGEN!=-1)
                        {
                            blendShape.Tangents = GetBuffer<Vector3>(targetJson["TANGENT"].GetInt32())/*.Select(ReverseZ).ToArray()*/;
                        }
#endif
                        }
                    }
                }

                foreach (var prim in gltfMesh.primitives)
                {
                    var indices =gltf.GetIndices(prim.indices).Select(x => x).ToArray();
                    subMeshes.Add(indices);

                    // material
                    materialIndices.Add(prim.material);
                }
            }
            else
            {
                var targets = gltfMesh.primitives[0].targets;
                for (int i = 1; i < gltfMesh.primitives.Count; ++i)
                {
                    if (!gltfMesh.primitives[i].targets.SequenceEqual(targets))
                    {
                        throw new NotImplementedException(string.Format("diffirent targets: {0} with {1}",
                            gltfMesh.primitives[i],
                            targets));
                    }
                }

                foreach (var prim in gltfMesh.primitives)
                {
                    var indexOffset = positions.Count;
                    var indexBuffer = prim.indices;

                    var positionCount = positions.Count;
                    positions.AddRange(gltf.GetArrayFromAccessor<Vector3>(prim.attributes.POSITION).Select(x => x.ReverseZ()));
                    positionCount = positions.Count - positionCount;

                    // normal
                    if (prim.attributes.NORMAL != -1)
                    {
                        normals.AddRange(gltf.GetArrayFromAccessor<Vector3>(prim.attributes.NORMAL).Select(x => x.ReverseZ()));
                    }

                    // uv
                    if (prim.attributes.TEXCOORD_0 != -1)
                    {
                        uv.AddRange(gltf.GetArrayFromAccessor<Vector2>(prim.attributes.TEXCOORD_0).Select(x => x.ReverseY()));
                    }
                    else
                    {
                        // for inconsistent attributes in primitives
                        uv.AddRange(new Vector2[positionCount]);
                    }

                    // skin
                    if (prim.attributes.JOINTS_0 != -1 && prim.attributes.WEIGHTS_0 != -1)
                    {
                        var joints0 = gltf.GetArrayFromAccessor<UShort4>(prim.attributes.JOINTS_0); // uint4
                        var weights0 = gltf.GetArrayFromAccessor<Float4>(prim.attributes.WEIGHTS_0).Select(x => x.One()).ToArray();

                        for (int j = 0; j < joints0.Length; ++j)
                        {
                            var bw = new BoneWeight();

                            bw.boneIndex0 = joints0[j].x;
                            bw.weight0 = weights0[j].x;

                            bw.boneIndex1 = joints0[j].y;
                            bw.weight1 = weights0[j].y;

                            bw.boneIndex2 = joints0[j].z;
                            bw.weight2 = weights0[j].z;

                            bw.boneIndex3 = joints0[j].w;
                            bw.weight3 = weights0[j].w;

                            boneWeights.Add(bw);
                        }
                    }

                    // blendshape
                    if (prim.targets != null && prim.targets.Count > 0)
                    {
                        if (blendShapes == null)
                        {
                            blendShapes = prim.targets.Select((x, i) => new BlendShape(i.ToString())).ToArray();
                        }
                        for (int i = 0; i < prim.targets.Count; ++i)
                        {
                            //var name = string.Format("target{0}", i++);
                            var primTarget = prim.targets[i];
                            var blendShape = blendShapes[i];

                            if (primTarget.POSITION != -1)
                            {
                                blendShape.Positions.AddRange(
                                    gltf.GetArrayFromAccessor<Vector3>(primTarget.POSITION).Select(x => x.ReverseZ()).ToArray());
                            }
                            if (primTarget.NORMAL != -1)
                            {
                                blendShape.Normals.AddRange(
                                    gltf.GetArrayFromAccessor<Vector3>(primTarget.NORMAL).Select(x => x.ReverseZ()).ToArray());
                            }
#if false
                        if (primTarget.TANGEN!=-1)
                        {
                            blendShape.Tangents = GetBuffer<Vector3>(targetJson["TANGENT"].GetInt32())/*.Select(ReverseZ).ToArray()*/;
                        }
#endif
                        }
                    }

                    var indices =
                     (indexBuffer >= 0)
                     ? gltf.GetIndices(indexBuffer).Select(x => x + indexOffset).ToArray()
                     : TriangleUtil.FlipTriangle(Enumerable.Range(0, positions.Count)).ToArray() // without index array
                     ;
                    subMeshes.Add(indices);

                    // material
                    materialIndices.Add(prim.material);
                }
            }
            if (!materialIndices.Any())
            {
                materialIndices.Add(0);
            }

            //Debug.Log(prims.ToJson());
            var mesh = new Mesh();
            mesh.name = gltfMesh.name;

            if (positions.Count > UInt16.MaxValue)
            {
#if UNITY_2017_3_OR_NEWER
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
#else
                Debug.LogWarningFormat("vertices {0} exceed 65535. not implemented. Unity2017.3 supports large mesh", positions.Count);
#endif
            }

            mesh.vertices = positions.ToArray();
            if (normals.Any())
            {
                mesh.normals = normals.ToArray();
            }
            else
            {
                mesh.RecalculateNormals();
            }
            if (uv.Any())
            {
                mesh.uv = uv.ToArray();
            }
            if (boneWeights.Any())
            {
                mesh.boneWeights = boneWeights.ToArray();
            }
            mesh.subMeshCount = subMeshes.Count;
            for (int i = 0; i < subMeshes.Count; ++i)
            {
                mesh.SetTriangles(subMeshes[i], i);
            }
            mesh.RecalculateNormals();
            var result = new MeshWithMaterials
            {
                Mesh = mesh,
                Materials = materialIndices.Select(x => materials[x]).ToArray()
            };

            if (blendShapes != null)
            {
                foreach (var blendShape in blendShapes)
                {
                    if (blendShape.Positions.Count > 0)
                    {
                        if (blendShape.Positions.Count == mesh.vertexCount)
                        {
                            mesh.AddBlendShapeFrame(blendShape.Name, FRAME_WEIGHT,
                                blendShape.Positions.ToArray(),
                                normals.Count == mesh.vertexCount ? blendShape.Normals.ToArray() : null,
                                null
                                );
                        }
                        else
                        {
                            Debug.LogWarningFormat("May be partial primitive has blendShape. Rquire separete mesh or extend blend shape, but not implemented: {0}", blendShape.Name);
                        }
                    }
                }
            }

            return result;
        }

        static GameObject ImportNode(glTFNode node)
        {
            var go = new GameObject(node.name);

            //
            // transform
            //
            if (node.translation != null && node.translation.Length > 0)
            {
                go.transform.localPosition = new Vector3(
                    node.translation[0],
                    node.translation[1],
                    node.translation[2]);
            }
            if (node.rotation != null && node.rotation.Length > 0)
            {
                go.transform.localRotation = new Quaternion(
                    node.rotation[0],
                    node.rotation[1],
                    node.rotation[2],
                    node.rotation[3]);
            }
            if (node.scale != null && node.scale.Length > 0)
            {
                go.transform.localScale = new Vector3(
                    node.scale[0],
                    node.scale[1],
                    node.scale[2]);
            }
            if (node.matrix != null && node.matrix.Length > 0)
            {
                var values = node.matrix;
#if UNITY_2017_OR_NEWER
                var col0 = new Vector4(values[0], values[1], values[2], values[3]);
                var col1 = new Vector4(values[4], values[5], values[6], values[7]);
                var col2 = new Vector4(values[8], values[9], values[10], values[11]);
                var col3 = new Vector4(values[12], values[13], values[14], values[15]);
                var m = new Matrix4x4(col0, col1, col2, col3);
                go.transform.localRotation = m.rotation;
                go.transform.localPosition = m.GetColumn(3);
#else
                // https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/
                var m = new Matrix4x4();
                m.m00 = values[0];
                m.m10 = values[1];
                m.m20 = values[2];
                m.m30 = values[3];
                m.m01 = values[4];
                m.m11 = values[5];
                m.m21 = values[6];
                m.m31 = values[7];
                m.m02 = values[8];
                m.m12 = values[9];
                m.m22 = values[10];
                m.m32 = values[11];
                m.m03 = values[12];
                m.m13 = values[13];
                m.m23 = values[14];
                m.m33 = values[15];
                go.transform.localRotation = m.ExtractRotation();
                go.transform.localPosition = m.ExtractPosition();
#endif
            }
            return go;
        }

        public static string ANIMATION_NAME = "animation";

        public static T GetOrCreate<T>(UnityEngine.Object[] assets, string name, Func<T> create) where T : UnityEngine.Object
        {
            var found = assets.FirstOrDefault(x => x.name == name);
            if (found != null)
            {
                return found as T;
            }
            return create();
        }

        public static void ImportAnimation(Transform root, AnimationClip clip, List<glTFAnimation> animations, Transform[] nodes, glTF gltf)
        {
            for (int i = 0; i < animations.Count; ++i)
            {
                var animation = animations[i];
                if (string.IsNullOrEmpty(animation.name))
                {
                    animation.name = string.Format("animation:{0}", i);
                }

                foreach (var y in animation.channels)
                {
                    var targetTransform = nodes[y.target.node];
                    var relativePath = targetTransform.RelativePathFrom(root);
                    switch (y.target.path)
                    {
                        case glTFAnimationTarget.PATH_TRANSLATION:
                            {
                                var curveX = new AnimationCurve();
                                var curveY = new AnimationCurve();
                                var curveZ = new AnimationCurve();

                                var sampler = animation.samplers[y.sampler];
                                var input = gltf.GetArrayFromAccessor<float>(sampler.input);
                                var output = gltf.GetArrayFromAccessor<Vector3>(sampler.output);
                                for (int j = 0; j < input.Length; ++j)
                                {
                                    var time = input[j];
                                    var pos = output[j].ReverseZ();
                                    curveX.AddKey(time, pos.x);
                                    curveY.AddKey(time, pos.y);
                                    curveZ.AddKey(time, pos.z);
                                }

                                clip.SetCurve(relativePath, typeof(Transform), "localPosition.x", curveX);
                                clip.SetCurve(relativePath, typeof(Transform), "localPosition.y", curveY);
                                clip.SetCurve(relativePath, typeof(Transform), "localPosition.z", curveZ);
                            }
                            break;

                        case glTFAnimationTarget.PATH_ROTATION:
                            {
                                var curveX = new AnimationCurve();
                                var curveY = new AnimationCurve();
                                var curveZ = new AnimationCurve();
                                var curveW = new AnimationCurve();

                                var sampler = animation.samplers[y.sampler];
                                var input = gltf.GetArrayFromAccessor<float>(sampler.input);
                                var output = gltf.GetArrayFromAccessor<Quaternion>(sampler.output);
                                for (int j = 0; j < input.Length; ++j)
                                {
                                    var time = input[j];
                                    var rot = output[j].ReverseZ();
                                    curveX.AddKey(time, rot.x);
                                    curveY.AddKey(time, rot.y);
                                    curveZ.AddKey(time, rot.z);
                                    curveW.AddKey(time, rot.w);
                                }

                                clip.SetCurve(relativePath, typeof(Transform), "localRotation.x", curveX);
                                clip.SetCurve(relativePath, typeof(Transform), "localRotation.y", curveY);
                                clip.SetCurve(relativePath, typeof(Transform), "localRotation.z", curveZ);
                                clip.SetCurve(relativePath, typeof(Transform), "localRotation.w", curveW);
                            }
                            break;

                        case glTFAnimationTarget.PATH_SCALE:
                            {
                                var curveX = new AnimationCurve();
                                var curveY = new AnimationCurve();
                                var curveZ = new AnimationCurve();

                                var sampler = animation.samplers[y.sampler];
                                var input = gltf.GetArrayFromAccessor<float>(sampler.input);
                                var output = gltf.GetArrayFromAccessor<Vector3>(sampler.output);
                                for (int j = 0; j < input.Length; ++j)
                                {
                                    var time = input[j];
                                    var scale = output[j];
                                    curveX.AddKey(time, scale.x);
                                    curveY.AddKey(time, scale.y);
                                    curveZ.AddKey(time, scale.z);
                                }

                                clip.SetCurve(relativePath, typeof(Transform), "localScale.x", curveX);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.y", curveY);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.z", curveZ);
                            }
                            break;

                        case glTFAnimationTarget.PATH_WEIGHT:
                            {
                                var node = gltf.nodes[y.target.node];
                                var mesh = gltf.meshes[node.mesh];
                                for (int k = 0; k < mesh.weights.Length; ++k)
                                {
                                    //var weight = mesh.weights[k];
                                    var curve = new AnimationCurve();
                                    var sampler = animation.samplers[y.sampler];
                                    var input = gltf.GetArrayFromAccessor<float>(sampler.input);
                                    var output = gltf.GetArrayFromAccessor<float>(sampler.output);
                                    for (int j = 0; j < input.Length; ++j)
                                    {
                                        curve.AddKey(input[j], output[j]);
                                    }

                                    clip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), "blendShape." + k, curve);
                                }
                            }
                            break;
                    }
                }
            }
        }
#endregion
    }
}
