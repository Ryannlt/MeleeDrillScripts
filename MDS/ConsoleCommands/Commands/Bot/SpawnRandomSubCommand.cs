using MDS.ConfigVariables;
using MDS.Core;
using MDS.Events;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot spawnrandom [count] - fully random faction/class (carbonPlayers spawn). AI/death from config.
    public class SpawnRandomSubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.SpawnRandom;

        public bool Validate(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length >= 1 && (!int.TryParse(args[0], out int count) || count <= 0))
            {
                errorMessage = $"Invalid count '{args[0]}'. Must be a positive integer. Usage: rc bot spawnrandom [count]";
                return false;
            }

            return true;
        }

        public void Execute(int playerId, string[] args)
        {
            int count = 1;
            if (args.Length >= 1) int.TryParse(args[0], out count);

            BotAiEnum ai = ((BotDefaultAiConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotDefaultAi)).DefaultAi;
            BotDeathPolicy death = ((BotDefaultDeathConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotDefaultDeathPolicy)).DefaultPolicy;

            bool success = EventDispatcher.Trigger(EventEnum.SpawnBots,
                new object[] { null, count, ai, death, null },
                out string eventError);

            if (!success)
            {
                Logger.Log($"SpawnRandom failed: {eventError}", LogLevel.WARNING);
                Feedback.Reply(playerId, eventError);
                return;
            }

            Feedback.Reply(playerId, $"Spawning {count} random bot(s), AI {ai}, death {death}.");
        }
    }
}
