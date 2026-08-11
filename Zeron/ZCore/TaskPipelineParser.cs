// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Text.Json;
using Zeron.ZCore.Type;

namespace Zeron.ZCore
{
    /// <summary>
    /// TaskPipelineParser
    /// </summary>
    public static class TaskPipelineParser
    {
        // JSON serializer options.
        private static readonly JsonSerializerOptions s_Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// ParseFile
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Returns list of SchedulerTaskDefinition.</returns>
        public static List<SchedulerTaskDefinitionType> ParseFile(
            string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return [];
            }

            string json = File.ReadAllText(filePath);

            return ParseJson(json);
        }

        /// <summary>
        /// ParseJson
        /// </summary>
        /// <param name="json"></param>
        /// <returns>Returns list of SchedulerTaskDefinition.</returns>
        public static List<SchedulerTaskDefinitionType> ParseJson(
            string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            TaskPipelineDocument? document = JsonSerializer.Deserialize<TaskPipelineDocument>(json, s_Options);

            return document?.Tasks ?? [];
        }

        /// <summary>
        /// FindTask
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="taskName"></param>
        /// <returns>Returns SchedulerTaskDefinition or null.</returns>
        public static SchedulerTaskDefinitionType? FindTask(
            IEnumerable<SchedulerTaskDefinitionType> tasks, 
            string? taskName)
        {
            if (string.IsNullOrWhiteSpace(taskName))
            {
                return null;
            }

            return tasks.FirstOrDefault(task =>
                task.Name != null
                && task.Name.Equals(taskName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// TaskPipelineDocument
        /// </summary>
        /// <returns>Returns list of SchedulerTaskDefinition.</returns>
        private sealed class TaskPipelineDocument
        {
            /// <summary>
            /// Tasks
            /// </summary>
            /// <returns>Returns list of SchedulerTaskDefinition.</returns>
            public List<SchedulerTaskDefinitionType>? Tasks
            {
                get;
                set;
            }
        }
    }
}
