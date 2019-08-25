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
            object requestParams = new ServerInfoRequest();
            string requestMessage = JsonConvert.SerializeObject(requestParams);

            using (RequestSocket client = new RequestSocket("tcp://localhost:5589"))
            {
                client.SendFrame(requestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine("requestSocket : Received '{0}'", message);
            }

            Console.ReadKey();
        }
    }
}
