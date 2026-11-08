using System.Collections.Generic;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class PlayersConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.Players;

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length == 0) return true;

            string group = args[0].ToLower();
            if (group is not ("all" or "attacking" or "defending" or "spectator"))
            {
                errorMessage = $"Unknown group '{args[0]}'. Valid options are: all, attacking, defending, spectator.";
                return false;
            }

            if (args.Length == 2 && args[1].ToLower() != "count")
            {
                errorMessage = $"Unknown option '{args[1]}'. Only 'count' is supported as a second argument.";
                return false;
            }

            if (args.Length > 2)
            {
                errorMessage = "Too many arguments. Usage: rc get players [all|attacking|defending|spectator] [count]";
                return false;
            }

            return true;
        }

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = "Set operation is not supported for 'players'.";
            return false;
        }

        public void Set(int playerId, string[] args)
        {
            Logger.Log("Set operation is not supported for 'players'.", LogLevel.WARNING);
            Feedback.Reply(playerId, "Set operation not supported for 'players'.");
        }

        public void Get(int playerId, string[] args)
        {
            string group = args.Length > 0 ? args[0].ToLower() : "all";
            bool countOnly = args.Length > 1 && args[1].ToLower() == "count";

            IReadOnlyList<IPlayer> list = group switch
            {
                "attacking" => StateTracker.AttackingPlayers,
                "defending" => StateTracker.DefendingPlayers,
                "spectator" => StateTracker.SpectatorPlayers,
                _ => StateTracker.AllPlayers
            };

            if (countOnly)
            {
                string countMsg = $"[{group}] Player Count: {list.Count}";
                Feedback.Reply(playerId, countMsg);
                return;
            }

            foreach (var p in list)
            {
                Logger.Log(p.ToString());
                Feedback.LogOnClient(playerId, p.ToString());
            }

            Feedback.Reply(playerId, $"Player info for [{group}] sent to console. Press (F2) to view.");
        }
    }
}
