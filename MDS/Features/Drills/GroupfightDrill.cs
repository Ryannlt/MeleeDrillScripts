using MDS.Features.Drills.Picking;

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
            "Usage: rc groupfight [random|repeat] [distance] [spacing] [facing] [arena=N], or by name: distance=25 facing=eastwest",
            0, DrillOption.Strategy, DrillOption.Distance, DrillOption.Spacing, DrillOption.Facing);

        // Everyone on the field is placed.
        public bool ClearsLeftovers => false;

        public bool Validate(DrillOptions options, out string error)
        {
            if (!_kit.HasArena(options, out error)) return false;

            PickStrategy strategy = options.GetStrategy(_kit.Settings.GroupfightStrategy.Value);
            if (!_selection.Allows(strategy)) error = $"A groupfight picks by random or repeat, not {strategy}.";
            else if (_kit.Players.Attacking.Count == 0 || _kit.Players.Defending.Count == 0) error = "Both teams need players for a groupfight.";
            return error.Length == 0;
        }

        public bool TryStart(DrillOptions options, out DrillRun run, out string error)
        {
            run = null;
            PickResult pick = _selection.Pick(options.GetStrategy(_kit.Settings.GroupfightStrategy.Value), Headcount.Everyone);
            error = pick.Error;
            if (!pick.Succeeded) return false;

            run = new DrillRun(Name, _kit.ArenaFor(options));
            _kit.PlaceLines(run, pick.Lineup, _kit.ShapeFor(options, _kit.Settings.GroupfightDistance, _kit.Settings.GroupfightSpacing));
            return true;
        }
    }
}
