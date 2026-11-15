using MDS.Features.Drills.Picking;
using MDS.Shared.Commands;

namespace MDS.Features.Drills
{
    public sealed class XvXCommand : IConsoleCommand
    {
        // Attackers and defenders.
        private const int RequiredOptions = 2;

        private static readonly DrillSyntax Syntax = new DrillSyntax(
            "Usage: rc xvx <attackers> <defenders> [strategy] [distance] [spacing] [facing], or by name: spacing=1 facing=eastwest",
            RequiredOptions, DrillOption.Attackers, DrillOption.Defenders, DrillOption.Strategy, DrillOption.Distance, DrillOption.Spacing, DrillOption.Facing);

        private readonly DrillKit _kit;
        private readonly Selection _selection;

        public XvXCommand(DrillKit kit, Selection selection)
        {
            _kit = kit;
            _selection = selection;
        }

        public string Name => "xvx";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            return Syntax.TryParse(parameters, out _, out errorMessage) && _kit.HasArena(out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            Syntax.TryParse(parameters, out DrillOptions options, out _);
            var count = new Headcount(options.GetCount(DrillOption.Attackers), options.GetCount(DrillOption.Defenders));
            PickResult pick = _selection.Pick(options.GetStrategy(_kit.Settings.XvXStrategy.Value), count);
            if (!pick.Succeeded)
            {
                _kit.Replies.Send(playerId, pick.Error);
                return;
            }

            _kit.PlaceLines(Name, pick.Lineup, _kit.ShapeFor(options, _kit.Settings.XvXDistance, _kit.Settings.XvXSpacing));
        }
    }
}
