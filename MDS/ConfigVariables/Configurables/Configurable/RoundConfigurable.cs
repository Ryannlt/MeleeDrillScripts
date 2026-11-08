using MDS.Core;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class RoundConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.Round;

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            if (args.Length > 0)
            {
                errorMessage = "Round does not accept any arguments. Usage: rc get round";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = "Round data cannot be modified at runtime.";
            return false;
        }

        public void Set(int playerId, string[] args)
        {
            string msg = "Round data cannot be modified at runtime.";
            Logger.Log(msg, LogLevel.WARNING);
            Feedback.Reply(playerId, msg);
        }

        public void Get(int playerId, string[] args)
        {
            string[] lines =
            {
                "[Round Info]",
                $"  Round ID: {StateTracker.RoundId}",
                $"  Server: {StateTracker.ServerName}",
                $"  Map: {StateTracker.MapName}",
                $"  Mode: {StateTracker.GameMode}",
                $"  GameType: {StateTracker.GameType}",
                $"  Attacking: {StateTracker.AttackingFaction}",
                $"  Defending: {StateTracker.DefendingFaction}"
            };

            foreach (var line in lines)
            {
                Logger.Log(line);
                Feedback.LogOnClient(playerId, line);
            }

            Feedback.Reply(playerId, "Round info sent to your console. Press (F2) to view.");
        }
    }
}
