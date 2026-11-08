using MDS.Core;
using MDS.Events;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot spawn [count] [faction class] [ai] [death] [name [regtag [uniformId]]]
    // Uses carbonPlayers spawnSpecific. faction/class default to the caller; ai/death to config defaults.
    public class SpawnSubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.Spawn;

        public bool Validate(string[] args, out string errorMessage) =>
            BotSpawnArgs.ValidateShape(args, allowCount: true, out errorMessage);

        public void Execute(int playerId, string[] args)
        {
            if (!BotSpawnArgs.TryResolve(args, playerId, allowCount: true, out var parsed, out string error))
            {
                Feedback.Reply(playerId, error);
                return;
            }

            bool success = EventDispatcher.Trigger(EventEnum.SpawnBots,
                new object[] { parsed.Spec, parsed.Count, parsed.Ai, parsed.Death, null },
                out string eventError);

            if (!success)
            {
                Logger.Log($"SpawnBots failed: {eventError}", LogLevel.WARNING);
                Feedback.Reply(playerId, eventError);
                return;
            }

            Feedback.Reply(playerId, $"Spawning {parsed.Count} bot(s): {FactionTokens.DisplayName(parsed.Spec.Faction)}/{parsed.Spec.Class}, AI {parsed.Ai}, death {parsed.Death}.");
        }
    }
}
