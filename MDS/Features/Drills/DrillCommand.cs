using MDS.Shared.Commands;
using MDS.Shared.Game;

namespace MDS.Features.Drills
{
    public sealed class DrillCommand : IConsoleCommand
    {
        private readonly IDrill _drill;
        private readonly DrillRunner _runner;
        private readonly Replies _replies;

        public DrillCommand(IDrill drill, DrillRunner runner, Replies replies)
        {
            _drill = drill;
            _runner = runner;
            _replies = replies;
        }

        public string Name => _drill.Name;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            return _drill.Syntax.TryParse(parameters, out DrillOptions options, out errorMessage) && _drill.Validate(options, out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            _drill.Syntax.TryParse(parameters, out DrillOptions options, out _);
            _runner.Start(_drill, options, reply => _replies.Send(playerId, reply));
        }
    }
}
