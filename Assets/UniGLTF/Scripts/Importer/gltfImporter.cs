using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
#if UNITY_2017_OR_NEWER
using UnityEditor.Experimental.AssetImporters;
#endif


namespace UniGLTF
{
#if UNITY_2017_OR_NEWER
    [ScriptedImporter(1, "gltf")]
#endif
    public class gltfImporter
#if UNITY_2017_OR_NEWER
        : ScriptedImporter
#endif
    {
#if UNITY_2017_OR_NEWER
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## Importer ##: {0}", ctx.assetPath);

            var json = File.ReadAllText(ctx.assetPath, Encoding.UTF8);

            Import(ctx, json, new ArraySegment<byte>());
        }

        class ScriptedImporterContext : IImporterContext
        {
            public string Path
            {
                get;
                private set;
            }

            public void Dispose()
            {
            }

            public AssetImportContext AssetImportContext;

            public ScriptedImporterContext(ScriptedImporterContext assetImportContext)
            {
                AssetImportContext = assetImportContext;
                Path = assetImportContext.assetPath;
            }

            public void AddObjectToAsset(string key, UnityEngine.Object o)
            {
                if (AssetImportContext == null)
                {
                    return;
                }
                AssetImportContext.AddObjectToAsset(key, o);
            }

            public void SetMainGameObject(string key, UnityEngine.GameObject go)
            {
                if (AssetImportContext == null)
                {
                    return;
                }
                AssetImportContext.SetMainObject(key, go);
            }
        }

        public static GameObject Import(AssetImportContext ctx, string json, ArraySegment<Byte> bytes = default(ArraySegment<Byte>))
        {
            return Import(new Context(ctx), json, bytes);
        }
#endif
        class PrefabContext : IImporterContext
        {
            public string Path
            {
                get;
                private set;
            }

            string m_prefabPath;

            IEnumerable<UnityEngine.Object> GetSubAssets()
            {
                return UnityEditor.AssetDatabase.LoadAllAssetsAtPath(m_prefabPath)
                    ;
            }

            public PrefabContext(String path)
            {
                Path = path;

                var dir = System.IO.Path.GetDirectoryName(Path);
                var name = System.IO.Path.GetFileNameWithoutExtension(Path);
                m_prefabPath = string.Format("{0}/{1}.prefab", dir, name);

                if (File.Exists(m_prefabPath))
                {
                    Debug.LogFormat("Exist: {0}", m_prefabPath);

                    // clear subassets
                    foreach (var x in GetSubAssets())
                    {
                        if(x is Transform
                            || x is GameObject)
                        {
                            continue;
                        }
                        GameObject.DestroyImmediate(x, true);
                    }
                }
            }

            GameObject m_go;
            public void SetMainGameObject(string key, GameObject go)
            {
                m_go = go;
            }

            public void AddObjectToAsset(string key, UnityEngine.Object o)
            {
                AssetDatabase.AddObjectToAsset(o, m_prefabPath);
            }

            public void Dispose()
            {
                ///
                /// create prefab, after subasset AssetDatabase.AddObjectToAsset
                ///
                if (File.Exists(m_prefabPath))
                {
                    Debug.LogFormat("ReplacePrefab: {0}", m_prefabPath);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m_prefabPath);
                    PrefabUtility.ReplacePrefab(m_go, prefab, ReplacePrefabOptions.ConnectToPrefab);
                }
                else
                {
                    Debug.LogFormat("CreatePrefab: {0}", m_prefabPath);
                    PrefabUtility.CreatePrefab(m_prefabPath, m_go, ReplacePrefabOptions.ConnectToPrefab);
                }

