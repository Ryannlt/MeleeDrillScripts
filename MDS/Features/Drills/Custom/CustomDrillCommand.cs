using System.Collections.Generic;
using MDS.Shared.Commands;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Custom
{
    // rc drill alone lists the custom drills, add, remove and show change or read one, and anything else runs one.
    public sealed class CustomDrillCommand : IConsoleCommand
    {
        public const string AddUsage = "Usage: rc drill add <name> " + DrillLineParser.GroupFormat + ", or rc drill add <name> strategy <random|any|next|repeat>, or rc drill add <name> turn <degrees|random>";

        private const string AddWord = "add";
        private const string RemoveWord = "remove";
        private const string ShowWord = "show";

        // add, the name, and at least one word of the line.
        private const int AddParts = 3;
        private const int LineStart = 2;

        // remove or show, and the name.
        private const int NameParts = 2;

        private readonly CustomDrillBook _book;
        private readonly DrillCommand _run;
        private readonly Replies _replies;

        public CustomDrillCommand(CustomDrillBook book, DrillCommand run, Replies replies)
        {
            _book = book;
            _run = run;
            _replies = replies;
        }

        public string Name => _run.Name;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            string verb = VerbOf(parameters);
            if (parameters.Length == 0) return true;
            if (verb == AddWord) return CanAdd(parameters, out errorMessage);
            if (verb == RemoveWord || verb == ShowWord) return CanFind(parameters, out errorMessage);
            return _run.Validate(parameters, out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            string verb = VerbOf(parameters);
            if (parameters.Length == 0) _replies.Send(playerId, List());
            else if (verb == AddWord) _replies.Send(playerId, Add(parameters));
            else if (verb == RemoveWord) _replies.Send(playerId, Remove(parameters[1]));
            else if (verb == ShowWord) _replies.Send(playerId, Show(parameters[1]));
            else _run.Execute(playerId, parameters);
        }

        private static string VerbOf(string[] parameters) => parameters.Length > 0 ? parameters[0].ToLowerInvariant() : string.Empty;

        private static string LineOf(string[] parameters) => string.Join(" ", parameters, LineStart, parameters.Length - LineStart);

        private bool CanAdd(string[] parameters, out string error)
        {
            error = AddUsage;
            return parameters.Length >= AddParts && _book.CanAdd(parameters[1], LineOf(parameters), out error);
        }

        private bool CanFind(string[] parameters, out string error)
        {
            error = string.Empty;
            if (parameters.Length != NameParts) error = $"Usage: rc drill {VerbOf(parameters)} <name|number>";
            else if (!_book.TryFind(parameters[1], out _)) error = $"No custom drill '{parameters[1]}'. rc drill lists them.";
            return error.Length == 0;
        }

        private string List()
        {
            if (_book.All.Count == 0) return "No custom drills yet. Add one with rc drill add <name> <group>, or an MDS:Drill config line.";

            var entries = new List<string>();
            for (int i = 0; i < _book.All.Count; i++) entries.Add($"{i + 1} {_book.All[i].Summary}");
            return $"Custom drills: {string.Join(", ", entries)}. Run one with rc drill <name|number>.";
        }

        private string Add(string[] parameters)
        {
            _book.TryAdd(parameters[1], LineOf(parameters), out _);
            return $"Added. {Show(parameters[1])}";
        }

        private string Remove(string nameOrNumber)
        {
            _book.TryFind(nameOrNumber, out CustomDrill drill);
            _book.Remove(nameOrNumber);
            return $"Removed {drill.Name}. A drill from the config comes back with the next map.";
        }

        private string Show(string nameOrNumber)
        {
            _book.TryFind(nameOrNumber, out CustomDrill drill);
            return $"{drill.Summary}: {string.Join(" | ", drill.Lines())}";
        }
    }
}
