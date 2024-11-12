// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// EncryptionProvider
    /// </summary>
    public static class EncryptionProvider
    {
        // Crypt Password hash.
        private const string m_CryptPasswordHash = "yyFDdat!@";

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

            if (iv == null || iv.Length == 0)
            {
                iv = m_CryptIVKey;
            }

            byte[] cipherTextBytes;
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Encoding.UTF8.GetByteCount(m_CryptSaltKey) == 32 
                ? Encoding.UTF8.GetBytes(m_CryptSaltKey) 
                : SHA256.HashData(Encoding.UTF8.GetBytes(m_CryptSaltKey));

            using (Aes aesProvider = Aes.Create())
            {
                aesProvider.Mode = CipherMode.CBC;
                aesProvider.Key = keyBytes;
                aesProvider.IV = Encoding.ASCII.GetBytes(iv);
                
                ICryptoTransform encryptor = aesProvider.CreateEncryptor(aesProvider.Key, aesProvider.IV);

                using (MemoryStream memoryStream = new())
                {
                    using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        cipherTextBytes = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(cipherTextBytes);
        }

        /// <summary>
        /// Decrypt
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="iv"></param>
        /// <returns>Returns string.</returns>
        public static string Decrypt(string? cipherText, string? iv = "")
        {
            if (cipherText == null || cipherText.Length == 0)
            {
                return "";
            }

            if (iv == null || iv.Length == 0)
            {
                iv = m_CryptIVKey;
            }

            string plainText;
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            byte[] keyBytes = Encoding.UTF8.GetByteCount(m_CryptSaltKey) == 32
                ? Encoding.UTF8.GetBytes(m_CryptSaltKey)
                : SHA256.HashData(Encoding.UTF8.GetBytes(m_CryptSaltKey));

            using (Aes aesProvider = Aes.Create())
            {
                aesProvider.Mode = CipherMode.CBC;
                aesProvider.Key = keyBytes;
                aesProvider.IV = Encoding.ASCII.GetBytes(iv);

                ICryptoTransform decryptor = aesProvider.CreateDecryptor(aesProvider.Key, aesProvider.IV);
                
                using (MemoryStream memoryStream = new(cipherTextBytes))
                {
                    using (CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new(cryptoStream))
                        {
                            plainText = streamReader.ReadToEnd();
                        }
                    }
                }
            }

            return plainText;
        }
    }
}
