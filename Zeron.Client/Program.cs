// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Zeron.Client.ZAttribute;
using Zeron.ZServers.RequestImpls;

namespace Zeron.Client
{
    /// <summary>
    /// Program
    /// </summary>
    public class Program
    {
        // Api key for client
        private static readonly string m_ClientRequestKey = "UmMg1m+spDw6BGBeFwsW9A==";

        // Options
        private static readonly List<OptionAttribute> m_Options = new();

        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        public static void Main(string[] args)
        {
            WriteOptionsMenu();

            m_Options.Add(new OptionAttribute("Run ServerInfoRequest", () => RunServerInfoRequest()));
            m_Options.Add(new OptionAttribute("Run ProcessInfoRequest", () => RunProcessInfoRequest()));
            m_Options.Add(new OptionAttribute("Run ManagedPackageRequest", () => RunManagedPackageRequest()));

            ConsoleKeyInfo consoleKeyinfo;

            do
            {
                consoleKeyinfo = Console.ReadKey();

                if (consoleKeyinfo.Key == ConsoleKey.D1 || consoleKeyinfo.Key == ConsoleKey.D2 || consoleKeyinfo.Key == ConsoleKey.D3)
                {
                    if (int.TryParse(consoleKeyinfo.KeyChar.ToString(), out int consoleKeyindex))
                    {
                        OptionAttribute? consoleOption = m_Options.ElementAt(consoleKeyindex - 1);
                        Action? consoleOptionAction = consoleOption.OptSelected;

                        if (consoleOptionAction != null)
                            consoleOptionAction.Invoke();
                    }
                }
            }
            while (consoleKeyinfo.Key != ConsoleKey.X);

            Console.ReadKey();
        }

        /// <summary>
        /// WriteOptionsMenu
        /// </summary>
        /// <returns>Returns void.</returns>
        private static void WriteOptionsMenu()
        {
            Console.WriteLine();
            Console.WriteLine("Select options?");
            Console.WriteLine("[1] ServerInfoRequest");
            Console.WriteLine("[2] ProcessInfoRequest");
            Console.WriteLine("[3] ManagedPackageRequest");
            Console.WriteLine("[x] Exit");
        }

        /// <summary>
        /// RunServerInfoRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        private static void RunServerInfoRequest()
        {
            Console.WriteLine();
            Console.WriteLine("Run API ServerInfoRequest");

            object serverInfoRequestParams = new ServerInfoImpl
            {
                APIKey = m_ClientRequestKey
            };

            string serverInfoRequestMessage = JsonConvert.SerializeObject(serverInfoRequestParams);

            using (RequestSocket client = new("tcp://localhost:5589"))
            {
                client.SendFrame(serverInfoRequestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine();
                Console.WriteLine("ServerInfoRequest : Received '{0}'", message);
            }

            WriteOptionsMenu();
        }

        /// <summary>
        /// ProcessInfoRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        private static void RunProcessInfoRequest()
        {
            Console.WriteLine();
            Console.WriteLine("Run API ProcessInfoRequest");

            object processInfoRequestParams = new ProcessInfoImpl
            {
                APIKey = m_ClientRequestKey
            };

            string processInfoRequestMessage = JsonConvert.SerializeObject(processInfoRequestParams);

            using (RequestSocket client = new("tcp://localhost:5589"))
            {
                client.SendFrame(processInfoRequestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine();
                Console.WriteLine("ProcessInfoRequest : Received '{0}'", message);
            }

            WriteOptionsMenu();
        }

        /// <summary>
        /// RunManagedPackageRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        private static void RunManagedPackageRequest()
        {
            Console.WriteLine();
            Console.WriteLine("Run API ManagedPackage");
            Console.WriteLine("Enter Api Commands?");

            string? apiCommand = Console.ReadLine();

            object managedPackageRequestParams = new ManagedPackageImpl
            {
                APIKey = m_ClientRequestKey,
                Command = apiCommand ?? ""
            };

            string managedPackageRequestMessage = JsonConvert.SerializeObject(managedPackageRequestParams);

            using (RequestSocket client = new("tcp://localhost:5589"))
            {
                client.SendFrame(managedPackageRequestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine();
                Console.WriteLine("ManagedPackageRequest : Received '{0}'", message);
            }

            WriteOptionsMenu();
        }
    }
}