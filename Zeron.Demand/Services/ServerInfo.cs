using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.Globalization;
using Zeron.Core;
using Zeron.Interfaces;

namespace Zeron.Demand.Services
{
    [ServicesRep(ZmqApiName = "ServerInfo", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

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

            try
            {
                response.success = true;
                response.machine_name = Environment.MachineName;
                response.os_version = Environment.OSVersion.ToString();
                response.user_name = Environment.UserName;
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ServerInfo Error:{0}\n{1}", e.Message, e.StackTrace));
            }

            return JsonConvert.SerializeObject(response);
        }

        /// <summary>
        /// OnRequestAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequestAsync(dynamic aJson)
        {
            return "";
        }

        /// <summary>
        /// OnNotifySubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <param name="processedMsg"></param>
        /// <returns>Returns string.</returns>
        public string OnNotifySubscriber(dynamic aJson, string processedMsg)
        {
            return "";
        }
    }
}
