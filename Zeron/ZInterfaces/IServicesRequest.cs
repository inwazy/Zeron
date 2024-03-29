﻿// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZInterfaces
{
    /// <summary>
    /// IServicesRequest
    /// </summary>
    public interface IServicesRequest
    {
        /// <summary>
        /// APIName
        /// </summary>
        string APIName { get; }

        /// <summary>
        /// APIKey
        /// </summary>
        string APIKey { get; set; }

        /// <summary>
        /// Command
        /// </summary>
        string Command { get; set; }

        /// <summary>
        /// Async
        /// </summary>
        bool Async { get; set; }
    }
}
