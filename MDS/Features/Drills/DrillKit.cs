using System;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Settings;
using MDS.Shared;
using MDS.Shared.Game;
using MDS.Shared.Logging;
using MDS.Shared.Players;

namespace MDS.Features.Drills
{
    public sealed class DrillKit
    {
        public DrillKit(ModContext context, ArenaStore arenas, DrillSettings settings)
        {
            Arenas = arenas;
            Settings = settings;
            Players = context.Players;
            Console = context.Console;
            Replies = context.Replies;
            Log = context.Log.For("Drills");
            Random = context.Runtime.CreateRandom();
            Teleporter = new Teleporter(context.Console, context.Runtime.CreateGround(Log));
        }

        public ArenaStore Arenas { get; }

        public DrillSettings Settings { get; }

        public PlayerRegistry Players { get; }

        public GameConsole Console { get; }

        public Replies Replies { get; }

        public ModLog Log { get; }

        public Random Random { get; }

        public Teleporter Teleporter { get; }

        public bool HasArena(out string error)
        {
            bool usable = Arenas.Current != null && Arenas.Current.IsUsable;
            error = usable ? string.Empty : ArenaStore.NotSet;
            return usable;
        }

        public LineShape ShapeFor(DrillOptions options, NumberSetting distance, NumberSetting spacing)
        {
            return new LineShape
            {
                Center = Arenas.Current.Center,
                Distance = options.GetNumber(DrillOption.Distance, distance.Value),
                Spacing = options.GetNumber(DrillOption.Spacing, spacing.Value),
                Degrees = options.GetFacing(Settings.Orientation.Value).Resolve(Random),
            };
        }

        public void PlaceLines(string drill, Lineup lineup, LineShape shape)
        {
            int moved = Teleporter.MoveAll(lineup.Attackers, LinePlacement.AttackerSpots(shape, lineup.Attackers.Count))
                + Teleporter.MoveAll(lineup.Defenders, LinePlacement.DefenderSpots(shape, lineup.Defenders.Count));
            Log.Info($"{drill}: {lineup.Attackers.Count} attackers vs {lineup.Defenders.Count} defenders, facing {Numbers.Format(shape.Degrees)} degrees; {moved} moved.");
        }
    }
}
