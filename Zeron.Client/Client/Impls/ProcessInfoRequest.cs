using Zeron.Interfaces;

namespace Zeron.Client.Client.Impls
{
    /// <summary>
    /// ProcessInfoRequest
    /// </summary>
    public class ProcessInfoRequest : IServicesRequest
    {
        // APIName.
        public string APIName { get; }

        // APIKey.
        public string APIKey { get; set; }

        // Async.
        public bool Async { get; set; }

        /// <summary>
        /// ProcessInfoRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        public ProcessInfoRequest()
        {
            APIName = "ProcessInfo";
            APIKey = "";
            Async = false;
        }
    }
}
