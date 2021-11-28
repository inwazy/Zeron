using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class EncryptionProviderTests
    {
        [TestMethod()]
        public void EncryptTest()
        {
            string userNamePayload = EncryptionProvider.Encrypt("Ji-Feng Tsai");

            Assert.IsNotNull(userNamePayload);
        }

        [TestMethod()]
        public void DecryptTest()
        {
            string userName = EncryptionProvider.Decrypt("bbZNJMF5fwxVk5F9ePLvkg==");

            Assert.AreEqual(userName, "Ji-Feng Tsai");
        }
    }
}