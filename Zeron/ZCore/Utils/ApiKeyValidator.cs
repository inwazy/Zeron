// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ApiKeyValidator - validates encrypted NetMQ client API keys.
    /// </summary>
    public static class ApiKeyValidator
    {
        /// <summary>
        /// Validate an encrypted client API key against configured keys.
        /// Configured keys may be plaintext or DPAPI-protected (prefix "dpapi:").
        /// Multiple keys are separated by | , or ;
        /// </summary>
        /// <param name="configuredKeys"></param>
        /// <param name="encryptedClientKey"></param>
        /// <returns>Returns bool.</returns>
        public static bool Validate(
            string? configuredKeys, 
            string? encryptedClientKey)
        {
            if (string.IsNullOrEmpty(configuredKeys) || string.IsNullOrEmpty(encryptedClientKey))
            {
                return false;
            }

            if (!EncryptionProvider.TryDecrypt(encryptedClientKey, out string? decryptedKey)
                || string.IsNullOrEmpty(decryptedKey))
            {
                return false;
            }

            foreach (string configuredKey in AgentApiKeyServer.SplitKeys(configuredKeys))
            {
                string resolvedKey = ResolveConfiguredKey(configuredKey);

                if (SecureCompareServer.FixedTimeEquals(decryptedKey, resolvedKey))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ResolveConfiguredKey
        /// </summary>
        /// <param name="configuredKey"></param>
        /// <returns>Returns string.</returns>
        private static string ResolveConfiguredKey(
            string configuredKey)
        {
            if (configuredKey.StartsWith("dpapi:", StringComparison.OrdinalIgnoreCase))
            {
                return SecureKeyStorage.Unprotect(configuredKey["dpapi:".Length..]);
            }

            return configuredKey;
        }
    }
}
