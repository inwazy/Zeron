namespace Zeron.Interfaces
{
    /// <summary>
    /// IServicesRequest
    /// </summary>
    public interface IServicesRequest
    {
        /// <summary>
        /// APIName
        /// </summary>
        string APIName { get; }

        /// <summary>
        /// APIKey
        /// </summary>
        string APIKey { get; set; }

        /// <summary>
        /// Async
        /// </summary>
        bool Async { get; set; }
    }
}
