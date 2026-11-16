using System;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Spawning;
using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // Reads one comma separated line of a custom drill: static spots, a formation written as its rc options, or one of the drill's settings.
    // A parsed line comes back as what it does to a drill, so the config and rc customdrill add apply it the same way.
    public static class CustomLineParser
    {
        public const string Format = "attacker,(x,z) | defender,(x,z) | <scatter|spread|groups|teams|random>,[spacing],[offset],[facing] | line,<attacker|defender>,(x,z),[spacing],[facing] | strategy,<random|any|next|repeat> | turn,<degrees|random> | countdown,<on|off> | godmode,<on|off> | godModeSeconds,<seconds>";

        private const string LineWord = "line";
        private const int SettingParts = 2;

        // The side and the position.
        private const int LineRequired = 2;

        private static readonly DrillSyntax OpenMelee = new DrillSyntax(
            "An open melee formation reads like rc openmelee: <scatter|spread|groups|teams|random>,[spacing],[offset],[facing].",
            1, DrillOption.Spawn, DrillOption.Spacing, DrillOption.Offset, DrillOption.Facing);

        private static readonly DrillSyntax Line = new DrillSyntax(
            "A line reads: line,<attacker|defender>,(x,z),[spacing],[facing].",
            LineRequired, DrillOption.Side, DrillOption.Position, DrillOption.Spacing, DrillOption.Facing);

        private static readonly Dictionary<string, SettingReader> Settings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["strategy"] = ReadStrategy,
            ["turn"] = ReadTurn,
            ["countdown"] = ReadCountdown,
            ["godmode"] = ReadGodMode,
            ["godModeSeconds"] = ReadGodModeSeconds,
        };

        private delegate string SettingReader(string value, out Action<CustomDrill> apply);

        public static bool TryParse(string text, out Action<CustomDrill> apply, out string error)
        {
            string[] parts = CommaParts.Split(text);
            string head = parts.Length > 0 ? parts[0] : string.Empty;
            apply = null;
            if (EnumParser.TryParseStrict(head, out TeamSide _)) error = ReadSpots(parts, out apply);
            else if (head.Equals(LineWord, StringComparison.OrdinalIgnoreCase)) error = ReadLine(parts, out apply);
            else if (EnumParser.TryParseStrict(head, out SpawnStrategy _)) error = ReadOpenMelee(parts, out apply);
            else if (Settings.TryGetValue(head, out SettingReader read)) error = parts.Length == SettingParts ? read(parts[1], out apply) : $"{head} takes one value, like {head},<value>.";
            else error = $"'{text}' isn't a custom drill line. A line is one of: {Format}";
            return error.Length == 0;
        }

        private static string ReadSpots(string[] parts, out Action<CustomDrill> apply)
        {
            apply = null;
            EnumParser.TryParseStrict(parts[0], out TeamSide side);
            var spots = new List<StaticSpot>();
            for (int i = 1; i < parts.Length; i++)
            {
                if (!DrillOptionValues.Accepts(DrillOption.Position, parts[i]) || !ArenaParser.TryParseCorner(parts[i], out Vector2 offset)) return $"'{parts[i]}' isn't a position like (-5,0).";

                spots.Add(new StaticSpot(side, offset));
            }
            if (spots.Count == 0) return $"{parts[0]} needs a position, like {parts[0]},(-5,0).";

            string written = string.Join(",", parts);
            apply = drill => drill.AddSpots(spots, written);
            return string.Empty;
        }

        private static string ReadOpenMelee(string[] parts, out Action<CustomDrill> apply)
        {
            apply = null;
            if (!OpenMelee.TryParse(parts, out DrillOptions options, out string error)) return error;
            if (options.Has(DrillOption.Arena)) return "A formation runs in the drill's arena, so it takes no arena number.";

            var formation = new OpenMeleeFormation(options, string.Join(",", parts));
            apply = drill => drill.Formation = formation;
            return string.Empty;
        }

        private static string ReadLine(string[] parts, out Action<CustomDrill> apply)
        {
            apply = null;
            if (!Line.TryParse(parts[1..], out DrillOptions options, out string error)) return error;
            if (options.Has(DrillOption.Arena)) return "A line runs in the drill's arena, so it takes no arena number.";
            if (options.GetNumber(DrillOption.Spacing, DrillKit.MinimumSpacing) < DrillKit.MinimumSpacing) return $"Spacing must be at least {Numbers.Format(DrillKit.MinimumSpacing)} m.";

            var line = new FormationLine(options, string.Join(",", parts));
            apply = drill => drill.SetLine(line);
            return string.Empty;
        }

        private static string ReadStrategy(string value, out Action<CustomDrill> apply)
        {
            bool read = EnumParser.TryParseStrict(value, out PickStrategy strategy);
            apply = read ? drill => drill.Strategy = strategy : null;
            return read ? string.Empty : $"'{value}' isn't a strategy: use random, any, next or repeat.";
        }

        private static string ReadTurn(string value, out Action<CustomDrill> apply)
        {
            bool read = Facing.TryParse(value, out Facing turn);
            apply = read ? drill => drill.Turn = turn : null;
            return read ? string.Empty : $"'{value}' isn't a turn: use 0 to 359 degrees or random.";
        }

        private static string ReadCountdown(string value, out Action<CustomDrill> apply)
        {
            bool read = TryParseSwitch(value, out bool on);
            apply = read ? drill => drill.CountsDown = on : null;
            return read ? string.Empty : $"countdown is on or off, not '{value}'.";
        }

        private static string ReadGodMode(string value, out Action<CustomDrill> apply)
        {
            bool read = TryParseSwitch(value, out bool on);
            apply = read ? drill => drill.GodMode = on : null;
            return read ? string.Empty : $"godmode is on or off, not '{value}'.";
        }

        private static string ReadGodModeSeconds(string value, out Action<CustomDrill> apply)
        {
            bool read = Numbers.TryParsePositive(value, out float seconds);
            apply = read ? drill => drill.GodModeSeconds = seconds : null;
            return read ? string.Empty : $"godModeSeconds takes a number of seconds above 0, not '{value}'.";
        }

        private static bool TryParseSwitch(string value, out bool on)
        {
            on = value.Equals("on", StringComparison.OrdinalIgnoreCase);
            return on || value.Equals("off", StringComparison.OrdinalIgnoreCase);
        }
    }
}
