// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using Zeron.Demand.ZCore;
using Zeron.Demand.ZServers;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZInterfaces;

namespace Zeron.Demand.ZServices
{
    [ServicesSub(ZmqApiName = "RemoteCommand", ZmqApiEnabled = true)]

    /// <summary>
    /// RemoteCommand - SUB handler for server-to-agent push commands.
    /// </summary>
    internal class RemoteCommand : IServices
    {
        /// <summary>
        /// OnSubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriber(
            dynamic aJson)
        {
            try
            {
                string? targetApi = Convert.ToString(aJson["TargetApi"]);
                string? command = Convert.ToString(aJson["Command"]);

                if (string.IsNullOrWhiteSpace(targetApi))
                {
                    string? combined = Convert.ToString(aJson["Command"]);
                    (string? verb, string? args) = Helper.SplitCommand(combined);

                    targetApi = verb;
                    command = args;
                }

                if (string.IsNullOrWhiteSpace(targetApi))
                {
                    AuditServer.Log("RemoteCommand", command, false, "Missing TargetApi", "sub");

                    return ServiceResponse.SerializeFailure("TargetApi is required.");
                }

                string? assignmentId = Convert.ToString(aJson["AssignmentId"]);
                RemoteCommandContext.AssignmentId = assignmentId;

                try
                {
                    string response = InternalServiceInvoker.Invoke(targetApi, command);
                    bool success = response.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase)
                        || response.Contains("\"success\": true", StringComparison.OrdinalIgnoreCase);

                    AuditServer.Log(targetApi, command, success, success ? "Remote command executed" : response, "sub");

                    // ManagedPackage queue success reports interim result; final status comes from install.* completion.
                    ReporterServer.ReportTaskResult(assignmentId, success, response, success ? null : response);

                    InstallEventPublisher.PublishObject("remotecommand.executed", new
                    {
                        targetApi,
                        command,
                        success,
                        assignmentId
                    });

                    return response;
                }
                finally
                {
                    RemoteCommandContext.AssignmentId = null;
                }
            }
            catch (Exception e)
            {
                RemoteCommandContext.AssignmentId = null;
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "RemoteCommand Error:{0}\n{1}", e.Message, e.StackTrace));
                AuditServer.Log("RemoteCommand", "", false, e.Message, "sub");

                return ServiceResponse.SerializeFailure(e.Message);
            }
        }

        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(
            dynamic aJson) => "";

        /// <summary>
        /// OnRequestAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequestAsync(
            dynamic aJson) => "";

        /// <summary>
        /// OnSubscriberAsync
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriberAsync(
            dynamic aJson) => "";

        /// <summary>
        /// OnNotifySubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <param name="processedMsg"></param>
        /// <returns>Returns string.</returns>
        public string OnNotifySubscriber(
            dynamic aJson, 
            string processedMsg) => "";
    }
}
