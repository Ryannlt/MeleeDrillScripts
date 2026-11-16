using System;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Popcorn;
using MDS.Features.Drills.Spawning;
using MDS.Shared;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills
{
    // DrillSyntax has already checked every value, so the getters parse without guarding.
    public sealed class DrillOptions
    {
        private readonly Dictionary<DrillOption, string> _values = new();

        // Set by a program for the players already in one of its fights, and never read from text.
        public BusyPlayers Busy { get; set; }

        public PickScope Scope => new PickScope(GetArena(), Busy);

        // The other team takes each side.
        public bool IsMirrored => Has(DrillOption.Mirror);

        public bool Has(DrillOption option) => _values.ContainsKey(option);

        public void Set(DrillOption option, string text) => _values[option] = text;

        public void Unset(DrillOption option) => _values.Remove(option);

        public int GetCount(DrillOption option) => _values.TryGetValue(option, out string text) ? int.Parse(text) : 0;

        public int GetArena() => _values.TryGetValue(DrillOption.Arena, out string text) ? int.Parse(text) : ArenaStore.First;

        public float GetNumber(DrillOption option, float fallback)
        {
            return _values.TryGetValue(option, out string text) && Numbers.TryParse(text, out float value) ? value : fallback;
        }

        public PickStrategy GetStrategy(PickStrategy fallback)
        {
            return _values.TryGetValue(DrillOption.Strategy, out string text) && EnumParser.TryParseStrict(text, out PickStrategy value) ? value : fallback;
        }

        public SpawnStrategy GetSpawnStrategy(SpawnStrategy fallback)
        {
            return _values.TryGetValue(DrillOption.Spawn, out string text) && EnumParser.TryParseStrict(text, out SpawnStrategy value) ? value : fallback;
        }

        public FightLayout GetFightLayout(FightLayout fallback)
        {
            return _values.TryGetValue(DrillOption.Layout, out string text) && EnumParser.TryParseStrict(text, out FightLayout value) ? value : fallback;
        }

        public FightSize GetFightSize(DrillOption option, FightSize fallback)
        {
            return _values.TryGetValue(option, out string text) && FightSize.TryParse(text, out FightSize value) ? value : fallback;
        }

        public string GetName() => _values.TryGetValue(DrillOption.Name, out string text) ? text : string.Empty;

        public TeamSide GetSide() => _values.TryGetValue(DrillOption.Side, out string text) && EnumParser.TryParseStrict(text, out TeamSide side) ? side : TeamSide.Attacker;

        public Vector2 GetPosition() => _values.TryGetValue(DrillOption.Position, out string text) && ArenaParser.TryParseCorner(text, out Vector2 position) ? position : Vector2.zero;

        public Facing GetFacing(Facing fallback) => GetAngle(DrillOption.Facing, fallback);

        public Facing GetTurn(Facing fallback) => GetAngle(DrillOption.Turn, fallback);

        private Facing GetAngle(DrillOption option, Facing fallback)
        {
            return _values.TryGetValue(option, out string text) && Facing.TryParse(text, out Facing value) ? value : fallback;
        }
    }
}
