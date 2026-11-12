using System;
using System.Collections.Generic;

// The ConfigManager handles custom config variables by parsing data from method PassConfigVariables for our own custom commands.

namespace MDS.ConfigVariables
{
    public static class ConfigManager
    {
        private static readonly Dictionary<ConfigCommandEnum, IConfigVariables> configCommands = new();

        static ConfigManager()
        {
            RegisterAllConfigVariables();
        }

        private static void RegisterAllConfigVariables()
        {
            RegisterConfigVariable(ConfigCommandEnum.EnableDebugLogging, new EnableDebugLogging());
            RegisterConfigVariable(ConfigCommandEnum.EnableAdminOnly, new EnableAdminOnly());
            RegisterConfigVariable(ConfigCommandEnum.SetArena, new SetArena());
            RegisterConfigVariable(ConfigCommandEnum.AddArena, new AddArena());
            RegisterConfigVariable(ConfigCommandEnum.SetArenaCorner1, new SetArenaCorner1());
            RegisterConfigVariable(ConfigCommandEnum.SetArenaCorner2, new SetArenaCorner2());
            RegisterConfigVariable(ConfigCommandEnum.SetOpenMeleeSpacing, new SetOpenMeleeSpacing());
            RegisterConfigVariable(ConfigCommandEnum.SetOpenMeleeOffset, new SetOpenMeleeOffset());
            RegisterConfigVariable(ConfigCommandEnum.SetXvXDistance, new SetXvXDistance());
            RegisterConfigVariable(ConfigCommandEnum.SetXvXSpacing, new SetXvXSpacing());
            RegisterConfigVariable(ConfigCommandEnum.SetXvXStrategy, new SetXvXStrategy());
            RegisterConfigVariable(ConfigCommandEnum.SetGroupfightDistance, new SetGroupfightDistance());
            RegisterConfigVariable(ConfigCommandEnum.SetGroupfightSpacing, new SetGroupfightSpacing());
            RegisterConfigVariable(ConfigCommandEnum.SetGroupfightStrategy, new SetGroupfightStrategy());
            RegisterConfigVariable(ConfigCommandEnum.SetOrientation, new SetOrientation());

            Logger.Log($"Registered {configCommands.Count} config variables.", LogLevel.DEBUG);
        }

        private static void RegisterConfigVariable(ConfigCommandEnum key, IConfigVariables command)
        {
            if (configCommands.ContainsKey(key))
            {
                Logger.Log($"Config variable {key} is already registered. Overwriting...", LogLevel.DEBUG);
            }

            configCommands[key] = command;
        }

        public static void ProcessConfigVariables(string[] configEntries)
        {
            if (configEntries == null || configEntries.Length == 0)
            {
                Logger.Log("Received empty config array. Ignoring.", LogLevel.DEBUG);
                return;
            }

            foreach (string entry in configEntries)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    Logger.Log("Ignoring empty config entry.", LogLevel.DEBUG);
                    continue;
                }

                string[] parts = entry.Split(':');

                if (parts.Length < 2)
                {
                    Logger.Log($"Ignoring malformed config entry: {entry}", LogLevel.DEBUG);
                    continue;
                }

                string modId = parts[0];
                string key = parts[1];
                string data = parts.Length > 2 ? parts[2] : "";

                if (!modId.Equals("MDS", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log($"Ignoring non-MDS config command: {modId}", LogLevel.DEBUG);
                    continue;
                }

                if (!Enum.TryParse(key, true, out ConfigCommandEnum parsedKey))
                {
                    Logger.Log($"Unknown config command key: {key}", LogLevel.WARNING);
                    continue;
                }

                if (!configCommands.TryGetValue(parsedKey, out IConfigVariables command))
                {
                    Logger.Log($"No handler registered for config variable: {parsedKey}", LogLevel.WARNING);
                    continue;
                }

                if (!command.Validate(data))
                {
                    Logger.Log($"Invalid value '{data}' for config variable '{parsedKey}'", LogLevel.WARNING);
                    continue;
                }

                command.Execute(data);
            }
        }
    }
}
