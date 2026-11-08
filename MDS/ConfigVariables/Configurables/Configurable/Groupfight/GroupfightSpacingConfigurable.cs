using MDS.Core;

namespace MDS.ConfigVariables
{
    public class GroupfightSpacingConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.GroupfightSpacing;

        public float GroupfightSpacing { get; set; } = 2f;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (args.Length != 1 || !float.TryParse(args[0], out float value) || value <= 0)
            {
                errorMessage = "Invalid groupfight spacing. Must be a positive number. Usage: rc set groupfightspacing <value>";
                return false;
            }
            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (args.Length != 0)
            {
                errorMessage = "GroupfightSpacing does not take any arguments for 'get'. Usage: rc get groupfightspacing";
                return false;
            }
            return true;
        }

        public void Set(int playerId, string[] args)
        {
            float value = float.Parse(args[0]);
            GroupfightSpacing = value;

            string message = $"Groupfight Spacing set to {value:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Current Groupfight Spacing is {GroupfightSpacing:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }
    }
}