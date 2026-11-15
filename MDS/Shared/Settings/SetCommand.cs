using MDS.Shared.Commands;

namespace MDS.Shared.Settings
{
    public sealed class SetCommand : IConsoleCommand
    {
        private readonly SettingsRegistry _settings;

        public SetCommand(SettingsRegistry settings)
        {
            _settings = settings;
        }

        public string Name => "set";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            return _settings.TryFind(parameters, out ISetting setting, out string[] args)
                && setting.ValidateSet(args, out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            if (_settings.TryFind(parameters, out ISetting setting, out string[] args)) setting.Set(playerId, args);
        }
    }
}
