// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Cryptography;
using System.Text;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// EncryptionProvider
    /// </summary>
    public static class EncryptionProvider
    {
        // Crypt Salt key.
        private const string m_CryptSaltKey = "YRjo1*9!";

        // Crypt IV key.
        private const string m_CryptIVKey = "cdTeAV#$^YiuDamK";

        /// <summary>
        /// Encrypt
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="iv"></param>
        /// <returns>Returns string.</returns>
        public static string Encrypt(string? plainText, string? iv = "")
        {
            if (plainText == null || plainText.Length == 0)
            {
                return "";
            }

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = DeriveKeyBytes(m_CryptSaltKey);
            byte[] ivBytes = DeriveIvBytes(string.IsNullOrEmpty(iv) ? m_CryptIVKey : iv);

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
        public static string Decrypt(string? cipherText, string? iv = "")
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
        public static bool TryDecrypt(string? cipherText, out string? plainText, string? iv = "")
        {
            plainText = null;

            if (cipherText == null || cipherText.Length == 0)
            {
                return false;
            }

            try
            {
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                byte[] keyBytes = DeriveKeyBytes(m_CryptSaltKey);
                byte[] ivBytes = DeriveIvBytes(string.IsNullOrEmpty(iv) ? m_CryptIVKey : iv);

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
        private static byte[] DeriveKeyBytes(string source)
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
        private static byte[] DeriveIvBytes(string source)
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
