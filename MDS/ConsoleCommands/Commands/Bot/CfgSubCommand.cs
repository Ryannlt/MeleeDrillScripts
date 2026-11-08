using System.Text;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot cfg <target> [<lever> <value>] - reads or sets per-bot AI levers.
    public class CfgSubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.Cfg;

        public bool Validate(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length < 1)
            {
                errorMessage = "Usage: rc bot cfg <target> [<lever> <value>]";
                return false;
            }

            if (!BotTargetSelector.IsValidToken(args[0]))
            {
                errorMessage = $"Invalid target '{args[0]}'. Use a playerId, all, attacking, defending, or a faction name.";
                return false;
            }

            if (args.Length == 2)
            {
                errorMessage = "Provide a value: rc bot cfg <target> <lever> <value> (or just <target> to list).";
                return false;
            }

            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            if (args.Length >= 3) SetLever(playerId, args[0], args[1], args[2]);
            else ListLevers(playerId, args[0]);
        }

        private static void SetLever(int playerId, string target, string lever, string value)
        {
            int applied = 0, skipped = 0;
            string lastError = null;
            string advisory = null;   // e.g. the lever this one depends on is off, so it will sit dormant

            foreach (int id in BotTargetSelector.Resolve(target))
            {
                if (FindController(id)?.Ai is IConfigurableAi cfg)
                {
                    if (cfg.TrySet(lever, value, out string message))
                    {
                        applied++;
                        if (!string.IsNullOrEmpty(message)) advisory = message;
                    }
                    else { skipped++; lastError = message; }
                }
                else skipped++;
            }

            string msg = applied > 0
                ? $"Set '{lever}' = '{value}' on {applied} bot(s)." + (skipped > 0 ? $" ({skipped} skipped.)" : "")
                  + (advisory != null ? $" Note: {advisory}" : "")
                : lastError ?? $"No configurable bot matched '{target}'.";
            Feedback.Reply(playerId, msg);
        }

        private static void ListLevers(int playerId, string target)
        {
            foreach (int id in BotTargetSelector.Resolve(target))
            {
                BotController controller = FindController(id);
                if (controller?.Ai is IConfigurableAi cfg)
                {
                    // Live levers first, then the dormant ones with what is holding them back. Listing 30-odd
                    // levers flat made it impossible to see which of them this preset actually uses.
                    var sb = new StringBuilder($"Bot {id} ({controller.AiType}):");
                    var dormant = new StringBuilder();

                    foreach (var (name, val, inactive) in cfg.ListParams())
                    {
                        if (inactive == null) sb.Append($" {name}={val}");
                        else dormant.Append($" {name}={val}(needs {inactive})");
                    }

                    if (dormant.Length > 0) sb.Append($" | inactive:{dormant}");
                    Feedback.Reply(playerId, sb.ToString());
                }
                else
                {
                    Feedback.Reply(playerId, $"Bot {id} ({controller?.AiType}) has no configurable levers.");
                }
            }
        }

        private static BotController FindController(int id)
        {
            foreach (var c in BotManager.Bots)
                if (c.PlayerId == id) return c;
            return null;
        }
    }
}
