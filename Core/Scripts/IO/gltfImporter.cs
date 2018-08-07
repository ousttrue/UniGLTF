using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using System.IO;
using System.Text;


namespace UniGLTF
{
    public static class gltfImporter
    {
        const float FRAME_WEIGHT = 100.0f;

        public static ImporterContext Load(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var context = Parse(path, bytes);
            gltfImporter.Load(context);
            context.Root.name = Path.GetFileNameWithoutExtension(path);
            return context;
        }

        public static ImporterContext Parse(string path, Byte[] bytes)
        {
            var ext = Path.GetExtension(path).ToLower();
            var context = new ImporterContext(UnityPath.FromFullpath(path));

            switch (ext)
            {
                case ".gltf":
                    context.ParseJson(Encoding.UTF8.GetString(bytes), new FileSystemStorage(Path.GetDirectoryName(path)));
                    break;

                case ".zip":
                    {
                        var zipArchive = Zip.ZipArchiveStorage.Parse(bytes);
                        var gltf = zipArchive.Entries.FirstOrDefault(x => x.FileName.ToLower().EndsWith(".gltf"));
                        if (gltf == null)
                        {
                            throw new Exception("no gltf in archive");
                        }
                        var jsonBytes = zipArchive.Extract(gltf);
                        var json = Encoding.UTF8.GetString(jsonBytes);
                        context.ParseJson(json, zipArchive);
                    }
                    break;

                case ".glb":
                    context.ParseGlb(bytes);
                    break;

                default:
                    throw new NotImplementedException();
            }
            return context;
        }

