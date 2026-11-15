using MDS.Shared.Config;

namespace MDS.Shared.Logging
{
    // MDS:EnableDebugLogging:true in a server config turns debug lines on from map load.
    public sealed class DebugLoggingVariable : IConfigVariable
    {
        private readonly ModLog _log;

        public DebugLoggingVariable(ModLog log)
        {
            _log = log;
        }

        public string Key => "EnableDebugLogging";

        public bool Validate(string value) => bool.TryParse(value, out _);

        public void Apply(string value)
        {
            _log.DebugEnabled = bool.Parse(value);
            _log.Debug("Debug logging enabled.");
        }
    }
}
