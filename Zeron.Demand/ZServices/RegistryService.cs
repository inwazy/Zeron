// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Text.Json;
using Zeron.Demand.ZCore;
using Zeron.ZAttribute;
using Zeron.ZCore;
using Zeron.ZInterfaces;
using Win32Registry = Microsoft.Win32.Registry;

namespace Zeron.Demand.ZServices
{
    [ServicesRep(ZmqApiName = "Registry", ZmqApiEnabled = true, ZmqNotifySubscriber = false, ApiScope = "write")]

    /// <summary>
    /// RegistryService
    /// </summary>
    internal class RegistryService : IServices
    {
        /// <summary>
        /// OnRequest
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnRequest(
            dynamic aJson)
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
                    "read" => ReadValue(arguments),
                    "write" => WriteValue(arguments),
                    "delete" => DeleteValue(arguments),
                    "list" => ListSubKeys(arguments),
                    _ => ServiceResponse.SerializeFailure($"Unknown Registry command: {verb}")
                };
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "Registry Error:{0}\n{1}", e.Message, e.StackTrace));

                return ServiceResponse.SerializeFailure(e.Message);
            }
        }

        /// <summary>
        /// ReadValue
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>Returns JSON response.</returns>
        private static string ReadValue(
            string? arguments)
        {
            if (!TryParseRegistryPath(arguments, out Microsoft.Win32.RegistryKey hive, out string? subKeyPath, out string? valueName))
            {
                return ServiceResponse.SerializeFailure("Usage: read Hive\\SubKey\\ValueName");
            }

            using Microsoft.Win32.RegistryKey? key = hive.OpenSubKey(subKeyPath ?? string.Empty);

            if (key == null)
            {
                return ServiceResponse.SerializeFailure("Registry key not found.", arguments);
            }

            object? value = key.GetValue(valueName ?? string.Empty);

            PublishEvent("registry.read", arguments);

            return ServiceResponse.SerializeSuccess(new
            {
                path = arguments,
                value = value?.ToString(),
                kind = value == null ? null : key.GetValueKind(valueName).ToString()
            });
        }

        /// <summary>
        /// WriteValue
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>Returns JSON response.</returns>
        private static string WriteValue(
            string? arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return ServiceResponse.SerializeFailure("Usage: write Hive\\SubKey\\ValueName|value");
            }

            int separatorIndex = arguments.LastIndexOf('|');

            if (separatorIndex < 0)
            {
                return ServiceResponse.SerializeFailure("Usage: write Hive\\SubKey\\ValueName|value");
            }

            string registryPath = arguments[..separatorIndex].Trim();
            string value = arguments[(separatorIndex + 1)..];

            if (!TryParseRegistryPath(registryPath, out Microsoft.Win32.RegistryKey hive, out string? subKeyPath, out string? valueName))
            {
                return ServiceResponse.SerializeFailure("Invalid registry path.");
            }

            using Microsoft.Win32.RegistryKey? key = hive.CreateSubKey(subKeyPath ?? string.Empty, true);

            if (key == null)
            {
                return ServiceResponse.SerializeFailure("Unable to open registry key.", registryPath);
            }

            key.SetValue(valueName ?? string.Empty, value, Microsoft.Win32.RegistryValueKind.String);

            PublishEvent("registry.write", registryPath);

            return ServiceResponse.SerializeSuccess(new { path = registryPath, value });
        }

        /// <summary>
        /// DeleteValue
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>Returns JSON response.</returns>
        private static string DeleteValue(
            string? arguments)
        {
            if (!TryParseRegistryPath(arguments, out Microsoft.Win32.RegistryKey hive, out string? subKeyPath, out string? valueName))
            {
                return ServiceResponse.SerializeFailure("Usage: delete Hive\\SubKey\\ValueName");
            }

            using Microsoft.Win32.RegistryKey? key = hive.OpenSubKey(subKeyPath ?? string.Empty, true);

            if (key == null)
            {
                return ServiceResponse.SerializeFailure("Registry key not found.", arguments);
            }

            key.DeleteValue(valueName ?? string.Empty, false);

            PublishEvent("registry.delete", arguments);

            return ServiceResponse.SerializeSuccess(new { path = arguments, deleted = true });
        }

        /// <summary>
        /// ListSubKeys
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>Returns JSON response.</returns>
        private static string ListSubKeys(
            string? arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return ServiceResponse.SerializeFailure("Usage: list Hive\\SubKey");
            }

            int slashIndex = arguments.IndexOf('\\');

            if (slashIndex < 0)
            {
                return ServiceResponse.SerializeFailure("Usage: list Hive\\SubKey");
            }

            string hiveName = arguments[..slashIndex];
            string subKeyPath = arguments[(slashIndex + 1)..];

            if (!TryGetHive(hiveName, out Microsoft.Win32.RegistryKey? hive) || hive == null)
            {
                return ServiceResponse.SerializeFailure($"Unknown registry hive: {hiveName}");
            }

            using Microsoft.Win32.RegistryKey? key = hive.OpenSubKey(subKeyPath);

            if (key == null)
            {
                return ServiceResponse.SerializeFailure("Registry key not found.", arguments);
            }

            var subKeys = key.GetSubKeyNames().Select(name => new { name }).ToList();
            var values = key.GetValueNames()
                .Select(name => new
                {
                    name,
                    value = key.GetValue(name)?.ToString()
                })
                .ToList();

            return ServiceResponse.SerializeSuccess(new { subKeys, values });
        }

        /// <summary>
        /// TryParseRegistryPath
        /// </summary>
        /// <param name="path"></param>
        /// <param name="hive"></param>
        /// <param name="subKeyPath"></param>
        /// <param name="valueName"></param>
        /// <returns>Returns bool.</returns>
        private static bool TryParseRegistryPath(
            string? path, 
            out Microsoft.Win32.RegistryKey hive, 
            out string? subKeyPath, 
            out string? valueName)
        {
            hive = Win32Registry.CurrentUser;
            subKeyPath = null;
            valueName = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            int firstSlash = path.IndexOf('\\');

            if (firstSlash < 0)
            {
                return false;
            }

            string hiveName = path[..firstSlash];
            string remainder = path[(firstSlash + 1)..];
            int lastSlash = remainder.LastIndexOf('\\');

            if (lastSlash < 0)
            {
                return false;
            }

            if (!TryGetHive(hiveName, out Microsoft.Win32.RegistryKey? resolvedHive) || resolvedHive == null)
            {
                return false;
            }

            hive = resolvedHive;
            subKeyPath = remainder[..lastSlash];
            valueName = remainder[(lastSlash + 1)..];

            return !string.IsNullOrWhiteSpace(subKeyPath) && !string.IsNullOrWhiteSpace(valueName);
        }

        /// <summary>
        /// TryGetHive
        /// </summary>
        /// <param name="hiveName"></param>
        /// <param name="hive"></param>
        /// <returns>Returns bool.</returns>
        private static bool TryGetHive(
            string hiveName, 
            out Microsoft.Win32.RegistryKey? hive)
        {
            hive = hiveName.ToUpperInvariant() switch
            {
                "HKCU" or "HKEY_CURRENT_USER" => Win32Registry.CurrentUser,
                "HKLM" or "HKEY_LOCAL_MACHINE" => Win32Registry.LocalMachine,
                "HKCR" or "HKEY_CLASSES_ROOT" => Win32Registry.ClassesRoot,
                "HKU" or "HKEY_USERS" => Win32Registry.Users,
                "HKCC" or "HKEY_CURRENT_CONFIG" => Win32Registry.CurrentConfig,
                _ => null
            };

            return hive != null;
        }

        /// <summary>
        /// PublishEvent
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="path"></param>
        /// <returns>Returns void.</returns>
        private static void PublishEvent(
            string topic, 
            string? path)
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
        public string OnRequestAsync(
            dynamic aJson) => "";

        /// <summary>
        /// OnSubscriber
        /// </summary>
        /// <param name="aJson"></param>
        /// <returns>Returns string.</returns>
        public string OnSubscriber(
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
