using MDS.Shared.Commands;

namespace MDS.Features.Drills
{
    public sealed class OpenMeleeCommand : IConsoleCommand
    {
        private static readonly DrillSyntax Syntax = new DrillSyntax(
            "Usage: rc openmelee [spacing] [offset], or by name: spacing=2 offset=3", 0, DrillOption.Spacing, DrillOption.Offset);

        private readonly DrillKit _kit;
        private readonly OpenMeleeDrill _drill;

        public OpenMeleeCommand(DrillKit kit, OpenMeleeDrill drill)
        {
            _kit = kit;
            _drill = drill;
        }

        public string Name => "openmelee";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            return Syntax.TryParse(parameters, out _, out errorMessage) && _kit.HasArena(out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            Syntax.TryParse(parameters, out DrillOptions options, out _);
            float spacing = options.GetNumber(DrillOption.Spacing, _kit.Settings.OpenMeleeSpacing.Value);
            float offset = options.GetNumber(DrillOption.Offset, _kit.Settings.OpenMeleeOffset.Value);
            if (!_drill.CanPlace(spacing, offset, out string error))
            {
                _kit.Replies.Send(playerId, error);
                return;
            }

            _drill.Start(spacing, offset);
        }
    }
}
