namespace Zeron.Interfaces
{
    /// <summary>
    /// IServices
    /// </summary>
    public interface IServices
    {
        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        string OnRequest(dynamic aJson);

        /// <summary>
        /// OnNotifySubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <param name="processedMsg"></param>
        /// <returns>Returns string.</returns>
        string OnNotifySubscriber(dynamic aJson, string processedMsg);
    }
}
