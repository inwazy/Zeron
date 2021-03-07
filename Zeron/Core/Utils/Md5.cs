using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Zeron.Core.Utils
{
    /// <summary>
    /// Md5
    /// </summary>
    public static class Md5
    {
        // MD5 provider.
        private static readonly MD5CryptoServiceProvider m_MD5Hasher = new MD5CryptoServiceProvider();

        /// <summary>
        /// GenerateBase64
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns>Returns string.</returns>
        public static string GenerateBase64(string plainText)
        {
            string result = "";

            if (plainText == null || plainText.Length == 0)
                return result;

            try
            {
                byte[] byteSource = Encoding.Default.GetBytes(plainText);
                byte[] byteMD5 = m_MD5Hasher.ComputeHash(byteSource);

                result = Convert.ToBase64String(byteMD5);
            }
            catch (ArgumentNullException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "GenerateBase64 Error:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (EncoderFallbackException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "GenerateBase64 Error:{0}\n{1}", e.Message, e.StackTrace));
            }
            catch (ObjectDisposedException e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "GenerateBase64 Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            return result;
        }
    }
}
