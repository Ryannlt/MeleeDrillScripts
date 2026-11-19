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
        // Names come first in rc customdrill, so no word needs keeping back.
        private static readonly string[] Reserved = new string[0];

        private readonly List<CustomDrill> _drills = new();
        private readonly PlayerRegistry _players;
        private readonly Random _random;

        public CustomDrillBook(PlayerRegistry players, Random random)
        {
            _players = players;
            _random = random;
        }

        public IReadOnlyList<CustomDrill> All => _drills;

        // Not a pick word either, since it would fill the strategy.
        public static bool IsName(string name, out string error)
        {
            if (!NameRules.TryCheck(name, Reserved, out error)) return false;
            if (EnumParser.TryParseStrict(name, out PickStrategy _)) error = $"'{name}' is a strategy word: pick another name.";
            return error.Length == 0;
        }

        public bool CanAdd(string name, string text, out string error) => Parsed(name, text, out error) != null;

        // Makes the drill when the name is new; a spot or formation becomes one of its parts, anything else a setting.
        public bool TryAdd(string name, string text, out string error)
        {
            Action<CustomDrill> apply = Parsed(name, text, out error);
            if (apply == null) return false;

            CustomDrill drill = TryFind(name, out CustomDrill found) ? found : Create(name);
            if (CustomLineParser.IsPart(text)) drill.AddPart(text);
            else apply(drill);
            return true;
        }

        public CustomDrill Create(string name)
        {
            var drill = new CustomDrill(name, Selection.ForXvX(_players, _random));
            _drills.Add(drill);
            return drill;
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

        private static Action<CustomDrill> Parsed(string name, string text, out string error)
        {
            return IsName(name, out error) && CustomLineParser.TryParse(text, out Action<CustomDrill> apply, out error) ? apply : null;
        }
    }
}
