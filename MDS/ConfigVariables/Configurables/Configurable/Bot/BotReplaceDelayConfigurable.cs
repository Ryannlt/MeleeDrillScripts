using MDS.Core;

namespace MDS.ConfigVariables
{
    // Seconds after a bot is kicked before its replacement spawns (Replace policy only). The gap lets
    // the kick free the bot slot before respawning. rc get/set botReplaceDelay.
    public class BotReplaceDelayConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.BotReplaceDelay;

        public float ReplaceDelay { get; set; } = 0.5f;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !float.TryParse(args[0], out float value) || value < 0f)
            {
                errorMessage = "Invalid delay. Must be a number >= 0. Usage: rc set botReplaceDelay <seconds>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length > 0)
            {
                errorMessage = "botReplaceDelay takes no arguments for 'get'. Usage: rc get botReplaceDelay";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            ReplaceDelay = float.Parse(args[0]);

            string message = $"Bot replace delay set to {ReplaceDelay:F2}s.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Bot replace delay is {ReplaceDelay:F2}s.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }
    }
}
