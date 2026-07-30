// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using NCrontab;
using System.Collections.Specialized;
using System.Globalization;
using System.Timers;
using Zeron.ZCore;
using Zeron.ZCore.Container;
using Zeron.ZCore.Foundation;
using Zeron.ZCore.Type;
using Zeron.ZInterfaces;

namespace Zeron.ZServers
{
    /// <summary>
    /// SchedulerServer - NCrontab-based scheduled task runner.
    /// </summary>
    public class SchedulerServer : ConfigurationTable, IServer
    {
        // Last run times for each task.
        private static readonly Dictionary<string, DateTime> s_LastRun = new(StringComparer.OrdinalIgnoreCase);

        // Schedules for each task.
        private static readonly Dictionary<string, CrontabSchedule> s_Schedules = new(StringComparer.OrdinalIgnoreCase);

        // Timer for checking task schedules.
        private static readonly System.Timers.Timer s_Timer = new();

        // Tasks to be executed.
        private static List<SchedulerTaskDefinition> s_Tasks = [];

        // Whether the scheduler is enabled.
        private static bool s_Enabled;

        // Path to the tasks file.
        private static string? s_TasksFile;

        /// <summary>
        /// CheckIntervalMs
        /// </summary>
        public static int CheckIntervalMs
        {
            get;
            set;
        } = 60000;

        /// <summary>
        /// OnTaskDue - callback invoked when a scheduled task is due.
        /// </summary>
        public static Action<SchedulerTaskDefinition>? OnTaskDue
        {
            get;
            set;
        }

        /// <summary>
        /// LoadConfig
        /// </summary>
        /// <param name="aConfig"></param>
        /// <returns>Returns void.</returns>
        public override void LoadConfig(NameValueCollection aConfig)
        {
            try
            {
                s_Enabled = bool.Parse(aConfig["scheduler_enabled"] ?? "true");
                s_TasksFile = aConfig["scheduler_tasks_file"] ?? "Resource/scheduler-tasks.json";
                CheckIntervalMs = int.Parse(aConfig["scheduler_check_interval_ms"] ?? "60000", CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture, "SchedulerServer Config Error:{0}\n{1}", e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Initialize()
        {
            ReloadTasks();

            if (!s_Enabled)
            {
                ZNLogger.Common.Info("SchedulerServer disabled.");

                return;
            }

            s_Timer.Elapsed += OnTimerElapsed;
            s_Timer.Interval = CheckIntervalMs;
            s_Timer.AutoReset = true;
            s_Timer.Enabled = true;

            ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                "SchedulerServer initialized with {0} task(s).", s_Tasks.Count));
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns>Returns void.</returns>
        public void Stop()
        {
            s_Timer.Stop();
            s_Timer.Dispose();

            ZNLogger.Common.Info("SchedulerServer stopped.");

            ServerIntegrate.FinishSingleStop();
        }

        /// <summary>
        /// ReloadTasks
        /// </summary>
        /// <returns>Returns void.</returns>
        public static void ReloadTasks()
        {
            s_Tasks = TaskPipelineParser.ParseFile(s_TasksFile);
            s_Schedules.Clear();

            foreach (SchedulerTaskDefinition task in s_Tasks)
            {
                if (task.Name == null || string.IsNullOrWhiteSpace(task.Cron))
                {
                    continue;
                }

                try
                {
                    s_Schedules[task.Name] = CrontabSchedule.Parse(task.Cron);
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                        "SchedulerServer invalid cron for task '{0}': {1}", task.Name, e.Message));
                }
            }
        }

        /// <summary>
        /// GetTasks
        /// </summary>
        /// <returns>Returns task list.</returns>
        public static IReadOnlyList<SchedulerTaskDefinition> GetTasks()
        {
            return s_Tasks;
        }

        /// <summary>
        /// OnTimerElapsed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns>Returns void.</returns>
        private static void OnTimerElapsed(object? sender, ElapsedEventArgs args)
        {
            DateTime now = DateTime.Now;

            foreach (SchedulerTaskDefinition task in s_Tasks)
            {
                if (!task.Enabled || task.Name == null || !s_Schedules.TryGetValue(task.Name, out CrontabSchedule? schedule))
                {
                    continue;
                }

                DateTime baseTime = s_LastRun.GetValueOrDefault(task.Name, now.AddMinutes(-1));
                DateTime nextOccurrence = schedule.GetNextOccurrence(baseTime);

                if (nextOccurrence > now)
                {
                    continue;
                }

                s_LastRun[task.Name] = now;

                ZNLogger.Common.Info(string.Format(CultureInfo.InvariantCulture,
                    "SchedulerServer triggering task '{0}'", task.Name));

                try
                {
                    OnTaskDue?.Invoke(task);
                }
                catch (Exception e)
                {
                    ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                        "SchedulerServer task '{0}' failed: {1}\n{2}", task.Name, e.Message, e.StackTrace));
                }
            }
        }
    }
}
