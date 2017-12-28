using NUnit.Framework;
using Osaru;
using System;

public class ByteUnionTests
{

    [Test]
    public void Int16ValueTest()
    {
        {
            var value = new ByteUnion.WordValue
            {
                Signed = 255 + 1
            };

            if (BitConverter.IsLittleEndian)
            {
                Assert.AreEqual(0x00, value.Byte0);
                Assert.AreEqual(0x01, value.Byte1);
            }
            else
            {
                Assert.AreEqual(0x01, value.Byte0);
                Assert.AreEqual(0x00, value.Byte1);
            }
        }
    }

    [Test]
    public void Int32ValueTest()
    {
        {
            var value = new ByteUnion.DWordValue
            {
                Signed = (1 << 16)
            };

            if (BitConverter.IsLittleEndian)
            {
                Assert.AreEqual(0x00, value.Byte0);
                Assert.AreEqual(0x00, value.Byte1);
                Assert.AreEqual(0x01, value.Byte2);
                Assert.AreEqual(0x00, value.Byte3);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    [Test]
    public void Int64ValueTest()
    {
        {
            var value = new ByteUnion.QWordValue
            {
                Signed = ((Int64)1 << 32)
            };

            if (BitConverter.IsLittleEndian)
            {
                Assert.AreEqual(0x00, value.Byte0);
                Assert.AreEqual(0x00, value.Byte1);
                Assert.AreEqual(0x00, value.Byte2);
                Assert.AreEqual(0x00, value.Byte3);
                Assert.AreEqual(0x01, value.Byte4);
                Assert.AreEqual(0x00, value.Byte5);
                Assert.AreEqual(0x00, value.Byte6);
                Assert.AreEqual(0x00, value.Byte7);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
