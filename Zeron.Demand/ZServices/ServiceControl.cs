// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.ServiceProcess;
using System.Text.Json;
using Zeron.Demand.ZCore;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServices
{
    [ServicesRep(ZmqApiName = "ServiceControl", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// ServiceControl
    /// </summary>
    internal class ServiceControl : IServices
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
                (string? verb, string? arguments) = Helper.SplitCommand(command);

                if (string.IsNullOrEmpty(verb))
                {
                    return ServiceResponse.SerializeFailure("Missing command verb.");
                }

                return verb.ToLowerInvariant() switch
                {
                    "list" => ListServices(),
                    "status" => ServiceStatus(arguments),
                    "start" => ControlService(arguments, ServiceAction.Start),
                    "stop" => ControlService(arguments, ServiceAction.Stop),
                    "restart" => RestartService(arguments),
                    _ => ServiceResponse.SerializeFailure($"Unknown ServiceControl command: {verb}")
                };
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "ServiceControl Error:{0}\n{1}", e.Message, e.StackTrace));

                return ServiceResponse.SerializeFailure(e.Message);
            }
        }

        /// <summary>
        /// ListServices
        /// </summary>
        /// <returns>Returns JSON response.</returns>
        private static string ListServices()
        {
            var services = ServiceController.GetServices()
                .Select(service => new
                {
                    name = service.ServiceName,
                    displayName = service.DisplayName,
                    status = service.Status.ToString(),
                    canStop = service.CanStop
                })
                .OrderBy(service => service.name)
                .ToList();

            return ServiceResponse.SerializeSuccess(services);
        }

        /// <summary>
        /// ServiceStatus
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns>Returns JSON response.</returns>
        private static string ServiceStatus(string? serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return ServiceResponse.SerializeFailure("Service name is required.");
            }

            using ServiceController controller = new(serviceName);
            controller.Refresh();

            return ServiceResponse.SerializeSuccess(new
            {
                name = controller.ServiceName,
                displayName = controller.DisplayName,
                status = controller.Status.ToString(),
                canStop = controller.CanStop,
                canPauseAndContinue = controller.CanPauseAndContinue
            });
        }

        /// <summary>
        /// ControlService
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="action"></param>
        /// <returns>Returns JSON response.</returns>
        private static string ControlService(string? serviceName, ServiceAction action)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return ServiceResponse.SerializeFailure("Service name is required.");
            }

            using ServiceController controller = new(serviceName);
            controller.Refresh();

            switch (action)
            {
                case ServiceAction.Start:
                    if (controller.Status != ServiceControllerStatus.Running)
                    {
                        controller.Start();
                        controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    }

                    break;

                case ServiceAction.Stop:
                    if (controller.CanStop && controller.Status != ServiceControllerStatus.Stopped)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }

                    break;
            }

            controller.Refresh();
            PublishEvent($"service.{action.ToString().ToLowerInvariant()}", serviceName, controller.Status.ToString());

            return ServiceResponse.SerializeSuccess(new
            {
                name = controller.ServiceName,
                status = controller.Status.ToString(),
                action = action.ToString()
            });
        }

        /// <summary>
        /// RestartService
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns>Returns JSON response.</returns>
        private static string RestartService(string? serviceName)
        {
            string stopResult = ControlService(serviceName, ServiceAction.Stop);
            if (!stopResult.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase))
            {
                return stopResult;
            }

            return ControlService(serviceName, ServiceAction.Start);
        }

        /// <summary>
        /// PublishEvent
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="serviceName"></param>
        /// <param name="status"></param>
        /// <returns>Returns void.</returns>
        private static void PublishEvent(string topic, string serviceName, string status)
        {
            InstallEventPublisher.Publish(topic, JsonSerializer.Serialize(new
            {
                service = serviceName,
                status,
                timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            }));
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

        /// <summary>
        /// ServiceAction
        /// </summary>
        /// <returns>Returns void.</returns>
        private enum ServiceAction
        {
            Start,
            Stop
        }
    }
}
