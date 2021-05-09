// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System;

namespace Zeron.Demand.Core
{
    /// <summary>
    /// SCLoader
    /// </summary>
    internal class SCLoader
    {
        /// <summary>
        /// WhenStart
        /// </summary>
        /// <returns>Returns void.</returns>
        public void WhenStart()
        {
            Console.WriteLine("WhenStart");
        }

        /// <summary>
        /// WhenStop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void WhenStop()
        {
            Console.WriteLine("WhenStop");
        }

        /// <summary>
        /// WhenPause
        /// </summary>
        /// <returns>Returns void.</returns>
        public void WhenPause()
        {
            Console.WriteLine("WhenPause");
        }

        /// <summary>
        /// WhenContinue
        /// </summary>
        /// <returns>Returns void.</returns>
        public void WhenContinue()
        {
            Console.WriteLine("WhenContinue");
        }

        /// <summary>
        /// WhenShutdown
        /// </summary>
        /// <returns>Returns void.</returns>
        public void WhenShutdown()
        {
            Console.WriteLine("WhenShutdown");
        }
    }
}
