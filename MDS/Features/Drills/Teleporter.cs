using System;
using System.Collections.Generic;
using MDS.Shared;
using MDS.Shared.Game;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills
{
    public sealed class Teleporter
    {
        // Under a ceiling over the spawn, above the spawn's own floor.
        private const float Headroom = 2f;

        private readonly GameConsole _console;
        private readonly IGround _ground;

        public Teleporter(GameConsole console, IGround ground)
        {
            _console = console;
            _ground = ground;
        }

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

        public bool Move(int playerId, Vector2 spot) => MoveTo(playerId, spot, _ground.HeightAt(spot));

        // Looking down from just above the spawn's floor finds that floor beside it, or the ground past the edge of a raised spawn.
        public bool MoveNear(int playerId, Vector2 spot, float spawnY) => MoveTo(playerId, spot, _ground.HeightBelow(spot, spawnY + Headroom));

        private bool MoveTo(int playerId, Vector2 spot, float y)
        {
            return _console.Execute($"teleport {playerId} {Numbers.Coordinate(spot.x)},{Numbers.Coordinate(y)},{Numbers.Coordinate(spot.y)}");
        }
    }
}
