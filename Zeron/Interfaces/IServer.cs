namespace Zeron.Interfaces
{
    /// <summary>
    /// IServer
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        void Initialize();

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        void Stop();
    }
}
