// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Cryptography;
using System.Text;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// Md5Provider
    /// </summary>
    public static class Md5Provider
    {
        /// <summary>
        /// GenerateBase64
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns>Returns string.</returns>
        public static string GenerateBase64(string? plainText)
        {
            string result = "";

            if (plainText == null || plainText.Length == 0)
            {
                return result;
            }

            using (MD5 md5Provider = MD5.Create())
            {
                byte[] byteSource = Encoding.Default.GetBytes(plainText);
                byte[] byteMD5 = md5Provider.ComputeHash(byteSource);

                result = Convert.ToBase64String(byteMD5);
            }

            return result;
        }
    }
}
