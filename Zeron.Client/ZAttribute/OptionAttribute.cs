// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.Client.ZAttribute
{
    /// <summary>
    /// OptionAttribute
    /// </summary>
    internal class OptionAttribute
    {
        /// <summary>
        /// Name
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// OptSelected
        /// </summary>
        public Action? OptSelected { 
            get;
            set;
        }

        /// <summary>
        /// OptionAttribute
        /// </summary>
        /// <param name="name"></param>
        /// <param name="optselected"></param>
        /// <returns>Returns void.</returns>
        public OptionAttribute(string? name, Action? optselected)
        {
            Name = name;
            OptSelected = optselected;
        }
    }
}
