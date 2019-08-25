using Zeron.Interfaces;
using Newtonsoft.Json;
using System.Dynamic;
using Zeron.Core;
using System;

namespace Zeron.Demand.Services
{
    [ServicesRep(ZmqApiName = "ServerInfo", ZmqApiEnabled = true)]

    /// <summary>
    /// ServerInfo
    /// </summary>
    internal class ServerInfo : IServices
    {
        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(dynamic aJson)
        {
            dynamic response = new ExpandoObject();

            response.success = true;
            response.machine_name = Environment.MachineName;
            response.os_version = Environment.OSVersion;

            return JsonConvert.SerializeObject(response);
        }
    }
}
