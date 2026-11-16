using System;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills;
using MDS.Features.Drills.Picking;
using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Programs
{
    // Reads one comma separated program line: a step line, or a drill line written as its rc options plus the arenas it runs in.
    // A drill line's own options are checked here by the drill's syntax; its arenas and players are checked when the program runs.
    public sealed class ProgramLineParser
    {
        public const string StepFormat = "step,[random|any|next|repeat],[fill|balance],[rolling|together],[minutes=],[fights=],[fightSeconds=],[pause=]";

        public const string Format = StepFormat + " | <drill>,<its rc options>,[arenas=(4,5,6)]";

        private const string StepWord = "step";
        private const string ArenasKey = "arenas=";

        private static readonly Dictionary<string, Action<ProgramStep, float>> StepNumbers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["minutes"] = (step, value) => step.Minutes = value,
            ["fights"] = (step, value) => step.Fights = Mathf.RoundToInt(value),
            ["fightSeconds"] = (step, value) => step.FightSeconds = value,
            ["pause"] = (step, value) => step.PauseSeconds = value,
        };

        private readonly DrillCatalog _catalog;

        public ProgramLineParser(DrillCatalog catalog)
        {
            _catalog = catalog;
        }

        public static bool IsArenas(string part) => part.StartsWith(ArenasKey, StringComparison.OrdinalIgnoreCase);

        public bool TryParse(string text, out Action<DrillProgram> apply, out string error)
        {
            string[] parts = CommaParts.Split(text);
            string head = parts.Length > 0 ? parts[0] : string.Empty;
            apply = null;
            if (head.Equals(StepWord, StringComparison.OrdinalIgnoreCase)) error = ReadStep(parts, out apply);
            else if (_catalog.TryFind(head, out IDrill drill)) error = ReadDrill(drill, parts, out apply);
            else error = $"'{text}' isn't a program line. A line is one of: {Format}";
            return error.Length == 0;
        }

        private static string ReadStep(string[] parts, out Action<DrillProgram> apply)
        {
            apply = null;
            var step = new ProgramStep { Text = string.Join(",", parts) };
            for (int i = 1; i < parts.Length; i++)
            {
                string error = ReadStepPart(parts[i], step);
                if (error.Length > 0) return error;
            }

            apply = program => program.AddStep(step);
            return string.Empty;
        }

        private static string ReadStepPart(string part, ProgramStep step)
        {
            if (EnumParser.TryParseStrict(part, out PickStrategy pick)) step.Pick = pick;
            else if (EnumParser.TryParseStrict(part, out ArenaStrategy arenas)) step.Arenas = arenas;
            else if (EnumParser.TryParseStrict(part, out StepMode mode)) step.Mode = mode;
            else return ReadStepNumber(part, step);
            return string.Empty;
        }

        private static string ReadStepNumber(string part, ProgramStep step)
        {
            int split = part.IndexOf('=');
            if (split > 0 && StepNumbers.TryGetValue(part.Substring(0, split), out Action<ProgramStep, float> set) && Numbers.TryParseZeroOrMore(part.Substring(split + 1), out float value))
            {
                set(step, value);
                return string.Empty;
            }
            return $"'{part}' isn't part of a step. A step reads: {StepFormat}";
        }

        // Several arenas need a drill that runs in parallel, and a custom drill not made yet is checked again when the program runs.
        private static string Checked(IDrill drill, List<string> tokens, List<int> arenas)
        {
            if (!drill.Syntax.TryParse(tokens.ToArray(), out DrillOptions options, out string error)) return error;
            if (options.Has(DrillOption.Strategy)) return "A drill line takes no pick word: the step's pick applies.";
            if (options.Has(DrillOption.Arena) && arenas.Count > 0) return "Give a drill line arena= or arenas=, not both.";
            return ParallelRefusal(drill, options, arenas.Count);
        }

        public static string ParallelRefusal(IDrill drill, DrillOptions options, int arenas)
        {
            return arenas > 1 && !drill.RunsInParallel(options) ? $"{drill.Name} takes everyone on the field, so it runs in one arena." : string.Empty;
        }

        private static List<int> ArenasOf(IDrill drill, List<string> tokens, List<int> written)
        {
            if (written.Count > 0) return written;

            drill.Syntax.TryParse(tokens.ToArray(), out DrillOptions options, out _);
            return new List<int> { options.GetArena() };
        }

        private static string SplitArenas(string[] parts, out List<string> tokens, out List<int> arenas)
        {
            tokens = new List<string>();
            arenas = new List<int>();
            for (int i = 1; i < parts.Length; i++)
            {
                if (!IsArenas(parts[i])) tokens.Add(parts[i]);
                else if (!TryParseArenas(parts[i].Substring(ArenasKey.Length), arenas)) return $"'{parts[i]}' isn't a list of different arenas like arenas=(4,5,6).";
            }
            return string.Empty;
        }

        private static bool TryParseArenas(string text, List<int> arenas)
        {
            if (!text.StartsWith("(") || !text.EndsWith(")")) return false;

            foreach (string part in text.Trim('(', ')').Split(','))
            {
                if (!ArenaParser.TryParseNumber(part.Trim(), out int arena) || arenas.Contains(arena)) return false;

                arenas.Add(arena);
            }
            return arenas.Count > 0;
        }

        private string ReadDrill(IDrill drill, string[] parts, out Action<DrillProgram> apply)
        {
            apply = null;
            string error = SplitArenas(parts, out List<string> tokens, out List<int> arenas);
            if (error.Length == 0) error = Checked(drill, tokens, arenas);
            if (error.Length > 0) return error;

            var line = new ProgramDrill(drill, parts, ArenasOf(drill, tokens, arenas));
            apply = program => program.AddDrill(line);
            return string.Empty;
        }
    }
}
