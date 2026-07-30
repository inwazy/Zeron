// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Cryptography;
using System.Text;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// SecureKeyStorage - Windows DPAPI helpers for protecting secrets at rest.
    /// </summary>
    public static class SecureKeyStorage
    {
        /// <summary>
        /// Protect a plaintext secret using DPAPI (LocalMachine scope).
        /// Returns a Base64 string suitable for App.config with "dpapi:" prefix.
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns>Returns string.</returns>
        public static string Protect(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.LocalMachine);

            return Convert.ToBase64String(protectedBytes);
        }

        /// <summary>
        /// Unprotect a DPAPI-protected Base64 secret.
        /// </summary>
        /// <param name="protectedBase64"></param>
        /// <returns>Returns string.</returns>
        public static string Unprotect(string protectedBase64)
        {
            byte[] protectedBytes = Convert.FromBase64String(protectedBase64);
            byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.LocalMachine);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
