using MDS.Core;

namespace MDS.ConfigVariables
{
    public class DebugLoggingConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.DebugLogging;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1)
            {
                errorMessage = "Usage: rc set DebugLogging <true|false>";
                return false;
            }

            string value = args[0].ToLower();
            if (value != "true" && value != "false")
            {
                errorMessage = $"Invalid value '{args[0]}'. Expected 'true' or 'false'.";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 0)
            {
                errorMessage = "Usage: rc get DebugLogging";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            bool enabled = args[0].ToLower() == "true";
            Logger.SetEnableDebugLogging(enabled);

            string message = $"EnableDebugLogging set to {enabled}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            bool enabled = Logger.EnableDebugLogging;
            string message = $"EnableDebugLogging is currently set to {enabled}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }
    }
}
