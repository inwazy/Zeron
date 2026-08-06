// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class SecureCompareServerTests
    {
        [TestMethod()]
        public void FixedTimeEqualsMatchesEqualStringsTest()
        {
            Assert.IsTrue(SecureCompareServer.FixedTimeEquals("zeron.testkey", "zeron.testkey"));
            Assert.IsFalse(SecureCompareServer.FixedTimeEquals("zeron.testkey", "zeron.other"));
            Assert.IsFalse(SecureCompareServer.FixedTimeEquals("short", "longer-value"));
            Assert.IsFalse(SecureCompareServer.FixedTimeEquals(null, "x"));
            Assert.IsFalse(SecureCompareServer.FixedTimeEquals("x", null));
        }

        [TestMethod()]
        public void FixedTimeEqualsHexIsCaseInsensitiveTest()
        {
            const string digest = "aabbccddeeff00112233445566778899";

            Assert.IsTrue(SecureCompareServer.FixedTimeEqualsHex(digest, digest.ToUpperInvariant()));
            Assert.IsTrue(SecureCompareServer.FixedTimeEqualsHex("  " + digest + "  ", digest));
            Assert.IsFalse(SecureCompareServer.FixedTimeEqualsHex(digest, "bb" + digest[2..]));
            Assert.IsFalse(SecureCompareServer.FixedTimeEqualsHex("", digest));
        }
    }
}
