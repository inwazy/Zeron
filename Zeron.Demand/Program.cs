// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NLog;
using Zeron.Demand;

namespace Zeron.Demand
{
    /// <summary>
    /// Program
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        public static void Main(string[] args)
        {
            LogManager.Setup().LoadConfigurationFromFile("NLog.config");

            bool result = BootLoader();

            if (!result)
            {
                return;
            }

            DemandHost.RunService();
        }

        /// <summary>
        /// BootLoader
        /// </summary>
        /// <returns>Returns bool.</returns>
        private static bool BootLoader() => DemandBoot.RegisterStandardServers();
    }
}