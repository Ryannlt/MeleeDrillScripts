using System;
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
            "Usage: rc xvx <attackers> <defenders> [strategy] [distance] [spacing] [facing] [mirror] [arena], numbers in that order, or by name: spacing=1 facing=90",
            RequiredOptions, DrillOption.Attackers, DrillOption.Defenders, DrillOption.Strategy, DrillOption.Distance, DrillOption.Spacing, DrillOption.Facing, DrillOption.Mirror);

        public bool ClearsLeftovers => true;

        public bool RunsInParallel(DrillOptions options) => true;

        public Headcount Needs(DrillOptions options) => new Headcount(options.GetCount(DrillOption.Attackers), options.GetCount(DrillOption.Defenders));

        public bool Validate(DrillOptions options, out string error) => _kit.HasArena(options, out error);

        public bool TryPreview(DrillOptions options, out DrillPreview preview, out string error)
        {
            LineFit lines = LinesFor(options);
            error = lines.Error;
            Headcount count = CountOf(options);
            preview = lines.Fits ? _kit.PreviewLines(lines, count.Attackers, count.Defenders) : null;
            return lines.Fits;
        }

        // The fit is checked before the pick, so a refused drill leaves Next and Repeat where they were.
        public bool TryStart(DrillOptions options, out DrillRun run, out string error)
        {
            run = null;
            LineFit lines = LinesFor(options);
            error = lines.Error;
            if (!lines.Fits) return false;

            PickResult pick = _selection.Pick(options.GetStrategy(_kit.Settings.XvXStrategy.Value), CountOf(options), options.Scope);
            error = pick.Error;
            if (!pick.Succeeded) return false;

            run = new DrillRun(Name, _kit.ArenaFor(options));
            _kit.PlaceLines(run, pick.Lineup, lines);
            return true;
        }

        private static Headcount CountOf(DrillOptions options)
        {
            var count = new Headcount(options.GetCount(DrillOption.Attackers), options.GetCount(DrillOption.Defenders));
            return options.IsMirrored ? count.Mirrored : count;
        }

        private LineFit LinesFor(DrillOptions options)
        {
            LineShape asked = _kit.ShapeFor(options, _kit.Settings.XvXDistance, _kit.Settings.XvXSpacing);
            int longest = Math.Max(options.GetCount(DrillOption.Attackers), options.GetCount(DrillOption.Defenders));
            return LineFit.Into(_kit.ArenaFor(options), asked, longest);
        }
    }
}
