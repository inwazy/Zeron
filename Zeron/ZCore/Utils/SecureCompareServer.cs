// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Security.Cryptography;
using System.Text;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// SecureCompareServer - constant-time string comparisons for secrets and hex digests.
    /// </summary>
    public static class SecureCompareServer
    {
        /// <summary>
        /// FixedTimeEquals - UTF-8 byte compare; length mismatch returns false.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>Returns bool.</returns>
        public static bool FixedTimeEquals(
            string? left,
            string? right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            byte[] leftBytes = Encoding.UTF8.GetBytes(left);
            byte[] rightBytes = Encoding.UTF8.GetBytes(right);

            if (leftBytes.Length != rightBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }

        /// <summary>
        /// FixedTimeEqualsHex - case-insensitive hex digest compare.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>Returns bool.</returns>
        public static bool FixedTimeEqualsHex(
            string? left,
            string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            return FixedTimeEquals(left.Trim().ToLowerInvariant(), right.Trim().ToLowerInvariant());
        }
    }
}
