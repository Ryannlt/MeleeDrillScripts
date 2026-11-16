using System;

namespace MDS.Features.Drills
{
    // A bare token fills the first empty slot that accepts it, so the old positional form and words in any order both work.
    // Every drill also takes an arena, in the last slot: a bare number only reaches it once every option before it is taken.
    // A slot that goes by name only, such as turn, never takes a bare token.
    public sealed class DrillSyntax
    {
        private const char NameSeparator = '=';

        private readonly DrillOption[] _slots;
        private readonly int _required;

        public DrillSyntax(string usage, int required, params DrillOption[] slots)
        {
            Usage = usage;
            _required = required;
            _slots = new DrillOption[slots.Length + 1];
            slots.CopyTo(_slots, 0);
            _slots[slots.Length] = DrillOption.Arena;
        }

        public string Usage { get; }

        public bool TryParse(string[] tokens, out DrillOptions options, out string error)
        {
            options = new DrillOptions();
            foreach (string token in tokens)
            {
                bool placed = token.IndexOf(NameSeparator) >= 0 ? TryPlaceNamed(token, options, out error) : TryPlaceBare(token, options, out error);
                if (!placed) return false;
            }
            return HasRequired(options, out error);
        }

        private bool TryPlaceNamed(string token, DrillOptions options, out string error)
        {
            int split = token.IndexOf(NameSeparator);
            string key = token.Substring(0, split);
            string value = token.Substring(split + 1);
            int slot = Array.FindIndex(_slots, option => DrillOptionValues.KeyOf(option).Equals(key, StringComparison.OrdinalIgnoreCase));

            if (slot < 0) error = $"'{key}' isn't an option here. {Usage}";
            else if (options.Has(_slots[slot])) error = $"{key} is given twice. {Usage}";
            else if (!DrillOptionValues.Accepts(_slots[slot], value)) error = $"'{value}' isn't a valid {key}. {Usage}";
            else error = string.Empty;

            if (error.Length == 0) options.Set(_slots[slot], value);
            return error.Length == 0;
        }

        private bool TryPlaceBare(string token, DrillOptions options, out string error)
        {
            error = string.Empty;
            foreach (DrillOption slot in _slots)
            {
                if (options.Has(slot) || !DrillOptionValues.TakesBare(slot) || !DrillOptionValues.Accepts(slot, token)) continue;

                options.Set(slot, token);
                return true;
            }

            error = $"'{token}' doesn't fit any option. {Usage}";
            return false;
        }

        private bool HasRequired(DrillOptions options, out string error)
        {
            error = string.Empty;
            for (int i = 0; i < _required; i++)
            {
                if (options.Has(_slots[i])) continue;

                error = $"Missing {DrillOptionValues.KeyOf(_slots[i])}. {Usage}";
                return false;
            }
            return true;
        }
    }
}
