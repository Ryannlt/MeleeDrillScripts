using System;
using System.Collections.Generic;
using MDS.Shared.Logging;

namespace MDS.Shared.Settings
{
    public sealed class SettingsRegistry
    {
        private readonly Dictionary<string, ISetting> _settings = new(StringComparer.OrdinalIgnoreCase);
        private readonly ModLog _log;

        public SettingsRegistry(ModLog log)
        {
            _log = log;
        }

        public IEnumerable<string> Keys => _settings.Keys;

        public void Register(ISetting setting)
        {
            if (_settings.ContainsKey(setting.Key))
            {
                _log.Warning($"Setting '{setting.Key}' is already registered.");
                return;
            }

            _settings[setting.Key] = setting;
        }

        // Finds the setting named by the first rc get/set parameter and hands back the rest as its arguments. A key
        // that isn't this mod's belongs to the game or another mod, so it fails without a message.
        public bool TryFind(string[] parameters, out ISetting setting, out string[] args)
        {
            setting = null;
            args = parameters.Length > 1 ? parameters[1..] : Array.Empty<string>();
            return parameters.Length > 0 && _settings.TryGetValue(parameters[0], out setting);
        }
    }
}
