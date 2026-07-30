using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.ZCore.Utils;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class ApiScopeValidatorTests
    {
        [TestMethod()]
        public void AllowAllWhenScopeIsStarTest()
        {
            Assert.IsTrue(ApiScopeValidator.IsAllowed("*", "PowerShell", "write"));
        }

        [TestMethod()]
        public void AllowMatchingApiAndActionTest()
        {
            string scopes = "HealthCheck:read,ManagedPackage:install";

            Assert.IsTrue(ApiScopeValidator.IsAllowed(scopes, "HealthCheck", "read"));
            Assert.IsTrue(ApiScopeValidator.IsAllowed(scopes, "ManagedPackage", "install"));
        }

        [TestMethod()]
        public void DenyMissingScopeTest()
        {
            string scopes = "HealthCheck:read";

            Assert.IsFalse(ApiScopeValidator.IsAllowed(scopes, "PowerShell", "write"));
        }

        [TestMethod()]
        public void AllowApiWildcardScopeTest()
        {
            Assert.IsTrue(ApiScopeValidator.IsAllowed("FileSystem:*", "FileSystem", "write"));
        }
    }
}
