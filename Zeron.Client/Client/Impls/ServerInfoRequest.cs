using Zeron.Interfaces;

namespace Zeron.Client.Client.Impls
{
    /// <summary>
    /// ServerInfoRequest
    /// </summary>
    public class ServerInfoRequest : IServicesRequest
    {
        /// <summary>
        /// APIName
        /// </summary>
        public string APIName { get; }

        /// <summary>
        /// APIKey
        /// </summary>
        public string APIKey { get; set; }

        /// <summary>
        /// Async
        /// </summary>
        public bool Async { get; set; }

        /// <summary>
        /// ServerInfoRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        public ServerInfoRequest()
        {
            APIName = "ServerInfo";
            APIKey = "";
            Async = false;
        }
    }
}
