using System;
using System.Collections.Generic;
using MDS.Shared.Commands;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Custom
{
    // rc customdrill alone lists the custom drills, rc customdrill <name> <verb> makes or changes one, and a name with anything else runs it.
    public sealed class CustomDrillCommand : IConsoleCommand
    {
        public const string Usage = "Usage: rc customdrill [<name> [strategy] [mirror] [arena] | <name> create|set|add|edit|remove|show|delete ...]";

        public const string PartFormat = "attacker <(x,z)|here> | defender <(x,z)|here> | line <attacker|defender> <(x,z)|here> [spacing] [facing] | <scatter|spread|groups|teams|random> [spacing] [offset] [facing]";

        private static readonly string[] Verbs = { "create", "set", "add", "edit", "remove", "show", "delete" };

        private readonly CustomDrillBook _book;
        private readonly DrillCommand _run;
        private readonly Replies _replies;
        private readonly StandingPosition _standing;

        public CustomDrillCommand(CustomDrillBook book, DrillCommand run, DrillKit kit)
        {
            _book = book;
            _run = run;
            _replies = kit.Replies;
            _standing = new StandingPosition(kit.Arenas, kit.Queries);
        }

        public string Name => _run.Name;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = Check(parameters, out _);
            return errorMessage.Length == 0;
        }

        public void Execute(int playerId, string[] parameters)
        {
            Check(parameters, out Func<int, string> act);
            string reply = act(playerId);
            if (reply.Length > 0) _replies.Send(playerId, reply);
        }

        // Why the command can't run, or what running it does; running a drill replies through the drill command itself.
        private string Check(string[] parameters, out Func<int, string> act)
        {
            act = null;
            if (parameters.Length == 0) return Done(out act, _ => List());

            var words = new NamedVerb(parameters);
            if (words.Verb == "create") return CheckCreate(words.Name, words.Args, out act);
            if (Array.IndexOf(Verbs, words.Verb) < 0) return CheckRun(parameters, out act);
            if (!_book.TryFind(words.Name, out CustomDrill drill)) return $"No custom drill '{words.Name}'. rc customdrill lists them, and rc customdrill {words.Name} create makes it.";

            return CheckVerb(drill, words, out act);
        }

        private string CheckVerb(CustomDrill drill, NamedVerb words, out Func<int, string> act)
        {
            act = null;
            string[] args = words.Args;
            if (words.Verb == "show") return Done(out act, _ => Show(drill));
            if (words.Verb == "delete") return Done(out act, _ => Delete(drill));
            if (words.Verb == "set") return CheckSet(drill, args, out act);

            int index = -1;
            string error = words.Verb == "add" ? string.Empty : Numbered(drill, args, out index);
            if (error.Length > 0) return error;
            if (words.Verb == "remove") return Done(out act, _ => Changed(drill, () => drill.RemovePart(index)));

            error = PartText(words.Verb == "add" ? args : args[1..], out string text);
            if (error.Length == 0) act = PlaceAct(drill, index, text);
            return error;
        }

        private string CheckRun(string[] parameters, out Func<int, string> act)
        {
            act = null;
            if (!_run.Validate(parameters, out string error)) return error;

            return Done(out act, starter => Ran(starter, parameters));
        }

        private string CheckCreate(string name, string[] args, out Func<int, string> act)
        {
            act = null;
            if (!CustomDrillBook.IsName(name, out string error)) return error;
            if (_book.TryFind(name, out CustomDrill existing)) return $"{existing.Name} already exists. Change it with rc customdrill {existing.Name} set.";

            error = SettingLines(args, out List<Action<CustomDrill>> settings);
            if (error.Length == 0) act = _ => Created(name, settings);
            return error;
        }

        private string CheckSet(CustomDrill drill, string[] args, out Func<int, string> act)
        {
            act = null;
            string error = SettingLines(args, out List<Action<CustomDrill>> settings);
            if (error.Length == 0) act = _ => Changed(drill, () => settings.ForEach(apply => apply(drill)));
            return error;
        }

        private static string PartText(string[] words, out string text)
        {
            text = string.Join(",", words);
            string check = StandingPosition.WithPlaceholder(text);
            if (!CustomLineParser.IsPart(check)) return $"'{text}' isn't a spot or a formation. A part reads: {PartFormat}";

            CustomLineParser.TryParse(check, out _, out string error);
            return error;
        }

        // "here" is read when the change is made, since only then is the admin's position known.
        private Func<int, string> PlaceAct(CustomDrill drill, int index, string text) => playerId =>
        {
            string line = _standing.Resolve(playerId, text, out string where);
            return line == null ? where : Changed(drill, () => Place(drill, index, line));
        };

        // A negative index adds the part at the end.
        private static void Place(CustomDrill drill, int index, string line)
        {
            if (index < 0) drill.AddPart(line);
            else drill.ReplacePart(index, line);
        }

        private static string SettingLines(string[] args, out List<Action<CustomDrill>> settings)
        {
            settings = new List<Action<CustomDrill>>();
            string error = CustomDrillSettings.Read(args, out List<string> lines);
            foreach (string line in lines)
            {
                if (error.Length > 0) break;
                if (CustomLineParser.TryParse(line, out Action<CustomDrill> apply, out error)) settings.Add(apply);
            }
            return error;
        }

        private static string Numbered(CustomDrill drill, string[] args, out int index)
        {
            index = args.Length > 0 && int.TryParse(args[0], out int number) && number >= 1 && number <= drill.Parts.Count ? number - 1 : -1;
            return index >= 0 ? string.Empty : $"{drill.Name} has parts 1 to {drill.Parts.Count}, not '{(args.Length > 0 ? args[0] : string.Empty)}'.";
        }

        private static string Done(out Func<int, string> act, Func<int, string> reply)
        {
            act = reply;
            return string.Empty;
        }

        private string List()
        {
            if (_book.All.Count == 0) return "No custom drills yet. Make one with rc customdrill <name> create, or an MDS:CustomDrill config line.";

            var entries = new List<string>();
            for (int i = 0; i < _book.All.Count; i++) entries.Add($"{i + 1} {_book.All[i].Summary}");
            return $"Custom drills: {string.Join(", ", entries)}. Run one with rc customdrill <name|number>.";
        }

        private string Ran(int starter, string[] parameters)
        {
            _run.Execute(starter, parameters);
            return string.Empty;
        }

        private string Created(string name, List<Action<CustomDrill>> settings)
        {
            CustomDrill drill = _book.Create(name);
            settings.ForEach(apply => apply(drill));
            return $"Made {drill.Name}. Add spots with rc customdrill {drill.Name} add attacker (x,z).";
        }

        private static string Changed(CustomDrill drill, Action change)
        {
            change();
            return $"Done. {Show(drill)}";
        }

        private string Delete(CustomDrill drill)
        {
            _book.Remove(drill.Name);
            return $"Deleted {drill.Name}. A drill from the config comes back with the next map.";
        }

        private static string Show(CustomDrill drill) => $"{drill.Summary}: {string.Join(" | ", drill.Lines())}";
    }
}
