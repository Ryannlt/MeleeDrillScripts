using System;
using System.Collections.Generic;
using MDS.Shared.Logging;

namespace MDS.Shared.Config
{
    // The game hands every mod every line, so lines with another prefix are skipped without a word.
    // The value keeps any further colons, so URLs survive.
    public sealed class ConfigRouter
    {
        private const int PartCount = 3;
        private const int KeyPart = 1;
        private const int ValuePart = 2;

        private readonly Dictionary<string, IConfigVariable> _variables = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _prefix;
        private readonly ModLog _log;

        public ConfigRouter(string prefix, ModLog log)
        {
            _prefix = prefix;
            _log = log;
        }

        public IEnumerable<string> Keys => _variables.Keys;

        public void Register(IConfigVariable variable)
        {
            if (_variables.ContainsKey(variable.Key)) _log.Warning($"Config variable '{variable.Key}' is already registered.");
            else _variables[variable.Key] = variable;
        }

        public void Apply(string[] entries)
        {
            if (entries == null) return;

            foreach (string entry in entries)
            {
                if (TryParse(entry, out IConfigVariable variable, out string value)) ApplyOne(variable, value);
            }
        }

        private bool TryParse(string entry, out IConfigVariable variable, out string value)
        {
            variable = null;
            string[] parts = (entry ?? string.Empty).Split(new[] { ':' }, PartCount);
            value = parts.Length > ValuePart ? parts[ValuePart] : string.Empty;
            if (parts.Length <= KeyPart || !parts[0].Equals(_prefix, StringComparison.OrdinalIgnoreCase)) return false;
            if (_variables.TryGetValue(parts[KeyPart], out variable)) return true;

            _log.Warning($"Unknown config variable: {_prefix}:{parts[KeyPart]}");
            return false;
        }

        private void ApplyOne(IConfigVariable variable, string value)
        {
            if (variable.Validate(value, out string reason)) variable.Apply(value);
            else _log.Warning($"Invalid value '{value}' for {_prefix}:{variable.Key}{(reason.Length > 0 ? $": {reason}" : string.Empty)}");
        }
    }
}
