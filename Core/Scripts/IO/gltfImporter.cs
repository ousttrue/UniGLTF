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

        public static void SetSampler(Texture2D texture, glTFTextureSampler sampler)
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
        public static CreateMaterialFunc CreateMaterialFuncFromShader(Shader shader)
        {
            if (shader == null) return null;

            return (ctx, i) =>
             {
                 var material = new Material(shader);
                 material.name = string.Format("material:{0:00}", i);

                 if (i >= 0 && i < ctx.GLTF.materials.Count)
                 {
                     var x = ctx.GLTF.materials[i];
                     if (x != null)
                     {
                         if (!string.IsNullOrEmpty(x.name))
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
                                 var texture = ctx.Textures[x.pbrMetallicRoughness.baseColorTexture.index];
                                 material.mainTexture = texture.Texture;
                             }

                             if (x.pbrMetallicRoughness.metallicRoughnessTexture.index != -1)
                             {
                                 material.EnableKeyword("_METALLICGLOSSMAP");
                                 var texture = ctx.Textures[x.pbrMetallicRoughness.metallicRoughnessTexture.index];
                                 material.SetTexture("_MetallicGlossMap", texture.GetMetallicRoughnessOcclusionConverted());
                             }
                         }

                         if (x.normalTexture.index != -1)
                         {
                             material.EnableKeyword("_NORMALMAP");
                             var texture = ctx.Textures[x.normalTexture.index];
                             material.SetTexture("_BumpMap", texture.Texture);
                         }

                         if (x.occlusionTexture.index != -1)
                         {
                             var texture = ctx.Textures[x.occlusionTexture.index];
                             material.SetTexture("_OcclusionMap", texture.GetMetallicRoughnessOcclusionConverted());
                         }

                         if (x.emissiveFactor != null
                             || x.emissiveTexture.index != -1)
                         {
                             material.EnableKeyword("_EMISSION");
                             material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                             if (x.emissiveFactor != null)
                             {
                                 material.SetColor("_EmissionColor", new Color(x.emissiveFactor[0], x.emissiveFactor[1], x.emissiveFactor[2]));
                             }

                             if (x.emissiveTexture.index != -1)
                             {
                                 var texture = ctx.Textures[x.emissiveTexture.index];
                                 material.SetTexture("_EmissionMap", texture.Texture);
                             }
                         }
                     }
                 }

                 return material;
             };
        }

        public static void Import<T>(ImporterContext ctx, ArraySegment<Byte> glbBinChunk) where T : glTF
        {
            // exclude not gltf-2.0
            var parsed = ctx.Json.ParseAsJson();
            try
            {
                if (parsed["asset"]["version"].GetString() != "2.0")
                {
                    throw new UniGLTFException("is not gltf-2.0: {0}", ctx.Path);
                }
            }
            catch (Exception)
            {
                throw new UniGLTFException("{0}: fail to parse json", ctx.Path);
            }

            // parse json
            try
            {
                ctx.GLTF = JsonUtility.FromJson<T>(ctx.Json);
            }
            catch (Exception)
            {
                throw new UniGLTFException("{0}: fail to parse json", ctx.Path);
            }
            if (ctx.GLTF == null)
            {
                throw new UniGLTFException("{0}: fail to parse json", ctx.Path);
            }

            if (ctx.GLTF.asset.version != "2.0")
            {
                throw new UniGLTFException("unknown gltf version {0}", ctx.GLTF.asset.version);
            }

            // parepare byte buffer
            ctx.GLTF.baseDir = Path.GetDirectoryName(ctx.Path);
            foreach (var buffer in ctx.GLTF.buffers)
            {
                buffer.OpenStorage(ctx.GLTF.baseDir, glbBinChunk);
            }

            // textures
            ctx.Textures.AddRange(ImportTextures(ctx.GLTF)
                    .Select(x =>
                    {
                        var samplerIndex = ctx.GLTF.textures[x.TextureIndex].sampler;
                        var sampler = ctx.GLTF.samplers[samplerIndex];

                        if (x.Texture == null)
                        {
                            Debug.LogWarningFormat("May be import order, not yet texture is not imported. Later, manualy reimport {0}", ctx.Path);
                        }
                        else
                        {
                            SetSampler(x.Texture, sampler);
                        }
                        return x;
                    }));

            // materials
            if (ctx.CreateMaterial == null)
            {
                ctx.CreateMaterial = CreateMaterialFuncFromShader(Shader.Find("Standard"));
            }
            if (ctx.GLTF.materials == null || !ctx.GLTF.materials.Any())
            {
                ctx.Materials.Add(ctx.CreateMaterial(ctx, 0));
            }
            else
            {
                for (int i = 0; i < ctx.GLTF.materials.Count; ++i)
                {
                    ctx.Materials.Add(ctx.CreateMaterial(ctx, i));
                }
            }

            // meshes
            if (ctx.GLTF.meshes
                .SelectMany(x => x.primitives)
                .Any(x => x.extensions.KHR_draco_mesh_compression != null))
            {
                throw new UniGLTFException("draco is not supported");
            }

            ctx.Meshes.AddRange(ctx.GLTF.meshes.Select((x, i) =>
            {
                var meshWithMaterials = ImportMesh(ctx, i);
                var mesh = meshWithMaterials.Mesh;
                if (string.IsNullOrEmpty(mesh.name))
                {
                    mesh.name = string.Format("UniGLTF import#{0}", i);
                }
                return meshWithMaterials;
            }));

            // nodes
            ctx.Nodes.AddRange(ctx.GLTF.nodes.Select(x => ImportNode(x).transform));

            var nodes = ctx.Nodes.Select((x, i) => BuildHierarchy(ctx, i)).ToList();

            gltfImporter.FixCoordinate(ctx, nodes);

            // skinning
            for (int i = 0; i < nodes.Count; ++i)
            {
                gltfImporter.SetupSkinning(ctx, nodes, i);
            }

            // connect root
            ctx.Root = new GameObject("_root_");
            foreach (var x in ctx.GLTF.rootnodes)
            {
                var t = nodes[x].Transform;
                t.SetParent(ctx.Root.transform, false);
            }

            ImportAnimation(ctx);

            //Debug.LogFormat("Import {0}", ctx.Path);
        }

        static void ImportAnimation(ImporterContext ctx)
        {
            // animation
            if (ctx.GLTF.animations != null && ctx.GLTF.animations.Any())
            {
                ctx.Animation = new AnimationClip();
                ctx.Animation.name = ANIMATION_NAME;
                ctx.Animation.ClearCurves();

                ImportAnimation(ctx, ctx.Animation);

                ctx.Animation.legacy = true;
                ctx.Animation.name = "legacy";
                ctx.Animation.wrapMode = WrapMode.Loop;
                var animation = ctx.Root.AddComponent<Animation>();
                animation.clip = ctx.Animation;
            }
        }

        #region Import
        static IEnumerable<TextureItem> ImportTextures(glTF gltf)
        {
            if (gltf.textures == null)
            {
                return new TextureItem[] { };
            }
            else
            {
                return gltf.textures.Select(x => ImportTexture(gltf, x.source));
            }
        }

        public static TextureItem ImportTexture(glTF gltf, int index)
        {
            var x = _ImportTexture(gltf, index);
            if (x.Texture == null)
            {
                throw new UniGLTFException("May be import order, not yet texture is not imported. Later, manualy reimport");
            }

            // set sampler
            var samplerIndex = gltf.textures[x.TextureIndex].sampler;
            var sampler = gltf.GetSampler(samplerIndex);
            gltfImporter.SetSampler(x.Texture, sampler);

            return x;
        }
        static TextureItem _ImportTexture(glTF gltf, int index)
        {
            var image = gltf.images[index];
            if (string.IsNullOrEmpty(image.uri))
            {
                // use buffer view
                var texture = new Texture2D(2, 2);
                texture.name = string.IsNullOrEmpty(image.extra.name) ? string.Format("buffer#{0:00}", index) : image.extra.name;
                var byteSegment = gltf.GetViewBytes(image.bufferView);

                var bytes = byteSegment.ToArray();

                texture.LoadImage(bytes, true);
                return new TextureItem(texture, index, false);
            }
            else if (image.uri.StartsWith("data:"))
            {
                // embeded
                var bytes = UriByteBuffer.ReadEmbeded(image.uri);
                var texture = new Texture2D(2, 2);
                texture.name = string.IsNullOrEmpty(image.extra.name) ? "embeded" : image.extra.name;
                texture.LoadImage(bytes);
                return new TextureItem(texture, index, false);
            }
#if UNITY_EDITOR
            else if (gltf.baseDir.StartsWith("Assets/"))
            {
                // local folder
                var path = Path.Combine(gltf.baseDir, image.uri);
                UnityEditor.AssetDatabase.ImportAsset(path);
                var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                texture.name = string.IsNullOrEmpty(image.extra.name) ? Path.GetFileNameWithoutExtension(path) : image.extra.name;
                return new TextureItem(texture, index, true);
            }
#endif
            else
            {
                // external
                var path = Path.Combine(gltf.baseDir, image.uri);
                var bytes = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2);
                texture.name = string.IsNullOrEmpty(image.extra.name) ? Path.GetFileNameWithoutExtension(path) : image.extra.name;
                texture.LoadImage(bytes);
                return new TextureItem(texture, index, false);
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

        public class MeshContext
        {
            public string name;
            public Vector3[] positions;
            public Vector3[] normals;
            public Vector4[] tangents;
            public Vector2[] uv;
            public List<BoneWeight> boneWeights=new List<BoneWeight>();
            public List<int[]> subMeshes = new List<int[]>();
            public List<int> materialIndices = new List<int>();
            public List<BlendShape> blendShapes = new List<BlendShape>();
        }

        public static MeshWithMaterials ImportMesh(ImporterContext ctx, int meshIndex)
        {
            var meshContext = ReadMesh(ctx, meshIndex);
            return BuildMesh(ctx, meshContext);
        }

        public static MeshContext ReadMesh(ImporterContext ctx, int meshIndex)
        {
            var gltfMesh = ctx.GLTF.meshes[meshIndex];
            glTFAttributes lastAttributes = null;
            var sharedAttributes = true;
            foreach (var prim in gltfMesh.primitives)
            {
                if (lastAttributes != null && !prim.attributes.Equals(lastAttributes))
                {
                    sharedAttributes = false;
                    break;
                }
                lastAttributes = prim.attributes;
            }

            var meshContext = sharedAttributes
                ? _ImportMeshSharingVertexBuffer(ctx, gltfMesh)
                : _ImportMeshIndependentVertexBuffer(ctx, gltfMesh)
                ;
            meshContext.name = gltfMesh.name;
            if (string.IsNullOrEmpty(meshContext.name))
            {
                meshContext.name = string.Format("UniGLTF import#{0}", meshIndex);
            }

            return meshContext;
        }

        public static MeshWithMaterials BuildMesh(ImporterContext ctx, MeshContext meshContext)
        {        
            if (!meshContext.materialIndices.Any())
            {
                meshContext.materialIndices.Add(0);
            }

            //Debug.Log(prims.ToJson());
            var mesh = new Mesh();
            mesh.name = meshContext.name;

            if (meshContext.positions.Length > UInt16.MaxValue)
            {
#if UNITY_2017_3_OR_NEWER
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
#else
                Debug.LogWarningFormat("vertices {0} exceed 65535. not implemented. Unity2017.3 supports large mesh", 
                    meshContext.positions.Length);
#endif
            }

            mesh.vertices=meshContext.positions;
            if (meshContext.normals!=null && meshContext.normals.Length>0)
            {
                mesh.normals=meshContext.normals;
            }
            else
            {
                mesh.RecalculateNormals();
            }
            if (meshContext.tangents!=null && meshContext.tangents.Length>0)
            {
                mesh.tangents=meshContext.tangents;
            }
            else
            {
                mesh.RecalculateTangents();
            }
            if (meshContext.uv!=null && meshContext.uv.Length>0)
            {
                mesh.uv=meshContext.uv;
            }
            if (meshContext.boneWeights!=null && meshContext.boneWeights.Count>0)
            {
                mesh.boneWeights = meshContext.boneWeights.ToArray();
            }
            mesh.subMeshCount = meshContext.subMeshes.Count;
            for (int i = 0; i < meshContext.subMeshes.Count; ++i)
            {
                mesh.SetTriangles(meshContext.subMeshes[i], i);
            }
            var result = new MeshWithMaterials
            {
                Mesh = mesh,
                Materials = meshContext.materialIndices.Select(x => ctx.Materials[x]).ToArray()
            };

            if (meshContext.blendShapes != null)
            {
                foreach (var blendShape in meshContext.blendShapes)
                {
                    if (blendShape.Positions.Count > 0)
                    {
                        if (blendShape.Positions.Count == mesh.vertexCount)
                        {
                            mesh.AddBlendShapeFrame(blendShape.Name, FRAME_WEIGHT,
                                blendShape.Positions.ToArray(),
                                (meshContext.normals!=null && meshContext.normals.Length == mesh.vertexCount) ? blendShape.Normals.ToArray() : null,
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

        // multiple submMesh is not sharing a VertexBuffer.
        // each subMesh use a independent VertexBuffer.
        private static MeshContext _ImportMeshIndependentVertexBuffer(ImporterContext ctx, glTFMesh gltfMesh)
        {
            //Debug.LogWarning("_ImportMeshIndependentVertexBuffer");

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

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uv = new List<Vector2>();
            var meshContext = new MeshContext();
            foreach (var prim in gltfMesh.primitives)
            {
                var indexOffset = positions.Count;
                var indexBuffer = prim.indices;

                var positionCount = positions.Count;
                positions.AddRange(ctx.GLTF.GetArrayFromAccessor<Vector3>(prim.attributes.POSITION).Select(x => x.ReverseZ()));
                positionCount = positions.Count - positionCount;

                // normal
                if (prim.attributes.NORMAL != -1)
                {
                    normals.AddRange(ctx.GLTF.GetArrayFromAccessor<Vector3>(prim.attributes.NORMAL).Select(x => x.ReverseZ()));
                }

                if (prim.attributes.TANGENT != -1)
                {
                    tangents.AddRange(ctx.GLTF.GetArrayFromAccessor<Vector4>(prim.attributes.TANGENT).Select(x => x.ReverseZ()));
                }

                // uv
                if (prim.attributes.TEXCOORD_0 != -1)
                {
                    uv.AddRange(ctx.GLTF.GetArrayFromAccessor<Vector2>(prim.attributes.TEXCOORD_0).Select(x => x.ReverseY()));
                }
                else
                {
                    // for inconsistent attributes in primitives
                    uv.AddRange(new Vector2[positionCount]);
                }

                // skin
                if (prim.attributes.JOINTS_0 != -1 && prim.attributes.WEIGHTS_0 != -1)
                {
                    var joints0 = ctx.GLTF.GetArrayFromAccessor<UShort4>(prim.attributes.JOINTS_0); // uint4
                    var weights0 = ctx.GLTF.GetArrayFromAccessor<Float4>(prim.attributes.WEIGHTS_0).Select(x => x.One()).ToArray();

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

                        meshContext.boneWeights.Add(bw);
                    }
                }

                // blendshape
                if (prim.targets != null && prim.targets.Count > 0)
                {
                    for (int i = 0; i < prim.targets.Count; ++i)
                    {
                        //var name = string.Format("target{0}", i++);
                        var primTarget = prim.targets[i];
                        var blendShape = new BlendShape(string.IsNullOrEmpty(prim.targets[i].extra.name)
                            ? i.ToString()
                            : prim.targets[i].extra.name)
                            ;
                        if (primTarget.POSITION != -1)
                        {
                            blendShape.Positions.AddRange(
                                ctx.GLTF.GetArrayFromAccessor<Vector3>(primTarget.POSITION).Select(x => x.ReverseZ()).ToArray());
                        }
                        if (primTarget.NORMAL != -1)
                        {
                            blendShape.Normals.AddRange(
                                ctx.GLTF.GetArrayFromAccessor<Vector3>(primTarget.NORMAL).Select(x => x.ReverseZ()).ToArray());
                        }
                        if (primTarget.TANGENT != -1)
                        {
                            blendShape.Tangents.AddRange(
                                ctx.GLTF.GetArrayFromAccessor<Vector3>(primTarget.TANGENT).Select(x => x.ReverseZ()).ToArray());
                        }
                        meshContext.blendShapes.Add(blendShape);
                    }
                }

                var indices =
                 (indexBuffer >= 0)
                 ? ctx.GLTF.GetIndices(indexBuffer, x => x + indexOffset)
                 : TriangleUtil.FlipTriangle(Enumerable.Range(0, meshContext.positions.Length)).ToArray() // without index array
                 ;
                meshContext.subMeshes.Add(indices);

                // material
                meshContext.materialIndices.Add(prim.material);
            }

            meshContext.positions = positions.ToArray();
            meshContext.normals = normals.ToArray();
            meshContext.tangents = tangents.ToArray();
            meshContext.uv = uv.ToArray();

            return meshContext;
        }

        // multiple submesh sharing same VertexBuffer
        private static MeshContext _ImportMeshSharingVertexBuffer(ImporterContext ctx, glTFMesh gltfMesh)
        {
            var context = new MeshContext();

            {
                var prim = gltfMesh.primitives.First();
                context.positions= ctx.GLTF.GetArrayFromAccessor<Vector3>(prim.attributes.POSITION).SelectInplace(x => x.ReverseZ());

                // normal
                if (prim.attributes.NORMAL != -1)
                {
                    context.normals = ctx.GLTF.GetArrayFromAccessor<Vector3>(prim.attributes.NORMAL).SelectInplace(x => x.ReverseZ());
                }

                // tangent
                if (prim.attributes.TANGENT != -1)
                {
                    context.tangents=ctx.GLTF.GetArrayFromAccessor<Vector4>(prim.attributes.TANGENT).SelectInplace(x => x.ReverseZ());
                }

                // uv
                if (prim.attributes.TEXCOORD_0 != -1)
                {
                    context.uv=ctx.GLTF.GetArrayFromAccessor<Vector2>(prim.attributes.TEXCOORD_0).SelectInplace(x => x.ReverseY());
                }
                else
                {
                    // for inconsistent attributes in primitives
                    context.uv = new Vector2[context.positions.Length];
                }

                // skin
                if (prim.attributes.JOINTS_0 != -1 && prim.attributes.WEIGHTS_0 != -1)
                {
                    var joints0 = ctx.GLTF.GetArrayFromAccessor<UShort4>(prim.attributes.JOINTS_0); // uint4
                    var weights0 = ctx.GLTF.GetArrayFromAccessor<Float4>(prim.attributes.WEIGHTS_0, x => x.One());

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

                        context.boneWeights.Add(bw);
                    }
                }

                // blendshape
                if (prim.targets != null && prim.targets.Count > 0)
                {
                    context.blendShapes.AddRange(prim.targets.Select((x, i) => new BlendShape(
                        string.IsNullOrEmpty(prim.targets[i].extra.name)
                        ? i.ToString()
                        : prim.targets[i].extra.name)));
                    for (int i = 0; i < prim.targets.Count; ++i)
                    {
                        //var name = string.Format("target{0}", i++);
                        var primTarget = prim.targets[i];
                        var blendShape = context.blendShapes[i];

                        if (primTarget.POSITION != -1)
                        {
                            blendShape.Positions.Assign(
                                ctx.GLTF.GetArrayFromAccessor<Vector3>(primTarget.POSITION), x => x.ReverseZ());
                        }
                        if (primTarget.NORMAL != -1)
                        {
                            blendShape.Normals.Assign(
                                ctx.GLTF.GetArrayFromAccessor<Vector3>(primTarget.NORMAL), x => x.ReverseZ());
                        }
                        if (primTarget.TANGENT != -1)
                        {
                            blendShape.Tangents.Assign(
                                ctx.GLTF.GetArrayFromAccessor<Vector3>(primTarget.TANGENT), x => x.ReverseZ());
                        }
                    }
                }
            }

            foreach (var prim in gltfMesh.primitives)
            {
                if (prim.indices == -1)
                {
                    context.subMeshes.Add(TriangleUtil.FlipTriangle(Enumerable.Range(0, context.positions.Length)).ToArray());
                }
                else
                {
                    var indices = ctx.GLTF.GetIndices(prim.indices);
                    context.subMeshes.Add(indices);
                }

                // material
                context.materialIndices.Add(prim.material);
            }

            return context;
        }

        public static GameObject ImportNode(glTFNode node)
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

        public static TransformWithSkin BuildHierarchy(ImporterContext context, int i)
        {
            var go = context.Nodes[i].gameObject;
            if (string.IsNullOrEmpty(go.name))
            {
                go.name = string.Format("node{0:000}", i);
            }

            var nodeWithSkin = new TransformWithSkin
            {
                Transform = go.transform,
            };

            //
            // build hierachy
            //
            var node = context.GLTF.nodes[i];
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    context.Nodes[child].transform.SetParent(context.Nodes[i].transform,
                        false // node has local transform
                        );
                }
            }

            //
            // attach mesh
            //
            if (node.mesh != -1)
            {
                var mesh = context.Meshes[node.mesh];
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
                        if (node.extra.skinRootBone != -1)
                        {
                            renderer.rootBone = context.Nodes[node.extra.skinRootBone];
                        }
                    }

                    renderer.sharedMesh = mesh.Mesh;
                    renderer.sharedMaterials = mesh.Materials;
                }
            }

            return nodeWithSkin;
        }

        //
        // fix node's coordinate. z-back to z-forward
        //
        public static void FixCoordinate(ImporterContext context, List<TransformWithSkin> nodes)
        {
            var globalTransformMap = nodes.ToDictionary(x => x.Transform, x => new PosRot
            {
                Position = x.Transform.position,
                Rotation = x.Transform.rotation,
            });
            foreach (var x in context.GLTF.rootnodes)
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
        }

        public static void SetupSkinning(ImporterContext context, List<TransformWithSkin> nodes, int i)
        {
            var x = nodes[i];
            var skinnedMeshRenderer = x.Transform.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                var mesh = skinnedMeshRenderer.sharedMesh;
                if (x.SkinIndex.HasValue)
                {
                    if (mesh == null) throw new Exception();
                    if (skinnedMeshRenderer == null) throw new Exception();

                    if (x.SkinIndex.Value < context.GLTF.skins.Count)
                    {
                        var skin = context.GLTF.skins[x.SkinIndex.Value];

                        skinnedMeshRenderer.sharedMesh = null;

                        var joints = skin.joints.Select(y => nodes[y].Transform).ToArray();
                        skinnedMeshRenderer.bones = joints;
                        //skinnedMeshRenderer.rootBone = nodes[skin.skeleton].Transform;

                        if (skin.inverseBindMatrices != -1)
                        {
                            // BlendShape only ?
#if false
                            // https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html
                            var hipsParent = nodes[0].Transform;
                            var calculatedBindPoses = joints.Select(y => y.worldToLocalMatrix * hipsParent.localToWorldMatrix).ToArray();
                            mesh.bindposes = calculatedBindPoses;
#else
                            var bindPoses = context.GLTF.GetArrayFromAccessor<Matrix4x4>(skin.inverseBindMatrices)
                                .Select(y => y.ReverseZ())
                                .ToArray()
                                ;
                            mesh.bindposes = bindPoses;
#endif
                        }

                        skinnedMeshRenderer.sharedMesh = mesh;
                    }
                }
            }
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

        public static void ImportAnimation(ImporterContext ctx, AnimationClip clip)
        {
            for (int i = 0; i < ctx.GLTF.animations.Count; ++i)
            {
                var animation = ctx.GLTF.animations[i];
                if (string.IsNullOrEmpty(animation.name))
                {
                    animation.name = string.Format("animation:{0}", i);
                }

                foreach (var y in animation.channels)
                {
                    var targetTransform = ctx.Nodes[y.target.node];
                    var relativePath = targetTransform.RelativePathFrom(ctx.Root.transform);
                    switch (y.target.path)
                    {
                        case glTFAnimationTarget.PATH_TRANSLATION:
                            {
                                var curveX = new AnimationCurve();
                                var curveY = new AnimationCurve();
                                var curveZ = new AnimationCurve();

                                var sampler = animation.samplers[y.sampler];
                                var input = ctx.GLTF.GetArrayFromAccessor<float>(sampler.input);
                                var output = ctx.GLTF.GetArrayFromAccessor<Vector3>(sampler.output);
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
                                var input = ctx.GLTF.GetArrayFromAccessor<float>(sampler.input);
                                var output = ctx.GLTF.GetArrayFromAccessor<Quaternion>(sampler.output);
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
                                var input = ctx.GLTF.GetArrayFromAccessor<float>(sampler.input);
                                var output = ctx.GLTF.GetArrayFromAccessor<Vector3>(sampler.output);
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
                                var node = ctx.GLTF.nodes[y.target.node];
                                var mesh = ctx.GLTF.meshes[node.mesh];
                                for (int k = 0; k < mesh.weights.Length; ++k)
                                {
                                    //var weight = mesh.weights[k];
                                    var curve = new AnimationCurve();
                                    var sampler = animation.samplers[y.sampler];
                                    var input = ctx.GLTF.GetArrayFromAccessor<float>(sampler.input);
                                    var output = ctx.GLTF.GetArrayFromAccessor<float>(sampler.output);
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

        public static Transform FindRootBone(Mesh mesh, Transform[] bones)
        {
            var weightMap = new HashSet<Transform>();
            foreach (var x in mesh.boneWeights)
            {
                if (x.weight0 > 0) weightMap.Add(bones[x.boneIndex0]);
                if (x.weight1 > 0) weightMap.Add(bones[x.boneIndex1]);
                if (x.weight2 > 0) weightMap.Add(bones[x.boneIndex2]);
                if (x.weight3 > 0) weightMap.Add(bones[x.boneIndex3]);
            }
            if (weightMap.Count == 0)
            {
                return null;
            }

            // 
            var roots = bones
                // すべてのWeightを子孫にもつボーンを探す
                .Where(x => weightMap.All(y => y.Ancestors().Any(z => z == x)))
                .ToArray();

            if (roots.Length == 0)
            {
                return null;
            }

            return roots
                .Select(x => new
                {
                    Transform = x,
                    Parents = x.Ancestors().Count(),
                })
                .OrderBy(x => x.Parents)
                .First().Transform;
        }
#endregion
    }
}
