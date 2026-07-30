// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Text.Json;
using Zeron.Demand.ZCore;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZCore.Utils;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZServices
{
    [ServicesRep(ZmqApiName = "PowerShell", ZmqApiEnabled = true, ZmqNotifySubscriber = false, ApiScope = "write")]

    /// <summary>
    /// PowerShellService
    /// </summary>
    internal class PowerShellService : IServices
    {
        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(dynamic aJson)
        {
            try
            {
                string? command = Convert.ToString(aJson["Command"]);

                if (string.IsNullOrWhiteSpace(command))
                {
                    return ServiceResponse.SerializeFailure("PowerShell script/command is required.");
                }

                bool success = ScriptExecutor.Execute(command);

                InstallEventPublisher.Publish(success ? "powershell.completed" : "powershell.failed",
                    JsonSerializer.Serialize(new
                    {
                        success,
                        timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                    }));

                return success
                    ? ServiceResponse.SerializeSuccess(new { executed = true })
                    : ServiceResponse.SerializeFailure("PowerShell execution failed.");
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "PowerShell Error:{0}\n{1}", e.Message, e.StackTrace));

                return ServiceResponse.SerializeFailure(e.Message);
            }
        }

        /// <summary>
        /// OnRequestAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequestAsync(dynamic aJson) => "";

        /// <summary>
        /// OnSubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriber(dynamic aJson) => "";

        /// <summary>
        /// OnSubscriberAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriberAsync(dynamic aJson) => "";

        /// <summary>
        /// OnNotifySubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <param name="processedMsg"></param>
        /// <returns>Returns string.</returns>
        public string OnNotifySubscriber(dynamic aJson, string processedMsg) => "";
    }
}
