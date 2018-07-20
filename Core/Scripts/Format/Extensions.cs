using System;
using UniJSON;


namespace UniGLTF
{
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
    public partial class gltfScene_extensions { }

    [Serializable]
    public partial class gltfScene_extras { }
}
