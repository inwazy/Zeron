// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils.Engines;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class ExternalProcessScriptEngineTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            ScriptHostServer.Clear();
        }

        [TestMethod()]
        public void ExpandArgumentsReplacesTokensTest()
        {
            string expanded = ExternalProcessScriptEngine.ExpandArguments(
                "-File {scriptPath} {arguments}",
                @"C:\tmp\a.ps1",
                "--dry-run",
                "ignored");

            Assert.AreEqual("-File \"C:\\tmp\\a.ps1\" --dry-run", expanded);
        }

        [TestMethod()]
        public void TryParseTrailingJsonReadsLastLineTest()
        {
            bool ok = ExternalProcessScriptEngine.TryParseTrailingJson(
                "noise\n{\"success\":false,\"exitCode\":7,\"message\":\"nope\"}\n",
                out ExternalScriptJsonResultType? parsed);

            Assert.IsTrue(ok);
            Assert.IsNotNull(parsed);
            Assert.IsFalse(parsed!.Success);
            Assert.AreEqual(7, parsed.ExitCode);
            Assert.AreEqual("nope", parsed.Message);
        }

        [TestMethod()]
        public void ParseOptionsReadsEnabledEnginesTest()
        {
            NameValueCollection config = new()
            {
                ["script_engine_mytool_enabled"] = "true",
                ["script_engine_mytool_exe"] = "mytool.exe",
                ["script_engine_mytool_args"] = "-f {scriptPath}",
                ["script_engine_mytool_platforms"] = "windows,linux",
                ["script_engine_mytool_inline_mode"] = "tempfile",
                ["script_engine_mytool_display"] = "My Tool",
                ["script_engine_off_enabled"] = "false",
                ["script_engine_off_exe"] = "off.exe",
                ["script_engine_powershell_enabled"] = "true",
                ["script_engine_powershell_exe"] = "should-skip.exe"
            };

            List<ExternalProcessScriptEngineOptionsType> options = ExternalScriptEngineConfig.ParseOptions(config);

            Assert.AreEqual(2, options.Count);
            ExternalProcessScriptEngineOptionsType mytool = options.First(item => item.Id == "mytool");
            Assert.AreEqual("mytool.exe", mytool.ExecutablePath);
            Assert.AreEqual("-f {scriptPath}", mytool.ArgumentsTemplate);
            Assert.AreEqual(ExternalScriptInlineModeType.TempFile, mytool.InlineMode);
            Assert.AreEqual("My Tool", mytool.DisplayName);
            CollectionAssert.AreEquivalent(new[] { "windows", "linux" }, mytool.Platforms.ToArray());

            List<ExternalProcessScriptEngine> engines = ExternalScriptEngineConfig.CreateEngines(config);
            Assert.AreEqual(1, engines.Count);
            Assert.AreEqual("mytool", engines[0].Id);
        }

        [TestMethod()]
        public void ExecuteTempFileInlineRunsProcessTest()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("cmd.exe integration test is Windows-only.");
            }

            ExternalProcessScriptEngine engine = new(new ExternalProcessScriptEngineOptionsType
            {
                Id = "cmdtype",
                ExecutablePath = "cmd.exe",
                ArgumentsTemplate = "/c type {scriptPath}",
                InlineMode = ExternalScriptInlineModeType.TempFile,
                Platforms = ["windows"],
                Enabled = true
            });

            Assert.IsTrue(engine.IsAvailable());

            ScriptResultType result = engine.Execute(new ScriptRequestType
            {
                EngineId = "cmdtype",
                Script = "hello-from-external"
            });

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.StdOut?.Contains("hello-from-external", StringComparison.Ordinal) == true);
        }

        [TestMethod()]
        public void ExecuteHonorsTrailingJsonResultTest()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("cmd.exe integration test is Windows-only.");
            }

            ExternalProcessScriptEngine engine = new(new ExternalProcessScriptEngineOptionsType
            {
                Id = "cmdjson",
                ExecutablePath = "cmd.exe",
                ArgumentsTemplate = "/c type {scriptPath}",
                InlineMode = ExternalScriptInlineModeType.TempFile,
                Platforms = ["windows"],
                Enabled = true
            });

            ScriptResultType result = engine.Execute(new ScriptRequestType
            {
                EngineId = "cmdjson",
                Script = "noise\r\n{\"success\":false,\"exitCode\":9,\"message\":\"blocked\"}"
            });

            Assert.IsFalse(result.Success);
            Assert.AreEqual(9, result.ExitCode);
            Assert.AreEqual("blocked", result.ErrorMessage);
        }

        [TestMethod()]
        public void ScriptHostRegistersExternalEngineTest()
        {
            ScriptHostServer.Clear();
            ScriptHostServer.Register(new ExternalProcessScriptEngine(new ExternalProcessScriptEngineOptionsType
            {
                Id = "echo",
                ExecutablePath = "cmd.exe",
                ArgumentsTemplate = "/c echo ok",
                InlineMode = ExternalScriptInlineModeType.None,
                Enabled = true
            }));

            Assert.IsTrue(ScriptHostServer.TryGet("echo", out _));
            Assert.IsTrue(ScriptHostServer.ListEngines().Any(item => item.Id == "echo"));
        }
    }
}
