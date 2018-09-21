using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


namespace UniGLTF
{
    public class MeshImporter
    {
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
                    if (ctx.IsGeneratedUniGLTFAndOlder(1, 16))
                    {
#pragma warning disable 0612
                        // backward compatibility
                        uv.AddRange(ctx.GLTF.GetArrayFromAccessor<Vector2>(prim.attributes.TEXCOORD_0).Select(x => x.ReverseY()));
#pragma warning restore 0612
                    }
                    else
                    {
                        uv.AddRange(ctx.GLTF.GetArrayFromAccessor<Vector2>(prim.attributes.TEXCOORD_0).Select(x => x.ReverseUV()));
                    }
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
                for (int i = 0; i < indices.Length; ++i)
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
                    if (ctx.IsGeneratedUniGLTFAndOlder(1, 16))
                    {
#pragma warning disable 0612
                        // backward compatibility
                        context.uv = ctx.GLTF.GetArrayFromAccessor<Vector2>(prim.attributes.TEXCOORD_0).SelectInplace(x => x.ReverseY());
#pragma warning restore 0612
                    }
                    else
                    {
                        context.uv = ctx.GLTF.GetArrayFromAccessor<Vector2>(prim.attributes.TEXCOORD_0).SelectInplace(x => x.ReverseUV());
                    }
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
                    for (int i = 0; i < weights0.Length; ++i)
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


        public MeshContext ReadMesh(ImporterContext ctx, int meshIndex)
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
    }
}
