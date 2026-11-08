using MDS.Core;

namespace MDS.ConfigVariables
{
    public class OpenMeleeSpacingConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.OpenMeleeSpacing;

        public float OpenMeleeSpacing { get; set; } = 1.5f;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !float.TryParse(args[0], out float value) || value <= 0)
            {
                errorMessage = "Invalid spacing. Must be a number greater than 0. Usage: rc set OpenMeleeSpacing <value>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 0)
            {
                errorMessage = "Usage: rc get OpenMeleeSpacing";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            OpenMeleeSpacing = float.Parse(args[0]);

            string message = $"Open Melee Spacing set to {OpenMeleeSpacing:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Open Melee Spacing is {OpenMeleeSpacing:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public float GetValue() => OpenMeleeSpacing;
    }
}
