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
            byte[] keyBytes = new Rfc2898DeriveBytes(m_CryptPasswordHash, Encoding.ASCII.GetBytes(m_CryptSaltKey)).GetBytes(256 / 8);

            using (Aes aesProvider = Aes.Create())
            {
                ICryptoTransform encryptor = aesProvider.CreateEncryptor(keyBytes, Encoding.ASCII.GetBytes(iv));

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
            byte[] keyBytes = new Rfc2898DeriveBytes(m_CryptPasswordHash, Encoding.ASCII.GetBytes(m_CryptSaltKey)).GetBytes(256 / 8);

            using (Aes aesProvider = Aes.Create())
            {
                ICryptoTransform decryptor = aesProvider.CreateDecryptor(keyBytes, Encoding.ASCII.GetBytes(iv));
                
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
