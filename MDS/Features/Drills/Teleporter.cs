using System;
using System.Collections.Generic;
using MDS.Shared;
using MDS.Shared.Game;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills
{
    // Moves players onto map positions, standing on the ground there (Adapter over the teleport command).
    public sealed class Teleporter
    {
        private readonly GameConsole _console;
        private readonly IGround _ground;

        public Teleporter(GameConsole console, IGround ground)
        {
            _console = console;
            _ground = ground;
        }

        // Pairs players with spots in order. Returns how many the game moved.
        public int MoveAll(IReadOnlyList<IPlayer> players, IReadOnlyList<Vector2> spots)
        {
            int moved = 0;
            int count = Math.Min(players.Count, spots.Count);
            for (int i = 0; i < count; i++)
            {
                if (Move(players[i].PlayerId, spots[i])) moved++;
            }
            return moved;
        }

        public bool Move(int playerId, Vector2 spot)
        {
            float y = _ground.HeightAt(spot);
            return _console.Execute($"teleport {playerId} {Numbers.Coordinate(spot.x)},{Numbers.Coordinate(y)},{Numbers.Coordinate(spot.y)}");
        }
    }
}
