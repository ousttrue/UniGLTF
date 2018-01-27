using System;
using System.Collections.Generic;
using System.Linq;


namespace UniGLTF
{
    [Serializable]
    public class gltfScene : IJsonSerializable
    {
        public int[] nodes;

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.KeyValue(() => nodes);
            f.EndMap();
            return f.ToString();
        }
    }

    public class glTF : IJsonSerializable
    {
        public string baseDir
        {
            get;
            set;
        }

        public glTFAssets asset = new glTFAssets
        {
            generator = "UniGLTF",
            version = "2.0",
        };

        #region Buffer      
        public List<glTFBuffer> buffers = new List<glTFBuffer>();
        public int AddBuffer(IBytesBuffer bytesBuffer)
        {
            var index = buffers.Count;
            buffers.Add(new glTFBuffer(bytesBuffer));
            return index;
        }

        public List<glTFBufferView> bufferViews = new List<glTFBufferView>();
        public int AddBufferView(glTFBufferView view)
        {
            var index = bufferViews.Count;
            bufferViews.Add(view);
            return index;
        }

        public List<glTFAccessor> accessors = new List<glTFAccessor>();

        T[] GetAttrib<T>(glTFAccessor accessor, glTFBufferView view) where T : struct
        {
            var attrib = new T[accessor.count];
            //
            var segment = buffers[view.buffer].Storage.GetBytes();
            var bytes = new ArraySegment<Byte>(segment.Array, segment.Offset + view.byteOffset + accessor.byteOffset, accessor.count * view.byteStride);
            bytes.MarshalCoyTo(attrib);
            return attrib;
        }

        public ArraySegment<Byte> GetViewBytes(int bufferView)
        {
            var view = bufferViews[bufferView];
            var segment = buffers[view.buffer].Storage.GetBytes();
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
                        return TriangleUtil.FlipTriangle(indices).ToArray();
                    }

                case glComponentType.UNSIGNED_SHORT:
                    {
                        var indices = GetAttrib<UInt16>(accessor, view);
                        return TriangleUtil.FlipTriangle(indices).ToArray();
                    }

                /*
            case glComponentType.INT:
                {
                    var indices = GetAttrib<Int32>(accessor, view);
                    return FlipTriangle(indices).ToArray();
                }
                */

                case glComponentType.UNSIGNED_INT:
                    {
                        var indices = GetAttrib<UInt32>(accessor, view);
                        return TriangleUtil.FlipTriangle(indices).ToArray();
                    }
            }

            throw new NotImplementedException("GetIndices: unknown componenttype: " + accessor.componentType);
        }

        public T[] GetArrayFromAccessor<T>(int index) where T : struct
        {
            var vertexAccessor = accessors[index];
            var view = bufferViews[vertexAccessor.bufferView];
            return GetAttrib<T>(vertexAccessor, view);
        }
        #endregion

        public List<glTFTexture> textures = new List<glTFTexture>();
        public List<glTFSampler> samplers = new List<glTFSampler>();
        public List<glTFImage> images = new List<glTFImage>();
        public List<glTFMaterial> materials = new List<glTFMaterial>();
        public List<glTFMesh> meshes = new List<glTFMesh>();
        public List<glTFNode> nodes = new List<glTFNode>();
        public List<glTFSkin> skins = new List<glTFSkin>();
        public int scene;
        public List<gltfScene> scenes = new List<gltfScene>();
        public int[] rootnodes
        {
            get
            {
                return scenes[scene].nodes;
            }
        }
        public List<GltfAnimation> animations;

        public override string ToString()
        {
            return string.Format("{0}", asset);
        }

        public string ToJson()
        {
            var f = new JsonFormatter();
            f.BeginMap();
            f.KeyValue(() => asset);

            // buffer
            if (buffers.Any())
            {
                f.KeyValue(() => buffers);
            }
            if (bufferViews.Any())
            {
                f.Key("bufferViews"); f.Value(bufferViews);
            }
            if (accessors.Any())
            {
                f.Key("accessors"); f.Value(accessors);
            }

            // materials
            if (images.Any())
            {
                f.Key("images"); f.Value(images);
            }
            if (samplers.Any())
            {
                f.Key("samplers"); f.Value(samplers);
            }
            if (textures.Any())
            {
                f.Key("textures"); f.Value(textures);
            }
            if (materials.Any())
            {
                f.Key("materials"); f.Value(materials);
            }

            // meshes
            if (meshes.Any())
            {
                f.Key("meshes"); f.Value(meshes);
            }
            if (skins.Any())
            {
                f.KeyValue(() => skins);
            }

            // scene
            if (nodes.Any())
            {
                f.Key("nodes"); f.Value(nodes);
            }
            if (scenes.Any())
            {
                f.KeyValue(() => scenes);
            }

            f.EndMap();
            return f.ToString();
        }
    }
}
