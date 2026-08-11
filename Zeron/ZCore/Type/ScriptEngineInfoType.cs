// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Type
{
    /// <summary>
    /// ScriptEngineInfoType - engine capability descriptor for heartbeat / HealthCheck.
    /// </summary>
    public sealed class ScriptEngineInfoType
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id
        {
            get;
            set;
        } = "";

        /// <summary>
        /// DisplayName
        /// </summary>
        public string DisplayName
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Platforms
        /// </summary>
        public List<string> Platforms
        {
            get;
            set;
        } = [];

        /// <summary>
        /// Available
        /// </summary>
        public bool Available
        {
            get;
            set;
        }
    }
}
