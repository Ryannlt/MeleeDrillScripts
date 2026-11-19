using System;
using System.Collections.Generic;
using MDS.Shared;

namespace MDS.Features.Programs
{
    // Programs by name, numbered in the order they were first made.
    // The mod reloads with every map, so a program made by rc lasts until the next map.
    public sealed class ProgramBook
    {
        // rc program stop would otherwise read as a program's name.
        private static readonly string[] Reserved = { "stop" };

        private readonly List<DrillProgram> _programs = new();

        public IReadOnlyList<DrillProgram> All => _programs;

        public static bool IsName(string name, out string error) => NameRules.TryCheck(name, Reserved, out error);

        public DrillProgram Create(string name)
        {
            var program = new DrillProgram(name);
            _programs.Add(program);
            return program;
        }

        public bool Delete(DrillProgram program) => _programs.Remove(program);

        // By its number in the list, or by name in any case.
        public bool TryFind(string nameOrNumber, out DrillProgram program)
        {
            bool numbered = int.TryParse(nameOrNumber, out int number) && number >= 1 && number <= _programs.Count;
            program = numbered ? _programs[number - 1] : _programs.Find(candidate => candidate.Name.Equals(nameOrNumber, StringComparison.OrdinalIgnoreCase));
            return program != null;
        }
    }

    // The changes rc program and the config make to one program.
    // Each returns why it can't, or hands back the change to make, so checking and doing never drift apart.
    public sealed class ProgramEdits
    {
        private const string RenameKey = "name=";

        // The drill's number and where it goes.
        private const int MoveArgs = 2;

        private readonly ProgramBook _book;
        private readonly ProgramLineParser _parser;

        public ProgramEdits(ProgramBook book, ProgramLineParser parser)
        {
            _book = book;
            _parser = parser;
        }

        public ProgramBook Book => _book;

        public string Set(DrillProgram program, string[] words, out Action change)
        {
            change = null;
            string rename = Array.Find(words, word => word.StartsWith(RenameKey, StringComparison.OrdinalIgnoreCase));
            string newName = rename?.Substring(RenameKey.Length);
            if (rename != null && !CanRename(program, newName, out string error)) return error;

            ProgramSettings settings = program.Settings.Copy();
            error = ProgramLineParser.ReadSettings(Array.FindAll(words, word => word != rename), settings);
            if (error.Length == 0) change = () => (program.Settings, program.Name) = (settings, newName ?? program.Name);
            return error;
        }

        public string Add(DrillProgram program, string[] words, out Action change)
        {
            change = null;
            if (!_parser.TryParseDrill(words, out ProgramDrill drill, out string error)) return error;

            change = () => program.Drills.Add(drill);
            return string.Empty;
        }

        public string Edit(DrillProgram program, string[] args, out Action change)
        {
            change = null;
            string error = Numbered(program, args, out int index);
            if (error.Length > 0) return error;
            if (!_parser.TryParseDrill(args[1..], out ProgramDrill drill, out error)) return error;

            change = () => program.Drills[index] = drill;
            return string.Empty;
        }

        public string Move(DrillProgram program, string[] args, out Action change)
        {
            change = null;
            string error = Numbered(program, args, out int from);
            if (error.Length == 0) error = args.Length == MoveArgs ? Numbered(program, args[1..], out _) : "Usage: rc program <name> move <drill number> <new number>";
            if (error.Length > 0) return error;

            int to = int.Parse(args[1]) - 1;
            change = () => MoveDrill(program.Drills, from, to);
            return string.Empty;
        }

        public string Remove(DrillProgram program, string[] args, out Action change)
        {
            change = null;
            string error = Numbered(program, args, out int index);
            if (error.Length == 0) change = () => program.Drills.RemoveAt(index);
            return error;
        }

        private bool CanRename(DrillProgram program, string name, out string error)
        {
            if (!ProgramBook.IsName(name, out error)) return false;
            if (_book.TryFind(name, out DrillProgram other) && other != program) error = $"There is already a program called {other.Name}.";
            return error.Length == 0;
        }

        private static string Numbered(DrillProgram program, string[] args, out int index)
        {
            index = -1;
            bool read = args.Length > 0 && int.TryParse(args[0], out int number) && number >= 1 && number <= program.Drills.Count;
            if (read) index = int.Parse(args[0]) - 1;
            return read ? string.Empty : $"{program.Name} has drills 1 to {program.Drills.Count}, not '{(args.Length > 0 ? args[0] : string.Empty)}'.";
        }

        private static void MoveDrill(List<ProgramDrill> drills, int from, int to)
        {
            ProgramDrill drill = drills[from];
            drills.RemoveAt(from);
            drills.Insert(to, drill);
        }
    }
}
