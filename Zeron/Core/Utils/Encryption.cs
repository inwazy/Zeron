using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Zeron.Core.Utils
{
    /// <summary>
    /// Encryption
    /// </summary>
    public static class Encryption
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
        public static string Encrypt(string plainText, string iv = "")
        {
            if (plainText == null || plainText.Length == 0)
                return "";

            if (iv == null || iv.Length == 0)
                iv = m_CryptIVKey;

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = new Rfc2898DeriveBytes(m_CryptPasswordHash, Encoding.ASCII.GetBytes(m_CryptSaltKey)).GetBytes(256 / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged()
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros
            };

            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.ASCII.GetBytes(iv));
            byte[] cipherTextBytes;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    cipherTextBytes = memoryStream.ToArray();

                    cryptoStream.Close();
                }

                memoryStream.Close();
            }

            symmetricKey.Dispose();

            return Convert.ToBase64String(cipherTextBytes);
        }

        /// <summary>
        /// Decrypt
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="iv"></param>
        /// <returns>Returns string.</returns>
        public static string Decrypt(string cipherText, string iv = "")
        {
            if (cipherText == null || cipherText.Length == 0)
                return "";

            if (iv == null || iv.Length == 0)
                iv = m_CryptIVKey;

            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            byte[] keyBytes = new Rfc2898DeriveBytes(m_CryptPasswordHash, Encoding.ASCII.GetBytes(m_CryptSaltKey)).GetBytes(256 / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged()
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.None
            };

            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.ASCII.GetBytes(iv));
            byte[] plainTextBytes;
            int decryptedByteCount;

            using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    plainTextBytes = new byte[cipherTextBytes.Length];
                    decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);

                    cryptoStream.Close();
                }

                memoryStream.Close();
            }

            decryptor.Dispose();
            symmetricKey.Dispose();

            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
        }
    }
}
