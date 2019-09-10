namespace Zeron.Interfaces
{
    /// <summary>
    /// IServicesRequest
    /// </summary>
    public interface IServicesRequest
    {
        // APIName.
        string APIName { get; }

        // APIKey.
        string APIKey { get; set; }

        // Async.
        bool Async { get; set; }
    }
}
