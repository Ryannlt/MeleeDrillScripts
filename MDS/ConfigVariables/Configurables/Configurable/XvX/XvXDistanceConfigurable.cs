using MDS.Core;

namespace MDS.ConfigVariables
{
    public class XvXDistanceConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.XvXDistance;

        public float XvXDistance { get; set; } = 20f;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !float.TryParse(args[0], out float value) || value <= 0)
            {
                errorMessage = "Invalid distance. Must be a number greater than 0. Usage: rc set XvXDistance <value>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length > 0)
            {
                errorMessage = "XvX distance does not take any arguments for 'get'. Usage: rc get XvXDistance";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            XvXDistance = float.Parse(args[0]);

            string message = $"XvX distance set to {XvXDistance:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"XvX distance is {XvXDistance:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public float GetValue() => XvXDistance;
    }
}
