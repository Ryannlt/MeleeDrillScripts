using MDS.Core;

namespace MDS.ConfigVariables
{
    // Seconds after a bot dies before it is kicked (applies to Kick and Replace policies). The delay
    // lets the killer be credited before the bot is removed. rc get/set botKickDelay.
    public class BotKickDelayConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.BotKickDelay;

        public float KickDelay { get; set; } = 2f;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !float.TryParse(args[0], out float value) || value < 0f)
            {
                errorMessage = "Invalid delay. Must be a number >= 0. Usage: rc set botKickDelay <seconds>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length > 0)
            {
                errorMessage = "botKickDelay takes no arguments for 'get'. Usage: rc get botKickDelay";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            KickDelay = float.Parse(args[0]);

            string message = $"Bot kick delay set to {KickDelay:F2}s.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Bot kick delay is {KickDelay:F2}s.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }
    }
}
