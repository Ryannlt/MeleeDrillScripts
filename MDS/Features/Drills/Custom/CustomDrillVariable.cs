using MDS.Shared;
using MDS.Shared.Config;

namespace MDS.Features.Drills.Custom
{
    // MDS:CustomDrill:<name>:<line>, one line per spot, formation or setting, so lines with the same name build one drill.
    // A line may start with add, as rc customdrill <name> add takes it.
    public sealed class CustomDrillVariable : IConfigVariable
    {
        private readonly CustomDrillBook _book;

        public CustomDrillVariable(CustomDrillBook book)
        {
            _book = book;
        }

        public string Key => "CustomDrill";

        public bool Validate(string value, out string reason)
        {
            if (NameRules.TrySplit(value, out string name, out string line)) return _book.CanAdd(name, WithoutAdd(line), out reason);

            reason = "expected MDS:CustomDrill:<name>:<line>.";
            return false;
        }

        public void Apply(string value)
        {
            NameRules.TrySplit(value, out string name, out string line);
            _book.TryAdd(name, WithoutAdd(line), out _);
        }

        private static string WithoutAdd(string line) => line.StartsWith("add,", System.StringComparison.OrdinalIgnoreCase) ? line.Substring("add,".Length) : line;
    }
}
