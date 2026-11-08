using MDS.Core;
using MDS.Events;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot remove <playerId|all|attacking|defending|faction>
    public class RemoveSubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.Remove;

        public bool Validate(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length < 1 || !BotTargetSelector.IsValidToken(args[0]))
            {
                errorMessage = "Usage: rc bot remove <playerId|all|attacking|defending|faction>";
                return false;
            }

            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            string target = args[0];

            bool success = EventDispatcher.Trigger(EventEnum.RemoveBots, new object[] { target }, out string error);

            if (!success)
            {
                Logger.Log($"RemoveBots failed: {error}", LogLevel.WARNING);
                Feedback.Reply(playerId, error);
                return;
            }

            Feedback.Reply(playerId, $"Removed bots matching '{target}'.");
        }
    }
}