        public static void Load(ImporterContext ctx)
        {
            // textures
            if (ctx.GLTF.textures != null)
            {
                ctx.Textures.AddRange(ctx.GLTF.textures.Select((x, i) => new TextureItem(ctx.GLTF, i, ctx.TextureBaseDir)));
            }
            foreach(var x in ctx.Textures)
            {
                x.Process(ctx.GLTF, ctx.Storage);
            }

            // materials
            if (ctx.MaterialImporter == null)
            {
                ctx.MaterialImporter = new MaterialImporter(new ShaderStore(ctx));
            }

            Func<int, TextureItem> getTexture = x =>
            {
                if(x<0 || x >= ctx.Textures.Count)
                {
                    return null;
                }
                return ctx.Textures[x];
            };

            if (ctx.GLTF.materials == null || !ctx.GLTF.materials.Any())
            {
                // no material
                ctx.AddMaterial(ctx.MaterialImporter.CreateMaterial(0, null, getTexture));
            }
            else
            {
                for (int i = 0; i < ctx.GLTF.materials.Count; ++i)
                {
                    var index = i;
                    var material = ctx.MaterialImporter.CreateMaterial(index, ctx.GLTF.materials[i], getTexture);
                    ctx.AddMaterial(material);
                }
            }

            // meshes
            if (ctx.GLTF.meshes
                .SelectMany(x => x.primitives)
                .Any(x => x.extensions.KHR_draco_mesh_compression != null))
            {
                throw new UniGLTFNotSupportedException("draco is not supported");
            }

            for(int i=0; i<ctx.GLTF.meshes.Count; ++i)
            {
                var meshWithMaterials = ImportMesh(ctx, i);

                var mesh = meshWithMaterials.Mesh;

                // mesh name
                if (string.IsNullOrEmpty(mesh.name))
                {
                    mesh.name = string.Format("UniGLTF import#{0}", i);
                }
                var originalName = mesh.name;
                for (int j = 1; ctx.Meshes.Any(x => x.Mesh.name == mesh.name); ++j)
                {
                    mesh.name = string.Format("{0}({1})", originalName, j);
                }

                ctx.Meshes.Add(meshWithMaterials);
            }

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
            public Color[] colors;
            public List<BoneWeight> boneWeights = new List<BoneWeight>();
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

            mesh.vertices = meshContext.positions;
            if (meshContext.normals != null && meshContext.normals.Length > 0)
            {
                mesh.normals = meshContext.normals;
            }
            else
            {
                mesh.RecalculateNormals();
            }
            if (meshContext.tangents != null && meshContext.tangents.Length > 0)
            {
                mesh.tangents = meshContext.tangents;
            }
            else
            {
#if UNITY_5_6_OR_NEWER
                mesh.RecalculateTangents();
#endif
            }
            if (meshContext.uv != null && meshContext.uv.Length > 0)
            {
                mesh.uv = meshContext.uv;
            }
            if(meshContext.colors!=null && meshContext.colors.Length > 0)
            {
                mesh.colors = meshContext.colors;
            }
            if (meshContext.boneWeights != null && meshContext.boneWeights.Count > 0)
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
                Materials = meshContext.materialIndices.Select(x => ctx.GetMaterials()[x]).ToArray()
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
                                (meshContext.normals != null && meshContext.normals.Length == mesh.vertexCount) ? blendShape.Normals.ToArray() : null,
                                null
                                );
                        }
                        else
                        {
                            Debug.LogWarningFormat("May be partial primitive has blendShape. Rquire separete mesh or extend blend shape, but not implemented: {0}", blendShape.Name);
                        }
                    }
                    else
                    {
                        Debug.LogFormat("empty blendshape: {0}.{1}", mesh.name, blendShape.Name);
                        // add empty blend shape for keep blend shape index
                        mesh.AddBlendShapeFrame(blendShape.Name, FRAME_WEIGHT,
                            //Enumerable.Range(0, mesh.vertexCount).Select(x => Vector3.zero).ToArray(),
                            null,
                            null,
                            null
                            );
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
            var colors = new List<Color>();
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

                // color
                if (prim.attributes.COLOR_0 != -1)
                {
                    colors.AddRange(ctx.GLTF.GetArrayFromAccessor<Color>(prim.attributes.COLOR_0));
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
                        var blendShape = new BlendShape(!string.IsNullOrEmpty(prim.extras.targetNames[i])
                            ? prim.extras.targetNames[i]
                            : i.ToString())
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
                 ? ctx.GLTF.GetIndices(indexBuffer)
                 : TriangleUtil.FlipTriangle(Enumerable.Range(0, meshContext.positions.Length)).ToArray() // without index array
                 ;
                for(int i=0; i<indices.Length; ++i)
                {
                    indices[i] += indexOffset;
                }

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
                context.positions = ctx.GLTF.GetArrayFromAccessor<Vector3>(prim.attributes.POSITION).SelectInplace(x => x.ReverseZ());

                // normal
                if (prim.attributes.NORMAL != -1)
                {
                    context.normals = ctx.GLTF.GetArrayFromAccessor<Vector3>(prim.attributes.NORMAL).SelectInplace(x => x.ReverseZ());
                }

                // tangent
                if (prim.attributes.TANGENT != -1)
                {
                    context.tangents = ctx.GLTF.GetArrayFromAccessor<Vector4>(prim.attributes.TANGENT).SelectInplace(x => x.ReverseZ());
                }

                // uv
                if (prim.attributes.TEXCOORD_0 != -1)
                {
                    context.uv = ctx.GLTF.GetArrayFromAccessor<Vector2>(prim.attributes.TEXCOORD_0).SelectInplace(x => x.ReverseY());
                }
                else
                {
                    // for inconsistent attributes in primitives
                    context.uv = new Vector2[context.positions.Length];
                }

                // color
                if (prim.attributes.COLOR_0 != -1)
                {
                    context.colors = ctx.GLTF.GetArrayFromAccessor<Color>(prim.attributes.COLOR_0);
                }

                // skin
                if (prim.attributes.JOINTS_0 != -1 && prim.attributes.WEIGHTS_0 != -1)
                {
                    var joints0 = ctx.GLTF.GetArrayFromAccessor<UShort4>(prim.attributes.JOINTS_0); // uint4
                    var weights0 = ctx.GLTF.GetArrayFromAccessor<Float4>(prim.attributes.WEIGHTS_0);
                    for(int i=0; i<weights0.Length; ++i)
                    {
                        weights0[i] = weights0[i].One();
                    }

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
                        i < prim.extras.targetNames.Count && !string.IsNullOrEmpty(prim.extras.targetNames[i])
                        ? prim.extras.targetNames[i]
                        : i.ToString())));
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
            var nodeName = node.name;
            if (!string.IsNullOrEmpty(nodeName) && nodeName.Contains("/"))
            {
                Debug.LogWarningFormat("node {0} contains /. replace _", node.name);
                nodeName = nodeName.Replace("/", "_");
            }
            var go = new GameObject(nodeName);

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
                var m = UnityExtensions.MatrixFromArray(node.matrix);
                go.transform.localRotation = m.ExtractRotation();
                go.transform.localPosition = m.ExtractPosition();
                go.transform.localScale = m.ExtractScale();
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
                    mesh.Renderer = renderer;
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
                    mesh.Renderer = renderer;
                }

                // invisible in loading
                mesh.Renderer.enabled = false;
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

        public class TransformWithSkin
        {
            public Transform Transform;
            public GameObject GameObject { get { return Transform.gameObject; } }
            public int? SkinIndex;
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

                        if (skin.skeleton >= 0 && skin.skeleton < nodes.Count)
                        {
                            skinnedMeshRenderer.rootBone = nodes[skin.skeleton].Transform;
                        }

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
                                    for (int j = 0, l = k; j < input.Length; ++j, l += mesh.weights.Length)
                                    {
                                        curve.AddKey(input[j], output[l] * 100);
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
