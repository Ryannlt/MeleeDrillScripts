using UnityEngine;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc summonLineAt <playerId> [count] [spec...] - a line summoned onto another player.
    public class SummonLineAtCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.SummonLineAt;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length < 1 || !int.TryParse(parameters[0], out _))
            {
                errorMessage = "Usage: rc summonLineAt <playerId> [count] [faction class] [ai] [death] [name [regtag [uniformId]]]";
                return false;
            }

            return LineArgs.ValidateTail(parameters[1..], out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            int targetPlayerId = int.Parse(parameters[0]);
            string[] rest = parameters[1..];

            // Resolve the placement first: it reports a missing or unspawned target in its own words, and it
            // guarantees the target is embodied, so the faction and class we copy below are actually there.
            SummonOrigin.ResolveAtPlayer(targetPlayerId,
                placement =>
                {
                    if (!LineArgs.ResolveTail(rest, targetPlayerId, out int count, out var spec, out string error))
                    {
                        Feedback.Reply(playerId, error);
                        return;
                    }

                    // The target doubles as the guard target, so a line of Guardians escorts that player.
                    Vector2 center = new Vector2(placement.Position.x, placement.Position.z);
                    LineArgs.Trigger(playerId, center, placement.Heading ?? 0f, count, spec, targetPlayerId);
                },
                reason => Feedback.Reply(playerId, reason));
        }
    }
}
