// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NetMQ;
using NetMQ.Sockets;
using System.Text;

namespace Zeron.ZCore.Tests
{
    [TestClass()]
    public class ZmqIntegrationTests
    {
        [TestMethod()]
        public void ReqRepLoopbackTest()
        {
            const string bindAddress = "tcp://127.0.0.1:15589";
            const string requestPayload = "{\"APIName\":\"Test\",\"APIKey\":\"test\"}";
            const string responsePayload = "{\"success\":true}";

            using ManualResetEventSlim serverReady = new(false);
            Exception? threadException = null;

            Thread serverThread = new(() =>
            {
                try
                {
                    using ResponseSocket responseSocket = new();
                    responseSocket.Bind(bindAddress);
                    serverReady.Set();

                    string request = responseSocket.ReceiveFrameString();
                    Assert.AreEqual(requestPayload, request);
                    responseSocket.SendFrame(responsePayload);
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            })
            {
                IsBackground = true
            };

            serverThread.Start();
            Assert.IsTrue(serverReady.Wait(TimeSpan.FromSeconds(5)));

            using RequestSocket requestSocket = new();
            requestSocket.Connect(bindAddress);
            requestSocket.SendFrame(Encoding.UTF8.GetBytes(requestPayload));

            string response = requestSocket.ReceiveFrameString();

            serverThread.Join(TimeSpan.FromSeconds(5));

            if (threadException != null)
            {
                Assert.Fail(threadException.Message);
            }

            Assert.AreEqual(responsePayload, response);
        }
    }
}
