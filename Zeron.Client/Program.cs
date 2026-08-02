// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Zeron.Client.ZAttribute;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;
using Zeron.ZServers.RequestImpls;

namespace Zeron.Client
{
    /// <summary>
    /// Program
    /// </summary>
    public class Program
    {
        private static readonly string m_ClientRequestKey = EncryptionProvider.Encrypt(
            Environment.GetEnvironmentVariable("ZERON_API_KEY") ?? "zeron.testkey");

        private static readonly List<OptionAttribute> m_Options = new();

        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        public static void Main(
            string[] args)
        {
            m_Options.Add(new OptionAttribute("Run HealthCheckRequest", () => RunCommandRequest(new HealthCheckImpl(), null)));
            m_Options.Add(new OptionAttribute("Run ServerInfoRequest", () => RunCommandRequest(new ServerInfoImpl(), null)));
            m_Options.Add(new OptionAttribute("Run ProcessInfoRequest", () => RunCommandRequest(new ProcessInfoImpl(), null)));
            m_Options.Add(new OptionAttribute("Run ManagedPackageRequest", () => RunCommandRequest(new ManagedPackageImpl(), "Enter command (e.g. install ccleaner /s | status | uninstall ccleaner):")));
            m_Options.Add(new OptionAttribute("Run FileSystemRequest", () => RunCommandRequest(new FileSystemImpl(), "Enter command (e.g. list C:\\Logs | exists C:\\Windows):")));
            m_Options.Add(new OptionAttribute("Run ServiceControlRequest", () => RunCommandRequest(new ServiceControlImpl(), "Enter command (e.g. list | status Spooler | start Spooler):")));
            m_Options.Add(new OptionAttribute("Run RegistryRequest", () => RunCommandRequest(new RegistryImpl(), "Enter command (e.g. list HKLM\\SOFTWARE):")));
            m_Options.Add(new OptionAttribute("Run PowerShellRequest", () => RunCommandRequest(new PowerShellImpl(), "Enter PowerShell command:")));
            m_Options.Add(new OptionAttribute("Run TaskPipelineRequest", () => RunCommandRequest(new TaskPipelineImpl(), "Enter command (e.g. list | run daily-serverinfo | reload):")));
            m_Options.Add(new OptionAttribute("Run SchedulerRequest", () => RunCommandRequest(new SchedulerImpl(), "Enter command (e.g. list | reload | run daily-serverinfo):")));

            WriteOptionsMenu();

            ConsoleKeyInfo consoleKeyinfo;

            do
            {
                consoleKeyinfo = Console.ReadKey();

                if (consoleKeyinfo.Key >= ConsoleKey.D1 && consoleKeyinfo.Key <= ConsoleKey.D9)
                {
                    if (int.TryParse(consoleKeyinfo.KeyChar.ToString(), out int consoleKeyindex))
                    {
                        OptionAttribute? consoleOption = m_Options.ElementAtOrDefault(consoleKeyindex - 1);
                        Action? consoleOptionAction = consoleOption?.OptSelected;

                        consoleOptionAction?.Invoke();
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
            for (int i = 0; i < m_Options.Count; i++)
            {
                Console.WriteLine("[{0}] {1}", i + 1, m_Options[i].Name);
            }
            Console.WriteLine("[x] Exit");
        }

        /// <summary>
        /// RunCommandRequest
        /// </summary>
        /// <param name="request"></param>
        /// <param name="commandPrompt"></param>
        /// <returns>Returns void.</returns>
        private static void RunCommandRequest(
            IServicesRequest request, 
            string? commandPrompt)
        {
            Console.WriteLine();
            Console.WriteLine("Run API {0}", request.APIName);

            if (!string.IsNullOrEmpty(commandPrompt))
            {
                Console.WriteLine(commandPrompt);
                request.Command = Console.ReadLine() ?? "";
            }

            request.APIKey = m_ClientRequestKey;

            string requestMessage = JsonConvert.SerializeObject(request);

            using (RequestSocket client = new("tcp://localhost:5589"))
            {
                client.SendFrame(requestMessage);

                string message = client.ReceiveFrameString();

                Console.WriteLine();
                Console.WriteLine("{0} : Received '{1}'", request.APIName, message);
            }

            WriteOptionsMenu();
        }
    }
}
