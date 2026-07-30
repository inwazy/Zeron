using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.ZCore.Utils;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class AgentIdProviderTests
    {
        [TestMethod()]
        public void LoadOrCreateUsesConfiguredValueTest()
        {
            string agentId = AgentIdProvider.LoadOrCreate("configured-id", Path.GetTempFileName());

            Assert.AreEqual("configured-id", agentId);
        }

        [TestMethod()]
        public void LoadOrCreatePersistsGeneratedIdTest()
        {
            string identityFile = Path.Combine(Path.GetTempPath(), "zeron-test-" + Guid.NewGuid().ToString("N") + ".id");

            try
            {
                string first = AgentIdProvider.LoadOrCreate("", identityFile);
                string second = AgentIdProvider.LoadOrCreate("", identityFile);

                Assert.IsFalse(string.IsNullOrWhiteSpace(first));
                Assert.AreEqual(first, second);
            }
            finally
            {
                if (File.Exists(identityFile))
                {
                    File.Delete(identityFile);
                }
            }
        }
    }
}
