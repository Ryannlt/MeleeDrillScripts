using System.Collections.Generic;
using MDS.Shared.Commands;
using MDS.Shared.Game;

namespace MDS.Features.Panel
{
    // Panel text goes through the same router and permission check as a typed rc.
    // A staged Apply sends several lines in one request, joined with ';', because the server drops bursts of messages.
    public sealed class PanelDoor
    {
        private const char Separator = ';';
        private const string Done = "Done.";

        private readonly CommandRouter _commands;
        private readonly Replies _replies;

        public PanelDoor(CommandRouter commands, Replies replies)
        {
            _commands = commands;
            _replies = replies;
        }

        public bool Run(int playerId, string text, out string reply)
        {
            var answers = new List<string>();
            bool ok = true;
            foreach (string part in text.Split(Separator))
            {
                string command = part.Trim();
                if (command.Length == 0) continue;
                ok &= RunOne(playerId, command, answers);
            }
            reply = answers.Count > 0 ? string.Join(" ", answers) : Done;
            return ok;
        }

        private bool RunOne(int playerId, string command, List<string> answers)
        {
            HandleResult result = HandleResult.NotMine;
            string said = _replies.Capture(playerId, () => result = _commands.Handle(playerId, command));
            if (result == HandleResult.NotMine)
            {
                answers.Add($"MDS has no command '{command.Split(' ')[0]}'.");
                return false;
            }
            answers.Add(said.Length > 0 ? said : Done);
            return result == HandleResult.Ran;
        }
    }
}
