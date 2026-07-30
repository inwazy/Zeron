namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class Md5ProviderTests
    {
        [TestMethod()]
        public void GenerateBase64Test()
        {
            string encodeMd5 = Md5Provider.GenerateBase64("zeron");

            Assert.AreEqual("b3z777naGB7+WBz7tMRQ/Q==", encodeMd5);
        }
    }
}