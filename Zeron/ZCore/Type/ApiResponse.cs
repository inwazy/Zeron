// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ApiResponse
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Success
        /// </summary>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Message
        /// </summary>
        public string? Message
        {
            get;
            set;
        }

        /// <summary>
        /// Result
        /// </summary>
        public T? Result
        {
            get;
            set;
        }

        /// <summary>
        /// AgentId
        /// </summary>
        public string? AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// Timestamp
        /// </summary>
        public string? Timestamp
        {
            get;
            set;
        }
    }
}
