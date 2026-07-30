// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore;
using Zeron.ZCore.Container;

namespace Zeron.Demand.ZCore
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
            ZNLogger.Common.Info("Zeron.Demand service started.");
        }

        /// <summary>
        /// WhenStop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void WhenStop()
        {
            ZNLogger.Common.Info("Zeron.Demand service stopping...");
            ServerIntegrate.StopAll();
            ZNLogger.Common.Info("Zeron.Demand service stopped.");
        }

        /// <summary>
        /// WhenPause
        /// </summary>
        /// <returns>Returns void.</returns>
        public void WhenPause()
        {
            ZNLogger.Common.Info("Zeron.Demand service paused.");
        }

        /// <summary>
        /// WhenContinue
        /// </summary>
        /// <returns>Returns void.</returns>
        public void WhenContinue()
        {
            ZNLogger.Common.Info("Zeron.Demand service continued.");
        }

        /// <summary>
        /// WhenShutdown
        /// </summary>
        /// <returns>Returns void.</returns>
        public void WhenShutdown()
        {
            ZNLogger.Common.Info("Zeron.Demand service shutting down...");
            ServerIntegrate.StopAll();
            ZNLogger.Common.Info("Zeron.Demand service shutdown complete.");
        }
    }
}
