using MDS.Core;

namespace MDS.ConfigVariables
{
    public class GroupfightDistanceConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.GroupfightDistance;

        public float GroupfightDistance { get; set; } = 25f;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (args.Length != 1 || !float.TryParse(args[0], out float value) || value <= 0)
            {
                errorMessage = "Invalid groupfight distance. Must be a positive number. Usage: rc set groupfightdistance <value>";
                return false;
            }
            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (args.Length != 0)
            {
                errorMessage = "GroupfightDistance does not take any arguments for 'get'. Usage: rc get groupfightdistance";
                return false;
            }
            return true;
        }

        public void Set(int playerId, string[] args)
        {
            float value = float.Parse(args[0]);
            GroupfightDistance = value;

            string message = $"Groupfight Distance set to {value:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Current Groupfight Distance is {GroupfightDistance:F2}";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }
    }
}
