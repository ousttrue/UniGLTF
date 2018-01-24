using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


namespace UniGLTF
{
    public struct PosRot
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public static PosRot FromGlobalTransform(Transform t)
        {
            return new PosRot
            {
                Position = t.position,
                Rotation = t.rotation,
            };
        }
    }

    public class glTF
    {
        public string baseDir
        {
            get;
            set;
        }

        public glTFAssets asset;

        #region Buffer      
        public List<glTFBuffer> buffers = new List<glTFBuffer>();
        public List<glTFBufferView> bufferViews = new List<glTFBufferView>();
        public List<glTFAccessor> accessors = new List<glTFAccessor>();

        IBytesBuffer[] m_bytesList;

        #region Importer
        T[] GetAttrib<T>(glTFAccessor accessor, glTFBufferView view) where T : struct
        {
            var attrib = new T[accessor.count];
            //
            var segment = m_bytesList[view.buffer].GetBytes();
            var bytes = new ArraySegment<Byte>(segment.Array, segment.Offset + view.byteOffset + accessor.byteOffset, accessor.count * view.byteStride);
            bytes.MarshalCoyTo(attrib);
            return attrib;
        }

        public ArraySegment<Byte> GetViewBytes(int bufferView)
        {
            var view = bufferViews[bufferView];
            var segment = m_bytesList[view.buffer].GetBytes();
            return new ArraySegment<byte>(segment.Array, segment.Offset + view.byteOffset, view.byteLength);
        }

        public int[] GetIndices(int index)
        {
            var accessor = accessors[index];
            var view = bufferViews[accessor.bufferView];
            switch ((glComponentType)accessor.componentType)
            {
                case glComponentType.UNSIGNED_BYTE:
                    {
                        var indices = GetAttrib<Byte>(accessor, view);
                        return FlipTriangle(indices).ToArray();
                    }

                case glComponentType.UNSIGNED_SHORT:
                    {
                        var indices = GetAttrib<UInt16>(accessor, view);
                        return FlipTriangle(indices).ToArray();
                    }

                case glComponentType.INT:
                    {
                        var indices = GetAttrib<Int32>(accessor, view);
                        return FlipTriangle(indices).ToArray();
                    }
            }

            throw new NotImplementedException("GetIndices: unknown componenttype: " + accessor.componentType);
        }

        public T[] GetBuffer<T>(int index) where T : struct
        {
            var vertexAccessor = accessors[index];
            var view = bufferViews[vertexAccessor.bufferView];
            return GetAttrib<T>(vertexAccessor, view);
        }

        static IEnumerable<int> FlipTriangle(IEnumerable<Byte> src)
        {
            return FlipTriangle(src.Select(x => (Int32)x));
        }

        static IEnumerable<int> FlipTriangle(IEnumerable<UInt16> src)
        {
            return FlipTriangle(src.Select(x => (Int32)x));
        }

        static IEnumerable<int> FlipTriangle(IEnumerable<Int32> src)
        {
            var it = src.GetEnumerator();
            while (true)
            {
                if (!it.MoveNext())
                {
                    yield break;
                }
                var i0 = it.Current;

                if (!it.MoveNext())
                {
                    yield break;
                }
                var i1 = it.Current;

                if (!it.MoveNext())
                {
                    yield break;
                }
                var i2 = it.Current;

                yield return i2;
                yield return i1;
                yield return i0;
            }
        }

        struct BlendShape
        {
            public Vector3[] Positions;
            public Vector3[] Normals;
            public Vector3[] Tangents;
        }

        BlendShape ReadBlendShape(JsonParser targetJson)
        {
            var blendShape = new BlendShape();
            if (targetJson.HasKey("POSITION"))
            {
                blendShape.Positions = GetBuffer<Vector3>(targetJson["POSITION"].GetInt32()).Select(x => x.ReverseZ()).ToArray();
            }
            if (targetJson.HasKey("NORMAL"))
            {
                blendShape.Normals = GetBuffer<Vector3>(targetJson["NORMAL"].GetInt32()).Select(x => x.ReverseZ()).ToArray();
            }
            if (targetJson.HasKey("TANGENT"))
            {
                blendShape.Tangents = GetBuffer<Vector3>(targetJson["TANGENT"].GetInt32())/*.Select(ReverseZ).ToArray()*/;
            }
            return blendShape;
        }

        [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct UShort4
        {
            public ushort x;
            public ushort y;
            public ushort z;
            public ushort w;

            public UShort4(ushort _x, ushort _y, ushort _z, ushort _w)
            {
                x = _x;
                y = _y;
                z = _z;
                w = _w;
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

        public struct MeshWithMaterials
        {
            public Mesh Mesh;
            public Material[] Materials;
        }

        public MeshWithMaterials ReadMesh(glTFMesh gltfMesh, Material[] materials)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var boneWeights = new List<BoneWeight>();
            var subMeshes = new List<int[]>();
            var materialIndices = new List<int>();
            foreach (var prim in gltfMesh.primitives)
            {
                var indexOffset = positions.Count;
                var indexBuffer = prim.indices;

                positions.AddRange(GetBuffer<Vector3>(prim.attributes.POSITION).Select(x => x.ReverseZ()));

                // normal
                if (prim.attributes.NORMAL != -1)
                {
                    normals.AddRange(GetBuffer<Vector3>(prim.attributes.NORMAL).Select(x => x.ReverseZ()));
                }
                // uv
                if (prim.attributes.TEXCOORD_0 != -1)
                {
                    uv.AddRange(GetBuffer<Vector2>(prim.attributes.TEXCOORD_0).Select(x => x.ReverseY()));
                }

                // skin
                if (prim.attributes.JOINTS_0 != -1 && prim.attributes.WEIGHTS_0 != -1)
                {
                    var joints0 = GetBuffer<UShort4>(prim.attributes.JOINTS_0); // uint4
                    var weights0 = GetBuffer<Float4>(prim.attributes.WEIGHTS_0).Select(x => x.One()).ToArray();

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

                subMeshes.Add(GetIndices(indexBuffer).Select(x => x + indexOffset).ToArray());

                // material
                materialIndices.Add(prim.material);

                /*
                // blendshape
                if (prim.HasKey("targets"))
                {
                    int j = 0;
                    foreach (var x in prim["targets"].ListItems)
                    {
                        var blendShape = ReadBlendShape(x);

                        var name = string.Format("target{0}", j++);

                        mesh.AddBlendShapeFrame(name, 1.0f, blendShape.Positions, blendShape.Normals, blendShape.Tangents);
                    }
                }
                */
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

            return result;
        }
        #endregion

        #region Exporter
        struct ComponentVec
        {
            public glComponentType ComponentType;
            public string VectorType;

            public ComponentVec(glComponentType componentType, string vectorType)
            {
                ComponentType = componentType;
                VectorType = vectorType;
            }
        }

        static Dictionary<Type, ComponentVec> ComponentTypeMap = new Dictionary<Type, ComponentVec>
        {
            { typeof(Vector2), new ComponentVec(glComponentType.FLOAT, "VEC2") },
            { typeof(Vector3), new ComponentVec(glComponentType.FLOAT, "VEC3") },
            { typeof(Vector4), new ComponentVec(glComponentType.FLOAT, "VEC4") },
            { typeof(UShort4), new ComponentVec(glComponentType.FLOAT, "VEC4") },
            { typeof(Matrix4x4), new ComponentVec(glComponentType.FLOAT, "MATRIX") },
        };

        static glComponentType GetComponentType<T>()
        {
            var cv = default(ComponentVec);
            if(ComponentTypeMap.TryGetValue(typeof(T), out cv))
            {
                return cv.ComponentType;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        static string GetAccessorType<T>()
        {
            var cv = default(ComponentVec);
            if (ComponentTypeMap.TryGetValue(typeof(T), out cv))
            {
                return cv.VectorType;
            }
            else
            {
                return "SCALAR";
            }
        }

        public int AddBuffer<T>(ArrayByteBuffer bytesBuffer, T[] array)where T: struct
        {
            if (array.Length == 0)
            {
                return -1;
            }
            var view = bytesBuffer.Add(array);
            var viewIndex = bufferViews.Count;
            bufferViews.Add(view);
            var accessorIndex = accessors.Count;
            accessors.Add(new glTFAccessor
            {
                bufferView = viewIndex,
                byteOffset = 0,
                componentType =  (int)GetComponentType<T>(),
                type = GetAccessorType<T>(),
                count = array.Length,
            });
            return accessorIndex;
        }
        #endregion
        #endregion

        #region Material & Texture
        public List<gltfTexture> textures = new List<gltfTexture>();
        public List<gltfImage> images = new List<gltfImage>();

        public IEnumerable<TextureWithIsAsset> ReadTextures()
        {
            if (textures == null)
            {
                return new TextureWithIsAsset[] { };
            }
            else
            {
                return textures.Select(x => x.GetTexture(baseDir, this, images));
            }
        }

        public List<GltfMaterial> materials;

        public IEnumerable<Material> ReadMaterials(Texture2D[] textures)
        {
            var shader = Shader.Find("Standard");
            if (materials == null)
            {
                var material = new Material(shader);
                return new Material[] { material };
            }
            else
            {
                return materials.Select(x =>
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
        #endregion

        public List<glTFMesh> meshes = new List<glTFMesh>();

        public List<glTFNode> nodes = new List<glTFNode>();

        public List<glTFSkin> skins = new List<glTFSkin>();

        public int scene;

        [Serializable]
        public struct gltfScene
        {
            public int[] nodes;
        }

        public List<gltfScene> scenes;

        public int[] rootnodes
        {
            get
            {
                return scenes[scene].nodes;
            }
        }

        public List<GltfAnimation> animations;

        struct MeshView
        {
            public glTFBufferView[] Indices;
            public Dictionary<String, glTFBufferView> Attributes;
        }

        public static glTF FromGameObject(GameObject go, ArrayByteBuffer bytesBuffer)
        {
            var copy = GameObject.Instantiate(go);
            try
            {
                // Left handed to Right handed
                copy.transform.ReverseZ();

                return _FromGameObject(copy, bytesBuffer);
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
        }

        static glTF _FromGameObject(GameObject go, ArrayByteBuffer bytesBuffer)
        {
            var gltf = new glTF();
            gltf.asset = new glTFAssets
            {
                generator = "UniGLTF",
                version = 2.0f,
            };

            var unityNodes = go.transform.Traverse()
                .Skip(1) // exclude root object for the symmetry with the importer
                .ToList();

            #region Material
            var unityMaterials = unityNodes.SelectMany(x => x.GetSharedMaterials()).Where(x => x != null).Distinct().ToList();
            var unityTextures = unityMaterials.Select(x => (Texture2D)x.mainTexture).Where(x => x != null).Distinct().ToList();

            var textureViews = unityTextures.Select(x =>
            {
                var bytes = x.EncodeToPNG();
                return bytesBuffer.Add(bytes);
            }).ToList();

            for (int i = 0; i < unityTextures.Count; ++i)
            {
                var texture = unityTextures[i];
                var bytes = texture.EncodeToPNG();

                // add view
                var view = bytesBuffer.Add(bytes);
                var viewIndex = gltf.bufferViews.Count;
                gltf.bufferViews.Add(view);

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

                var positionAccessorIndex = gltf.AddBuffer(bytesBuffer, x.vertices.Select(y => y.ReverseZ()).ToArray());
                var normalAccessorIndex = gltf.AddBuffer(bytesBuffer, x.normals.Select(y => y.ReverseZ()).ToArray());
                var uvAccessorIndex = gltf.AddBuffer(bytesBuffer, x.uv.Select(y => y.ReverseY()).ToArray());
                var tangentAccessorIndex = gltf.AddBuffer(bytesBuffer, x.tangents);

                var boneweights = x.boneWeights;
                var weightAccessorIndex = gltf.AddBuffer(bytesBuffer, boneweights.Select(y => new Vector4(y.weight0, y.weight1, y.weight2, y.weight3)).ToArray());
                var jointsAccessorIndex = gltf.AddBuffer(bytesBuffer, boneweights.Select(y => new UShort4((ushort)y.boneIndex0, (ushort)y.boneIndex1, (ushort)y.boneIndex2, (ushort)y.boneIndex3)).ToArray());

                gltf.meshes.Add(new glTFMesh(x.name));
                for (int j = 0; j < x.subMeshCount; ++j)
                {
                    var indices = x.GetIndices(j);
                    var indicesView = bytesBuffer.Add(indices);
                    var indicesViewIndex = gltf.bufferViews.Count;
                    gltf.bufferViews.Add(indicesView);
                    var indicesAccessorIndex = gltf.accessors.Count;
                    gltf.accessors.Add(new glTFAccessor
                    {
                        bufferView = indicesViewIndex,
                        byteOffset = 0,
                        componentType = (int)glComponentType.INT,
                        type = "SCALAR",
                        count = indices.Length,
                    });

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
            gltf.nodes = unityNodes.Select(x => glTFNode.Create(x, unityNodes, unityMeshes, unitySkins)).ToList();
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
                var accessor = gltf.AddBuffer(bytesBuffer, matrices);

                var skin = new glTFSkin
                {
                    inverseBindMatrices = accessor,
                    joints = x.bones.Select(y => unityNodes.IndexOf(y)).ToArray(),
                };
                var skinIndex = gltf.skins.Count;
                gltf.skins.Add(skin);
            }

            return gltf;
        }

        public override string ToString()
        {
            return string.Format("{0}", asset);
        }

        public ArraySegment<Byte> ToJson()
        {
            var formatter = new JsonFormatter();
            formatter.BeginMap();



            formatter.EndMap();
            return formatter.GetStore().Bytes;
        }

        public static glTF Parse(string json, string baseDir, ArraySegment<Byte> glbDataBytes)
        {
            var parsed = json.ParseAsJson();

            var gltf = new glTF
            {
                baseDir = baseDir,
            };

            // asset
            gltf.asset = JsonUtility.FromJson<glTFAssets>(parsed["asset"].Segment.ToString());
            if (gltf.asset.version != 2.0f)
            {
                throw new NotImplementedException(string.Format("unknown version: {0}", gltf.asset.version));
            }

            // buffer
            //gltf.buffer = new GltfBuffer(parsed, baseDir, bytes);
            gltf.buffers = parsed["buffers"].DeserializeList<glTFBuffer>();
            gltf.bufferViews = parsed["bufferViews"].DeserializeList<glTFBufferView>();
            gltf.accessors = parsed["accessors"].DeserializeList<glTFAccessor>();
            if (glbDataBytes.Count > 0)
            {
                gltf.m_bytesList = new[] { new ArraySegmentByteBuffer(glbDataBytes) };
            }
            else
            {
                gltf.m_bytesList = gltf.buffers.Select(x => new ArrayByteBuffer(x.GetBytes(baseDir))).ToArray();
            }

            // texture
            if (parsed.HasKey("textures"))
            {
                gltf.textures = parsed["textures"].DeserializeList<gltfTexture>();
            }

            if (parsed.HasKey("images"))
            {
                gltf.images = parsed["images"].DeserializeList<gltfImage>();
            }

            // material
            if (parsed.HasKey("materials"))
            {
                gltf.materials = parsed["materials"].DeserializeList<GltfMaterial>();
            }

            // mesh
            if (parsed.HasKey("meshes"))
            {
                gltf.meshes = parsed["meshes"].DeserializeList<glTFMesh>();
            }

            // nodes
            gltf.nodes = parsed["nodes"].DeserializeList<glTFNode>();

            // skins
            if (parsed.HasKey("skins"))
            {
                gltf.skins = parsed["skins"].DeserializeList<glTFSkin>();
            }

            // scene;
            if (parsed.HasKey("scene"))
            {
                gltf.scene=parsed["scene"].GetInt32();
            }
            gltf.scenes = parsed["scenes"].DeserializeList<gltfScene>();

            // animations
            if (parsed.HasKey("animations"))
            {
                gltf.animations = parsed["animations"].DeserializeList<GltfAnimation>();
            }

            return gltf;
        }
    }
}
