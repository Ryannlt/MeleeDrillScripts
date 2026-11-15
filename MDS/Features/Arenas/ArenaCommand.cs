using MDS.Shared;
using MDS.Shared.Commands;
using MDS.Shared.Game;
using UnityEngine;

namespace MDS.Features.Arenas
{
    public sealed class ArenaCommand : IConsoleCommand
    {
        public const string Usage = "Usage: rc arena [corner1|corner2] [x z]";

        private readonly ArenaStore _store;
        private readonly PlayerQueries _queries;
        private readonly Replies _replies;

        public ArenaCommand(ArenaStore store, PlayerQueries queries, Replies replies)
        {
            _store = store;
            _queries = queries;
            _replies = replies;
        }

        public string Name => "arena";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            bool valid = parameters.Length == 0
                || (EnumParser.TryParseStrict(parameters[0], out ArenaCorner _)
                    && (parameters.Length == 1 || ArenaParser.TryParseCorner(parameters[1..], out _)));
            errorMessage = valid ? string.Empty : Usage;
            return valid;
        }

        public void Execute(int playerId, string[] parameters)
        {
            if (parameters.Length == 0)
            {
                _replies.Send(playerId, _store.Current == null ? ArenaStore.NotSet : $"The arena is {_store.Current}.");
                return;
            }

            EnumParser.TryParseStrict(parameters[0], out ArenaCorner corner);
            if (!TryGetPosition(playerId, parameters[1..], out Vector2 position))
            {
                _replies.Send(playerId, "Can't read your position: spawn in first, or give the corner as x z.");
                return;
            }

            _store.SetCorner(corner, position);
            _replies.Send(playerId, $"Arena {corner} set to {Arena.Format(position)}. The arena is {_store.Current}.");
        }

        private bool TryGetPosition(int playerId, string[] coordinates, out Vector2 position)
        {
            if (coordinates.Length > 0) return ArenaParser.TryParseCorner(coordinates, out position);

            bool found = _queries.TryGetPosition(playerId, out Vector3 standing);
            position = new Vector2(standing.x, standing.z);
            return found;
        }
    }
}
