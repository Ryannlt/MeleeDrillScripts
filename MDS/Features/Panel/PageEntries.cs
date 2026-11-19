using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Custom;
using MDS.Features.Programs;
using MDS.Shared;
using MDS.Shared.Panel.Protocol;
using UnityEngine;

namespace MDS.Features.Panel
{
    // Each row is cloned from its prototype in PanelCatalog, so its options match the mockup.
    public static class PageEntries
    {
        private const string NumberOption = "number";

        public static List<PanelEntry> CustomDrillsToRun(IReadOnlyList<CustomDrill> drills)
        {
            var entries = new List<PanelEntry>();
            foreach (CustomDrill drill in drills)
            {
                PanelEntry entry = Prototype(PanelCatalog.CustomDrillRun);
                entry.Label = drill.Name;
                entry.Meta = drill.Count.ToString();
                entry.Command = $"customdrill {drill.Name}";
                entry.Lines = drill.Lines();
                entries.Add(entry);
            }
            return entries;
        }

        public static List<PanelEntry> Arenas(List<Arena> arenas)
        {
            var entries = new List<PanelEntry>();
            foreach (Arena arena in arenas)
            {
                PanelEntry entry = Prototype(PanelCatalog.Arena);
                Vector2 size = arena.Max - arena.Min;
                entry.Label = $"Arena {arena.Number}";
                entry.Meta = $"{Numbers.Format(size.x)} x {Numbers.Format(size.y)} m";
                entry.Template = $"arena {arena.Number} {{corner}} {{x}} {{z}}";
                entry.Help = arena.ToString();
                entry.Lines = new List<string> { $"first corner {Arena.Format(arena.Corner1)}", $"opposite corner {Arena.Format(arena.Corner2)}" };
                entries.Add(entry);
            }
            return entries;
        }

        public static void PointNewArena(PanelGroup group, List<Arena> arenas)
        {
            PanelEntry entry = group.Entries.Find(e => e.Label == "New arena");
            PanelOption number = entry?.Options.Find(o => o.Name == NumberOption);
            if (number == null) return;

            int next = ArenaStore.First;
            foreach (Arena arena in arenas) next = Mathf.Max(next, arena.Number + 1);
            number.Min = next;
            number.Value = next.ToString();
        }

        public static PanelEntry Prototype(string wrapper)
        {
            PanelCodec.TryRead(wrapper, out PanelPage page);
            return page.Groups[0].Entries[0];
        }

        // A value matches its choice in any case, since rc takes words in any case.
        public static void SetValue(PanelEntry entry, string option, string value)
        {
            PanelOption found = entry.Options.Find(o => o.Name == option);
            if (found == null) return;

            PanelChoice choice = found.Values.Find(c => string.Equals(c.Value, value, System.StringComparison.OrdinalIgnoreCase));
            found.Value = choice?.Value ?? value ?? string.Empty;
        }

        // Words before an item's options: "program pits set", and "program pits edit 1" for a child.
        public const int ParentWords = 3;
        public const int ChildWords = 4;

        // The prototype's template names its example ("program pits edit 1 ..."), so the first words are swapped for this item's.
        public static string Retarget(string template, int words, string prefix)
        {
            string[] parts = template.Split(new[] { ' ' }, words + 1);
            return parts.Length > words ? $"{prefix} {parts[words]}" : prefix;
        }

        public static PanelAction Act(string label, string text, bool confirm = false) => new() { Label = label, Text = text, Confirm = confirm };
    }
}
