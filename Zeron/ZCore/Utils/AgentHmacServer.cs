// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// AgentHmacServer - HMAC-SHA256 request signing for agent HTTP APIs.
    /// Canonical string: {method}\n{path}\n{timestamp}\n{bodySha256Hex}
    /// </summary>
    public static class AgentHmacServer
    {
        // Default clock skew in seconds.
        public const int DefaultSkewSeconds = 300;

        // Timestamp Header names.
        public const string TimestampHeader = "X-Zeron-Timestamp";

        // Signature Header names.
        public const string SignatureHeader = "X-Zeron-Signature";

        // Agent Key Header names.
        public const string AgentKeyHeader = "X-Zeron-Agent-Key";

        /// <summary>
        /// ComputeBodySha256Hex
        /// </summary>
        /// <param name="body"></param>
        /// <returns>Returns hex digest.</returns>
        public static string ComputeBodySha256Hex(
            ReadOnlySpan<byte> body)
        {
            byte[] hash = SHA256.HashData(body);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// BuildCanonicalString
        /// </summary>
        /// <param name="method"></param>
        /// <param name="path"></param>
        /// <param name="timestampUnix"></param>
        /// <param name="bodySha256Hex"></param>
        /// <returns>Returns canonical string.</returns>
        public static string BuildCanonicalString(
            string method,
            string path,
            long timestampUnix,
            string bodySha256Hex)
        {
            return method.ToUpperInvariant()
                + "\n"
                + path
                + "\n"
                + timestampUnix.ToString(CultureInfo.InvariantCulture)
                + "\n"
                + bodySha256Hex;
        }

        /// <summary>
        /// CreateSignature
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="method"></param>
        /// <param name="path"></param>
        /// <param name="timestampUnix"></param>
        /// <param name="bodySha256Hex"></param>
        /// <returns>Returns hex HMAC.</returns>
        public static string CreateSignature(
            string secret,
            string method,
            string path,
            long timestampUnix,
            string bodySha256Hex)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secret);

            string canonical = BuildCanonicalString(method, path, timestampUnix, bodySha256Hex);
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] dataBytes = Encoding.UTF8.GetBytes(canonical);
            byte[] hash = HMACSHA256.HashData(keyBytes, dataBytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// TryValidate
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="method"></param>
        /// <param name="path"></param>
        /// <param name="timestampHeader"></param>
        /// <param name="signatureHeader"></param>
        /// <param name="body"></param>
        /// <param name="skewSeconds"></param>
        /// <param name="error"></param>
        /// <returns>Returns bool.</returns>
        public static bool TryValidate(
            string secret,
            string method,
            string path,
            string? timestampHeader,
            string? signatureHeader,
            ReadOnlySpan<byte> body,
            int skewSeconds,
            out string? error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(secret))
            {
                error = "HMAC secret is empty.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(timestampHeader)
                || !long.TryParse(timestampHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out long timestampUnix))
            {
                error = "Missing or invalid X-Zeron-Timestamp.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                error = "Missing X-Zeron-Signature.";

                return false;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int skew = skewSeconds > 0 ? skewSeconds : DefaultSkewSeconds;

            if (Math.Abs(now - timestampUnix) > skew)
            {
                error = "Request timestamp outside allowed skew.";

                return false;
            }

            string bodyHash = ComputeBodySha256Hex(body);
            string expected = CreateSignature(secret, method, path, timestampUnix, bodyHash);

            if (!SecureCompareServer.FixedTimeEqualsHex(expected, signatureHeader))
            {
                error = "Invalid request signature.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// TryValidateAny
        /// </summary>
        /// <param name="secrets"></param>
        /// <param name="method"></param>
        /// <param name="path"></param>
        /// <param name="timestampHeader"></param>
        /// <param name="signatureHeader"></param>
        /// <param name="body"></param>
        /// <param name="skewSeconds"></param>
        /// <param name="error"></param>
        /// <returns>Returns bool.</returns>
        public static bool TryValidateAny(
            IEnumerable<string> secrets,
            string method,
            string path,
            string? timestampHeader,
            string? signatureHeader,
            ReadOnlySpan<byte> body,
            int skewSeconds,
            out string? error)
        {
            error = "No matching HMAC secret.";

            foreach (string secret in secrets)
            {
                if (string.IsNullOrWhiteSpace(secret))
                {
                    continue;
                }

                if (TryValidate(secret, method, path, timestampHeader, signatureHeader, body, skewSeconds, out string? singleError))
                {
                    error = null;

                    return true;
                }

                error = singleError;
            }

            return false;
        }
    }
}
