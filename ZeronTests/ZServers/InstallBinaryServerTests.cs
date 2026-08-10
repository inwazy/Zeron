// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.ZCore.Type;
using Zeron.ZServers;

namespace Zeron.ZServers.Tests
{
    [TestClass()]
    public class InstallBinaryServerTests
    {
        /// <summary>
        /// VerifySha256OrCleanup accepts matching digest.
        /// </summary>
        [TestMethod()]
        public void VerifySha256OrCleanupAcceptsMatchTest()
        {
            string path = Path.Combine(Path.GetTempPath(), "zeron-sha-" + Guid.NewGuid().ToString("N") + ".bin");
            byte[] payload = Encoding.UTF8.GetBytes("zeron-package");
            File.WriteAllBytes(path, payload);
            string expected = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

            try
            {
                Assert.IsTrue(InstallBinaryServer.VerifySha256OrCleanup(path, expected));
                Assert.IsTrue(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        /// <summary>
        /// VerifySha256OrCleanup deletes file on mismatch.
        /// </summary>
        [TestMethod()]
        public void VerifySha256OrCleanupDeletesOnMismatchTest()
        {
            string path = Path.Combine(Path.GetTempPath(), "zeron-sha-" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllText(path, "zeron-package");

            try
            {
                Assert.IsFalse(InstallBinaryServer.VerifySha256OrCleanup(path, new string('a', 64)));
                Assert.IsFalse(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        /// <summary>
        /// VerifySha256OrCleanup skips check when expected hash is empty.
        /// </summary>
        [TestMethod()]
        public void VerifySha256OrCleanupSkipsWhenEmptyTest()
        {
            string path = Path.Combine(Path.GetTempPath(), "zeron-sha-" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllText(path, "zeron-package");

            try
            {
                Assert.IsTrue(InstallBinaryServer.VerifySha256OrCleanup(path, null));
                Assert.IsTrue(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
