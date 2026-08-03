// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NetMQ;
using NetMQ.Sockets;

namespace Zeron.ZCore.Utils.Tests
{
    [TestClass()]
    public class CurveKeyServerTests
    {
        /// <summary>
        /// LoadOrCreate persists and reloads the same key pair.
        /// </summary>
        [TestMethod()]
        public void LoadOrCreateRoundTripTest()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "zeron-curve-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string secretPath = Path.Combine(tempDir, "server.secret");
                string publicPath = Path.Combine(tempDir, "server.public");

                NetMQCertificate created = CurveKeyServer.LoadOrCreate(secretPath, publicPath);
                NetMQCertificate loaded = CurveKeyServer.LoadOrCreate(secretPath, publicPath);
                byte[] publicKey = CurveKeyServer.LoadPublicKey(publicPath);

                Assert.IsTrue(File.Exists(secretPath));
                Assert.IsTrue(File.Exists(publicPath));
                CollectionAssert.AreEqual(created.PublicKey, loaded.PublicKey);
                CollectionAssert.AreEqual(created.PublicKey, publicKey);
                Assert.AreEqual(CurveKeyServer.KeyLength, publicKey.Length);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        /// <summary>
        /// CURVE PUB/SUB can exchange a multipart message on loopback.
        /// </summary>
        [TestMethod()]
        public void CurvePubSubRoundTripTest()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "zeron-curve-pubsub-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string serverSecret = Path.Combine(tempDir, "server.secret");
                string serverPublic = Path.Combine(tempDir, "server.public");
                string clientSecret = Path.Combine(tempDir, "client.secret");
                string clientPublic = Path.Combine(tempDir, "client.public");

                NetMQCertificate serverCert = CurveKeyServer.LoadOrCreate(serverSecret, serverPublic);
                NetMQCertificate clientCert = CurveKeyServer.LoadOrCreate(clientSecret, clientPublic);
                byte[] serverPublicKey = CurveKeyServer.LoadPublicKey(serverPublic);

                using PublisherSocket publisher = new();
                CurveKeyServer.ApplyCurveServer(publisher.Options, serverCert);
                int port = publisher.BindRandomPort("tcp://127.0.0.1");

                using SubscriberSocket subscriber = new();
                CurveKeyServer.ApplyCurveClient(subscriber.Options, clientCert, serverPublicKey);
                subscriber.Connect("tcp://127.0.0.1:" + port);
                subscriber.Subscribe("");

                // PUB/SUB slow-joiner: allow subscription to propagate.
                Thread.Sleep(300);

                publisher.SendMoreFrame("remotecommand.test").SendFrame("hello-curve");

                Assert.IsTrue(subscriber.TryReceiveFrameString(TimeSpan.FromSeconds(5), out string? topic));
                Assert.AreEqual("remotecommand.test", topic);
                Assert.AreEqual("hello-curve", subscriber.ReceiveFrameString());
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
