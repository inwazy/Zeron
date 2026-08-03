// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NetMQ;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// CurveKeyServer - NetMQ CURVE key file helpers (raw 32-byte keys).
    /// </summary>
    public static class CurveKeyServer
    {
        // CURVE key length.
        public const int KeyLength = 32;

        /// <summary>
        /// LoadOrCreate
        /// </summary>
        /// <param name="secretKeyPath"></param>
        /// <param name="publicKeyPath"></param>
        /// <returns>Returns NetMQCertificate.</returns>
        public static NetMQCertificate LoadOrCreate(
            string secretKeyPath,
            string publicKeyPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secretKeyPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPath);

            if (File.Exists(secretKeyPath))
            {
                byte[] secretKey = File.ReadAllBytes(secretKeyPath);
                ValidateKeyLength(secretKey, secretKeyPath);

                NetMQCertificate existing = NetMQCertificate.CreateFromSecretKey(secretKey);
                EnsureDirectory(publicKeyPath);
                File.WriteAllBytes(publicKeyPath, existing.PublicKey);

                return existing;
            }

            NetMQCertificate created = new();
            EnsureDirectory(secretKeyPath);
            EnsureDirectory(publicKeyPath);
            File.WriteAllBytes(secretKeyPath, created.SecretKey!);
            File.WriteAllBytes(publicKeyPath, created.PublicKey);

            return created;
        }

        /// <summary>
        /// LoadPublicKey
        /// </summary>
        /// <param name="publicKeyPath"></param>
        /// <returns>Returns public key bytes.</returns>
        public static byte[] LoadPublicKey(
            string publicKeyPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPath);

            if (!File.Exists(publicKeyPath))
            {
                throw new FileNotFoundException("CURVE public key file not found.", publicKeyPath);
            }

            byte[] publicKey = File.ReadAllBytes(publicKeyPath);
            ValidateKeyLength(publicKey, publicKeyPath);

            return publicKey;
        }

        /// <summary>
        /// ApplyCurveServer
        /// </summary>
        /// <param name="options"></param>
        /// <param name="certificate"></param>
        /// <returns>Returns void.</returns>
        public static void ApplyCurveServer(
            SocketOptions options,
            NetMQCertificate certificate)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(certificate);

            options.CurveServer = true;
            options.CurveCertificate = certificate;
        }

        /// <summary>
        /// ApplyCurveClient
        /// </summary>
        /// <param name="options"></param>
        /// <param name="clientCertificate"></param>
        /// <param name="serverPublicKey"></param>
        /// <returns>Returns void.</returns>
        public static void ApplyCurveClient(
            SocketOptions options,
            NetMQCertificate clientCertificate,
            byte[] serverPublicKey)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(clientCertificate);
            ArgumentNullException.ThrowIfNull(serverPublicKey);
            ValidateKeyLength(serverPublicKey, nameof(serverPublicKey));

            options.CurveCertificate = clientCertificate;
            options.CurveServerKey = serverPublicKey;
        }

        /// <summary>
        /// EnsureDirectory
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Returns void.</returns>
        private static void EnsureDirectory(
            string filePath)
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// ValidateKeyLength
        /// </summary>
        /// <param name="key"></param>
        /// <param name="pathOrName"></param>
        /// <returns>Returns void.</returns>
        private static void ValidateKeyLength(
            byte[] key,
            string pathOrName)
        {
            if (key.Length != KeyLength)
            {
                throw new InvalidDataException(
                    $"CURVE key '{pathOrName}' must be {KeyLength} bytes, got {key.Length}.");
            }
        }
    }
}
