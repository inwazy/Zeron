// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore.Type;

namespace Zeron.ZInterfaces
{
    /// <summary>
    /// IScriptEngine - pluggable script runtime for ScriptHostServer.
    /// </summary>
    public interface IScriptEngine
    {
        /// <summary>
        /// Id - stable lowercase engine key (e.g. powershell).
        /// </summary>
        string Id { get; }

        /// <summary>
        /// DisplayName
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Platforms - e.g. windows, linux, macos.
        /// </summary>
        IReadOnlyList<string> Platforms { get; }

        /// <summary>
        /// IsAvailable - runtime binary present / enabled.
        /// </summary>
        /// <returns>Returns bool.</returns>
        bool IsAvailable();

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns ScriptResult.</returns>
        ScriptResult Execute(
            ScriptRequest request);
    }
}
