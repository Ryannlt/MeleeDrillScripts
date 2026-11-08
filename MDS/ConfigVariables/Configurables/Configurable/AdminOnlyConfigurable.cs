using MDS.Systems;
using MDS.Core;

namespace MDS.ConfigVariables
{
    public class AdminOnlyConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.AdminOnly;

        public static bool IsAdminOnlyEnabled { get; set; } = false; //must be static to be accessed from the command handler

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1)
            {
                errorMessage = "Usage: rc set EnableAdminOnly <true|false>";
                return false;
            }

            if (!bool.TryParse(args[0], out _))
            {
                errorMessage = $"Invalid input '{args[0]}'. Expected 'true' or 'false'.";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 0)
            {
                errorMessage = "Too many arguments. Usage: rc get AdminOnly";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            if (!StateTracker.IsPlayerAdmin(playerId))
            {
                string errorMsg = "Only logged-in admins may modify admin-only settings.";
                Logger.Log($"Unauthorized Set attempt by Player {playerId}.", LogLevel.WARNING);
                Feedback.Reply(playerId, errorMsg);
                return;
            }

            bool value = bool.Parse(args[0]);
            IsAdminOnlyEnabled = value;

            string msg = $"Admin-only command mode {(value ? "enabled" : "disabled")}.";
            Logger.Log(msg, LogLevel.INFO);
            Feedback.Reply(playerId, msg);
        }


        public void Get(int playerId, string[] args)
        {
            string msg = $"Admin-only command mode is {(IsAdminOnlyEnabled ? "enabled" : "disabled")}.";
            Logger.Log(msg, LogLevel.INFO);
            Feedback.Reply(playerId, msg);
        }
    }
}
