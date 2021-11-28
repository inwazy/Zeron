using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.ZCore.Type;

namespace Zeron.ZCore.Tests
{
    [TestClass()]
    public class HelperTests
    {
        [TestMethod()]
        public void BuildCommandsTest()
        {
            ServicesSubCommandType buildCommandsResult1 = new()
            {
                Option = "install",
                PackageName = "ccleaner",
                Args = "/s"
            };

            ServicesSubCommandType? buildCommands1 = Helper.BuildCommands("install ccleaner /s");

            ServicesSubCommandType buildCommandsResult2 = new()
            {
                Option = "install",
                PackageName = "ccleaner",
                Args = "/s /d"
            };

            ServicesSubCommandType? buildCommands2 = Helper.BuildCommands("install ccleaner /s /d");

            Assert.AreEqual(buildCommands1.Option, buildCommandsResult1.Option);
            Assert.AreEqual(buildCommands1.PackageName, buildCommandsResult1.PackageName);
            Assert.AreEqual(buildCommands1.Args, buildCommandsResult1.Args);

            Assert.AreEqual(buildCommands2.Option, buildCommandsResult2.Option);
            Assert.AreEqual(buildCommands2.PackageName, buildCommandsResult2.PackageName);
            Assert.AreEqual(buildCommands2.Args, buildCommandsResult2.Args);
        }
    }
}