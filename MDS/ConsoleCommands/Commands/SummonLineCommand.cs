using UnityEngine;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc summonLine [count] [spec...] - a line summoned onto the caller.
    public class SummonLineCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.SummonLine;

        public bool Validate(string[] parameters, out string errorMessage) =>
            LineArgs.ValidateTail(parameters, out errorMessage);

        public void Execute(int playerId, string[] parameters)
        {
            if (!LineArgs.ResolveTail(parameters, playerId, out int count, out var spec, out string error))
            {
                Feedback.Reply(playerId, error);
                return;
            }

            // The caller's body when embodied, else their free-roam viewpoint (the corpse would otherwise be used).
            SummonOrigin.Resolve(playerId,
                placement =>
                {
                    // The caller doubles as the guard target, so a line of Guardians escorts whoever summoned it.
                    Vector2 center = new Vector2(placement.Position.x, placement.Position.z);
                    LineArgs.Trigger(playerId, center, placement.Heading ?? 0f, count, spec, playerId);
                },
                reason => Feedback.Reply(playerId, reason));
        }
    }
}
