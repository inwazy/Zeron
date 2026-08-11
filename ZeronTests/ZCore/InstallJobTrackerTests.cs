// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

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
            InstallJobStatusType runningStatus = InstallJobTracker.GetStatus();

            Assert.IsTrue(runningStatus.IsRunning);
            Assert.AreEqual("ccleaner", runningStatus.CurrentPackage);
            Assert.AreEqual("install", runningStatus.CurrentOperation);
            Assert.AreEqual(2, runningStatus.QueueCount);

            InstallJobTracker.MarkCompleted("ccleaner", "install", true, 0);
            InstallJobStatusType completedStatus = InstallJobTracker.GetStatus();

            Assert.IsFalse(completedStatus.IsRunning);
            Assert.AreEqual("ccleaner", completedStatus.LastPackage);
            Assert.IsTrue(completedStatus.LastSuccess);
            Assert.AreEqual(0, completedStatus.LastExitCode);
        }
    }
}
