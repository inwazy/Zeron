using Zeron.Interfaces;

namespace Zeron.Client.Client.Impls
{
    /// <summary>
    /// ProcessInfoRequest
    /// </summary>
    public class InstallGitRequest : IServicesRequest
    {
        // APIName.
        public string APIName { get; }

        // APIKey.
        public string APIKey { get; set; }

        /// <summary>
        /// InstallGitRequest
        /// </summary>
        /// <returns>Returns void.</returns>
        public InstallGitRequest()
        {
            APIName = "InstallGit";
            APIKey = "";
        }
    }
}