                GameObject.DestroyImmediate(m_go);
            }
        }

        static void TraverseTransform(Transform t, Action<Transform> pred)
        {
            pred(t);

            foreach (Transform child in t)
            {
                TraverseTransform(child, pred);
            }
        }

        public struct TransformWithSkin
        {
            public Transform Transform;
            public int? SkinIndex;
        }

        static void SetSampler(Texture2D texture, glTFTextureSampler sampler)
        {
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

            /*
            if (sampler.minFilter != sampler.magFilter)
            {
                throw new NotImplementedException();
            }
            */

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

        public static GameObject Import(string path, string json, ArraySegment<Byte> bytes, bool isPrefab)
        {
            if (isPrefab)
            {
                using (var context = new PrefabContext(path))
                {
                    return Import(context, json, bytes);
                }
            }
            else
            {
                using (var context = new RuntimeContext(path))
                {
                    return Import(context, json, bytes);
                }
            }
        }

        static GameObject Import(IImporterContext ctx, string json, ArraySegment<Byte> bytes)
        {
            var root = new GameObject("_root_");
            ctx.SetMainGameObject("root", root);

            var baseDir = Path.GetDirectoryName(ctx.Path);

            var gltf = glTFExtensions.Parse(json, baseDir, bytes);
            Debug.Log(gltf);

            // textures
            var textures = ImportTextures(gltf)
                    .Select(x =>
                    {
                        var samplerIndex = gltf.textures[x.TextureIndex].sampler;
                        var sampler = gltf.samplers[samplerIndex];

                        SetSampler(x.Texture, sampler);

                        if (!x.IsAsset)
                        {
                            ctx.AddObjectToAsset(x.Texture.name, x.Texture);
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

                TraverseTransform(t, transform =>
                {
                    var g = globalTransformMap[transform];
                    transform.position = g.Position.ReverseZ();
                    transform.rotation = g.Rotation.ReverseZ();
                });
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
                            skinnedMeshRenderer.rootBone = nodes[skin.skeleton].Transform;

#if false
                            // https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html
                            var _b = joints.Select(y => y.worldToLocalMatrix * nodes[0].Transform.localToWorldMatrix).ToArray();
                            mesh.bindposes = _b;
#else
                            var bindPoses = gltf.GetArrayFromAccessor<Matrix4x4>(skin.inverseBindMatrices).ToArray();
                            var bindPosesR = bindPoses
                                .Select(y => y.ReverseZ())
                                //.Select(y => y * nodes[0].Transform.localToWorldMatrix)
                                //.Select(y => nodes[0].Transform.localToWorldMatrix * y)
                                //.Select(y => y * Matrix4x4.Inverse(nodes[0].Transform.localToWorldMatrix))
                                .ToArray()
                                ;
                            mesh.bindposes = bindPosesR;
#endif

                            skinnedMeshRenderer.sharedMesh = mesh;
                        }
                    }
                }
            }

            foreach (var x in gltf.rootnodes)
            {
                // fix nodes coordinate
                // reverse Z in global
                var t = nodes[x].Transform;
                t.SetParent(root.transform, false);
            }

            // animation
            if (gltf.animations != null)
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
            else if (gltf.baseDir.StartsWith("Assets/"))
            {
                // local folder
                var path = Path.Combine(gltf.baseDir, image.uri);
                Debug.LogFormat("load texture: {0}", path);

                var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                return new TextureWithIsAsset { TextureIndex = index, Texture = texture, IsAsset = true };
            }
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

        static IEnumerable<Material> ImportMaterials(glTF gltf, Texture2D[] textures)
        {
            var shader = Shader.Find("Standard");
            if (gltf.materials == null)
            {
                var material = new Material(shader);
                return new Material[] { material };
            }
            else
            {
                return gltf.materials.Select(x =>
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
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var boneWeights = new List<BoneWeight>();
            var subMeshes = new List<int[]>();
            var materialIndices = new List<int>();

            var targets = gltfMesh.primitives[0].targets;
            for (int i = 1; i < gltfMesh.primitives.Count; ++i)
            {
                if (gltfMesh.primitives[i].targets != targets)
                {
                    throw new FormatException(string.Format("diffirent targets: {0} with {1}",
                        gltfMesh.primitives[i],
                        targets));
                }
            }

            BlendShape[] blendShapes = null;
            foreach (var prim in gltfMesh.primitives)
            {
                var indexOffset = positions.Count;
                var indexBuffer = prim.indices;

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

                // skin
                if (prim.attributes.JOINTS_0 != -1 && prim.attributes.WEIGHTS_0 != -1)
                {
                    var joints0 = gltf.GetArrayFromAccessor<UShort4>(prim.attributes.JOINTS_0); // uint4
                    var weights0 = gltf.GetArrayFromAccessor<Float4>(prim.attributes.WEIGHTS_0).Select(x => x.One()).ToArray();

                    var weightNorms = weights0.Select(x => x.x + x.y + x.z + x.w).ToArray();

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
                if (prim.targets != null && prim.targets.Length > 0)
                {
                    if (blendShapes == null)
                    {
                        blendShapes = prim.targets.Select((x, i) => new BlendShape("blendShape: " + i)).ToArray();
                    }
                    for (int i = 0; i < prim.targets.Length; ++i)
                    {
                        var name = string.Format("target{0}", i++);
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

                subMeshes.Add(gltf.GetIndices(indexBuffer).Select(x => x + indexOffset).ToArray());

                // material
                materialIndices.Add(prim.material);
            }
            if (!materialIndices.Any())
            {
                materialIndices.Add(0);
            }

            //Debug.Log(prims.ToJson());
            var mesh = new Mesh();
            mesh.name = gltfMesh.name;

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
                        mesh.AddBlendShapeFrame(blendShape.Name, 100.0f,
                            blendShape.Positions.ToArray(),
                            blendShape.Normals.ToArray(),
                            null
                            );
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
                m.m01 = values[1];
                m.m02 = values[2];
                m.m03 = values[3];
                m.m10 = values[4];
                m.m11 = values[5];
                m.m12 = values[6];
                m.m13 = values[7];
                m.m20 = values[8];
                m.m21 = values[9];
                m.m22 = values[10];
                m.m23 = values[11];
                m.m30 = values[12];
                m.m31 = values[13];
                m.m32 = values[14];
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

        public static void ImportAnimation(Transform root, AnimationClip clip, List<glTFAnimation> animations, Transform[] nodes, glTF buffer)
        {
            foreach (var x in animations)
            {
                foreach (var y in x.channels)
                {
                    var node = nodes[y.target.node];
                    var relativePath = node.RelativePathFrom(root);
                    switch (y.target.path)
                    {
                        case glTFAnimationTarget.PATH_TRANSLATION:
                            {
                                var curveX = new AnimationCurve();
                                var curveY = new AnimationCurve();
                                var curveZ = new AnimationCurve();

                                var sampler = x.samplers[y.sampler];
                                var input = buffer.GetArrayFromAccessor<float>(sampler.input);
                                var output = buffer.GetArrayFromAccessor<Vector3>(sampler.output);
                                for (int i = 0; i < input.Length; ++i)
                                {
                                    var time = input[i];
                                    var pos = output[i].ReverseZ();
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

                                var sampler = x.samplers[y.sampler];
                                var input = buffer.GetArrayFromAccessor<float>(sampler.input);
                                var output = buffer.GetArrayFromAccessor<Quaternion>(sampler.output);
                                for (int i = 0; i < input.Length; ++i)
                                {
                                    var time = input[i];
                                    var rot = output[i].ReverseZ();
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

                                var sampler = x.samplers[y.sampler];
                                var input = buffer.GetArrayFromAccessor<float>(sampler.input);
                                var output = buffer.GetArrayFromAccessor<Vector3>(sampler.output);
                                for (int i = 0; i < input.Length; ++i)
                                {
                                    var time = input[i];
                                    var scale = output[i];
                                    curveX.AddKey(time, scale.x);
                                    curveY.AddKey(time, scale.y);
                                    curveZ.AddKey(time, scale.z);
                                }

                                clip.SetCurve(relativePath, typeof(Transform), "localScale.x", curveX);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.y", curveY);
                                clip.SetCurve(relativePath, typeof(Transform), "localScale.z", curveZ);
                            }
                            break;
                    }
                }
            }
        }
#endregion
    }
}
