using MDS.Core;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot act <playerId> <actionToken> [argument] - fires one raw player action, for probing.
    public class ActSubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.Act;

        public bool Validate(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length < 2)
            {
                errorMessage = "Usage: rc bot act <playerId> <actionToken> [argument]";
                return false;
            }

            if (!int.TryParse(args[0], out _))
            {
                errorMessage = $"Invalid playerId '{args[0]}'.";
                return false;
            }

            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            int targetId = int.Parse(args[0]);
            string action = args[1];

            if (args.Length > 2)
                CarbonPlayerCommands.PerformAction(targetId, action, args[2]);
            else
                CarbonPlayerCommands.PerformAction(targetId, action);

            Feedback.Reply(playerId, $"Fired playerAction '{action}'{(args.Length > 2 ? $" '{args[2]}'" : "")} at player {targetId}.");
        }
    }
}
