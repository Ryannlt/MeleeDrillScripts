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
            if (TrySplit(value, out string name, out string line)) return _book.CanAdd(name, line, out reason);

            reason = "expected MDS:CustomDrill:<name>:<line>.";
            return false;
        }

        public void Apply(string value)
        {
            TrySplit(value, out string name, out string line);
            _book.TryAdd(name, line, out _);
        }

        private static bool TrySplit(string value, out string name, out string line)
        {
            int colon = (value ?? string.Empty).IndexOf(':');
            name = colon > 0 ? value.Substring(0, colon) : string.Empty;
            line = colon > 0 ? value.Substring(colon + 1) : string.Empty;
            return colon > 0;
        }
    }
}
