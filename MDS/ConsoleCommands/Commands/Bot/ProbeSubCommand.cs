using System.Collections.Generic;
using System.Linq;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot probe <target> [on|off] - per-tick melee traces for the named bots.
    public class ProbeSubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.Probe;

        private const string Usage = "Usage: rc bot probe <playerId|me|all|attacking|defending|faction> [on|off]";

        public bool Validate(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length < 1)
            {
                errorMessage = Usage;
                return false;
            }

            if (!IsMe(args[0]) && !BotTargetSelector.IsValidToken(args[0]))
            {
                errorMessage = $"Invalid target '{args[0]}'. Use a playerId, me, all, attacking, defending, or a faction name.";
                return false;
            }

            if (args.Length > 1 &&
                !args[1].Equals("on", System.StringComparison.OrdinalIgnoreCase) &&
                !args[1].Equals("off", System.StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Second argument must be 'on' or 'off'.";
                return false;
            }

            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            List<int> targets = ResolveTargets(playerId, args[0]);

            if (targets.Count == 0)
            {
                Feedback.Reply(playerId, $"No players matched '{args[0]}'.");
                return;
            }

            // Toggling a set of targets flips them together rather than individually, so a group half of which is
            // already being probed ends up all on or all off instead of inverted into a mix.
            bool on = args.Length > 1
                ? args[1].Equals("on", System.StringComparison.OrdinalIgnoreCase)
                : !targets.Any(MeleeProbe.IsProbing);

            foreach (int id in targets)
                MeleeProbe.Set(id, on);

            string state = on ? "Probing" : "Stopped probing";
            string who = targets.Count == 1 ? $"player {targets[0]}" : $"{targets.Count} players matching '{args[0]}'";
            Feedback.Reply(playerId, $"{state} {who}. Watch the server log while they attack/block.");
        }

        // A raw id is taken as given rather than run through BotTargetSelector, which only returns tracked bots.
        // Probing a human is the common case for this tool, and the caller is one.
        private static List<int> ResolveTargets(int callerId, string token)
        {
            if (IsMe(token)) return new List<int> { callerId };
            if (int.TryParse(token, out int id)) return new List<int> { id };

            return BotTargetSelector.Resolve(token);
        }

        private static bool IsMe(string token) => token.Equals("me", System.StringComparison.OrdinalIgnoreCase);
    }
}
