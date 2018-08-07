using NUnit.Framework;


namespace UniGLTF
{
    public class MaterialTests
    {

        [Test]
        public void ShaderImportTest()
        {
            var shaderStore = new ShaderStore(null);
            var shader = shaderStore.GetShader(new glTFMaterial
            {

            });

            Assert.AreEqual("Standard", shader.name);
        }
    }
}
