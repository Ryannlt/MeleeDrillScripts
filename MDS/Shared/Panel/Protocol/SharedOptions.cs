using System;
using System.Collections.Generic;

// This file is copied into every module (shared code is copied), so a change goes into every copy.
namespace MDS.Shared.Panel.Protocol
{
    // Rows built from one prototype repeat the same option definitions, so a definition seen twice is written once at page level and each row sends only its value.
    public sealed class SharedOptions
    {
        private const int Repeats = 2;

        private readonly Func<PanelOption, string> _definition;
        private readonly Dictionary<string, int> _seen = new();
        private readonly Dictionary<string, int> _index = new();
        private readonly Dictionary<PanelOption, string> _keys = new();

        private SharedOptions(Func<PanelOption, string> definition)
        {
            _definition = definition;
        }

        public List<PanelOption> Definitions { get; } = new();

        // definition writes an option without its value, so two rows differing only in value share it.
        public static SharedOptions Of(PanelPage page, Func<PanelOption, string> definition)
        {
            var shared = new SharedOptions(definition);
            foreach (PanelGroup group in page.Groups) group.Entries.ForEach(shared.Count);
            return shared;
        }

        public int IndexOf(PanelOption option) =>
            _keys.TryGetValue(option, out string key) && _index.TryGetValue(key, out int index) ? index : -1;

        // The copy has lists of its own, so changing one row's option never reaches another row.
        public static PanelOption Expand(PanelOption definition, string value) => new()
        {
            Name = definition.Name, Label = definition.Label, Description = definition.Description, Type = definition.Type,
            Value = value, Min = definition.Min, Max = definition.Max, HardMax = definition.HardMax, Step = definition.Step,
            Values = new List<PanelChoice>(definition.Values), Setting = definition.Setting, SettingValue = definition.SettingValue,
            DefaultLabel = definition.DefaultLabel, Placeholder = definition.Placeholder, Positional = definition.Positional,
            Word = definition.Word, Optional = definition.Optional, Free = definition.Free, Stepper = definition.Stepper,
            When = new Dictionary<string, List<string>>(definition.When), HereValue = definition.HereValue,
        };

        private void Count(PanelEntry entry)
        {
            entry.Options.ForEach(Note);
            entry.Children.ForEach(Count);
        }

        private void Note(PanelOption option)
        {
            string key = _definition(option);
            _keys[option] = key;
            int seen = _seen.TryGetValue(key, out int count) ? count + 1 : 1;
            _seen[key] = seen;
            if (seen != Repeats) return;

            _index[key] = Definitions.Count;
            Definitions.Add(option);
        }
    }
}
