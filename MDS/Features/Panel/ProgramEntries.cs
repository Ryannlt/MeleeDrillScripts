using System.Collections.Generic;
using System.Globalization;
using MDS.Features.Drills;
using MDS.Features.Drills.Custom;
using MDS.Features.Programs;
using MDS.Shared;
using MDS.Shared.Panel.Protocol;

namespace MDS.Features.Panel
{
    // A program is a parent row for its settings, with one child per drill and an add row; each row starts from its prototype in PanelCatalog.
    public static class ProgramEntries
    {
        private const string DrillOptionName = "drill";
        private const string CustomDrillName = "customdrill";
        private const string WithOption = "with";
        private const string On = "true";

        public static List<PanelEntry> For(IReadOnlyList<DrillProgram> programs, IReadOnlyList<CustomDrill> customs)
        {
            var entries = new List<PanelEntry>();
            foreach (DrillProgram program in programs) entries.Add(Program(program, customs));
            return entries;
        }

        private static PanelEntry Program(DrillProgram program, IReadOnlyList<CustomDrill> customs)
        {
            string name = program.Name;
            PanelEntry entry = PageEntries.Prototype(PanelCatalog.Program);
            entry.Label = name;
            entry.Meta = program.Drills.Count == 1 ? "1 drill" : $"{program.Drills.Count} drills";
            entry.Template = PageEntries.Retarget(entry.Template, PageEntries.ParentWords, $"program {name} set");
            entry.Lines = program.Lines();
            entry.More = new List<PanelAction> { PageEntries.Act("Run", $"program {name} run"), PageEntries.Act("Delete", $"program {name} delete", true) };
            FillSettings(entry, program);
            for (int i = 0; i < program.Drills.Count; i++) entry.Children.Add(Drill(program, i, customs));
            entry.Children.Add(AddDrill(program, customs));
            return entry;
        }

        private static void FillSettings(PanelEntry entry, DrillProgram program)
        {
            ProgramSettings settings = program.Settings;
            PageEntries.SetValue(entry, "name", program.Name);
            PageEntries.SetValue(entry, "repeat", settings.Loop ? "loop" : "once");
            PageEntries.SetValue(entry, "pick", Lower(settings.Pick));
            PageEntries.SetValue(entry, "sides", Lower(settings.Sides));
            PageEntries.SetValue(entry, "deal", Lower(settings.Deal));
            PageEntries.SetValue(entry, "pause", Numbers.Format(settings.Pause));
            PageEntries.SetValue(entry, "fightSeconds", Numbers.Format(settings.FightSeconds));
        }

        private static PanelEntry Drill(DrillProgram program, int index, IReadOnlyList<CustomDrill> customs)
        {
            ProgramDrill drill = program.Drills[index];
            string name = program.Name;
            int number = index + 1;
            PanelEntry entry = PageEntries.Prototype(PanelCatalog.ProgramDrill);
            entry.Prefix = drill.Pace.With && index > 0 ? $"+{number}" : number.ToString(CultureInfo.InvariantCulture);
            entry.Indent = drill.Pace.With && index > 0 ? 1 : 0;
            (entry.Label, entry.Meta) = Summary(drill);
            entry.Template = PageEntries.Retarget(entry.Template, PageEntries.ChildWords, $"program {name} edit {number}");
            entry.More = DrillActions(name, number, program.Drills.Count);
            ShowCustomDrills(entry, customs, index > 0);
            FillDrill(entry, drill);
            return entry;
        }

        private static PanelEntry AddDrill(DrillProgram program, IReadOnlyList<CustomDrill> customs)
        {
            PanelEntry entry = PageEntries.Prototype(PanelCatalog.ProgramDrillAdd);
            entry.Template = PageEntries.Retarget(entry.Template, PageEntries.ParentWords, $"program {program.Name} add");
            ShowCustomDrills(entry, customs, program.Drills.Count > 0);
            return entry;
        }

        private static List<PanelAction> DrillActions(string program, int number, int count)
        {
            var actions = new List<PanelAction> { PageEntries.Act("Try it once", $"program {program} try {number}") };
            if (number > 1) actions.Add(PageEntries.Act("Move up", $"program {program} move {number} {number - 1}"));
            if (number < count) actions.Add(PageEntries.Act("Move down", $"program {program} move {number} {number + 1}"));
            actions.Add(PageEntries.Act("Remove", $"program {program} remove {number}", true));
            return actions;
        }

