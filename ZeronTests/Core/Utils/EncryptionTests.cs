using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zeron.Core.Utils.Tests
{
    [TestClass()]
    public class EncryptionTests
    {
        [TestMethod()]
        public void EncryptTest()
        {
            string userNamePayload = Encryption.Encrypt("Ji-Feng Tsai");

            Assert.IsNotNull(userNamePayload);
        }

        [TestMethod()]
        public void DecryptTest()
        {
            string userName = Encryption.Decrypt("JozAlZESAof1SwpzMSMvIQ==");

            Assert.AreEqual(userName, "Ji-Feng Tsai");
        }
    }
}