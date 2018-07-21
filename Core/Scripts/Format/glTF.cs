using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UniJSON;


namespace UniGLTF
{
    [Serializable]
    public class gltfScene : JsonSerializableBase
    {
        [JsonSchema(MinItems = 1)]
        [ItemJsonSchema(Minimum = 0)]
        public int[] nodes;

        public gltfScene_extensions extensions;
        public gltfScene_extras extras;
        public string name;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => nodes);
        }
    }

    public partial class glTFUsedExtensions
    {
        public static IEnumerable<string> GeetUsedExtensions()
        {
            foreach (var prop in typeof(glTFUsedExtensions).GetProperties(BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.GetCustomAttributes(typeof(UsedExtensionAttribute), true).Any())
                {
                    var extension = (string)prop.GetValue(null, new object[] { });
                    yield return extension;
                }
            }
        }
    }

    [Serializable]
    public class glTF : JsonSerializableBase, IEquatable<glTF>
    {
        public string baseDir
        {
            get;
            set;
        }

        [JsonSchema(Required = true)]
        public glTFAssets asset = new glTFAssets();

        #region Buffer      
        [JsonSchema(MinItems = 1)]
        public List<glTFBuffer> buffers = new List<glTFBuffer>();
        public int AddBuffer(IBytesBuffer bytesBuffer)
        {
            var index = buffers.Count;
            buffers.Add(new glTFBuffer(bytesBuffer));
            return index;
        }

        [JsonSchema(MinItems = 1)]
        public List<glTFBufferView> bufferViews = new List<glTFBufferView>();
        public int AddBufferView(glTFBufferView view)
        {
            var index = bufferViews.Count;
            bufferViews.Add(view);
            return index;
        }

        [JsonSchema(MinItems = 1)]
        public List<glTFAccessor> accessors = new List<glTFAccessor>();

        T[] GetAttrib<T>(glTFAccessor accessor, glTFBufferView view) where T : struct
        {
            return GetAttrib<T>(accessor.count, accessor.byteOffset, view);
        }
        T[] GetAttrib<T>(int count, int byteOffset, glTFBufferView view) where T : struct
        {
            var attrib = new T[count];
            //
            var segment = buffers[view.buffer].GetBytes();
            var bytes = new ArraySegment<Byte>(segment.Array, segment.Offset + view.byteOffset + byteOffset, count * view.byteStride);
            bytes.MarshalCoyTo(attrib);
            return attrib;
        }

        public ArraySegment<Byte> GetViewBytes(int bufferView)
        {
            var view = bufferViews[bufferView];
            var segment = buffers[view.buffer].GetBytes();
            return new ArraySegment<byte>(segment.Array, segment.Offset + view.byteOffset, view.byteLength);
        }

        IEnumerable<int> _GetIndices(glTFAccessor accessor, out int count)
        {
            count = accessor.count;
            var view = bufferViews[accessor.bufferView];
            switch ((glComponentType)accessor.componentType)
            {
                case glComponentType.UNSIGNED_BYTE:
                    {
                        return GetAttrib<Byte>(accessor, view).Select(x => (int)(x));
                    }

                case glComponentType.UNSIGNED_SHORT:
                    {
                        return GetAttrib<UInt16>(accessor, view).Select(x => (int)(x));
                    }

                case glComponentType.UNSIGNED_INT:
                    {
                        return GetAttrib<UInt32>(accessor, view).Select(x => (int)(x));
                    }
            }
            throw new NotImplementedException("GetIndices: unknown componenttype: " + accessor.componentType);
        }

        IEnumerable<int> _GetIndices(glTFBufferView view, int count, int byteOffset, glComponentType componentType)
        {
            switch (componentType)
            {
                case glComponentType.UNSIGNED_BYTE:
                    {
                        return GetAttrib<Byte>(count, byteOffset, view).Select(x => (int)(x));
                    }

                case glComponentType.UNSIGNED_SHORT:
                    {
                        return GetAttrib<UInt16>(count, byteOffset, view).Select(x => (int)(x));
                    }

                case glComponentType.UNSIGNED_INT:
                    {
                        return GetAttrib<UInt32>(count, byteOffset, view).Select(x => (int)(x));
                    }
            }
            throw new NotImplementedException("GetIndices: unknown componenttype: " + componentType);
        }

        public int[] GetIndices(int accessorIndex)
        {
            int count;
            var result = _GetIndices(accessors[accessorIndex], out count);
            var indices = new int[count];

            // flip triangles
            var it = result.GetEnumerator();
            {
                for (int i = 0; i < count; i += 3)
                {
                    it.MoveNext(); indices[i + 2] = it.Current;
                    it.MoveNext(); indices[i + 1] = it.Current;
                    it.MoveNext(); indices[i] = it.Current;
                }
            }

            return indices;
        }

        public T[] GetArrayFromAccessor<T>(int accessorIndex) where T : struct
        {
            var vertexAccessor = accessors[accessorIndex];

            if (vertexAccessor.count <= 0) return new T[] { };

            var result = (vertexAccessor.bufferView != -1)
                ? GetAttrib<T>(vertexAccessor, bufferViews[vertexAccessor.bufferView])
                : new T[vertexAccessor.count]
                ;

            var sparse = vertexAccessor.sparse;
            if (sparse != null && sparse.count > 0)
            {
                // override sparse values
                var indices = _GetIndices(bufferViews[sparse.indices.bufferView], sparse.count, sparse.indices.byteOffset, sparse.indices.componentType);
                var values = GetAttrib<T>(sparse.count, sparse.values.byteOffset, bufferViews[sparse.values.bufferView]);

                var it = indices.GetEnumerator();
                for (int i = 0; i < sparse.count; ++i)
                {
                    it.MoveNext();
                    result[it.Current] = values[i];
                }
            }
            return result;
        }
        #endregion

        [JsonSchema(MinItems = 1)]
        public List<glTFTexture> textures = new List<glTFTexture>();

        [JsonSchema(MinItems = 1)]
        public List<glTFTextureSampler> samplers = new List<glTFTextureSampler>();
        public glTFTextureSampler GetSampler(int index)
        {
            if (samplers.Count == 0)
            {
                samplers.Add(new glTFTextureSampler()); // default sampler
            }

            return samplers[index];
        }

        [JsonSchema(MinItems = 1)]
        public List<glTFImage> images = new List<glTFImage>();

        public glTFImage GetImageFromTextureIndex(int textureIndex)
        {
            return images[textures[textureIndex].source];
        }

        public glTFTextureSampler GetSamplerFromTextureIndex(int textureIndex)
        {
            var samplerIndex = textures[textureIndex].sampler;
            return GetSampler(samplerIndex);
        }

        [JsonSchema(MinItems = 1)]
        public List<glTFMaterial> materials = new List<glTFMaterial>();
        public string GetUniqueMaterialName(int index)
        {
            if (materials.Any(x => string.IsNullOrEmpty(x.name))
                || materials.Select(x => x.name).Distinct().Count() != materials.Count)
            {
                return String.Format("{0:00}_{1}", index, materials[index].name);
            }
            else
            {
                return materials[index].name;
            }
        }

        [JsonSchema(MinItems = 1)]
        public List<glTFMesh> meshes = new List<glTFMesh>();

        [JsonSchema(MinItems = 1)]
        public List<glTFNode> nodes = new List<glTFNode>();

        [JsonSchema(MinItems = 1)]
        public List<glTFSkin> skins = new List<glTFSkin>();

        [JsonSchema(Dependencies = new string[] { "scenes" }, Minimum = 0)]
        public int scene;

        [JsonSchema(MinItems = 1)]
        public List<gltfScene> scenes = new List<gltfScene>();
        public int[] rootnodes
        {
            get
            {
                return scenes[scene].nodes;
            }
        }

        [JsonSchema(MinItems = 1)]
        public List<glTFAnimation> animations = new List<glTFAnimation>();

        [JsonSchema(MinItems = 1)]
        public List<glTFCamera> cameras = new List<glTFCamera>();

        [JsonSchema(MinItems = 1)]
        public List<string> extensionsUsed = glTFUsedExtensions.GeetUsedExtensions().ToList();

        [JsonSchema(MinItems = 1)]
        public List<string> extensionsRequired = new List<string>();

        public glTF_extensions extensions = new glTF_extensions();
        public glTF_extras extras = new glTF_extras();

        public override string ToString()
        {
            return string.Format("{0}", asset);
        }

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            if (extensionsUsed.Count > 0)
            {
                f.KeyValue(() => extensionsUsed);
            }
            if (extensions.Count > 0)
            {
                f.KeyValue(() => extensions);
            }
            if (extras.Count > 0)
            {
                f.KeyValue(() => extras);
            }

            f.KeyValue(() => asset);

            // buffer
            if (buffers.Any())
            {
                f.KeyValue(() => buffers);
            }
            if (bufferViews.Any())
            {
                f.Key("bufferViews"); f.GLTFValue(bufferViews);
            }
            if (accessors.Any())
            {
                f.Key("accessors"); f.GLTFValue(accessors);
            }

            // materials
            if (images.Any())
            {
                f.Key("images"); f.GLTFValue(images);
                if (samplers.Count == 0)
                {
                    samplers.Add(new glTFTextureSampler());
                }
            }

            if (samplers.Any())
            {
                f.Key("samplers"); f.GLTFValue(samplers);
            }

            if (textures.Any())
            {
                f.Key("textures"); f.GLTFValue(textures);
            }
            if (materials.Any())
            {
                f.Key("materials"); f.GLTFValue(materials);
            }

            // meshes
            if (meshes.Any())
            {
                f.KeyValue(() => meshes);
            }
            if (skins.Any())
            {
                f.KeyValue(() => skins);
            }

            // scene
            if (nodes.Any())
            {
                f.KeyValue(() => nodes);
            }
            if (scenes.Any())
            {
                f.KeyValue(() => scenes);
            }
            if (scene >= 0)
            {
                f.KeyValue(() => scene);
            }

            // animations
            if (animations.Any())
            {
                f.Key("animations"); f.GLTFValue(animations);
            }
        }

        public bool Equals(glTF other)
        {
            return
                textures.SequenceEqual(other.textures)
                && samplers.SequenceEqual(other.samplers)
                && images.SequenceEqual(other.images)
                && materials.SequenceEqual(other.materials)
                && meshes.SequenceEqual(other.meshes)
                && nodes.SequenceEqual(other.nodes)
                && skins.SequenceEqual(other.skins)
                && scene == other.scene
                && scenes.SequenceEqual(other.scenes)
                && animations.SequenceEqual(other.animations)
                ;
        }

        public byte[] ToGlbBytes()
        {
            var json = ToJson();

            var buffer = buffers[0];
            using (var s = new MemoryStream())
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

                return s.ToArray();
            }
        }
    }
}
