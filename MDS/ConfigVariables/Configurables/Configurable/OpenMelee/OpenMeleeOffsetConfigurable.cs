using MDS.Core;

namespace MDS.ConfigVariables
{
    public class OpenMeleeOffsetConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.OpenMeleeOffset;

        public float OpenMeleeOffset { get; set; } = 2f;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !float.TryParse(args[0], out float value) || value < 0)
            {
                errorMessage = "Invalid offset. Must be a number greater than or equal to 0. Usage: rc set OpenMeleeOffset <value>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 0)
            {
                errorMessage = "Usage: rc get OpenMeleeOffset";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            OpenMeleeOffset = float.Parse(args[0]);

            string message = $"Open Melee Offset set to {OpenMeleeOffset:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Current Open Melee Offset is {OpenMeleeOffset:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public float GetValue() => OpenMeleeOffset;
    }
}
