using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.ZCore.Utils;

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
            Assert.IsTrue(userNamePayload.Length > 0);
        }

        [TestMethod()]
        public void EncryptDecryptRoundTripTest()
        {
            const string plainText = "Ji-Feng Tsai";

            string cipherText = EncryptionProvider.Encrypt(plainText);
            bool decrypted = EncryptionProvider.TryDecrypt(cipherText, out string? result);

            Assert.IsTrue(decrypted);
            Assert.AreEqual(plainText, result);
        }

        [TestMethod()]
        public void TryDecryptInvalidCipherTest()
        {
            bool decrypted = EncryptionProvider.TryDecrypt("not-valid-base64!!!", out string? result);

            Assert.IsFalse(decrypted);
            Assert.IsNull(result);
        }
    }
}
