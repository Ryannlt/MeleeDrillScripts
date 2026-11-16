using MDS.Shared;
using MDS.Shared.Config;

namespace MDS.Features.Drills.Custom
{
    // MDS:CustomDrill:<name>:<line>, one line per spot, formation or setting, so lines with the same name build one drill.
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
            if (NameRules.TrySplit(value, out string name, out string line)) return _book.CanAdd(name, line, out reason);

            reason = "expected MDS:CustomDrill:<name>:<line>.";
            return false;
        }

        public void Apply(string value)
        {
            NameRules.TrySplit(value, out string name, out string line);
            _book.TryAdd(name, line, out _);
        }
    }
}
