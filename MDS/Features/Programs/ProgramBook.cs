using System;
using System.Collections.Generic;
using MDS.Shared;

namespace MDS.Features.Programs
{
    // Programs by name, numbered in the order they were first made.
    // The mod reloads with every map, so a program added by rc lasts until the next map.
    public sealed class ProgramBook
    {
        private static readonly string[] Reserved = { "run", "stop", "add", "remove", "show", "loop" };

        private readonly List<DrillProgram> _programs = new();
        private readonly ProgramLineParser _parser;

        public ProgramBook(ProgramLineParser parser)
        {
            _parser = parser;
        }

        public IReadOnlyList<DrillProgram> All => _programs;

        public bool CanAdd(string name, string text, out string error) => Parsed(name, text, out error) != null;

        // Makes the program when the name is new.
        public bool TryAdd(string name, string text, out string error)
        {
            Action<DrillProgram> apply = Parsed(name, text, out error);
            if (apply == null) return false;
            if (!TryFind(name, out DrillProgram program)) _programs.Add(program = new DrillProgram(name));

            apply(program);
            return true;
        }

        public bool Remove(string nameOrNumber) => TryFind(nameOrNumber, out DrillProgram program) && _programs.Remove(program);

        // By its number in the list, or by name in any case.
        public bool TryFind(string nameOrNumber, out DrillProgram program)
        {
            bool numbered = int.TryParse(nameOrNumber, out int number) && number >= 1 && number <= _programs.Count;
            program = numbered ? _programs[number - 1] : _programs.Find(candidate => candidate.Name.Equals(nameOrNumber, StringComparison.OrdinalIgnoreCase));
            return program != null;
        }

        private Action<DrillProgram> Parsed(string name, string text, out string error)
        {
            return NameRules.TryCheck(name, Reserved, out error) && _parser.TryParse(text, out Action<DrillProgram> apply, out error) ? apply : null;
        }
    }
}
