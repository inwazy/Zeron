// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.ZServers.Tests
{
    [TestClass()]
    public class InstallScriptHostTests
    {
        private InstallServer? m_Install;

        [TestCleanup]
        public void Cleanup()
        {
            if (m_Install != null && ReferenceEquals(InstallServer.Current, m_Install))
            {
                m_Install.Stop();
            }

            m_Install = null;
            ScriptHostServer.Clear();
        }

        /// <summary>
        /// Install before script executes through ScriptHostServer using ScriptEngine.
        /// </summary>
        [TestMethod()]
        public void ExecuteInstallQueuesUsesScriptEngineTest()
        {
            if (InstallServer.Current != null)
            {
                Assert.Inconclusive("InstallServer.Current already set by another test.");
            }

            ScriptHostServer.Clear();
            ScriptHostServer.Register(new RecordingEngine("mytool"));

            m_Install = new InstallServer();
            m_Install.LoadConfig(new NameValueCollection
            {
                ["install_timer_queue_trigger_interval"] = "50000"
            });
            m_Install.Initialize();

            bool ok = InstallServer.ExecuteInstallQueues(new InstallQueuesType
            {
                PackageName = "pkg",
                Operation = "install",
                ScriptBefore = "before",
                ScriptAfter = "after",
                ScriptEngine = "mytool",
                FilePath = Path.Combine(Path.GetTempPath(), "zeron-missing-installer.exe"),
                RepoUrl = ""
            });

            Assert.IsFalse(ok);
            Assert.AreEqual(1, RecordingEngine.Scripts.Count);
            Assert.AreEqual("before", RecordingEngine.Scripts[0]);
        }

        private sealed class RecordingEngine : IScriptEngine
        {
            public static List<string> Scripts { get; } = [];

            public RecordingEngine(
                string id)
            {
                Id = id;
                Scripts.Clear();
            }

            public string Id { get; }

            public string DisplayName => Id;

            public IReadOnlyList<string> Platforms { get; } = ["windows"];

            public bool IsAvailable() => true;

            public ScriptResult Execute(
                ScriptRequest request)
            {
                Scripts.Add(request.Script ?? "");

                return new ScriptResult
                {
                    EngineId = Id,
                    Success = true,
                    ExitCode = 0
                };
            }
        }
    }
}
