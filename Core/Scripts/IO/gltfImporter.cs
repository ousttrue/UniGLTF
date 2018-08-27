using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;


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
                for(int i=0; i<ctx.GLTF.textures.Count; ++i)
                {
                    var item = new TextureItem(ctx.GLTF, i, ctx.TextureBaseDir);
                    ctx.AddTexture(item);
                }
            }
            foreach(var x in ctx.GetTextures())
            {
                x.Process(ctx.GLTF, ctx.Storage);
            }

            // materials
            if (ctx.MaterialImporter == null)
            {
                ctx.MaterialImporter = new MaterialImporter(new ShaderStore(ctx), ctx);
            }

            if (ctx.GLTF.materials == null || !ctx.GLTF.materials.Any())
            {
                // no material
                ctx.AddMaterial(ctx.MaterialImporter.CreateMaterial(0, null));
            }
            else
            {
                for (int i = 0; i < ctx.GLTF.materials.Count; ++i)
                {
                    var index = i;
                    var material = ctx.MaterialImporter.CreateMaterial(index, ctx.GLTF.materials[i]);
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

            var meshImporter = new MeshImporter();
            for(int i=0; i<ctx.GLTF.meshes.Count; ++i)
            {
                var meshContext = meshImporter.ReadMesh(ctx, i);
                var meshWithMaterials = BuildMesh(ctx, meshContext);

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
        public static MeshWithMaterials BuildMesh(ImporterContext ctx, MeshImporter.MeshContext meshContext)
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
            bool recalculateNormals = false;
            if (meshContext.normals != null && meshContext.normals.Length > 0)
            {
                mesh.normals = meshContext.normals;
            }
            else
            {
                recalculateNormals = true;
            }

            if (meshContext.uv != null && meshContext.uv.Length > 0)
            {
                mesh.uv = meshContext.uv;
            }

            bool recalculateTangents = true;
#if UNIGLTF_IMPORT_TANGENTS
            if (meshContext.tangents != null && meshContext.tangents.Length > 0)
            {
                mesh.tangents = meshContext.tangents;
                recalculateTangents = false;
            }
#endif

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

            if (recalculateNormals)
            {
                mesh.RecalculateNormals();
            }
            if (recalculateTangents)
            {
#if UNITY_5_6_OR_NEWER
                mesh.RecalculateTangents();
#else
                Debug.LogWarning("recalculateTangents");
#endif
            }

            var result = new MeshWithMaterials
            {
                Mesh = mesh,
                Materials = meshContext.materialIndices.Select(x => ctx.GetMaterials()[x]).ToArray()
            };

            if (meshContext.blendShapes != null)
            {
                Vector3[] emptyVertices = null;
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
                        if (emptyVertices == null)
                        {
                            emptyVertices = new Vector3[mesh.vertexCount];
                        }
                        Debug.LogFormat("empty blendshape: {0}.{1}", mesh.name, blendShape.Name);
                        // add empty blend shape for keep blend shape index
                        mesh.AddBlendShapeFrame(blendShape.Name, FRAME_WEIGHT,
                            emptyVertices,
                            null,
                            null
                            );
                    }
                }
            }

            return result;
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
                                    curveX.AddKey(new Keyframe(time, pos.x, 0, 0));
                                    curveY.AddKey(new Keyframe(time, pos.y, 0, 0));
                                    curveZ.AddKey(new Keyframe(time, pos.z, 0, 0));
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
                                var last = Quaternion.identity;
                                for (int j = 0; j < input.Length; ++j)
                                {
                                    var time = input[j];
                                    var rot = output[j].ReverseZ();
                                    if (j > 0)
                                    {
                                        if(Quaternion.Dot(last, rot) < 0)
                                        {
                                            rot.x = -rot.x;
                                            rot.y = -rot.y;
                                            rot.z = -rot.z;
                                            rot.w = -rot.w;
                                        }
                                    }
                                    curveX.AddKey(new Keyframe(time, rot.x, 0, 0));
                                    curveY.AddKey(new Keyframe(time, rot.y, 0, 0));
                                    curveZ.AddKey(new Keyframe(time, rot.z, 0, 0));
                                    curveW.AddKey(new Keyframe(time, rot.w, 0, 0));
                                    last = rot;
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
                                    curveX.AddKey(new Keyframe(time, scale.x, 0, 0));
                                    curveY.AddKey(new Keyframe(time, scale.y, 0, 0));
                                    curveZ.AddKey(new Keyframe(time, scale.z, 0, 0));
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

                        default:
                            Debug.LogWarningFormat("unknown path: {0}", y.target.path);
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
