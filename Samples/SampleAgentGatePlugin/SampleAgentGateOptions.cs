// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Text.Json;

namespace SampleAgentGatePlugin
{
    /// <summary>
    /// SampleAgentGateOptions - proceed / pause-resume / cancel for gate.install.
    /// </summary>
    public sealed class SampleAgentGateOptions
    {
        /// <summary>
        /// DefaultDelayMs
        /// </summary>
        public const int DefaultDelayMs = 2000;

        /// <summary>
        /// Mode - proceed | pause-resume | cancel.
        /// </summary>
        public string Mode
        {
            get;
            init;
        } = ModeProceed;

        /// <summary>
        /// DelayMs - wait before auto-Resume when Mode is pause-resume.
        /// </summary>
        public int DelayMs
        {
            get;
            init;
        } = DefaultDelayMs;

        /// <summary>
        /// PackageFilter - if set, only intercept this catalog package name.
        /// </summary>
        public string? PackageFilter
        {
            get;
            init;
        }

        /// <summary>
        /// ModeProceed
        /// </summary>
        public const string ModeProceed = "proceed";

        /// <summary>
        /// ModePauseResume
        /// </summary>
        public const string ModePauseResume = "pause-resume";

        /// <summary>
        /// ModeCancel
        /// </summary>
        public const string ModeCancel = "cancel";

        /// <summary>
        /// Load - env overrides files next to the plugin DLL.
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <returns>Returns SampleAgentGateOptions.</returns>
        public static SampleAgentGateOptions Load(
            string? baseDirectory = null)
        {
            string directory = string.IsNullOrWhiteSpace(baseDirectory)
                ? Path.GetDirectoryName(typeof(SampleAgentGatePlugin).Assembly.Location)
                    ?? AppDomain.CurrentDomain.BaseDirectory
                : baseDirectory;

            string mode = ReadEnv("ZERON_SAMPLE_GATE_MODE")
                ?? ReadFirstDataLine(Path.Combine(directory, "SampleAgentGatePlugin.mode"))
                ?? ModeProceed;
            string? delayText = ReadEnv("ZERON_SAMPLE_GATE_DELAY_MS")
                ?? ReadFirstDataLine(Path.Combine(directory, "SampleAgentGatePlugin.delay-ms"));
            string? package = ReadEnv("ZERON_SAMPLE_GATE_PACKAGE")
                ?? ReadFirstDataLine(Path.Combine(directory, "SampleAgentGatePlugin.package"));

            int delayMs = DefaultDelayMs;

            if (int.TryParse(delayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && parsed >= 0)
            {
                delayMs = parsed;
            }

            return new SampleAgentGateOptions
            {
                Mode = NormalizeMode(mode),
                DelayMs = delayMs,
                PackageFilter = string.IsNullOrWhiteSpace(package) ? null : package.Trim()
            };
        }

        /// <summary>
        /// NormalizeMode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns>Returns string.</returns>
        public static string NormalizeMode(
            string? mode)
        {
            string value = (mode ?? ModeProceed).Trim().ToLowerInvariant();

            return value switch
            {
                "cancel" => ModeCancel,
                "pause" => ModePauseResume,
                "pause-resume" => ModePauseResume,
                "pause_resume" => ModePauseResume,
                _ => ModeProceed
            };
        }

        /// <summary>
        /// MatchesPackage
        /// </summary>
        /// <param name="payloadJson"></param>
        /// <returns>Returns bool.</returns>
        public bool MatchesPackage(
            string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(PackageFilter))
            {
                return true;
            }

            string filter = PackageFilter.Trim();

            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(payloadJson);

                if (document.RootElement.TryGetProperty("package", out JsonElement package)
                    && package.ValueKind == JsonValueKind.String)
                {
                    return string.Equals(package.GetString(), filter, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (JsonException)
            {
            }

            return payloadJson.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// ReadEnv
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Returns string.</returns>
        private static string? ReadEnv(
            string name)
        {
            string? value = Environment.GetEnvironmentVariable(name);

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// ReadFirstDataLine
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns string.</returns>
        private static string? ReadFirstDataLine(
            string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                foreach (string line in File.ReadAllLines(path))
                {
                    string trimmed = line.Trim();

                    if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith("//"))
                    {
                        continue;
                    }

                    return trimmed;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return null;
        }
    }
}
