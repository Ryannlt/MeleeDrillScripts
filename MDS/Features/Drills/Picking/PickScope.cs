using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    // Who a pick may take, and the arena the fight is in.
    // Outside a program anyone on the field may be picked, as rc always has.
    // Inside one, a player in another of its fights is busy, and a player must be alive: a fight's fallen are free when it ends, but only once they respawn.
    public sealed class PickScope
    {
        private readonly BusyPlayers _busy;

        public PickScope(int arena, BusyPlayers busy = null)
        {
            Arena = arena;
            _busy = busy;
        }

        public int Arena { get; }

        public bool InProgram => _busy != null;

        public static PickScope Anyone(int arena) => new PickScope(arena);

        public bool IsFree(IPlayer player) => _busy == null || (!_busy.Contains(player) && player.IsAlive);

        public List<IPlayer> Free(IEnumerable<IPlayer> players) => new List<IPlayer>(players).FindAll(IsFree);

        // So the next arena a program deals in the same tick can't take the same players.
        public void Claim(Lineup lineup) => _busy?.Claim(lineup.Everyone());
    }
}
