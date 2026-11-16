using System;
using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Custom
{
    // Custom drills by name, numbered in the order they were first made.
    // The mod reloads with every map, so a drill added by rc lasts until the next map.
    public sealed class CustomDrillBook
    {
        private const int LongestName = 32;
        private static readonly string[] Reserved = { "add", "remove", "show" };

        private readonly List<CustomDrill> _drills = new();
        private readonly PlayerRegistry _players;
        private readonly Random _random;

        public CustomDrillBook(PlayerRegistry players, Random random)
        {
            _players = players;
            _random = random;
        }

        public IReadOnlyList<CustomDrill> All => _drills;

        // Letters, digits, - and _.
        // Not a bare number, which picks a drill by its place in the list, nor a pick word, which fills the strategy, nor a word rc drill already uses.
        public static bool IsName(string name, out string error)
        {
            bool shaped = !string.IsNullOrEmpty(name) && name.Length <= LongestName && Array.TrueForAll(name.ToCharArray(), c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
            if (!shaped) error = $"'{name}' isn't a drill name: use up to {LongestName} letters, digits, - and _.";
            else if (int.TryParse(name, out _)) error = $"'{name}' would read as a drill number: give it a name with a letter in it.";
            else if (EnumParser.TryParseStrict(name, out PickStrategy _)) error = $"'{name}' is a strategy word: pick another name.";
            else if (Array.Exists(Reserved, word => word.Equals(name, StringComparison.OrdinalIgnoreCase))) error = $"'{name}' is a word rc drill uses: pick another name.";
            else error = string.Empty;
            return error.Length == 0;
        }

        public bool CanAdd(string name, string text, out string error) => Checked(name, text, out error) != null;

        // Makes the drill when the name is new.
        public bool TryAdd(string name, string text, out string error)
        {
            DrillLine line = Checked(name, text, out error);
            if (line == null) return false;
            if (!TryFind(name, out CustomDrill drill)) _drills.Add(drill = new CustomDrill(name, Selection.ForXvX(_players, _random)));

            drill.Apply(line);
            return true;
        }

        public bool Remove(string nameOrNumber) => TryFind(nameOrNumber, out CustomDrill drill) && _drills.Remove(drill);

        // By its number in the list, or by name in any case.
        public bool TryFind(string nameOrNumber, out CustomDrill drill)
        {
            bool numbered = int.TryParse(nameOrNumber, out int number) && number >= 1 && number <= _drills.Count;
            drill = numbered ? _drills[number - 1] : _drills.Find(candidate => candidate.Name.Equals(nameOrNumber, StringComparison.OrdinalIgnoreCase));
            return drill != null;
        }

        // A new round brings new teams, so there is nothing to repeat or continue from.
        public void ResetSelections()
        {
            foreach (CustomDrill drill in _drills) drill.Selection.Reset();
        }

        // Null with the reason when the name or the line won't do, or when a group would take one side of its drill past the most players.
        private DrillLine Checked(string name, string text, out string error)
        {
            if (!IsName(name, out error) || !DrillLineParser.TryParse(text, out DrillLine line, out error)) return null;
            if (line.Group == null || !TryFind(name, out CustomDrill drill)) return line;

            int side = drill.CountOf(line.Group.Side) + line.Group.Count;
            if (side <= DrillLineParser.MostPlayers) return line;

            error = $"{drill.Name} would need {side} {line.Group.Side.ToString().ToLowerInvariant()} players, more than {DrillLineParser.MostPlayers}.";
            return null;
        }
    }
}
