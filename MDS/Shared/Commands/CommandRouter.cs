using System;
using System.Collections.Generic;
using MDS.Shared.Game;
using MDS.Shared.Logging;
using MDS.Shared.Players;

namespace MDS.Shared.Commands
{
    // Routes a command to the feature that registered it. A command that isn't this mod's (the game's own, or another
    // mod's) passes silently, which is what lets several mods share the console. Every entry point goes through
    // Handle, so the admin check here covers rc today and a UI later.
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

        public void Handle(int playerId, string input)
        {
            if (!TryFind(input, out IConsoleCommand command, out string[] parameters)) return;

            if (!command.Validate(parameters, out string errorMessage)) Refuse(playerId, command, errorMessage);
            else if (!_admins.IsAdmin(playerId)) Refuse(playerId, command, AdminsOnly);
            else command.Execute(playerId, parameters);
        }

        // An empty message means the command turned out not to be this mod's, as with another mod's rc get key.
        private void Refuse(int playerId, IConsoleCommand command, string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _log.Warning($"'{command.Name}' from player {playerId} refused: {message}");
            _replies.Send(playerId, message);
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
