using System;
using System.Collections.Generic;
using MDS.Shared.Game;
using MDS.Shared.Logging;
using MDS.Shared.Players;

namespace MDS.Shared.Commands
{
    public enum HandleResult
    {
        NotMine,
        Refused,
        Ran,
    }

    // A command that isn't this mod's passes silently, which is what lets several mods share the console.
    // Every entry point goes through Handle, so the admin check here covers rc today and a UI later.
    public sealed class CommandRouter
    {
        public const string AdminsOnly = "Only logged-in admins can run this.";

        private readonly Dictionary<string, IConsoleCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
        private readonly Replies _replies;
        private readonly AdminRegistry _admins;
        private readonly ModLog _log;

        public CommandRouter(Replies replies, AdminRegistry admins, ModLog log)
        {
            _replies = replies;
            _admins = admins;
            _log = log;
        }

        public IEnumerable<string> Names => _commands.Keys;

        public void Register(IConsoleCommand command)
        {
            if (_commands.ContainsKey(command.Name))
            {
                _log.Warning($"Command '{command.Name}' is already registered.");
                return;
            }

            _commands[command.Name] = command;
        }

        // The result lets the panel's door tell a refusal from a command that ran; rc ignores it.
        public HandleResult Handle(int playerId, string input)
        {
            if (!TryFind(input, out IConsoleCommand command, out string[] parameters)) return HandleResult.NotMine;

            if (!command.Validate(parameters, out string errorMessage)) return Refuse(playerId, command, errorMessage);
            if (!_admins.IsAdmin(playerId)) return Refuse(playerId, command, AdminsOnly);

            command.Execute(playerId, parameters);
            return HandleResult.Ran;
        }

        // An empty message means the command turned out not to be this mod's, as with another mod's rc get key.
        private HandleResult Refuse(int playerId, IConsoleCommand command, string message)
        {
            if (string.IsNullOrEmpty(message)) return HandleResult.NotMine;

            _log.Warning($"'{command.Name}' from player {playerId} refused: {message}");
            _replies.Send(playerId, message);
            return HandleResult.Refused;
        }

        private bool TryFind(string input, out IConsoleCommand command, out string[] parameters)
        {
            command = null;
            parameters = Array.Empty<string>();
            string[] parts = (input ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !_commands.TryGetValue(parts[0], out command)) return false;

            parameters = parts[1..];
            return true;
        }
    }
}
