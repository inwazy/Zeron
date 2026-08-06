// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class EncryptionProviderTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            EncryptionProvider.ResetToDefaults();
        }

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

        [TestMethod()]
        public void ConfigureCustomKeysRoundTripTest()
        {
            EncryptionProvider.Configure("prod-salt-key-A", "prod-iv-key-ABCDEF");
            const string plainText = "zeron.prod.key";

            string cipherText = EncryptionProvider.Encrypt(plainText);
            bool decrypted = EncryptionProvider.TryDecrypt(cipherText, out string? result);

            Assert.AreEqual("prod-salt-key-A", EncryptionProvider.SaltKey);
            Assert.AreEqual("prod-iv-key-ABCDEF", EncryptionProvider.IvKey);
            Assert.IsTrue(decrypted);
            Assert.AreEqual(plainText, result);
        }

        [TestMethod()]
        public void ConfigureKeysMismatchCannotDecryptTest()
        {
            EncryptionProvider.Configure("salt-one", "iv-source-one!");
            string cipherText = EncryptionProvider.Encrypt("secret-payload");

            EncryptionProvider.Configure("salt-two", "iv-source-two!");
            bool decrypted = EncryptionProvider.TryDecrypt(cipherText, out string? result);

            Assert.IsFalse(decrypted);
            Assert.IsNull(result);
        }

        [TestMethod()]
        public void ConfigureIgnoresEmptyValuesTest()
        {
            EncryptionProvider.Configure("keep-salt", "keep-iv-value!!");
            EncryptionProvider.Configure("", "   ");

            Assert.AreEqual("keep-salt", EncryptionProvider.SaltKey);
            Assert.AreEqual("keep-iv-value!!", EncryptionProvider.IvKey);
        }

        [TestMethod()]
        public void ConfigureFromEnvironmentOverridesTest()
        {
            string? previousSalt = Environment.GetEnvironmentVariable(EncryptionProvider.EnvSaltKey);
            string? previousIv = Environment.GetEnvironmentVariable(EncryptionProvider.EnvIvKey);

            try
            {
                Environment.SetEnvironmentVariable(EncryptionProvider.EnvSaltKey, "env-salt-material");
                Environment.SetEnvironmentVariable(EncryptionProvider.EnvIvKey, "env-iv-material!!");

                EncryptionProvider.ResetToDefaults();
                EncryptionProvider.ConfigureFromEnvironment();

                Assert.AreEqual("env-salt-material", EncryptionProvider.SaltKey);
                Assert.AreEqual("env-iv-material!!", EncryptionProvider.IvKey);

                string cipher = EncryptionProvider.Encrypt("env-roundtrip");
                Assert.IsTrue(EncryptionProvider.TryDecrypt(cipher, out string? plain));
                Assert.AreEqual("env-roundtrip", plain);
            }
            finally
            {
                Environment.SetEnvironmentVariable(EncryptionProvider.EnvSaltKey, previousSalt);
                Environment.SetEnvironmentVariable(EncryptionProvider.EnvIvKey, previousIv);
            }
        }

        [TestMethod()]
        public void ResetToDefaultsRestoresLegacyKeysTest()
        {
            EncryptionProvider.Configure("custom", "custom-iv-value");
            EncryptionProvider.ResetToDefaults();

            Assert.AreEqual(EncryptionProvider.DefaultSaltKey, EncryptionProvider.SaltKey);
            Assert.AreEqual(EncryptionProvider.DefaultIvKey, EncryptionProvider.IvKey);
        }
    }
}
