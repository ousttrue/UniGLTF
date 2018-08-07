using System;
using UniJSON;

namespace UniGLTF
{
    [Serializable]
    public class glTF_KHR_materials_unlit : JsonSerializableBase
    {
        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            //throw new System.NotImplementedException();
        }
    }

    [Serializable]
    public partial class glTFMaterial_extensions : ExtensionsBase<glTFMaterial_extensions>
    {
        [JsonSchema(Required = true)]
        public glTF_KHR_materials_unlit KHR_materials_unlit;
    }
}
