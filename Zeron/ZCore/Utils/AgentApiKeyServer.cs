// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// AgentApiKeyServer - shared agent API key parsing and comparison.
    /// Multiple keys may be separated by | , or ; for rotation.
    /// </summary>
    public static class AgentApiKeyServer
    {
        // Key separators.
        private static readonly char[] s_KeySeparators = ['|', ',', ';'];

        /// <summary>
        /// SplitKeys
        /// </summary>
        /// <param name="configuredKeys"></param>
        /// <returns>Returns key list.</returns>
        public static IReadOnlyList<string> SplitKeys(
            string? configuredKeys)
        {
            if (string.IsNullOrWhiteSpace(configuredKeys))
            {
                return Array.Empty<string>();
            }

            return configuredKeys
                .Split(s_KeySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }

        /// <summary>
        /// GetPrimaryKey
        /// </summary>
        /// <param name="configuredKeys"></param>
        /// <returns>Returns primary key or empty.</returns>
        public static string GetPrimaryKey(
            string? configuredKeys)
        {
            IReadOnlyList<string> keys = SplitKeys(configuredKeys);

            return keys.Count > 0 ? keys[0] : string.Empty;
        }

        /// <summary>
        /// Matches
        /// </summary>
        /// <param name="configuredKeys"></param>
        /// <param name="presentedKey"></param>
        /// <returns>Returns bool.</returns>
        public static bool Matches(
            string? configuredKeys,
            string? presentedKey)
        {
            if (string.IsNullOrWhiteSpace(presentedKey))
            {
                return false;
            }

            foreach (string key in SplitKeys(configuredKeys))
            {
                if (SecureCompareServer.FixedTimeEquals(presentedKey, key))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
