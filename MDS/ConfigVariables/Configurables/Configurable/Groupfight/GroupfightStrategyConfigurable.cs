using System;
using MDS.Systems;
using MDS.Core;

namespace MDS.ConfigVariables
{
    public class GroupfightStrategyConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.GroupfightStrategy;

        public SelectionStrategyType Strategy { get; set; } = SelectionStrategyType.Random;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1)
            {
                errorMessage = "Usage: rc set groupfightstrategy <Random|Repeat>";
                return false;
            }

            string input = args[0];
            if (int.TryParse(input, out _))
            {
                errorMessage = $"Invalid strategy '{input}'. Strategy name must be one of: Random, Repeat.";
                return false;
            }

            if (!Enum.TryParse(input, true, out SelectionStrategyType parsed))
            {
                errorMessage = $"Invalid strategy '{input}'. Valid options are: Random, Repeat.";
                return false;
            }

            if (parsed != SelectionStrategyType.Random && parsed != SelectionStrategyType.Repeat)
            {
                errorMessage = "GroupfightStrategy can only be Random or Repeat.";
                return false;
            }

            Strategy = parsed;
            return true;
        }


        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (args.Length != 0)
            {
                errorMessage = "Usage: rc get groupfightstrategy";
                return false;
            }
            return true;
        }

        public void Set(int playerId, string[] args)
        {
            if (ValidateSet(args, out string errorMessage))
            {
                Logger.Log($"Groupfight Strategy set to {Strategy}", LogLevel.INFO);
                Feedback.Reply(playerId, $"Groupfight Strategy set to {Strategy}");
            }
            else
            {
                Feedback.Reply(playerId, errorMessage);
            }
        }

        public void Get(int playerId, string[] args)
        {
            Logger.Log($"Current Groupfight Strategy: {Strategy}", LogLevel.INFO);
            Feedback.Reply(playerId, $"Current Groupfight Strategy: {Strategy}");
        }
    }
}
