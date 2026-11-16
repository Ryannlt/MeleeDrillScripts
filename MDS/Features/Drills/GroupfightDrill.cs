using System;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Preview;

namespace MDS.Features.Drills
{
    public sealed class GroupfightDrill : IDrill
    {
        private readonly DrillKit _kit;
        private readonly Selection _selection;

        public GroupfightDrill(DrillKit kit, Selection selection)
        {
            _kit = kit;
            _selection = selection;
        }

        public string Name => "groupfight";

        public DrillSyntax Syntax { get; } = new DrillSyntax(
            "Usage: rc groupfight [random|repeat] [distance] [spacing] [facing] [arena], numbers in that order, or by name: distance=25 facing=eastwest",
            0, DrillOption.Strategy, DrillOption.Distance, DrillOption.Spacing, DrillOption.Facing);

        // Everyone on the field is placed.
        public bool ClearsLeftovers => false;

        public bool RunsInParallel(DrillOptions options) => false;

        public Headcount Needs(DrillOptions options) => Headcount.Everyone;

        public bool Validate(DrillOptions options, out string error)
        {
            if (!_kit.HasArena(options, out error)) return false;

            PickStrategy strategy = options.GetStrategy(_kit.Settings.GroupfightStrategy.Value);
            if (!_selection.Allows(strategy)) error = $"A groupfight picks by random or repeat, not {strategy}.";
            else if (!HasBothTeams(_kit.Field(options.Scope))) error = "Both teams need players for a groupfight.";
            return error.Length == 0;
        }

        public bool TryPreview(DrillOptions options, out DrillPreview preview, out string error)
        {
            LineFit lines = LinesFor(options);
            error = lines.Error;
            Lineup field = _kit.Field(options.Scope);
            preview = lines.Fits ? _kit.PreviewLines(lines, field.Attackers.Count, field.Defenders.Count) : null;
            return lines.Fits;
        }

        public bool TryStart(DrillOptions options, out DrillRun run, out string error)
        {
            run = null;
            LineFit lines = LinesFor(options);
            error = lines.Error;
            if (!lines.Fits) return false;

            PickResult pick = _selection.Pick(options.GetStrategy(_kit.Settings.GroupfightStrategy.Value), Headcount.Everyone, options.Scope);
            error = pick.Error;
            if (!pick.Succeeded) return false;

            run = new DrillRun(Name, _kit.ArenaFor(options));
            _kit.PlaceLines(run, pick.Lineup, lines);
            return true;
        }

        private static bool HasBothTeams(Lineup field) => field.Attackers.Count > 0 && field.Defenders.Count > 0;

        // Everyone on the field is placed, so the longer team sets the line length.
        private LineFit LinesFor(DrillOptions options)
        {
            LineShape asked = _kit.ShapeFor(options, _kit.Settings.GroupfightDistance, _kit.Settings.GroupfightSpacing);
            Lineup field = _kit.Field(options.Scope);
            int longest = Math.Max(field.Attackers.Count, field.Defenders.Count);
            return LineFit.Into(_kit.ArenaFor(options), asked, longest);
        }
    }
}
