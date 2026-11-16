using System.Collections.Generic;
using MDS.Shared.Commands;
using MDS.Shared.Game;

namespace MDS.Features.Programs
{
    // rc program alone lists the programs, or says how the running one is doing; run, stop, add, remove and show do the rest.
    public sealed class ProgramCommand : IConsoleCommand
    {
        public const string Usage = "Usage: rc program [run <name|number> [loop] | stop | add <name> <line> | show <name|number> | remove <name|number>]";

        private const string RunWord = "run";
        private const string StopWord = "stop";
        private const string AddWord = "add";
        private const string RemoveWord = "remove";
        private const string ShowWord = "show";
        private const string LoopWord = "loop";

        // add, the name, and at least one word of the line.
        private const int AddParts = 3;
        private const int LineStart = 2;

        // A verb and a name, and for run an optional loop.
        private const int NameParts = 2;
        private const int RunParts = 3;

        private readonly ProgramBook _book;
        private readonly ProgramRunner _runner;
        private readonly Replies _replies;

        public ProgramCommand(ProgramBook book, ProgramRunner runner, Replies replies)
        {
            _book = book;
            _runner = runner;
            _replies = replies;
        }

        public string Name => "program";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            string verb = VerbOf(parameters);
            if (parameters.Length == 0 || (verb == StopWord && parameters.Length == 1)) return true;
            if (verb == RunWord) return CanRun(parameters, out errorMessage);
            if (verb == AddWord) return CanAdd(parameters, out errorMessage);
            if ((verb == RemoveWord || verb == ShowWord) && parameters.Length == NameParts) return CanFind(parameters[1], out errorMessage);

            errorMessage = Usage;
            return false;
        }

        public void Execute(int playerId, string[] parameters)
        {
            string verb = VerbOf(parameters);
            if (parameters.Length == 0) _replies.Send(playerId, Overview());
            else if (verb == RunWord) _replies.Send(playerId, Run(parameters, playerId));
            else if (verb == StopWord) _replies.Send(playerId, Stop());
            else if (verb == AddWord) _replies.Send(playerId, Add(parameters));
            else if (verb == RemoveWord) _replies.Send(playerId, Remove(parameters[1]));
            else _replies.Send(playerId, Show(parameters[1]));
        }

        private static string VerbOf(string[] parameters) => parameters.Length > 0 ? parameters[0].ToLowerInvariant() : string.Empty;

        private static string LineOf(string[] parameters) => string.Join(" ", parameters, LineStart, parameters.Length - LineStart);

        private static bool Loops(string[] parameters) => parameters.Length == RunParts && parameters[RunParts - 1].ToLowerInvariant() == LoopWord;

        private bool CanRun(string[] parameters, out string error)
        {
            error = string.Empty;
            if (parameters.Length < NameParts || parameters.Length > RunParts || (parameters.Length == RunParts && !Loops(parameters))) error = "Usage: rc program run <name|number> [loop]";
            else if (!_book.TryFind(parameters[1], out DrillProgram program)) error = $"No program '{parameters[1]}'. rc program lists them.";
            else if (_runner.Current != null) error = $"{_runner.Current.Name} is running. Stop it first with rc program stop.";
            else error = _runner.Refusal(program);
            return error.Length == 0;
        }

        private bool CanAdd(string[] parameters, out string error)
        {
            error = $"Usage: rc program add <name> <line>, where a line is one of: {ProgramLineParser.Format}";
            return parameters.Length >= AddParts && _book.CanAdd(parameters[1], LineOf(parameters), out error);
        }

        private bool CanFind(string nameOrNumber, out string error)
        {
            error = _book.TryFind(nameOrNumber, out _) ? string.Empty : $"No program '{nameOrNumber}'. rc program lists them.";
            return error.Length == 0;
        }

        private string Overview()
        {
            if (_runner.Current != null) return _runner.Current.Status;
            if (_book.All.Count == 0) return "No programs yet. Add one with rc program add <name> <line>, or an MDS:Program config line.";

            var entries = new List<string>();
            for (int i = 0; i < _book.All.Count; i++) entries.Add($"{i + 1} {_book.All[i].Summary}");
            return $"Programs: {string.Join(", ", entries)}. Run one with rc program run <name|number>.";
        }

        private string Run(string[] parameters, int playerId)
        {
            _book.TryFind(parameters[1], out DrillProgram program);
            _runner.Start(program, Loops(parameters), playerId);
            return $"Starting {program.Name}{(Loops(parameters) ? " on a loop" : string.Empty)}.";
        }

        private string Stop()
        {
            if (_runner.Current == null) return "No program is running.";

            string name = _runner.Current.Name;
            _runner.Stop();
            return $"Stopped {name}. Fights already running carry on.";
        }

        private string Add(string[] parameters)
        {
            _book.TryAdd(parameters[1], LineOf(parameters), out _);
            return $"Added. {Show(parameters[1])}";
        }

        private string Remove(string nameOrNumber)
        {
            _book.TryFind(nameOrNumber, out DrillProgram program);
            _book.Remove(nameOrNumber);
            return $"Removed {program.Name}. A program from the config comes back with the next map.";
        }

        private string Show(string nameOrNumber)
        {
            _book.TryFind(nameOrNumber, out DrillProgram program);
            return $"{program.Summary}: {string.Join(" | ", program.Lines())}";
        }
    }
}
