// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Server
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
        public static void Main(
            string[] args)
        {
            // Ensure relative paths (Data/, NLog.config) resolve next to the binary when run as a service.
            Environment.CurrentDirectory = AppContext.BaseDirectory;
            CreateApp(args).Run();
        }

        /// <summary>
        /// CreateApp
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns WebApplication.</returns>
        public static WebApplication CreateApp(
            string[] args)
        {
            return ServerHost.BuildApplication(args);
        }
    }
}
