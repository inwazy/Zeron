using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using Zeron.Client.Client.Impls;

namespace Zeron.Client
{
    /// <summary>
    /// Program
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        public static void Main(string[] args)
        {
            // Core.Utils.Encryption.Encrypt("zeron.testkey");
            string clientRequestKey = "8TAoVPkmYaphto4LFTCtKw==";

            // Test Key
            Console.WriteLine(Core.Utils.Encryption.Decrypt(clientRequestKey) );

            // ServerInfoRequest
            object serverInfoRequestParams = new ServerInfoRequest
            {
                APIKey = clientRequestKey
            };

            string serverInfoRequestMessage = JsonConvert.SerializeObject(serverInfoRequestParams);

            using (RequestSocket client = new RequestSocket("tcp://localhost:5589"))
            {
                client.SendFrame(serverInfoRequestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine();
                Console.WriteLine("ServerInfoRequest : Received '{0}'", message);
            }

            // ProcessInfoRequest
            object processInfoRequestParams = new ProcessInfoRequest
            {
                APIKey = clientRequestKey
            };

            string processInfoRequestMessage = JsonConvert.SerializeObject(processInfoRequestParams);

            using (RequestSocket client = new RequestSocket("tcp://localhost:5589"))
            {
                client.SendFrame(processInfoRequestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine();
                Console.WriteLine("ProcessInfoRequest : Received '{0}'", message);
            }

            // InstallGitRequest
            /*object installGitRequestParams = new InstallGitRequest
            {
                APIKey = clientRequestKey,
                Async = true
            };

            string installGitRequestMessage = JsonConvert.SerializeObject(installGitRequestParams);

            using (RequestSocket client = new RequestSocket("tcp://localhost:5589"))
            {
                client.SendFrame(installGitRequestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine();
                Console.WriteLine("InstallGitRequest : Received '{0}'", message);
            }*/

            // Install7ZipRequest
            /*object install7ZipRequestParams = new Install7ZipRequest
            {
                APIKey = clientRequestKey,
                Async = true
            };

            string install7ZipRequestMessage = JsonConvert.SerializeObject(install7ZipRequestParams);

            using (RequestSocket client = new RequestSocket("tcp://localhost:5589"))
            {
                client.SendFrame(install7ZipRequestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine();
                Console.WriteLine("Install7ZipRequest : Received '{0}'", message);
            }*/

            // InstallKLiteRequest
            /*object installKLiteRequestParams = new InstallKLiteRequest
            {
                APIKey = clientRequestKey,
                Async = true
            };

            string installKLiteRequestMessage = JsonConvert.SerializeObject(installKLiteRequestParams);

            using (RequestSocket client = new RequestSocket("tcp://localhost:5589"))
            {
                client.SendFrame(installKLiteRequestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine();
                Console.WriteLine("InstallKLiteRequest : Received '{0}'", message);
            }*/

            Console.ReadKey();
        }
    }
}
