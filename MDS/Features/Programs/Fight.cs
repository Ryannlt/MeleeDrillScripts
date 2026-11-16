using System;
using System.Collections.Generic;
using MDS.Features.Drills;
using MDS.Features.Drills.Picking;
using MDS.Shared.Game;
using MDS.Shared.Players;

namespace MDS.Features.Programs
{
    // One drill run a program waits on: over when one side has all fallen since it was placed, or when its time is up.
    // A player who died stays fallen for this fight even after respawning.
    public sealed class Fight
    {
        private readonly HashSet<IPlayer> _fallen = new();
        private readonly float _seconds;
        private float _placedAt;

        public Fight(DrillRun run, float seconds)
        {
            Run = run;
            _seconds = seconds;
        }

        public DrillRun Run { get; }

        // A countdown puts the placing off, and the fight's clock starts then.
        public bool IsPlaced => Run.Lineup != null;

        public void Placed(float now) => _placedAt = now;

        public void Fall(IPlayer player)
        {
            if (IsPlaced && Run.Lineup.Includes(player)) _fallen.Add(player);
        }

        public bool IsOver(float now) => IsPlaced && (AllFallen(Run.Lineup.Attackers) || AllFallen(Run.Lineup.Defenders) || now - _placedAt >= _seconds);

        private bool AllFallen(IReadOnlyList<IPlayer> side) => side.Count > 0 && new List<IPlayer>(side).TrueForAll(_fallen.Contains);
    }

    // Tells every fight a program waits on when one of its players dies or leaves.
    public sealed class FightWatcher : IDisposable
    {
        private readonly PlayerRegistry _players;
        private readonly IClock _clock;
        private readonly List<Fight> _fights = new();

        public FightWatcher(PlayerRegistry players, IClock clock)
        {
            _players = players;
            _clock = clock;
            _players.PlayerDied += OnFallen;
            _players.PlayerLeft += OnFallen;
        }

        public Fight Watch(DrillRun run, float seconds)
        {
            var fight = new Fight(run, seconds);
            if (fight.IsPlaced) fight.Placed(_clock.GameSeconds);
            else run.PlayersPlaced += placed => fight.Placed(_clock.GameSeconds);
            _fights.Add(fight);
            return fight;
        }

        public void Forget(Fight fight) => _fights.Remove(fight);

        public void Dispose()
        {
            _players.PlayerDied -= OnFallen;
            _players.PlayerLeft -= OnFallen;
        }

        private void OnFallen(IPlayer player)
        {
            foreach (Fight fight in _fights) fight.Fall(player);
        }
    }
}
