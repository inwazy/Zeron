using Zeron.Interfaces;

namespace Zeron.Client.Client.Impls
{
    /// <summary>
    /// ProcessInfoRequest
    /// </summary>
    public class InstallGitRequest : IServicesRequest
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
        /// InstallGitRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        public InstallGitRequest()
        {
            APIName = "InstallGit";
            APIKey = "";
            Async = false;
        }
    }
}
