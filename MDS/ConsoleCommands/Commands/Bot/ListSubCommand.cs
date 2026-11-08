using MDS.Core;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot list
    public class ListSubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.List;

        public bool Validate(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            var bots = BotManager.Bots;

            if (bots.Count == 0)
            {
                Feedback.Reply(playerId, "No bots are currently tracked.");
                return;
            }

            Feedback.Reply(playerId, $"Tracked bots ({bots.Count}):");
            foreach (var bot in bots)
            {
                string spec = bot.Spec == null ? "random" : $"{FactionTokens.DisplayName(bot.Spec.Faction)}/{bot.Spec.Class}";
                string line = $"  id {bot.PlayerId} | {spec} | AI {bot.AiType} | death {bot.DeathPolicy} | {(bot.Initialized ? "spawned" : "pending")}";
                Feedback.Reply(playerId, line);
            }
        }
    }
}
