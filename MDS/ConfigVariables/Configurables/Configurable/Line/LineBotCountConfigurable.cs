using MDS.Core;

namespace MDS.ConfigVariables
{
    // Default number of bots in a summonLine / spawnLine (overridable inline). rc get/set lineBotCount.
    public class LineBotCountConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.LineBotCount;

        public int LineBotCount { get; set; } = 10;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !int.TryParse(args[0], out int value) || value <= 0)
            {
                errorMessage = "Invalid count. Must be a positive integer. Usage: rc set lineBotCount <count>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length > 0)
            {
                errorMessage = "lineBotCount takes no arguments for 'get'. Usage: rc get lineBotCount";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            LineBotCount = int.Parse(args[0]);

            string message = $"Line bot count set to {LineBotCount}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Line bot count is {LineBotCount}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }
    }
}
