// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using Zeron.ZCore.Type;

namespace Zeron.ZCore.Utils.Engines
{
    /// <summary>
    /// ExternalScriptEngineConfig - parses App.config script_engine_* keys.
    /// </summary>
    public static partial class ExternalScriptEngineConfig
    {
        // Matches script_engine_{id}_enabled
        [GeneratedRegex(@"^script_engine_(?<id>[A-Za-z0-9_-]+)_enabled$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex EnabledKeyRegex();

        /// <summary>
        /// CreateEngines - builds engines from appSettings; skips id "powershell".
        /// </summary>
        /// <param name="config"></param>
        /// <returns>Returns engine list.</returns>
        public static List<ExternalProcessScriptEngine> CreateEngines(
            NameValueCollection? config)
        {
            List<ExternalProcessScriptEngine> engines = [];

            foreach (ExternalProcessScriptEngineOptionsType options in ParseOptions(config))
            {
                if (string.Equals(options.Id, "powershell", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(options.ExecutablePath))
                {
                    ZNLogger.Common.Warn(string.Format(CultureInfo.InvariantCulture,
                        "ExternalScriptEngineConfig skipped '{0}': missing script_engine_{0}_exe.", options.Id));
                    continue;
                }

                engines.Add(new ExternalProcessScriptEngine(options));
            }

            return engines;
        }

        /// <summary>
        /// ParseOptions
        /// </summary>
        /// <param name="config"></param>
        /// <returns>Returns option list.</returns>
        public static List<ExternalProcessScriptEngineOptionsType> ParseOptions(
            NameValueCollection? config)
        {
            List<ExternalProcessScriptEngineOptionsType> result = [];

            if (config?.AllKeys == null)
            {
                return result;
            }

            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            foreach (string? key in config.AllKeys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                Match match = EnabledKeyRegex().Match(key);

                if (!match.Success)
                {
                    continue;
                }

                string id = match.Groups["id"].Value.Trim().ToLowerInvariant();

                if (id.Length == 0 || !seen.Add(id))
                {
                    continue;
                }

                if (!bool.TryParse(config[key], out bool enabled) || !enabled)
                {
                    continue;
                }

                string prefix = "script_engine_" + id + "_";
                string exe = (config[prefix + "exe"] ?? "").Trim();
                string args = config[prefix + "args"] ?? "{scriptPath} {arguments}";
                string platformsRaw = config[prefix + "platforms"] ?? "windows";
                string inlineRaw = (config[prefix + "inline_mode"] ?? "stdin").Trim();
                string? display = config[prefix + "display"];

                result.Add(new ExternalProcessScriptEngineOptionsType
                {
                    Id = id,
                    DisplayName = string.IsNullOrWhiteSpace(display) ? id : display.Trim(),
                    ExecutablePath = exe,
                    ArgumentsTemplate = args,
                    Platforms = ParsePlatforms(platformsRaw),
                    InlineMode = ParseInlineMode(inlineRaw),
                    Enabled = true
                });
            }

            return result;
        }

        /// <summary>
        /// ParsePlatforms
        /// </summary>
        private static IReadOnlyList<string> ParsePlatforms(
            string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return ["windows"];
            }

            List<string> platforms = [.. raw
                .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item => item.ToLowerInvariant())
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)];

            return platforms.Count == 0 ? ["windows"] : platforms;
        }

        /// <summary>
        /// ParseInlineMode
        /// </summary>
        private static ExternalScriptInlineModeType ParseInlineMode(
            string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return ExternalScriptInlineModeType.StdIn;
            }

            return raw.Trim().ToLowerInvariant() switch
            {
                "none" => ExternalScriptInlineModeType.None,
                "tempfile" or "temp" or "file" => ExternalScriptInlineModeType.TempFile,
                _ => ExternalScriptInlineModeType.StdIn
            };
        }
    }
}
