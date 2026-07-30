using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.ZCore.Utils;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class ApiKeyValidatorTests
    {
        [TestMethod()]
        public void ValidateMatchingKeyTest()
        {
            string encryptedKey = EncryptionProvider.Encrypt("zeron.testkey");

            bool isValid = ApiKeyValidator.Validate("zeron.testkey", encryptedKey);

            Assert.IsTrue(isValid);
        }

        [TestMethod()]
        public void ValidateMultipleKeysTest()
        {
            string encryptedKey = EncryptionProvider.Encrypt("key-two");

            bool isValid = ApiKeyValidator.Validate("key-one|key-two;key-three", encryptedKey);

            Assert.IsTrue(isValid);
        }

        [TestMethod()]
        public void ValidateWrongKeyTest()
        {
            string encryptedKey = EncryptionProvider.Encrypt("wrong-key");

            bool isValid = ApiKeyValidator.Validate("zeron.testkey", encryptedKey);

            Assert.IsFalse(isValid);
        }

        [TestMethod()]
        public void ValidateEmptyKeyTest()
        {
            bool isValid = ApiKeyValidator.Validate("", EncryptionProvider.Encrypt("zeron.testkey"));

            Assert.IsFalse(isValid);
        }
    }
}
