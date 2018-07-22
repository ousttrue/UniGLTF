using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniJSON;


namespace UniGLTF
{
    #region Base
    public class UsedExtensionAttribute : Attribute { }

    public class JsonSerializeMembersAttribute : Attribute { }

    public class PartialExtensionBase<T> : JsonSerializableBase
    {
        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            foreach (var method in this.GetType().GetMethods(BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttributes(typeof(JsonSerializeMembersAttribute), true).Any())
                {
                    method.Invoke(this, new[] { f });
                }
            }
        }

        public int __count
        {
            get
            {
                return typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.GetCustomAttributes(typeof(JsonSerializeMembersAttribute), true).Any())
                    .Count();
            }
        }
    }

    [Serializable]
    [ItemJsonSchema(ValueType = JsonValueType.Object)]
    public partial class ExtensionsBase<T> : PartialExtensionBase<T>
    {
    }

    [Serializable]
    public partial class ExtraBase<T> : PartialExtensionBase<T>
    {
    }
    #endregion

    public partial class glTF_extensions : ExtensionsBase<glTF_extensions> { }

    public partial class gltf_extras : ExtraBase<gltf_extras> { }

    #region Camera
    [Serializable]
    [ItemJsonSchema(ValueType = JsonValueType.Object)]
    public partial class glTFOrthographic_extensions: ExtensionsBase<glTFOrthographic_extensions> { }

    [Serializable]
    public partial class glTFOrthographic_extras: ExtraBase<glTFOrthographic_extras> { }

    [Serializable]
    [ItemJsonSchema(ValueType = JsonValueType.Object)]
    public partial class glTFPerspective_extensions: ExtensionsBase<glTFPerspective_extensions> { }

    [Serializable]
    public partial class glTFPerspective_extras: ExtraBase<glTFPerspective_extras> { }

    [Serializable]
    [ItemJsonSchema(ValueType = JsonValueType.Object)]
    public partial class glTFCamera_extensions: ExtensionsBase<glTFCamera_extensions> { }

    [Serializable]
    public partial class glTFCamera_extras: ExtraBase<glTFCamera_extras> { }
    #endregion

    #region Mesh
    /// <summary>
    /// https://github.com/KhronosGroup/glTF/issues/1036
    /// </summary>
    [Serializable]
    public partial class glTFPrimitives_extras : ExtraBase<glTFPrimitives_extras>
    {
        [JsonSchema(Required = true, MinItems = 1)]
        public List<string> targetNames = new List<string>();

        [JsonSerializeMembers]
        void PrimitiveMembers(GLTFJsonFormatter f)
        {
            if (targetNames.Count > 0)
            {
                f.Key("targetNames");
                f.BeginList();
                foreach (var x in targetNames)
                {
                    f.Value(x);
                }
                f.EndList();
            }
        }
    }

    #region Draco
    [Serializable]
    public class glTF_KHR_draco_mesh_compression : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int bufferView = -1;
        public glTFAttributes attributes;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            //throw new NotImplementedException();
        }
    }

    [Serializable]
    public partial class glTFPrimitives_extensions : ExtensionsBase<glTFPrimitives_extensions>
    {
        [JsonSchema(Required = true)]
        public glTF_KHR_draco_mesh_compression KHR_draco_mesh_compression;

        [JsonSerializeMembers]
        void SerializeMembers_draco(GLTFJsonFormatter f)
        {
            //throw new NotImplementedException();
        }
    }
    #endregion
    #endregion

    #region Scene
    /// <summary>
    /// for Unity's SkinnedMeshRenderer.rootBone
    /// </summary>
    [Serializable]
    public partial class glTFNode_extra : ExtraBase<glTFNode_extra>
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int skinRootBone = -1; 

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => skinRootBone);
        }
    }
    #endregion
}
