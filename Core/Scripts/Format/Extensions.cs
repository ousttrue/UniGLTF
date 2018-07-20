using System;
using System.Linq;
using System.Reflection;
using UniJSON;


namespace UniGLTF
{
    public class UsedExtensionAttribute : Attribute { }

    public class JsonSerializeMembersAttribute : Attribute { }

    [Serializable]
    [ItemJsonSchema(ValueType = JsonValueType.Object)]
    public partial class glTF_extensions : JsonSerializableBase
    {
        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            foreach (var method in typeof(glTF_extensions).GetMethods(BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttributes(typeof(JsonSerializeMembersAttribute), true).Any())
                {
                    method.Invoke(this, new[] { f });
                }
            }
        }
    }

    #region Camera
    [Serializable]
    [ItemJsonSchema(ValueType = JsonValueType.Object)]
    public partial class glTFOrthographic_extensions { }

    [Serializable]
    public partial class glTFOrthographic_extras { }

    [Serializable]
    [ItemJsonSchema(ValueType = JsonValueType.Object)]
    public partial class glTFPerspective_extensions { }

    [Serializable]
    public partial class glTFPerspective_extrans { }

    [Serializable]
    [ItemJsonSchema(ValueType = JsonValueType.Object)]
    public partial class glTFCamera_extensions { }

    [Serializable]
    public partial class glTFCamera_extras { }
    #endregion

    [Serializable]
    [ItemJsonSchema(ValueType = JsonValueType.Object)]
    public partial class gltfScene_extensions { }

    [Serializable]
    public partial class gltfScene_extras { }
}
