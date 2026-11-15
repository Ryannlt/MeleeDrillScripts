using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Preview;

namespace MDS.Features.Drills
{
    public sealed class XvXDrill : IDrill
    {
        // Attackers and defenders.
        private const int RequiredOptions = 2;

        private readonly DrillKit _kit;
        private readonly Selection _selection;

        public XvXDrill(DrillKit kit, Selection selection)
        {
            _kit = kit;
            _selection = selection;
        }

        public string Name => "xvx";

        public DrillSyntax Syntax { get; } = new DrillSyntax(
            "Usage: rc xvx <attackers> <defenders> [strategy] [distance] [spacing] [facing] [arena=N], or by name: spacing=1 facing=eastwest",
            RequiredOptions, DrillOption.Attackers, DrillOption.Defenders, DrillOption.Strategy, DrillOption.Distance, DrillOption.Spacing, DrillOption.Facing);

        public bool ClearsLeftovers => true;

        public bool Validate(DrillOptions options, out string error) => _kit.HasArena(options, out error);

        public bool TryPreview(DrillOptions options, out DrillPreview preview, out string error)
        {
            error = string.Empty;
            LineShape shape = _kit.ShapeFor(options, _kit.Settings.XvXDistance, _kit.Settings.XvXSpacing);
            preview = _kit.PreviewLines(shape, options.GetCount(DrillOption.Attackers), options.GetCount(DrillOption.Defenders));
            return true;
        }

        public bool TryStart(DrillOptions options, out DrillRun run, out string error)
        {
            run = null;
            var count = new Headcount(options.GetCount(DrillOption.Attackers), options.GetCount(DrillOption.Defenders));
            PickResult pick = _selection.Pick(options.GetStrategy(_kit.Settings.XvXStrategy.Value), count);
            error = pick.Error;
            if (!pick.Succeeded) return false;

            run = new DrillRun(Name, _kit.ArenaFor(options));
            _kit.PlaceLines(run, pick.Lineup, _kit.ShapeFor(options, _kit.Settings.XvXDistance, _kit.Settings.XvXSpacing));
            return true;
        }
    }
}
