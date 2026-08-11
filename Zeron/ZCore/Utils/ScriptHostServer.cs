// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Concurrent;
using Zeron.ZCore.Type;
using Zeron.ZCore.Utils.Engines;
using Zeron.ZInterfaces;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ScriptHostServer - registry and facade for pluggable script engines.
    /// </summary>
    public static class ScriptHostServer
    {
        // Default timeout (5 minutes).
        public const int DefaultTimeoutMs = 300000;

        // Registered engines by id.
        private static readonly ConcurrentDictionary<string, IScriptEngine> s_Engines =
            new(StringComparer.OrdinalIgnoreCase);

        // Default timeout override from config.
        private static int s_DefaultTimeoutMs = DefaultTimeoutMs;

        // Sync for lazy default registration.
        private static readonly object s_DefaultLock = new();

        /// <summary>
        /// ConfigureDefaultTimeoutMs
        /// </summary>
        /// <param name="timeoutMs"></param>
        /// <returns>Returns void.</returns>
        public static void ConfigureDefaultTimeoutMs(
            int timeoutMs)
        {
            s_DefaultTimeoutMs = timeoutMs > 0 ? timeoutMs : DefaultTimeoutMs;
        }

        /// <summary>
        /// GetDefaultTimeoutMs
        /// </summary>
        /// <returns>Returns int.</returns>
        public static int GetDefaultTimeoutMs()
        {
            return s_DefaultTimeoutMs;
        }

        /// <summary>
        /// Clear - test helper; removes all registered engines.
        /// </summary>
        /// <returns>Returns void.</returns>
        public static void Clear()
        {
            s_Engines.Clear();
        }

        /// <summary>
        /// Register
        /// </summary>
        /// <param name="engine"></param>
        /// <returns>Returns void.</returns>
        public static void Register(
            IScriptEngine engine)
        {
            ArgumentNullException.ThrowIfNull(engine);

            if (string.IsNullOrWhiteSpace(engine.Id))
            {
                throw new ArgumentException("Engine Id is required.", nameof(engine));
            }

            s_Engines[engine.Id.Trim().ToLowerInvariant()] = engine;
        }

        /// <summary>
        /// TryGet
        /// </summary>
        /// <param name="engineId"></param>
        /// <param name="engine"></param>
        /// <returns>Returns bool.</returns>
        public static bool TryGet(
            string? engineId,
            out IScriptEngine? engine)
        {
            engine = null;
            EnsureBuiltInEngines();

            if (string.IsNullOrWhiteSpace(engineId))
            {
                return false;
            }

            return s_Engines.TryGetValue(engineId.Trim().ToLowerInvariant(), out engine);
        }

        /// <summary>
        /// ListEngines
        /// </summary>
        /// <returns>Returns engine info list.</returns>
        public static List<ScriptEngineInfoType> ListEngines()
        {
            EnsureBuiltInEngines();
            List<ScriptEngineInfoType> result = [];

            foreach (IScriptEngine engine in s_Engines.Values.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(new ScriptEngineInfoType
                {
                    Id = engine.Id,
                    DisplayName = engine.DisplayName,
                    Platforms = engine.Platforms.ToList(),
                    Available = engine.IsAvailable()
                });
            }

            return result;
        }

        /// <summary>
        /// ListAvailable
        /// </summary>
        /// <returns>Returns available engine info list.</returns>
        public static List<ScriptEngineInfoType> ListAvailable()
        {
            return ListEngines().Where(engine => engine.Available).ToList();
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="engineId"></param>
        /// <param name="script"></param>
        /// <param name="timeoutMs"></param>
        /// <returns>Returns ScriptResult.</returns>
        public static ScriptResultType Execute(
            string? engineId,
            string? script,
            int timeoutMs = 0)
        {
            return Execute(new ScriptRequestType
            {
                EngineId = engineId,
                Script = script,
                TimeoutMs = timeoutMs
            });
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns ScriptResult.</returns>
        public static ScriptResultType Execute(
            ScriptRequestType request)
        {
            ArgumentNullException.ThrowIfNull(request);
            EnsureBuiltInEngines();

            string engineId = string.IsNullOrWhiteSpace(request.EngineId)
                ? "powershell"
                : request.EngineId.Trim().ToLowerInvariant();

            if (!s_Engines.TryGetValue(engineId, out IScriptEngine? engine) || engine == null)
            {
                return new ScriptResultType
                {
                    EngineId = engineId,
                    Success = false,
                    ExitCode = -1,
                    ErrorMessage = "Script engine is not registered: " + engineId
                };
            }

            ScriptRequestType normalized = new()
            {
                EngineId = engineId,
                Script = request.Script,
                ScriptPath = request.ScriptPath,
                Arguments = request.Arguments,
                WorkingDirectory = request.WorkingDirectory,
                TimeoutMs = request.TimeoutMs > 0 ? request.TimeoutMs : s_DefaultTimeoutMs
            };

            try
            {
                return engine.Execute(normalized);
            }
            catch (Exception e)
            {
                return new ScriptResultType
                {
                    EngineId = engineId,
                    Success = false,
                    ExitCode = -1,
                    ErrorMessage = e.Message
                };
            }
        }

        /// <summary>
        /// EnsureBuiltInEngines - registers default PowerShell when registry is empty.
        /// </summary>
        /// <returns>Returns void.</returns>
        private static void EnsureBuiltInEngines()
        {
            if (!s_Engines.IsEmpty)
            {
                return;
            }

            lock (s_DefaultLock)
            {
                if (!s_Engines.IsEmpty)
                {
                    return;
                }

                Register(new PowerShellScriptEngine());
            }
        }
    }
}
