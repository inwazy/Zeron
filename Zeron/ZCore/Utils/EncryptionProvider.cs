// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Cryptography;
using System.Text;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// EncryptionProvider - AES helpers with configurable key material.
    /// </summary>
    public static class EncryptionProvider
    {
        // Default salt (legacy / development only).
        public const string DefaultSaltKey = "YRjo1*9!";

        // Default IV source (legacy / development only).
        public const string DefaultIvKey = "cdTeAV#$^YiuDamK";

        // Environment variable names.
        public const string EnvSaltKey = "ZERON_CRYPT_SALT";

        // Environment variable name for IV key.
        public const string EnvIvKey = "ZERON_CRYPT_IV";

        // Active Crypt Salt key.
        private static string s_CryptSaltKey = DefaultSaltKey;

        // Active Crypt IV key.
        private static string s_CryptIvKey = DefaultIvKey;

        // Sync lock for configuration.
        private static readonly object s_ConfigLock = new();

        /// <summary>
        /// SaltKey
        /// </summary>
        public static string SaltKey
        {
            get
            {
                lock (s_ConfigLock)
                {
                    return s_CryptSaltKey;
                }
            }
        }

        /// <summary>
        /// IvKey
        /// </summary>
        public static string IvKey
        {
            get
            {
                lock (s_ConfigLock)
                {
                    return s_CryptIvKey;
                }
            }
        }

        /// <summary>
        /// Configure - empty / whitespace values keep the current setting.
        /// </summary>
        /// <param name="saltKey"></param>
        /// <param name="ivKey"></param>
        /// <returns>Returns void.</returns>
        public static void Configure(
            string? saltKey,
            string? ivKey)
        {
            lock (s_ConfigLock)
            {
                if (!string.IsNullOrWhiteSpace(saltKey))
                {
                    s_CryptSaltKey = saltKey.Trim();
                }

                if (!string.IsNullOrWhiteSpace(ivKey))
                {
                    s_CryptIvKey = ivKey.Trim();
                }
            }
        }

        /// <summary>
        /// ConfigureFromEnvironment - applies ZERON_CRYPT_SALT / ZERON_CRYPT_IV when set.
        /// </summary>
        /// <returns>Returns void.</returns>
        public static void ConfigureFromEnvironment()
        {
            Configure(
                Environment.GetEnvironmentVariable(EnvSaltKey),
                Environment.GetEnvironmentVariable(EnvIvKey));
        }

        /// <summary>
        /// ResetToDefaults
        /// </summary>
        /// <returns>Returns void.</returns>
        public static void ResetToDefaults()
        {
            lock (s_ConfigLock)
            {
                s_CryptSaltKey = DefaultSaltKey;
                s_CryptIvKey = DefaultIvKey;
            }
        }

        /// <summary>
        /// Encrypt
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="iv"></param>
        /// <returns>Returns string.</returns>
        public static string Encrypt(
            string? plainText, 
            string? iv = "")
        {
            if (plainText == null || plainText.Length == 0)
            {
                return "";
            }

            string saltKey;
            string defaultIv;

            lock (s_ConfigLock)
            {
                saltKey = s_CryptSaltKey;
                defaultIv = s_CryptIvKey;
            }

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = DeriveKeyBytes(saltKey);
            byte[] ivBytes = DeriveIvBytes(string.IsNullOrEmpty(iv) ? defaultIv : iv);

            using Aes aesProvider = Aes.Create();
            aesProvider.Mode = CipherMode.CBC;
            aesProvider.Padding = PaddingMode.PKCS7;
            aesProvider.Key = keyBytes;
            aesProvider.IV = ivBytes;

            using ICryptoTransform encryptor = aesProvider.CreateEncryptor();
            using MemoryStream memoryStream = new();
            using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        /// <summary>
        /// Decrypt
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="iv"></param>
        /// <returns>Returns string.</returns>
        public static string Decrypt(
            string? cipherText, 
            string? iv = "")
        {
            if (!TryDecrypt(cipherText, out string? plainText, iv))
            {
                throw new CryptographicException("Unable to decrypt the provided ciphertext.");
            }

            return plainText ?? "";
        }

        /// <summary>
        /// TryDecrypt
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="plainText"></param>
        /// <param name="iv"></param>
        /// <returns>Returns bool.</returns>
        public static bool TryDecrypt(
            string? cipherText, 
            out string? plainText, 
            string? iv = "")
        {
            plainText = null;

            if (cipherText == null || cipherText.Length == 0)
            {
                return false;
            }

            string saltKey;
            string defaultIv;

            lock (s_ConfigLock)
            {
                saltKey = s_CryptSaltKey;
                defaultIv = s_CryptIvKey;
            }

            try
            {
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                byte[] keyBytes = DeriveKeyBytes(saltKey);
                byte[] ivBytes = DeriveIvBytes(string.IsNullOrEmpty(iv) ? defaultIv : iv);

                using Aes aesProvider = Aes.Create();
                aesProvider.Mode = CipherMode.CBC;
                aesProvider.Padding = PaddingMode.PKCS7;
                aesProvider.Key = keyBytes;
                aesProvider.IV = ivBytes;

                using ICryptoTransform decryptor = aesProvider.CreateDecryptor();
                using MemoryStream memoryStream = new(cipherTextBytes);
                using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
                using StreamReader streamReader = new(cryptoStream);

                plainText = streamReader.ReadToEnd();

                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// DeriveKeyBytes
        /// </summary>
        /// <param name="source"></param>
        /// <returns>Returns byte array.</returns>
        private static byte[] DeriveKeyBytes(
            string source)
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes(source);

            return sourceBytes.Length == 32
                ? sourceBytes
                : SHA256.HashData(sourceBytes);
        }

        /// <summary>
        /// DeriveIvBytes - AES requires a 16-byte IV.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>Returns byte array.</returns>
        private static byte[] DeriveIvBytes(
            string source)
        {
            byte[] sourceBytes = Encoding.UTF8.GetBytes(source);

            if (sourceBytes.Length == 16)
            {
                return sourceBytes;
            }

            byte[] hash = SHA256.HashData(sourceBytes);
            byte[] iv = new byte[16];
            Array.Copy(hash, iv, 16);

            return iv;
        }
    }
}
