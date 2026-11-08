using MDS.Core;

namespace MDS.ConfigVariables
{
    public class XvXSpacingConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.XvXSpacing;

        public float XvXSpacing { get; set; } = 2f;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !float.TryParse(args[0], out float value) || value <= 0)
            {
                errorMessage = "Invalid spacing. Must be a number greater than 0. Usage: rc set XvXSpacing <value>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length > 0)
            {
                errorMessage = "XvXSpacing does not take any arguments for 'get'. Usage: rc get XvXSpacing";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            XvXSpacing = float.Parse(args[0]);

            string message = $"XvX spacing set to {XvXSpacing:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"XvX spacing is {XvXSpacing:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public float GetValue() => XvXSpacing;
    }
}
