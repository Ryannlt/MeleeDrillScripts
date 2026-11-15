using MDS.Shared.Commands;

namespace MDS.Shared.Settings
{
    public sealed class GetCommand : IConsoleCommand
    {
        private readonly SettingsRegistry _settings;

        public GetCommand(SettingsRegistry settings)
        {
            _settings = settings;
        }

        public string Name => "get";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            return _settings.TryFind(parameters, out ISetting setting, out string[] args)
                && setting.ValidateGet(args, out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            if (_settings.TryFind(parameters, out ISetting setting, out string[] args)) setting.Get(playerId, args);
        }
    }
}
