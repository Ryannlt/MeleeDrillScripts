using System.Collections.Generic;
using MDS.Features.Drills;
using MDS.Features.Drills.Picking;
using MDS.Shared.Players;

namespace MDS.Features.Programs
{
    // One step running: a slot per drill line and arena, each holding at most one fight at a time.
    // Rolling deals an idle slot as soon as it is free; together waits for the whole round to end.
    // Once the step's minutes or fights are reached no new fight starts, and the step is done when its last fight ends.
    public sealed class StepRun
    {
        // How often idle slots try again when nothing has changed, such as while too few players are free.
        private const float RetrySeconds = 1f;
        private const float SecondsPerMinute = 60f;

        private static readonly Dictionary<ArenaStrategy, IArenaStrategy> Strategies = new()
        {
            [ArenaStrategy.Fill] = new FillArenas(),
            [ArenaStrategy.Balance] = new BalanceArenas(),
        };

        private readonly ProgramKit _kit;
        private readonly ProgramStep _step;
        private readonly BusyPlayers _busy;
        private readonly List<Slot> _slots = new();
        private float _startedAt;
        private float _nextDealAt;
        private int _started;
        private int _limit;
        private string _lastRefusal = string.Empty;

        public StepRun(ProgramKit kit, ProgramStep step, BusyPlayers busy)
        {
            _kit = kit;
            _step = step;
            _busy = busy;
            foreach (ProgramDrill drill in step.Drills) _slots.AddRange(drill.Arenas.ConvertAll(arena => new Slot(drill, arena)));
            _limit = step.Fights > 0 ? step.Fights : int.MaxValue;
        }

        public bool IsDone { get; private set; }

        public int Running => _slots.FindAll(slot => slot.Fight != null).Count;

        public string Begin(float now)
        {
            _startedAt = now;
            DealIdle(now);
            return Summary();
        }

        // Without minutes or fights, the step is the one round its first deal that starts anything starts.
        private bool IsOneRound => _step.Fights <= 0 && _step.Minutes <= 0f;

        public void Update(float now)
        {
            bool ended = EndFights(now);
            if (IsOver(now))
            {
                IsDone = Running == 0;
                return;
            }

            bool mayDeal = _step.Mode == StepMode.Rolling || Running == 0;
            if (mayDeal && (ended || now >= _nextDealAt)) DealIdle(now);
        }

        // Stopping leaves the fights as they are and only frees their players for the program.
        public void Release()
        {
            foreach (Slot slot in _slots) Free(slot);
        }

        private bool IsOver(float now) => _started >= _limit || (_step.Minutes > 0f && now - _startedAt >= _step.Minutes * SecondsPerMinute);

        private void CloseRound()
        {
            if (IsOneRound && _started > 0 && _limit == int.MaxValue) _limit = _started;
        }

        private string Summary()
        {
            List<string> arenas = _slots.FindAll(slot => slot.Fight != null).ConvertAll(slot => slot.Arena.ToString());
            Lineup free = _kit.Drills.Kit.Field(new PickScope(0, _busy));
            string waiting = $"{free.Attackers.Count + free.Defenders.Count} player(s) wait.";
            if (arenas.Count == 0) return $"no fights could start yet: {_lastRefusal} {waiting}".TrimEnd();
            return $"{arenas.Count} fight(s) in arena(s) {string.Join(", ", arenas)}; {waiting}";
        }

        private void DealIdle(float now)
        {
            _nextDealAt = now + RetrySeconds;
            foreach (Slot slot in _slots)
            {
                if (slot.Fight == null && _started < _limit) TryDeal(slot);
            }
            CloseRound();
        }

        private void TryDeal(Slot slot)
        {
            DrillOptions options = slot.Drill.OptionsFor(slot.Arena, _step.Pick);
            options.Busy = _busy;
            Orient(slot.Drill.Drill, options);
            DrillRun run = Started(slot.Drill.Drill, options);
            if (run == null) return;

            slot.Fight = _kit.Watcher.Watch(run, _step.FightSeconds ?? _kit.Settings.FightSeconds.Value);
            ClaimWhenPlaced(run);
            _started++;
        }

        // Null with the reason kept for the step's summary; notes the drill sends after it starts are only logged.
        private DrillRun Started(IDrill drill, DrillOptions options)
        {
            string refusal = string.Empty;
            DrillRun run = drill.Validate(options, out refusal) ? _kit.Drills.Runner.Start(drill, options, message => refusal = message) : null;
            if (run == null) _lastRefusal = refusal;
            return run;
        }

        // The arena strategy turns the drill line's own orientation, mirror written or not.
        private void Orient(IDrill drill, DrillOptions options)
        {
            Headcount needs = options.IsMirrored ? drill.Needs(options).Mirrored : drill.Needs(options);
            bool turn = Strategies[_step.Arenas].ShouldMirror(needs, _kit.Drills.Kit.Field(options.Scope));
            if (turn && options.IsMirrored) options.Unset(DrillOption.Mirror);
            else if (turn) options.Set(DrillOption.Mirror, "mirror");
        }

        // Drills that take everyone pick nobody through the pool, so their players count as busy once placed.
        private void ClaimWhenPlaced(DrillRun run)
        {
            if (run.Lineup != null) _busy.Claim(run.Lineup.Everyone());
            else run.PlayersPlaced += placed => _busy.Claim(placed.Lineup.Everyone());
        }

        private bool EndFights(float now)
        {
            bool ended = false;
            foreach (Slot slot in _slots)
            {
                if (slot.Fight == null || !slot.Fight.IsOver(now)) continue;

                Free(slot);
                ended = true;
            }
            return ended;
        }

        private void Free(Slot slot)
        {
            if (slot.Fight == null) return;

            _kit.Watcher.Forget(slot.Fight);
            if (slot.Fight.IsPlaced) _busy.Release(slot.Fight.Run.Lineup.Everyone());
            slot.Fight = null;
        }

        private sealed class Slot
        {
            public Slot(ProgramDrill drill, int arena)
            {
                Drill = drill;
                Arena = arena;
            }

            public ProgramDrill Drill { get; }

            public int Arena { get; }

            public Fight Fight { get; set; }
        }
    }
}
