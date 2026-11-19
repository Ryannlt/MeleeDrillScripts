using System;
using System.Collections.Generic;
using System.Globalization;
using MDS.Features.Drills.Custom;
using MDS.Shared;
using MDS.Shared.Panel.Protocol;

namespace MDS.Features.Panel
{
    // A custom drill is a parent row for its settings, with one child per spot or formation part and an add row.
    public static class CustomDrillEntries
    {
        private const string LineWord = "line";

        // line,side, then the position; a spot's side, then its positions.
        private const int LineHead = 2;
        private const int SpotHead = 1;
        private const string On = "on";
        private const string Off = "off";

        public static List<PanelEntry> For(IReadOnlyList<CustomDrill> drills)
        {
            var entries = new List<PanelEntry>();
            foreach (CustomDrill drill in drills) entries.Add(Drill(drill));
            return entries;
        }

        private static PanelEntry Drill(CustomDrill drill)
        {
            string name = drill.Name;
            PanelEntry entry = PageEntries.Prototype(PanelCatalog.CustomDrill);
            entry.Label = name;
            entry.Meta = drill.Count.ToString();
            entry.Template = PageEntries.Retarget(entry.Template, PageEntries.ParentWords, $"customdrill {name} set");
            entry.Lines = drill.Lines();
            entry.More = DrillActions(name);
            FillSettings(entry, drill);
            for (int i = 0; i < drill.Parts.Count; i++) entry.Children.Add(Part(drill, i));

            PanelEntry add = PageEntries.Prototype(PanelCatalog.CustomPartAdd);
            add.Template = PageEntries.Retarget(add.Template, PageEntries.ParentWords, $"customdrill {name} add");
            entry.Children.Add(add);
            return entry;
        }

        private static List<PanelAction> DrillActions(string name) => new()
        {
            PageEntries.Act("Run", $"customdrill {name}"), PageEntries.Act("Preview", $"preview customdrill {name}"),
            PageEntries.Act("Delete", $"customdrill {name} delete", true),
        };

        private static void FillSettings(PanelEntry entry, CustomDrill drill)
        {
            PageEntries.SetValue(entry, "strategy", drill.Strategy.ToString().ToLowerInvariant());
            PageEntries.SetValue(entry, "turn", drill.Turn.IsRandom ? string.Empty : Numbers.Format(drill.Turn.Degrees));
            PageEntries.SetValue(entry, "countdown", drill.CountsDown ? On : Off);
            PageEntries.SetValue(entry, "godmode", drill.GodMode ? On : Off);
            PageEntries.SetValue(entry, "godModeSeconds", Numbers.Format(drill.GodModeSeconds));
        }

        private static PanelEntry Part(CustomDrill drill, int index)
        {
            string number = (index + 1).ToString(CultureInfo.InvariantCulture);
            PanelEntry entry = PageEntries.Prototype(PanelCatalog.CustomPart);
            entry.Prefix = number;
            entry.Template = PageEntries.Retarget(entry.Template, PageEntries.ChildWords, $"customdrill {drill.Name} edit {number}");
            entry.More = new List<PanelAction>
            {
                PageEntries.Act("Preview drill", $"preview customdrill {drill.Name}"), PageEntries.Act("Remove", $"customdrill {drill.Name} remove {number}", true),
            };
            FillPart(entry, CommaParts.Split(drill.Parts[index]));
            return entry;
        }

        // Spots read side,(x,z)..., lines line,side,(x,z),[spacing],[facing], open melee strategy,[spacing], each also by name.
        private static void FillPart(PanelEntry entry, string[] words)
        {
            bool line = words.Length > SpotHead && words[0].Equals(LineWord, StringComparison.OrdinalIgnoreCase);
            string kind = line ? $"{LineWord} {words[1].ToLowerInvariant()}" : words[0].ToLowerInvariant();
            int rest = line ? LineHead : SpotHead;
            bool spot = !line && words.Length > SpotHead && words[SpotHead].StartsWith("(");
            string at = line || spot ? Word(words, rest) : string.Empty;
            PageEntries.SetValue(entry, "kind", kind);
            PageEntries.SetValue(entry, "at", at);
            PageEntries.SetValue(entry, "spacing", Named(words, "spacing", line ? rest + 1 : spot ? -1 : rest));
            PageEntries.SetValue(entry, "facing", line ? Named(words, "facing", rest + LineHead) : string.Empty);

            PanelChoice choice = entry.Options.Find(o => o.Name == "kind")?.Values.Find(c => c.Value == kind);
            bool several = spot && words.Length > SpotHead + 1;
            entry.Label = (choice?.Label ?? kind) + (several ? "s" : string.Empty);
            entry.Meta = spot ? string.Join(",", words[SpotHead..]) : at;
        }

        // A named value anywhere, else the bare word at its position.
        private static string Named(string[] words, string key, int position)
        {
            string named = Array.Find(words, w => w.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
            if (named != null) return named.Substring(key.Length + 1);
            string bare = position >= 0 ? Word(words, position) : string.Empty;
            return bare.Contains("=") ? string.Empty : bare;
        }

        private static string Word(string[] words, int index) => index < words.Length ? words[index] : string.Empty;
    }
}
