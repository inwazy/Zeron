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
    }
}
