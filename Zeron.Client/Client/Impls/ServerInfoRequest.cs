namespace Zeron.Client.Client.Impls
{
    /// <summary>
    /// ServerInfoRequest
    /// </summary>
    public class ServerInfoRequest
    {
        // APIName.
        public string APIName { get; }

        // APIKey.
        public string APIKey { get; set; }

        /// <summary>
        /// ServerInfo
        /// </summary>
        /// <returns>Returns void.</returns>
        public ServerInfoRequest()
        {
            APIName = "ServerInfo";
            APIKey = "";
        }
    }
}
