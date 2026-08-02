// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Zeron.ZCore.Type;

namespace Zeron.ZCore
{
    /// <summary>
    /// Helper
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// BuildCommands
        /// </summary>
        /// <param name="command"></param>
        /// <returns>Returns ServicesSubCommandType.</returns>
        public static ServicesSubCommandType BuildCommands(
            string? command)
        {
            ServicesSubCommandType result = new();

            if (command == null || command.Length == 0)
            {
                return result;
            }

            string[] commands = command.Split(' ', 3);

            if (commands.Length == 2)
            {
                result.Option = commands[0].Trim();
                result.PackageName = commands[1].Trim();
            }

            if (commands.Length == 3)
            {
                result.Option = commands[0].Trim();
                result.PackageName = commands[1].Trim();
                result.Args = commands[2];
            }

            return result;
        }

        /// <summary>
        /// SplitCommand - splits "verb remainder" from a command string.
        /// </summary>
        /// <param name="command"></param>
        /// <returns>Returns verb and arguments.</returns>
        public static (string? Verb, string? Arguments) SplitCommand(
            string? command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return (null, null);
            }

            command = command.Trim();
            int spaceIndex = command.IndexOf(' ');

            if (spaceIndex < 0)
            {
                return (command, null);
            }

            return (command[..spaceIndex].Trim(), command[(spaceIndex + 1)..].Trim());
        }
    }
}
