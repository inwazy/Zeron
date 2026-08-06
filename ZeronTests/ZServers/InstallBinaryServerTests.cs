// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore.Type;
using Zeron.ZServers;

namespace Zeron.ZServers.Tests
{
    [TestClass()]
    public class InstallBinaryServerTests
    {
        [TestMethod()]
        public void TryDownloadRejectsIncompleteQueuesTypeTest()
        {
            Assert.IsFalse(InstallBinaryServer.TryDownload(null));
            Assert.IsFalse(InstallBinaryServer.TryDownload(new InstallQueuesType
            {
                RepoUrl = "",
                FilePath = "C:\\temp\\installer.exe"
            }));
            Assert.IsFalse(InstallBinaryServer.TryDownload(new InstallQueuesType
            {
                RepoUrl = "https://example.com/pkg.exe",
                FilePath = ""
            }));
        }

        [TestMethod()]
        public void TryDownloadReturnsTrueWhenLocalFileExistsTest()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "zeron-install-" + Guid.NewGuid().ToString("N") + ".bin");

            try
            {
                File.WriteAllBytes(tempFile, [1, 2, 3]);

                bool downloaded = InstallBinaryServer.TryDownload(new InstallQueuesType
                {
                    RepoUrl = "https://example.invalid/should-not-download.exe",
                    FilePath = tempFile
                });

                Assert.IsTrue(downloaded);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [TestMethod()]
        public void InstallServerGetBinaryFileFromUrlDelegatesTest()
        {
            Assert.IsFalse(InstallServer.GetBinaryFileFromUrl(null));
        }
    }
}
