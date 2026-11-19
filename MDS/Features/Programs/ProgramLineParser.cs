using System;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills;
using MDS.Features.Drills.Picking;
using MDS.Shared;

namespace MDS.Features.Programs
{
    // Reads a program's words: the program's settings, or a drill with its rc options, arenas and own timing.
    // Values go in order without names; words that only one option takes go anywhere, and "-" keeps a slot's default.
    public sealed class ProgramLineParser
    {
        public const string SetFormat = "[random|any|next|repeat] [fill|balance] [together|rolling] [pause] [fightSeconds] [loop|once]";

        public const string AddFormat = "<drill> <its rc options> [(arenas)] [with] [random|any|next|repeat] [together|rolling] [round | rounds <n> | each <n> | minutes <n>] [pause] [fightSeconds]";

        public const string LoopWord = "loop";

        private const string OnceWord = "once";
        private const string WithWord = "with";
        private const string ArenasKey = "arenas";
        private const char NameSeparator = '=';

        private readonly DrillCatalog _catalog;

        public ProgramLineParser(DrillCatalog catalog)
        {
            _catalog = catalog;
        }

        // Onto the settings given, so a caller can check on a copy first.
        public static string ReadSettings(string[] words, ProgramSettings settings)
        {
            var numbers = new NumberSlots(Seconds(v => settings.Pause = v), Seconds(v => settings.FightSeconds = v));
            foreach (string word in words)
            {
                if (!TrySetting(word, settings, numbers)) return $"'{word}' isn't a program setting. They read: {SetFormat}";
            }
            return string.Empty;
        }

        public bool TryParseDrill(string[] words, out ProgramDrill drill, out string error)
        {
            drill = null;
            string head = words.Length > 0 ? words[0] : string.Empty;
            if (!_catalog.TryFind(head, out IDrill found))
            {
                error = $"'{head}' isn't a drill. A drill reads: {AddFormat}";
                return false;
            }

            var reader = new DrillWords(found);
            error = reader.Read(words[1..]);
            if (error.Length == 0) error = reader.Check();
            drill = error.Length == 0 ? reader.Build(string.Join(",", words)) : null;
            return error.Length == 0;
        }

        public static string ParallelRefusal(IDrill drill, DrillOptions options, int arenas)
        {
            return arenas > 1 && !drill.RunsInParallel(options) ? $"{drill.Name} takes everyone on the field, so it runs in one arena." : string.Empty;
        }

        public static bool TryParseArenas(string text, List<int> arenas)
        {
            if (!text.StartsWith("(") || !text.EndsWith(")")) return false;

            foreach (string part in text.Trim('(', ')').Split(','))
            {
                if (!ArenaParser.TryParseNumber(part.Trim(), out int arena) || arenas.Contains(arena)) return false;

                arenas.Add(arena);
            }
            return arenas.Count > 0;
        }

        private static bool TrySetting(string word, ProgramSettings settings, NumberSlots numbers)
        {
            if (word.IndexOf(NameSeparator) > 0) return TryNamedSetting(word, settings, numbers);
            if (EnumParser.TryParseStrict(word, out PickStrategy pick)) settings.Pick = pick;
            else if (EnumParser.TryParseStrict(word, out ArenaStrategy sides)) settings.Sides = sides;
            else if (EnumParser.TryParseStrict(word, out StepMode deal)) settings.Deal = deal;
            else if (IsWord(word, LoopWord) || IsWord(word, OnceWord)) settings.Loop = IsWord(word, LoopWord);
            else return numbers.TryFill(word);
            return true;
        }

        private static bool TryNamedSetting(string word, ProgramSettings settings, NumberSlots numbers)
        {
            TrySplit(word, out string key, out string value);
            if (IsWord(key, "pause") || IsWord(key, "fightSeconds")) return numbers.TryFill(IsWord(key, "pause") ? 0 : 1, value);
            if (IsWord(key, LoopWord)) return TrySwitch(value, on => settings.Loop = on);
            bool known = IsWord(key, "pick") || IsWord(key, "sides") || IsWord(key, "deal");
            return known && !value.Contains(NameSeparator.ToString()) && TrySetting(value, settings, numbers);
        }

        private static Func<string, bool> Seconds(Action<float> set) => text =>
        {
            bool read = Numbers.TryParseZeroOrMore(text, out float value);
            if (read) set(value);
            return read;
        };

        private static bool TrySwitch(string value, Action<bool> set)
        {
            bool on = IsWord(value, "on") || IsWord(value, "true");
            if (on || IsWord(value, "off") || IsWord(value, "false")) set(on);
            return on || IsWord(value, "off") || IsWord(value, "false");
        }

        private static bool TrySplit(string word, out string key, out string value)
        {
            int split = word.IndexOf(NameSeparator);
            key = split > 0 ? word.Substring(0, split) : string.Empty;
            value = split > 0 ? word.Substring(split + 1) : string.Empty;
            return split > 0;
        }

        private static bool IsWord(string text, string word) => text.Equals(word, StringComparison.OrdinalIgnoreCase);

        // Numbers in order, each slot once, and "-" passing a slot by.
        private sealed class NumberSlots
        {
            private readonly Func<string, bool>[] _slots;
            private int _next;

            public NumberSlots(params Func<string, bool>[] slots)
            {
                _slots = slots;
            }

            public bool TryFill(string word)
            {
                if (_next >= _slots.Length) return false;

                int slot = _next++;
                return word == DrillSyntax.Skip || TryFill(slot, word);
            }

            public bool TryFill(int slot, string value) => slot < _slots.Length && _slots[slot](value);
        }

