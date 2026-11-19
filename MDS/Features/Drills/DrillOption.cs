using System;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Popcorn;
using MDS.Features.Drills.Spawning;
using MDS.Shared;

namespace MDS.Features.Drills
{
    public enum DrillOption
    {
        Attackers,
        Defenders,
        Strategy,
        Distance,
        Spacing,
        Facing,
        Offset,
        Arena,
        Spawn,
        Min,
        Max,
        LineDistance,
        Layout,
        Name,
        Turn,
        Side,
        Position,
        Mirror,
        Teleport
    }

    public static class DrillOptionValues
    {
        private static readonly Dictionary<DrillOption, Func<string, bool>> Rules = new()
        {
            [DrillOption.Attackers] = IsCount,
            [DrillOption.Defenders] = IsCount,
            [DrillOption.Strategy] = text => EnumParser.TryParseStrict(text, out PickStrategy _),
            [DrillOption.Spawn] = text => EnumParser.TryParseStrict(text, out SpawnStrategy _),
            [DrillOption.Layout] = text => EnumParser.TryParseStrict(text, out FightLayout _),
            [DrillOption.Distance] = IsPositive,
            [DrillOption.Spacing] = IsPositive,
            [DrillOption.LineDistance] = IsPositive,
            [DrillOption.Facing] = text => Facing.TryParse(text, out _),

            // Zero uses the arena edge to edge, as the old offset setting allowed.
            [DrillOption.Offset] = text => Numbers.TryParseZeroOrMore(text, out _),
            [DrillOption.Arena] = text => ArenaParser.TryParseNumber(text, out _),
            [DrillOption.Min] = IsFightSize,
            [DrillOption.Max] = IsFightSize,

            // Any word but a pick word or mirror, which fill their own slots, so the name and those can come in any order.
            [DrillOption.Name] = text => !EnumParser.TryParseStrict(text, out PickStrategy _) && !IsMirror(text) && !IsTeleport(text),
            [DrillOption.Mirror] = IsMirror,
            [DrillOption.Teleport] = IsTeleport,
            [DrillOption.Turn] = text => Facing.TryParse(text, out _),
            [DrillOption.Side] = text => EnumParser.TryParseStrict(text, out TeamSide _),

            // Brackets are required, since the lines that hold a position are split on commas.
            [DrillOption.Position] = text => text.StartsWith("(") && text.EndsWith(")") && ArenaParser.TryParseCorner(text, out _),
        };

        public static bool Accepts(DrillOption option, string text) => Rules[option](text);

        // A bare number after a custom drill's name would otherwise turn the drill instead of picking the arena.
        public static bool TakesBare(DrillOption option) => option != DrillOption.Turn;

        private static bool IsMirror(string text) => text.Equals("mirror", StringComparison.OrdinalIgnoreCase);

        private static bool IsTeleport(string text) => EnumParser.TryParseStrict(text, out TeleportTime _);

        private static bool IsCount(string text) => int.TryParse(text, out int count) && count > 0;

        private static bool IsPositive(string text) => Numbers.TryParsePositive(text, out _);

        private static bool IsFightSize(string text) => FightSize.TryParse(text, out _);

        // A drill takes one kind of strategy (picks, spawns or fight layout), never two, so they share the name.
        public static string KeyOf(DrillOption option) => option == DrillOption.Spawn || option == DrillOption.Layout ? KeyOf(DrillOption.Strategy) : option.ToString().ToLowerInvariant();
    }
}
