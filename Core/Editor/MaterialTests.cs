using NUnit.Framework;


namespace UniGLTF
{
    public class MaterialTests
    {

        [Test]
        public void ShaderImportTest()
        {
            var shaderStore = new ShaderStore(null);

            {
                var shader = shaderStore.GetShader(null);
                Assert.AreEqual("Standard", shader.name);
            }

            {
                var shader = shaderStore.GetShader(new glTFMaterial
                {
                });
                Assert.AreEqual("Standard", shader.name);
            }

            {
                var shader = shaderStore.GetShader(new glTFMaterial
                {
                    alphaMode="BLEND",
                    extensions = new glTFMaterial_extensions
                    {
                        KHR_materials_unlit = new glTF_KHR_materials_unlit { }                      
                    }
                });
                Assert.AreEqual("Unlit/Transparent", shader.name);
            }

            {
                var shader = shaderStore.GetShader(new glTFMaterial
                {
                    alphaMode = "MASK",
                    extensions = new glTFMaterial_extensions
                    {
                        KHR_materials_unlit = new glTF_KHR_materials_unlit { }
                    }
                });
                Assert.AreEqual("Unlit/Transparent Cutout", shader.name);
            }

            {
                var shader = shaderStore.GetShader(new glTFMaterial
                {
                    extensions=new glTFMaterial_extensions
                    {
                        KHR_materials_unlit=new glTF_KHR_materials_unlit { }
                    }
                });
                Assert.AreEqual("Unlit/Texture", shader.name);
            }
        }
    }
}
