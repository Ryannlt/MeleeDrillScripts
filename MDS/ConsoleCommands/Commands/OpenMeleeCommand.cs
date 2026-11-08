using MDS.Events;
using MDS.ConfigVariables;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    public class OpenMeleeCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.OpenMelee;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            var arena = ArenaManager.Arena;
            if (arena == null || arena.Min == arena.Max)
            {
                errorMessage = "Arena is not properly defined. Set arena bounds.";
                return false;
            }

            if (parameters.Length == 0)
            {
                return true; // Use default values from configurables
            }

            if (parameters.Length == 2)
            {
                if (!float.TryParse(parameters[0], out _) || !float.TryParse(parameters[1], out _))
                {
                    errorMessage = "Spacing and offset must be valid numbers.";
                    return false;
                }
                return true;
            }

            errorMessage = "Invalid Arguments. Usage: rc openMelee [spacing:float] [offset:float]";
            return false;
        }

        public void Execute(int playerId, string[] parameters)
        {
            // Get spacing and offset from configurables by default
            var spacingConfig = ConfigurableRegistry.Get(ConfigurableEnum.OpenMeleeSpacing) as OpenMeleeSpacingConfigurable;
            var offsetConfig = ConfigurableRegistry.Get(ConfigurableEnum.OpenMeleeOffset) as OpenMeleeOffsetConfigurable;

            float spacing = spacingConfig?.GetValue() ?? 1.5f;
            float offset = offsetConfig?.GetValue() ?? 2f;

            // Override with custom values if provided
            if (parameters.Length == 2)
            {
                float.TryParse(parameters[0], out spacing);
                float.TryParse(parameters[1], out offset);
            }

            Logger.Log($"Attempting Open Melee with spacing {spacing} and offset {offset}", LogLevel.DEBUG);

            object[] eventParams = new object[] { spacing, offset };
            bool success = EventDispatcher.Trigger(EventEnum.OpenMelee, eventParams, out string errorMessage);

            if (!success && !string.IsNullOrEmpty(errorMessage))
            {
                Logger.Log($"OpenMeleeEvent failed: {errorMessage}", LogLevel.WARNING);
                Feedback.Reply(playerId, errorMessage);
            }
            else
            {
                Logger.Log("OpenMeleeEvent triggered successfully.", LogLevel.INFO);
            }
        }
    }
}
