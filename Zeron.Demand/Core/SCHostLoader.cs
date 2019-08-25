using System;

namespace Zeron.Demand.Core
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
