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
            _watcher = new FightWatcher(context.Players, context.Clock);
            var kit = new ProgramKit { Drills = drills, Watcher = _watcher, Console = context.Console, Replies = context.Replies, Clock = context.Clock };
            Book = new ProgramBook();
            var edits = new ProgramEdits(Book, new ProgramLineParser(drills));
            _runner = new ProgramRunner(kit, context.Routines);
            context.Config.Register(new ProgramVariable(edits));
            context.Commands.Register(new ProgramCommand(edits, _runner, context.Replies));
            _round = context.Round;
            _round.RoundStarted += OnRoundStarted;
        }

        public ProgramBook Book { get; }

        public ProgramRunner Runner => _runner;

        public void Dispose()
        {
            _round.RoundStarted -= OnRoundStarted;
            _runner.Stop();
            _watcher.Dispose();
        }

        // A new round brings new teams, so a running program stops.
        private void OnRoundStarted(RoundInfo round) => _runner.Stop();
    }

    // MDS:Program:<name>:set,<settings> or MDS:Program:<name>:add,<drill>: the rc program words with commas, so lines with the same name build one program.
    public sealed class ProgramVariable : IConfigVariable
    {
        private readonly ProgramEdits _edits;

        public ProgramVariable(ProgramEdits edits)
        {
            _edits = edits;
        }

        public string Key => "Program";

        public bool Validate(string value, out string reason)
        {
            reason = Check(value, out _);
            return reason.Length == 0;
        }

        public void Apply(string value)
        {
            Check(value, out Action change);
            change();
        }

        // A new name is checked against an empty program, and made only when the line applies.
        private string Check(string value, out Action change)
        {
            change = null;
            if (!NameRules.TrySplit(value, out string name, out string line)) return "expected MDS:Program:<name>:set,<settings> or add,<drill>.";
            if (!ProgramBook.IsName(name, out string error)) return error;

            bool known = _edits.Book.TryFind(name, out DrillProgram program);
            DrillProgram target = known ? program : new DrillProgram(name);
            string[] words = CommaParts.Split(line);
            string verb = words.Length > 0 ? words[0].ToLowerInvariant() : string.Empty;
            if (verb == "set") error = _edits.Set(target, words[1..], out change);
            else if (verb == "add") error = _edits.Add(target, words[1..], out change);
            else return $"'{line}' starts with set or add.";

            if (error.Length == 0 && !known) change = () => MakeAndApply(name, value);
            return error;
        }

        // The change checked against the empty stand-in would land on it, so the real program is made first and the line read again.
        private void MakeAndApply(string name, string value)
        {
            _edits.Book.Create(name);
            Check(value, out Action change);
            change();
        }
    }
}