        // Splits a drill's words into its own rc options, its arenas and its timing, then checks them together.
        private sealed class DrillWords
        {
            private readonly IDrill _drill;
            private readonly List<string> _options = new();
            private readonly List<int> _arenas = new();
            private readonly DrillPace _pace = new();
            private readonly NumberSlots _timing;
            private bool _inTiming;
            private bool _wantsCount;

            public DrillWords(IDrill drill)
            {
                _drill = drill;
                _timing = new NumberSlots(Seconds(v => _pace.Pause = v), Seconds(v => _pace.FightSeconds = v));
            }

            public string Read(string[] words)
            {
                foreach (string word in words)
                {
                    string error = ReadWord(word);
                    if (error.Length > 0) return error;
                }
                return _wantsCount ? $"{_pace.End.ToString().ToLowerInvariant()} needs a number after it." : string.Empty;
            }

            // Several arenas need a drill that runs in parallel, and a custom drill not made yet is checked again when the program runs.
            public string Check()
            {
                if (!_drill.Syntax.TryParse(_options.ToArray(), out DrillOptions options, out string error)) return error;
                if (options.Has(DrillOption.Strategy) && _drill.ClearsLeftovers) return "Give the pick as a word, like next, not strategy=.";
                if (options.Has(DrillOption.Arena) && _arenas.Count > 0) return "Give arena= or (arenas), not both.";
                if (_arenas.Count == 0) _arenas.Add(options.GetArena());
                return PaceRefusal().Length > 0 ? PaceRefusal() : ParallelRefusal(_drill, options, _arenas.Count);
            }

            public ProgramDrill Build(string text) => new ProgramDrill { Drill = _drill, Options = _options.ToArray(), Arenas = _arenas, Pace = _pace, Text = text };

            private string PaceRefusal()
            {
                bool rounds = _pace.End == StepEnd.Round || _pace.End == StepEnd.Rounds;
                if (_pace.With && _pace.HasTiming) return "A drill run with the one above takes its timing, so it sets none of its own.";
                if (_pace.Deal == StepMode.Rolling && rounds) return "Fights started as soon as free have no rounds: end it with each <n> or minutes <n>.";
                if (_drill.ClearsLeftovers) return string.Empty;
                if (_pace.Pick.HasValue || _pace.Deal.HasValue) return $"{_drill.Name} takes everyone, so it has no pick and no start fights.";
                return _pace.End == StepEnd.Each ? $"{_drill.Name} takes everyone, so everyone fights every round: end it with rounds or minutes." : string.Empty;
            }

            private string ReadWord(string word)
            {
                if (_wantsCount) return ReadCount(word);
                if (word.StartsWith("(") || IsNamed(word, ArenasKey)) return ReadArenas(word);
                if (IsWord(word, WithWord)) return Flag(() => _pace.With = true);
                if (_drill.ClearsLeftovers && EnumParser.TryParseStrict(word, out PickStrategy pick)) return Flag(() => _pace.Pick = pick);
                if (EnumParser.TryParseStrict(word, out StepMode deal)) return Flag(() => _pace.Deal = deal);
                if (EnumParser.TryParseStrict(word, out StepEnd end)) return StartEnd(end);
                if (TrySplit(word, out string key, out string value) && IsTimingKey(key)) return ReadNamedTiming(key, value);
                if (_inTiming) return _timing.TryFill(word) ? string.Empty : $"'{word}' isn't a pause or a fight time. A drill reads: {AddFormat}";

                _options.Add(word);
                return string.Empty;
            }

            private string ReadArenas(string word)
            {
                string list = IsNamed(word, ArenasKey) ? word.Substring(ArenasKey.Length + 1) : word;
                return _arenas.Count == 0 && TryParseArenas(list, _arenas) ? string.Empty : $"'{word}' isn't a list of different arenas like (4,5,6).";
            }

            private string StartEnd(StepEnd end)
            {
                _pace.End = end;
                _inTiming = true;
                _wantsCount = end != StepEnd.Round;
                return string.Empty;
            }

            private string ReadCount(string word)
            {
                _wantsCount = false;
                bool read = Numbers.TryParsePositive(word, out float count) && (_pace.End == StepEnd.Minutes || count == (int)count);
                _pace.Count = count;
                return read ? string.Empty : $"'{word}' isn't a count for {_pace.End.ToString().ToLowerInvariant()}.";
            }

            private static bool IsTimingKey(string key) =>
                Array.Exists(new[] { "pick", "deal", "with", "round", "rounds", "each", "minutes", "pause", "fightSeconds" }, name => IsWord(key, name));

            private string ReadNamedTiming(string key, string value)
            {
                if (IsWord(key, "pause") || IsWord(key, "fightSeconds")) return _timing.TryFill(IsWord(key, "pause") ? 0 : 1, value) ? string.Empty : $"'{value}' isn't a number of seconds for {key}.";
                if (IsWord(key, "with")) return TrySwitch(value, on => _pace.With = on) ? string.Empty : $"with is on or off, not '{value}'.";
                if (IsWord(key, "pick") || IsWord(key, "deal")) return ReadWord(value);

                EnumParser.TryParseStrict(key, out StepEnd end);
                StartEnd(end);
                return _wantsCount ? ReadCount(value) : string.Empty;
            }

            private static string Flag(Action set)
            {
                set();
                return string.Empty;
            }

            private static bool IsNamed(string word, string key) => word.StartsWith(key + NameSeparator, StringComparison.OrdinalIgnoreCase);
        }
    }
}
