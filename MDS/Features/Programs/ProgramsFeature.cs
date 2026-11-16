using System;
using MDS.Features.Drills;
using MDS.Shared;
using MDS.Shared.Config;
using MDS.Shared.Rounds;

namespace MDS.Features.Programs
{
    // Programs run drills step by step, across arenas, with the drills feature doing every placement.
    public sealed class ProgramsFeature : IDisposable
    {
        private readonly RoundTracker _round;
        private readonly FightWatcher _watcher;
        private readonly ProgramRunner _runner;

        public ProgramsFeature(ModContext context, DrillCatalog drills)
        {
            var settings = new ProgramSettings(context.Replies);
            settings.RegisterWith(context.Settings, context.Config);
            _watcher = new FightWatcher(context.Players, context.Clock);
            var kit = new ProgramKit { Drills = drills, Watcher = _watcher, Settings = settings, Console = context.Console, Replies = context.Replies, Clock = context.Clock };
            var book = new ProgramBook(new ProgramLineParser(drills));
            _runner = new ProgramRunner(kit, context.Routines);
            context.Config.Register(new ProgramVariable(book));
            context.Commands.Register(new ProgramCommand(book, _runner, context.Replies));
            _round = context.Round;
            _round.RoundStarted += OnRoundStarted;
        }

        public void Dispose()
        {
            _round.RoundStarted -= OnRoundStarted;
            _runner.Stop();
            _watcher.Dispose();
        }

        // A new round brings new teams, so a running program stops.
        private void OnRoundStarted(RoundInfo round) => _runner.Stop();
    }

    // MDS:Program:<name>:<line>, one line per step or drill line, in order.
    public sealed class ProgramVariable : IConfigVariable
    {
        private readonly ProgramBook _book;

        public ProgramVariable(ProgramBook book)
        {
            _book = book;
        }

        public string Key => "Program";

        public bool Validate(string value, out string reason)
        {
            if (NameRules.TrySplit(value, out string name, out string line)) return _book.CanAdd(name, line, out reason);

            reason = "expected MDS:Program:<name>:<line>.";
            return false;
        }

        public void Apply(string value)
        {
            NameRules.TrySplit(value, out string name, out string line);
            _book.TryAdd(name, line, out _);
        }
    }
}
