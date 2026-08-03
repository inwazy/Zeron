// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class AgentApiKeyServerTests
    {
        /// <summary>
        /// SplitKeys and Matches support rotation lists.
        /// </summary>
        [TestMethod()]
        public void MatchesSupportsRotationKeysTest()
        {
            Assert.AreEqual("old", AgentApiKeyServer.GetPrimaryKey("old|new"));
            Assert.IsTrue(AgentApiKeyServer.Matches("old|new", "old"));
            Assert.IsTrue(AgentApiKeyServer.Matches("old|new", "new"));
            Assert.IsFalse(AgentApiKeyServer.Matches("old|new", "other"));
            Assert.AreEqual(2, AgentApiKeyServer.SplitKeys("old, new").Count);
        }
    }
}
