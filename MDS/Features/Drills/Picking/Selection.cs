using System;
using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    public enum PickStrategy
    {
        Random,
        Any,
        Next,
        Repeat
    }

    // Each drill has its own, so an xvx repeat never replays a groupfight.
    public sealed class Selection
    {
        private readonly Dictionary<PickStrategy, ISelectionStrategy> _strategies = new();
        private readonly LineupMemory _memory = new();
        private readonly NextPick _next;
        private readonly LineSlots _slots;

        private Selection(PlayerRegistry players, Random random)
        {
            _next = new NextPick(players, random);
            _slots = new LineSlots(random);
            _strategies[PickStrategy.Random] = new RandomPick(players, random, _slots);
        }

        public static Selection ForXvX(PlayerRegistry players, Random random)
        {
            var selection = new Selection(players, random);
            selection._strategies[PickStrategy.Any] = new AnyPick(players, random, selection._slots);
            selection._strategies[PickStrategy.Next] = selection._next;
            selection._strategies[PickStrategy.Repeat] = new RepeatPick(selection._memory, players);
            selection.Default = PickStrategy.Next;
            return selection;
        }

        // A groupfight takes everyone, so there is nothing for Any or Next to choose.
        public static Selection ForGroupfight(PlayerRegistry players, Random random)
        {
            var selection = new Selection(players, random);
            selection._strategies[PickStrategy.Repeat] = new RepeatPick(selection._memory, players);
            return selection;
        }

        public IEnumerable<PickStrategy> Allowed => _strategies.Keys;

        // What the drill's strategy setting starts at.
        public PickStrategy Default { get; private set; } = PickStrategy.Random;

        public bool Allows(PickStrategy strategy) => _strategies.ContainsKey(strategy);

        public PickResult Pick(PickStrategy strategy, Headcount count, PickScope scope)
        {
            if (!_strategies.TryGetValue(strategy, out ISelectionStrategy picker)) return PickResult.Failed($"This drill can't pick by {strategy}.");

            PickResult result = picker.Pick(count, scope);
            if (!result.Succeeded) return result;

            _memory.Remember(scope.Arena, result.Lineup);
            scope.Claim(result.Lineup);
            return result;
        }

        // A new round brings new teams, so there is nothing to repeat or continue from.
        public void Reset()
        {
            _memory.Clear();
            _next.Reset();
            _slots.Clear();
        }
    }
}
