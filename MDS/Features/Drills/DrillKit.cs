using System;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Preview;
using MDS.Features.Drills.Settings;
using MDS.Shared;
using MDS.Shared.Game;
using MDS.Shared.Logging;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills
{
    public sealed class DrillKit
    {
        public DrillKit(ModContext context, ArenaStore arenas, DrillSettings settings)
        {
            Arenas = arenas;
            Settings = settings;
            Players = context.Players;
            Queries = context.Queries;
            Console = context.Console;
            Replies = context.Replies;
            Log = context.Log.For("Drills");
            Random = context.Runtime.CreateRandom();
            Teleporter = new Teleporter(context.Console, context.Runtime.CreateGround(Log));
        }

        public ArenaStore Arenas { get; }

        public DrillSettings Settings { get; }

        public PlayerRegistry Players { get; }

        public PlayerQueries Queries { get; }

        public GameConsole Console { get; }

        public Replies Replies { get; }

        public ModLog Log { get; }

        public System.Random Random { get; }

        public Teleporter Teleporter { get; }

        public bool HasArena(DrillOptions options, out string error) => Arenas.TryGetUsable(options.GetArena(), out _, out error);

        // Validate has checked it is set.
        public Arena ArenaFor(DrillOptions options) => Arenas.Find(options.GetArena());

        public LineShape ShapeFor(DrillOptions options, NumberSetting distance, NumberSetting spacing)
        {
            Facing facing = options.GetFacing(Settings.Orientation.Value);
            return new LineShape
            {
                Center = ArenaFor(options).Center,
                Distance = options.GetNumber(DrillOption.Distance, distance.Value),
                Spacing = options.GetNumber(DrillOption.Spacing, spacing.Value),
                Degrees = facing.Resolve(Random),
                FacingIsRandom = facing.IsRandom,
            };
        }

        public DrillPreview PreviewLines(LineShape shape, int attackers, int defenders)
        {
            var preview = new DrillPreview { IsSample = shape.FacingIsRandom };
            preview.Attackers.AddRange(LinePlacement.AttackerSpots(shape, attackers));
            preview.Defenders.AddRange(LinePlacement.DefenderSpots(shape, defenders));
            return preview;
        }

        public void PlaceLines(DrillRun run, Lineup lineup, LineShape shape)
        {
            List<Vector2> attackerSpots = LinePlacement.AttackerSpots(shape, lineup.Attackers.Count);
            List<Vector2> defenderSpots = LinePlacement.DefenderSpots(shape, lineup.Defenders.Count);
            int moved = Teleporter.MoveAll(lineup.Attackers, attackerSpots) + Teleporter.MoveAll(lineup.Defenders, defenderSpots);
            attackerSpots.AddRange(defenderSpots);
            run.Placed(lineup, attackerSpots);
            Log.Info($"{run.Drill} in arena {run.Arena.Number}: {lineup.Attackers.Count} attackers vs {lineup.Defenders.Count} defenders, facing {Numbers.Format(shape.Degrees)} degrees; {moved} moved.");
        }
    }
}
