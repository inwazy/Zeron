using Newtonsoft.Json;
using System;
using System.Dynamic;
using Zeron.Core;
using Zeron.Interfaces;

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
            response.os_version = Environment.OSVersion.ToString();
            response.user_name = Environment.UserName;

            return JsonConvert.SerializeObject(response);
        }
    }
}
