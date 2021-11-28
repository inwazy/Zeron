// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Demand.ZCore
{
    /// <summary>
    /// SCHostLoader
    /// </summary>
    internal class SCHostLoader
    {
        /// <summary>
        /// BeforeInstall
        /// </summary>
        /// <returns>Returns void.</returns>
        public void BeforeInstall()
        {
            Console.WriteLine("BeforeInstall");
        }

        /// <summary>
        /// BeforeUninstall
        /// </summary>
        /// <returns>Returns void.</returns>
        public void BeforeUninstall()
        {
            Console.WriteLine("BeforeUninstall");
        }

        /// <summary>
        /// AfterInstall
        /// </summary>
        /// <returns>Returns void.</returns>
        public void AfterInstall()
        {
            Console.WriteLine("AfterInstall");
        }

        /// <summary>
        /// AfterUninstall
        /// </summary>
        /// <returns>Returns void.</returns>
        public void AfterUninstall()
        {
            Console.WriteLine("AfterUninstall");
        }
    }
}
