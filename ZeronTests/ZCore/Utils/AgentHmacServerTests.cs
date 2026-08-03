// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Text;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class AgentHmacServerTests
    {
        /// <summary>
        /// Valid signature is accepted.
        /// </summary>
        [TestMethod()]
        public void TryValidateAcceptsValidSignatureTest()
        {
            const string secret = "zeron.testkey";
            const string method = "POST";
            const string path = "/api/agents/heartbeat";
            byte[] body = Encoding.UTF8.GetBytes("{\"agentId\":\"a1\"}");
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string bodyHash = AgentHmacServer.ComputeBodySha256Hex(body);
            string signature = AgentHmacServer.CreateSignature(secret, method, path, timestamp, bodyHash);

            bool valid = AgentHmacServer.TryValidate(
                secret,
                method,
                path,
                timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture),
                signature,
                body,
                AgentHmacServer.DefaultSkewSeconds,
                out string? error);

            Assert.IsTrue(valid);
            Assert.IsNull(error);
        }

        /// <summary>
        /// Expired timestamp is rejected.
        /// </summary>
        [TestMethod()]
        public void TryValidateRejectsExpiredTimestampTest()
        {
            const string secret = "zeron.testkey";
            const string method = "POST";
            const string path = "/api/events";
            byte[] body = Encoding.UTF8.GetBytes("{}");
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600;
            string bodyHash = AgentHmacServer.ComputeBodySha256Hex(body);
            string signature = AgentHmacServer.CreateSignature(secret, method, path, timestamp, bodyHash);

            bool valid = AgentHmacServer.TryValidate(
                secret,
                method,
                path,
                timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture),
                signature,
                body,
                300,
                out string? error);

            Assert.IsFalse(valid);
            Assert.IsNotNull(error);
        }

        /// <summary>
        /// Tampered body is rejected.
        /// </summary>
        [TestMethod()]
        public void TryValidateRejectsTamperedBodyTest()
        {
            const string secret = "zeron.testkey";
            const string method = "POST";
            const string path = "/api/tasks/results";
            byte[] originalBody = Encoding.UTF8.GetBytes("{\"ok\":true}");
            byte[] tamperedBody = Encoding.UTF8.GetBytes("{\"ok\":false}");
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string bodyHash = AgentHmacServer.ComputeBodySha256Hex(originalBody);
            string signature = AgentHmacServer.CreateSignature(secret, method, path, timestamp, bodyHash);

            bool valid = AgentHmacServer.TryValidate(
                secret,
                method,
                path,
                timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture),
                signature,
                tamperedBody,
                300,
                out string? error);

            Assert.IsFalse(valid);
            Assert.IsNotNull(error);
        }

        /// <summary>
        /// TryValidateAny accepts any configured rotation key.
        /// </summary>
        [TestMethod()]
        public void TryValidateAnyAcceptsSecondaryKeyTest()
        {
            const string method = "POST";
            const string path = "/api/agents/heartbeat";
            byte[] body = Encoding.UTF8.GetBytes("{}");
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string bodyHash = AgentHmacServer.ComputeBodySha256Hex(body);
            string signature = AgentHmacServer.CreateSignature("new-key", method, path, timestamp, bodyHash);

            bool valid = AgentHmacServer.TryValidateAny(
                ["old-key", "new-key"],
                method,
                path,
                timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture),
                signature,
                body,
                300,
                out string? error);

            Assert.IsTrue(valid);
            Assert.IsNull(error);
        }
    }
}
