// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Text;
using System.Text.Json;
using Zeron.Demand.ZCore;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZInterfaces;
using Zeron.ZServers;

namespace Zeron.Demand.ZServices
{
    [ServicesRep(ZmqApiName = "FileSystem", ZmqApiEnabled = true, ZmqNotifySubscriber = false)]

    /// <summary>
    /// FileSystem
    /// </summary>
    internal class FileSystem : IServices
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
                    "list" => ListPath(arguments),
                    "read" => ReadFile(arguments),
                    "write" => WriteFile(arguments),
                    "delete" => DeletePath(arguments),
                    "exists" => ExistsPath(arguments),
                    _ => ServiceResponse.SerializeFailure($"Unknown FileSystem command: {verb}")
                };
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "FileSystem Error:{0}\n{1}", e.Message, e.StackTrace));

                return ServiceResponse.SerializeFailure(e.Message);
            }
        }

        /// <summary>
        /// ListPath
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns JSON response.</returns>
        private static string ListPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return ServiceResponse.SerializeFailure("Directory not found.", path);
            }

            var entries = Directory.GetFileSystemEntries(path)
                .Select(entry => new
                {
                    name = Path.GetFileName(entry),
                    path = entry,
                    isDirectory = Directory.Exists(entry)
                })
                .ToList();

            PublishEvent("filesystem.list", path);

            return ServiceResponse.SerializeSuccess(entries);
        }

        /// <summary>
        /// ReadFile
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns JSON response.</returns>
        private static string ReadFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return ServiceResponse.SerializeFailure("File not found.", path);
            }

            byte[] content = File.ReadAllBytes(path);

            PublishEvent("filesystem.read", path);

            return ServiceResponse.SerializeSuccess(new
            {
                path,
                size = content.Length,
                contentBase64 = Convert.ToBase64String(content)
            });
        }

        /// <summary>
        /// WriteFile - format: "path|base64content"
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>Returns JSON response.</returns>
        private static string WriteFile(string? arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return ServiceResponse.SerializeFailure("Usage: write path|base64content");
            }

            int separatorIndex = arguments.IndexOf('|');

            if (separatorIndex < 0)
            {
                return ServiceResponse.SerializeFailure("Usage: write path|base64content");
            }

            string path = arguments[..separatorIndex].Trim();
            string base64 = arguments[(separatorIndex + 1)..].Trim();
            byte[] content = Convert.FromBase64String(base64);

            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, content);

            PublishEvent("filesystem.write", path);

            return ServiceResponse.SerializeSuccess(new { path, size = content.Length });
        }

        /// <summary>
        /// DeletePath
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns JSON response.</returns>
        private static string DeletePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ServiceResponse.SerializeFailure("Path is required.");
            }

            if (File.Exists(path))
            {
                File.Delete(path);
                PublishEvent("filesystem.delete", path);

                return ServiceResponse.SerializeSuccess(new { path, deleted = true, type = "file" });
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                PublishEvent("filesystem.delete", path);

                return ServiceResponse.SerializeSuccess(new { path, deleted = true, type = "directory" });
            }

            return ServiceResponse.SerializeFailure("Path not found.", path);
        }

        /// <summary>
        /// ExistsPath
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns JSON response.</returns>
        private static string ExistsPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ServiceResponse.SerializeFailure("Path is required.");
            }

            return ServiceResponse.SerializeSuccess(new
            {
                path,
                exists = File.Exists(path) || Directory.Exists(path),
                isFile = File.Exists(path),
                isDirectory = Directory.Exists(path)
            });
        }

        /// <summary>
        /// PublishEvent
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="path"></param>
        /// <returns>Returns void.</returns>
        private static void PublishEvent(string topic, string? path)
        {
            InstallEventPublisher.Publish(topic, JsonSerializer.Serialize(new
            {
                path,
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
    }
}
