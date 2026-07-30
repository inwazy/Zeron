using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.ZCore.Tests
{
    [TestClass()]
    public class InstallJobTrackerTests
    {
        [TestMethod()]
        public void GetStatusReflectsRunningAndCompletedJobsTest()
        {
            InstallJobTracker.QueueCountProvider = () => 2;
            InstallJobTracker.MarkRunning("ccleaner", "install");
            InstallJobStatus runningStatus = InstallJobTracker.GetStatus();

            Assert.IsTrue(runningStatus.IsRunning);
            Assert.AreEqual("ccleaner", runningStatus.CurrentPackage);
            Assert.AreEqual("install", runningStatus.CurrentOperation);
            Assert.AreEqual(2, runningStatus.QueueCount);

            InstallJobTracker.MarkCompleted("ccleaner", "install", true, 0);
            InstallJobStatus completedStatus = InstallJobTracker.GetStatus();

            Assert.IsFalse(completedStatus.IsRunning);
            Assert.AreEqual("ccleaner", completedStatus.LastPackage);
            Assert.IsTrue(completedStatus.LastSuccess);
            Assert.AreEqual(0, completedStatus.LastExitCode);
        }
    }
}
