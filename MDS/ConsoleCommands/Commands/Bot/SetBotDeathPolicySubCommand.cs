using System;
using MDS.Core;
using MDS.Events;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot setBotDeathPolicy <playerId|all|attacking|defending|faction> <policy>
    public class SetBotDeathPolicySubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.SetBotDeathPolicy;

        public bool Validate(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length < 2)
            {
                errorMessage = $"Usage: rc bot setBotDeathPolicy <playerId|all|attacking|defending|faction> <policy>. Policies: {string.Join(", ", Enum.GetNames(typeof(BotDeathPolicy)))}.";
                return false;
            }

            if (!BotTargetSelector.IsValidToken(args[0]))
            {
                errorMessage = $"Invalid target '{args[0]}'. Use a playerId, all, attacking, defending, or a faction name.";
                return false;
            }

            if (!EnumParser.TryParseEnumStrict(args[1], out BotDeathPolicy _))
            {
                errorMessage = $"Unknown policy '{args[1]}'. Valid: {string.Join(", ", Enum.GetNames(typeof(BotDeathPolicy)))}.";
                return false;
            }

            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            string target = args[0];
            EnumParser.TryParseEnumStrict(args[1], out BotDeathPolicy policy);

            bool success = EventDispatcher.Trigger(EventEnum.SetBotDeathPolicy, new object[] { target, policy }, out string error);

            if (!success)
            {
                Logger.Log($"SetBotDeathPolicy failed: {error}", LogLevel.WARNING);
                Feedback.Reply(playerId, error);
                return;
            }

            Feedback.Reply(playerId, $"Set death policy '{policy}' on bots matching '{target}'.");
        }
    }
}
