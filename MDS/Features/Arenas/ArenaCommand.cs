using System;
using System.Collections.Generic;
using MDS.Shared;
using MDS.Shared.Commands;
using MDS.Shared.Game;
using UnityEngine;

namespace MDS.Features.Arenas
{
    public sealed class ArenaCommand : IConsoleCommand
    {
        public const string Usage = "Usage: rc arena [number] [corner1|corner2] [x z], or rc arena show [on|off]";

        private const string ShowWord = "show";

        // "show", then on or off.
        private const int ShowAndState = 2;

        private readonly ArenaStore _store;
        private readonly ArenaViewers _viewers;
        private readonly PlayerQueries _queries;
        private readonly Replies _replies;

        public ArenaCommand(ArenaStore store, ArenaViewers viewers, ModContext context)
        {
            _store = store;
            _viewers = viewers;
            _queries = context.Queries;
            _replies = context.Replies;
        }

        public string Name => "arena";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            bool valid = IsShow(parameters) ? IsShowRequest(parameters) : IsCornerRequest(parameters);
            errorMessage = valid ? string.Empty : Usage;
            return valid;
        }

        public void Execute(int playerId, string[] parameters)
        {
            if (IsShow(parameters)) Toggle(playerId, parameters[1..]);
            else ShowOrSetCorner(playerId, parameters);
        }

        private static bool IsShow(string[] parameters) => parameters.Length > 0 && parameters[0].Equals(ShowWord, StringComparison.OrdinalIgnoreCase);

        private static bool IsShowRequest(string[] parameters) => parameters.Length == 1 || (parameters.Length == ShowAndState && OnOff.TryParse(parameters[1], out _));

        private static bool IsCornerRequest(string[] parameters)
        {
            return TrySplitNumber(parameters, out _, out string[] rest)
                && (rest.Length == 0 || (EnumParser.TryParseStrict(rest[0], out ArenaCorner _) && (rest.Length == 1 || ArenaParser.TryParseCorner(rest[1..], out _))));
        }

        // The arena number is optional, and a bare corner means arena 1.
        private static bool TrySplitNumber(string[] parameters, out int number, out string[] rest)
        {
            number = ArenaStore.First;
            rest = parameters;
            if (parameters.Length == 0 || !int.TryParse(parameters[0], out _)) return true;

            rest = parameters[1..];
            return ArenaParser.TryParseNumber(parameters[0], out number);
        }

        private void Toggle(int playerId, string[] args)
        {
            bool on = args.Length == 0 ? !_viewers.IsShowing(playerId) : OnOff.TryParse(args[0], out bool given) && given;
            if (!on)
            {
                _viewers.Hide(playerId);
                _replies.Send(playerId, "Arena outlines hidden.");
                return;
            }

            _viewers.Show(playerId);
            _replies.Send(playerId, $"Showing the arena outlines to you only: {Legend()}.");
        }

        private string Legend()
        {
            var entries = new List<string>();
            foreach (Arena arena in _store.All) entries.Add($"arena {arena.Number} {ArenaColors.NameOf(arena.Number)}{(arena.IsUsable ? string.Empty : " (one corner so far)")}");
            return entries.Count == 0 ? "none is set yet, and each appears once it is" : string.Join(", ", entries);
        }

        private void ShowOrSetCorner(int playerId, string[] parameters)
        {
            TrySplitNumber(parameters, out int number, out string[] rest);
            if (rest.Length == 0)
            {
                _replies.Send(playerId, parameters.Length == 0 ? _store.Describe() : Describe(number));
                return;
            }

            EnumParser.TryParseStrict(rest[0], out ArenaCorner corner);
            if (!TryGetPosition(playerId, rest[1..], out Vector2 position))
            {
                _replies.Send(playerId, "Can't read your position: spawn in first, or give the corner as x z.");
                return;
            }

            _store.SetCorner(number, corner, position);
            _replies.Send(playerId, $"{corner} of arena {number} set to {Arena.Format(position)}. {Describe(number)}");
        }

        private string Describe(int number)
        {
            Arena arena = _store.Find(number);
            return arena == null ? ArenaStore.NotSet(number) : $"{arena}.";
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
