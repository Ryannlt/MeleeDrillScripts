using System;
using System.Collections.Generic;
using MDS.Shared.Commands;
using MDS.Shared.Game;

namespace MDS.Features.Programs
{
    // rc program alone lists the programs, rc program stop stops the running one, and rc program <name> <verb> does the rest.
    // A name alone shows the program.
    public sealed class ProgramCommand : IConsoleCommand
    {
        public const string Usage = "Usage: rc program [stop | <name> [create|set|add|edit|move|remove|try|run|delete] ...]";

        private const string StopWord = "stop";
        private const string CreateWord = "create";
        private const string RunWord = "run";
        private const string TryWord = "try";
        private const string DeleteWord = "delete";

        private delegate string EditVerb(DrillProgram program, string[] args, out Action change);

        private readonly ProgramBook _book;
        private readonly ProgramRunner _runner;
        private readonly Replies _replies;
        private readonly Dictionary<string, EditVerb> _edits;

        public ProgramCommand(ProgramEdits edits, ProgramRunner runner, Replies replies)
        {
            _book = edits.Book;
            _runner = runner;
            _replies = replies;
            _edits = new Dictionary<string, EditVerb>(StringComparer.OrdinalIgnoreCase)
            {
                ["set"] = edits.Set, ["add"] = edits.Add, ["edit"] = edits.Edit, ["move"] = edits.Move, ["remove"] = edits.Remove,
            };
        }

        public string Name => "program";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = Check(parameters, out _);
            return errorMessage.Length == 0;
        }

        public void Execute(int playerId, string[] parameters)
        {
            Check(parameters, out Func<int, string> act);
            _replies.Send(playerId, act(playerId));
        }

        // Why the command can't run, or what running it does.
        private string Check(string[] parameters, out Func<int, string> act)
        {
            act = null;
            if (parameters.Length == 0) return Done(out act, _ => Overview());
            if (parameters.Length == 1 && Is(parameters[0], StopWord)) return Done(out act, _ => Stop());

            var words = new NamedVerb(parameters);
            if (Is(words.Verb, CreateWord)) return CheckCreate(words.Name, words.Args, out act);
            if (!_book.TryFind(words.Name, out DrillProgram program)) return $"No program '{words.Name}'. rc program lists them, and rc program {words.Name} create makes it.";

            return CheckVerb(program, words, out act);
        }

        private string CheckVerb(DrillProgram program, NamedVerb words, out Func<int, string> act)
        {
            act = null;
            string verb = words.Verb;
            string[] args = words.Args;
            if (verb.Length == 0) return Done(out act, _ => Show(program));
            if (Is(verb, DeleteWord)) return Done(out act, _ => Delete(program));
            if (Is(verb, RunWord)) return CheckRun(program.Plan(), args, out act);
            if (Is(verb, TryWord)) return CheckTry(program, args, out act);
            if (!_edits.TryGetValue(verb, out EditVerb edit)) return $"'{verb}' isn't something rc program does. {Usage}";

            string error = edit(program, args, out Action change);
            if (error.Length == 0) act = _ => Changed(change, program);
            return error;
        }

        private string CheckCreate(string name, string[] args, out Func<int, string> act)
        {
            act = null;
            if (!ProgramBook.IsName(name, out string error)) return error;
            if (_book.TryFind(name, out DrillProgram existing)) return $"{existing.Name} already exists. Change it with rc program {existing.Name} set.";

            var settings = new ProgramSettings();
            error = ProgramLineParser.ReadSettings(args, settings);
            if (error.Length == 0) act = _ => Created(name, settings);
            return error;
        }

        private string CheckRun(ProgramPlan plan, string[] args, out Func<int, string> act)
        {
            act = null;
            if (args.Length > 1 || (args.Length == 1 && !Is(args[0], ProgramLineParser.LoopWord))) return "Usage: rc program <name> run [loop]";
            if (_runner.Current != null) return $"{_runner.Current.Name} is running. Stop it first with rc program stop.";

            string error = _runner.Refusal(plan);
            plan.Loops |= args.Length == 1;
            if (error.Length == 0) act = starter => Started(plan, starter);
            return error;
        }

        private string CheckTry(DrillProgram program, string[] args, out Func<int, string> act)
        {
            act = null;
            string error = args.Length == 1 && int.TryParse(args[0], out int number) && number >= 1 && number <= program.Drills.Count
                ? string.Empty : $"Usage: rc program {program.Name} try <drill number, 1 to {program.Drills.Count}>";
            return error.Length > 0 ? error : CheckRun(program.TryPlan(int.Parse(args[0]) - 1), new string[0], out act);
        }

        private static string Done(out Func<int, string> act, Func<int, string> reply)
        {
            act = reply;
            return string.Empty;
        }

        private static bool Is(string word, string expected) => word.Equals(expected, StringComparison.OrdinalIgnoreCase);

        private string Overview()
        {
            if (_runner.Current != null) return _runner.Current.Status;
            if (_book.All.Count == 0) return "No programs yet. Make one with rc program <name> create, or an MDS:Program config line.";

            var entries = new List<string>();
            for (int i = 0; i < _book.All.Count; i++) entries.Add($"{i + 1} {_book.All[i].Summary}");
            return $"Programs: {string.Join(", ", entries)}. Run one with rc program <name> run.";
        }

        private string Stop()
        {
            if (_runner.Current == null) return "No program is running.";

            string name = _runner.Current.Name;
            _runner.Stop();
            return $"Stopped {name}. Fights already running carry on.";
        }

        private string Created(string name, ProgramSettings settings)
        {
            DrillProgram program = _book.Create(name);
            program.Settings = settings;
            return $"Made {program.Name}. Add drills with rc program {program.Name} add <drill>.";
        }

        private string Started(ProgramPlan plan, int starter)
        {
            _runner.Start(plan, starter);
            return $"Starting {plan.Name}{(plan.Loops ? " on a loop" : string.Empty)}.";
        }

        private string Changed(Action change, DrillProgram program)
        {
            change();
            return $"Done. {Show(program)}";
        }

        private string Delete(DrillProgram program)
        {
            _book.Delete(program);
            return $"Deleted {program.Name}. A program from the config comes back with the next map.";
        }

        private static string Show(DrillProgram program)
        {
            List<string> lines = program.Lines();
            return lines.Count == 0 ? $"{program.Summary}: nothing yet." : $"{program.Summary}: {string.Join(" | ", lines)}";
        }
    }
}
