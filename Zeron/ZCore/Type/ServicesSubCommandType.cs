// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ServicesSubCommandType
    /// </summary>
    public class ServicesSubCommandType
    {
        /// <summary>
        /// Option
        /// </summary>
        public string? Option
        {
            get;
            set;
        }

        /// <summary>
        /// PackageName
        /// </summary>
        public string? PackageName
        {
            get;
            set;
        }

        /// <summary>
        /// Args
        /// </summary>
        public string? Args
        {
            get;
            set;
        }

        /// <summary>
        /// ServicesSubCommandType
        /// </summary>
        /// <returns>Returns void.</returns>
        public ServicesSubCommandType()
        {
            Option = "";
            PackageName = "";
            Args = "";
        }
    }
}
