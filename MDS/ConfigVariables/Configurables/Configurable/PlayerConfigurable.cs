using MDS.Core;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class PlayerConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.Player;

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1)
            {
                errorMessage = "Missing or too many arguments. Usage: rc get player <playerId>";
                return false;
            }

            if (!int.TryParse(args[0], out int playerId))
            {
                errorMessage = $"'{args[0]}' is not a valid player ID.";
                return false;
            }

            var player = StateTracker.GetPlayerById(playerId);
            if (player == null)
            {
                errorMessage = $"No player found with ID {playerId}.";
                return false;
            }

            return true;
        }

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = "Setting player data is not supported.";
            return false;
        }

        public void Set(int playerId, string[] args)
        {
            Logger.Log("Setting player data is not supported.", LogLevel.WARNING);
            Feedback.Reply(playerId, "Setting player data is not supported.");
        }

        public void Get(int playerId, string[] args)
        {
            int targetId = int.Parse(args[0]);
            var player = StateTracker.GetPlayerById(targetId);

            string info = player.ToString();
            Logger.Log(info, LogLevel.INFO);
            Feedback.Reply(playerId, info);
        }
    }
}
