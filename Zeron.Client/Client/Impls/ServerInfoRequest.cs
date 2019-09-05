using Zeron.Interfaces;

namespace Zeron.Client.Client.Impls
{
    /// <summary>
    /// ServerInfoRequest
    /// </summary>
    public class ServerInfoRequest : IServicesRequest
    {
        // APIName.
        public string APIName { get; }

        // APIKey.
        public string APIKey { get; set; }

        /// <summary>
        /// ServerInfoRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        public ServerInfoRequest()
        {
            APIName = "ServerInfo";
            APIKey = "";
        }
    }
}
