using System;
using System.Collections.Generic;
using MDS.Shared;
using MDS.Shared.Commands;
using MDS.Shared.Game;
using MDS.Shared.Players;

namespace MDS.Features.Status
{
    public sealed class MdsCommand : IConsoleCommand
    {
        public const string Usage = "Usage: rc mds [players [all|attacking|defending|spectator] [count] | player <id> | round]";

        private const string CountOnly = "count";

        // A group, then "count".
        private const int GroupAndCount = 2;

        private readonly StatusReport _report;
        private readonly PlayerRegistry _players;
        private readonly Replies _replies;

        public MdsCommand(StatusReport report, PlayerRegistry players, Replies replies)
        {
            _report = report;
            _players = players;
            _replies = replies;
        }

        private enum Topic
        {
            Players,
            Player,
            Round
        }

        public string Name => "mds";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            bool valid = parameters.Length == 0 || (EnumParser.TryParseStrict(parameters[0], out Topic topic) && IsValid(topic, parameters[1..]));
            errorMessage = valid ? string.Empty : Usage;
            return valid;
        }

        public void Execute(int playerId, string[] parameters)
        {
            if (parameters.Length == 0)
            {
                _replies.Send(playerId, _report.Summary());
                return;
            }

            EnumParser.TryParseStrict(parameters[0], out Topic topic);
            if (topic == Topic.Players) ListPlayers(playerId, parameters[1..]);
            else if (topic == Topic.Player) DescribePlayer(playerId, int.Parse(parameters[1]));
            else _replies.Send(playerId, _report.Round());
        }

        private static bool IsValid(Topic topic, string[] args)
        {
            switch (topic)
            {
                case Topic.Players:
                    return args.Length == 0
                        || (EnumParser.TryParseStrict(args[0], out PlayerGroup _) && (args.Length == 1 || IsCountOnly(args)));
                case Topic.Player:
                    return args.Length == 1 && int.TryParse(args[0], out _);
                default:
                    return args.Length == 0;
            }
        }

        private static bool IsCountOnly(string[] args) => args.Length == GroupAndCount && args[1].Equals(CountOnly, StringComparison.OrdinalIgnoreCase);

        // The full list goes to the F2 client log so a big server doesn't flood the screen.
        private void ListPlayers(int playerId, string[] args)
        {
            PlayerGroup group = PlayerGroup.All;
            if (args.Length > 0) EnumParser.TryParseStrict(args[0], out group);
            IReadOnlyList<IPlayer> players = _report.Group(group);
            if (IsCountOnly(args))
            {
                _replies.Send(playerId, $"{group}: {players.Count} players.");
                return;
            }

            foreach (IPlayer player in players) _replies.LogOnClient(playerId, _report.Describe(player));
            _replies.Send(playerId, $"{group}: {players.Count} players, listed in your F2 log.");
        }

        private void DescribePlayer(int playerId, int targetId)
        {
            IPlayer target = _players.Find(targetId);
            _replies.Send(playerId, target == null ? $"No player {targetId} is tracked." : _report.Describe(target));
        }
    }
}
