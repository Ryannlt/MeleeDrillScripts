using MDS.Features.Drills.Picking;
using MDS.Shared.Commands;

namespace MDS.Features.Drills
{
    public sealed class GroupfightCommand : IConsoleCommand
    {
        private static readonly DrillSyntax Syntax = new DrillSyntax(
            "Usage: rc groupfight [random|repeat] [distance] [spacing] [facing], or by name: distance=25 facing=eastwest",
            0, DrillOption.Strategy, DrillOption.Distance, DrillOption.Spacing, DrillOption.Facing);

        private readonly DrillKit _kit;
        private readonly Selection _selection;

        public GroupfightCommand(DrillKit kit, Selection selection)
        {
            _kit = kit;
            _selection = selection;
        }

        public string Name => "groupfight";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            if (!Syntax.TryParse(parameters, out DrillOptions options, out errorMessage) || !_kit.HasArena(out errorMessage)) return false;

            PickStrategy strategy = options.GetStrategy(_kit.Settings.GroupfightStrategy.Value);
            if (!_selection.Allows(strategy)) errorMessage = $"A groupfight picks by random or repeat, not {strategy}.";
            else if (_kit.Players.Attacking.Count == 0 || _kit.Players.Defending.Count == 0) errorMessage = "Both teams need players for a groupfight.";
            return errorMessage.Length == 0;
        }

        public void Execute(int playerId, string[] parameters)
        {
            Syntax.TryParse(parameters, out DrillOptions options, out _);
            PickResult pick = _selection.Pick(options.GetStrategy(_kit.Settings.GroupfightStrategy.Value), Headcount.Everyone);
            if (!pick.Succeeded)
            {
                _kit.Replies.Send(playerId, pick.Error);
                return;
            }

            _kit.PlaceLines(Name, pick.Lineup, _kit.ShapeFor(options, _kit.Settings.GroupfightDistance, _kit.Settings.GroupfightSpacing));
        }
    }
}
