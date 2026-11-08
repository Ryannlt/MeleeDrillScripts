using MDS.Core;

namespace MDS.ConfigVariables
{
    // Lateral spacing (metres) between bots in a line - tune so the line is shoulder-to-shoulder.
    // rc get/set lineSpacing.
    public class LineSpacingConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.LineSpacing;

        public float LineSpacing { get; set; } = 0.55f;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !float.TryParse(args[0], out float value) || value <= 0f)
            {
                errorMessage = "Invalid spacing. Must be a number greater than 0. Usage: rc set lineSpacing <metres>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length > 0)
            {
                errorMessage = "lineSpacing takes no arguments for 'get'. Usage: rc get lineSpacing";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            LineSpacing = float.Parse(args[0]);

            string message = $"Line spacing set to {LineSpacing:F2}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Line spacing is {LineSpacing:F2}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }
    }
}
