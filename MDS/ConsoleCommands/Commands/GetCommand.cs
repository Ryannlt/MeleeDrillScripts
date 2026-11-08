using System;
using MDS.ConfigVariables;

namespace MDS.ConsoleCommands
{
    public class GetCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.Get;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length < 1)
            {
                errorMessage = "Missing get key. Usage: rc get <key> [value]";
                return false;
            }

            if (!Enum.TryParse(parameters[0], true, out ConfigurableEnum parsedKey))
            {
                // Silent failure for unknown keys like the game’s own `rc get` usage
                return false;
            }

            if (!ConfigurableRegistry.TryGet(parsedKey, out var configurable))
            {
                errorMessage = $"Configurable '{parsedKey}' is not registered.";
                return false;
            }

            string[] args = parameters.Length > 1 ? parameters[1..] : Array.Empty<string>();
            return configurable.ValidateGet(args, out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            if (!Enum.TryParse(parameters[0], true, out ConfigurableEnum parsedKey)) return;

            if (!ConfigurableRegistry.TryGet(parsedKey, out var configurable)) return;

            string[] args = parameters.Length > 1 ? parameters[1..] : Array.Empty<string>();
            configurable.Get(playerId, args);
        }
    }
}
