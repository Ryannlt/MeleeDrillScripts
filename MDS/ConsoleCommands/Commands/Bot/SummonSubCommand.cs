using MDS.Core;
using MDS.Events;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot summon [faction class] [ai] [death] [name [regtag [uniform]]] - summons onto the caller.
    public class SummonSubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.Summon;

        public bool Validate(string[] args, out string errorMessage) =>
            BotSpawnArgs.ValidateShape(args, allowCount: false, out errorMessage);

        public void Execute(int playerId, string[] args)
        {
            if (!BotSpawnArgs.TryResolve(args, playerId, allowCount: false, out var parsed, out string error))
            {
                Feedback.Reply(playerId, error);
                return;
            }

            // The caller's body when embodied, else their free-roam viewpoint (the corpse would otherwise be used).
            SummonOrigin.Resolve(playerId,
                placement =>
                {
                    // The caller doubles as the guard target, so 'summon ... Guardian' escorts whoever summoned it.
                    bool success = EventDispatcher.Trigger(EventEnum.SpawnBots,
                        new object[] { parsed.Spec, parsed.Count, parsed.Ai, parsed.Death, placement, playerId },
                        out string eventError);

                    if (!success)
                    {
                        Logger.Log($"Summon failed: {eventError}", LogLevel.WARNING);
                        Feedback.Reply(playerId, eventError);
                        return;
                    }

                    Feedback.Reply(playerId, $"Summoning bot: {FactionTokens.DisplayName(parsed.Spec.Faction)}/{parsed.Spec.Class}, AI {parsed.Ai}, death {parsed.Death}.");
                },
                reason => Feedback.Reply(playerId, reason));
        }
    }
}
