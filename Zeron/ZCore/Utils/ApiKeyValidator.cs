// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Cryptography;
using System.Text;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ApiKeyValidator
    /// </summary>
    public static class ApiKeyValidator
    {
        // Key separators.
        private static readonly char[] s_KeySeparators = ['|', ',', ';'];

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

            foreach (string configuredKey in configuredKeys.Split(s_KeySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string resolvedKey = ResolveConfiguredKey(configuredKey);

                if (FixedTimeEquals(decryptedKey, resolvedKey))
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

        /// <summary>
        /// FixedTimeEquals
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>Returns bool.</returns>
        private static bool FixedTimeEquals(
            string left, 
            string right)
        {
            byte[] leftBytes = Encoding.UTF8.GetBytes(left);
            byte[] rightBytes = Encoding.UTF8.GetBytes(right);

            if (leftBytes.Length != rightBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
