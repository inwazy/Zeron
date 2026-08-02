// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// AgentIdProvider - loads or creates a persistent agent identifier.
    /// </summary>
    public static class AgentIdProvider
    {
        /// <summary>
        /// LoadOrCreate
        /// </summary>
        /// <param name="configuredAgentId"></param>
        /// <param name="identityFilePath"></param>
        /// <returns>Returns agent id.</returns>
        public static string LoadOrCreate(
            string? configuredAgentId, 
            string? identityFilePath)
        {
            if (!string.IsNullOrWhiteSpace(configuredAgentId))
            {
                return configuredAgentId.Trim();
            }

            string filePath = ResolveIdentityFilePath(identityFilePath);

            if (File.Exists(filePath))
            {
                string existingId = File.ReadAllText(filePath).Trim();

                if (!string.IsNullOrWhiteSpace(existingId))
                {
                    return existingId;
                }
            }

            string newId = Guid.NewGuid().ToString("D");
            string? directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, newId);

            return newId;
        }

        /// <summary>
        /// ResolveIdentityFilePath
        /// </summary>
        /// <param name="identityFilePath"></param>
        /// <returns>Returns file path.</returns>
        public static string ResolveIdentityFilePath(
            string? identityFilePath)
        {
            if (!string.IsNullOrWhiteSpace(identityFilePath))
            {
                return Path.IsPathRooted(identityFilePath)
                    ? identityFilePath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, identityFilePath);
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "agent.id");
        }
    }
}
