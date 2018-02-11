using NUnit.Framework;
using UniGLTF;
using System;
using System.Text;


namespace UniGLTF
{
    public class StoreTests
    {
        [Test]
        public void StringBuilderStoreTest()
        {
            var sb = new StringBuilder();
            var stream = new StringBuilderStore(sb);

            stream.Write("abc");
            Assert.AreEqual("abc", sb.ToString());

            stream.Write("d");
            Assert.AreEqual("abcd", sb.ToString());

            sb.Length = 0;
            stream.Write("e");
            Assert.AreEqual("e", sb.ToString());
        }
    }
}