        // The source marks options for drills that pick players with "customdrill"; each custom drill's own value joins that list.
        // The template names every option, so a placeholder left behind would reach the rc as written.
        private static void DropWith(PanelEntry entry)
        {
            entry.Options.RemoveAll(o => o.Name == WithOption);
            entry.Template = entry.Template.Replace($"{{{WithOption}}} ", string.Empty);
        }

        private static void ShowCustomDrills(PanelEntry entry, IReadOnlyList<CustomDrill> customs, bool mayJoin)
        {
            if (!mayJoin) DropWith(entry);
            var values = new List<string>();
            foreach (CustomDrill custom in customs) values.Add($"{CustomDrillName} {custom.Name}");

            PanelOption drill = entry.Options.Find(o => o.Name == DrillOptionName);
            values.ForEach(value => drill.Values.Add(new PanelChoice { Value = value, Label = $"Custom: {value.Substring(CustomDrillName.Length + 1)}", Description = "One of your custom drills." }));
            foreach (PanelOption option in entry.Options)
            {
                AddWhen(option.When, values);
                option.Values.ForEach(choice => AddWhen(choice.When, values));
            }
        }

        private static void AddWhen(Dictionary<string, List<string>> when, List<string> values)
        {
            if (when.TryGetValue(DrillOptionName, out List<string> drills) && drills.Contains(CustomDrillName)) drills.AddRange(values);
        }

        private static (string Label, string Meta) Summary(ProgramDrill drill)
        {
            drill.Drill.Syntax.TryParse(drill.Options, out DrillOptions options, out _);
            string arenas = drill.Arenas.Count == 1 ? $"arena {drill.Arenas[0]}" : $"arenas {string.Join(",", drill.Arenas)}";
            string label = drill.Drill.Name switch
            {
                "xvx" => $"xvx {options.Get(DrillOption.Attackers)}v{options.Get(DrillOption.Defenders)}",
                "groupfight" => "Groupfight",
                "openmelee" => "Open melee",
                "popcorn" => "Popcorn",
                _ => $"Custom: {options.GetName()}",
            };
            return (label, arenas);
        }

        private static void FillDrill(PanelEntry entry, ProgramDrill drill)
        {
            drill.Drill.Syntax.TryParse(drill.Options, out DrillOptions options, out _);
            bool custom = drill.Drill.Name == CustomDrillName;
            PageEntries.SetValue(entry, DrillOptionName, custom ? $"{CustomDrillName} {options.GetName()}" : drill.Drill.Name);
            PageEntries.SetValue(entry, "attackers", options.Get(DrillOption.Attackers));
            PageEntries.SetValue(entry, "defenders", options.Get(DrillOption.Defenders));
            PageEntries.SetValue(entry, "distance", options.Get(DrillOption.Distance));
            PageEntries.SetValue(entry, "spacing", options.Get(DrillOption.Spacing));
            PageEntries.SetValue(entry, "placement", options.Get(DrillOption.Spawn));
            PageEntries.SetValue(entry, "teleport", options.Get(DrillOption.Teleport));
            PageEntries.SetValue(entry, "mirror", options.IsMirrored ? On : string.Empty);
            PageEntries.SetValue(entry, drill.Drill.ClearsLeftovers ? "arenas" : "arena", string.Join(",", drill.Arenas));
            FillPace(entry, drill.Pace);
        }

        private static void FillPace(PanelEntry entry, DrillPace pace)
        {
            PageEntries.SetValue(entry, WithOption, pace.With ? On : string.Empty);
            PageEntries.SetValue(entry, "pick", pace.Pick.HasValue ? Lower(pace.Pick.Value) : string.Empty);
            PageEntries.SetValue(entry, "deal", pace.Deal.HasValue ? Lower(pace.Deal.Value) : string.Empty);
            PageEntries.SetValue(entry, "ends", pace.End.HasValue ? Lower(pace.End.Value) : string.Empty);
            if (pace.End.HasValue && pace.End != StepEnd.Round) PageEntries.SetValue(entry, Lower(pace.End.Value), Numbers.Format(pace.Count));
            PageEntries.SetValue(entry, "pause", pace.Pause.HasValue ? Numbers.Format(pace.Pause.Value) : string.Empty);
            PageEntries.SetValue(entry, "fightSeconds", pace.FightSeconds.HasValue ? Numbers.Format(pace.FightSeconds.Value) : string.Empty);
        }

        private static string Lower(object value) => value.ToString().ToLowerInvariant();
    }
}
