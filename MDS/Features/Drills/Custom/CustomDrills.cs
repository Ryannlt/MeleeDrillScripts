using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Preview;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // Runs a custom drill by name or number: attackers fill the attacking groups in order, defenders the defending ones.
    public sealed class CustomDrills : IDrill
    {
        private readonly DrillKit _kit;
        private readonly CustomDrillBook _book;
        private readonly CustomLayout _layout;

        public CustomDrills(DrillKit kit, CustomDrillBook book)
        {
            _kit = kit;
            _book = book;
            _layout = new CustomLayout(kit.Random);
        }

        public string Name => "drill";

        public DrillSyntax Syntax { get; } = new DrillSyntax(
            "Usage: rc drill <name|number> [strategy] [arena], or by name: strategy=next turn=90 arena=2. A turn goes by name only. rc drill alone lists them",
            1, DrillOption.Name, DrillOption.Strategy, DrillOption.Turn);

        // Only the drill's own groups are placed.
        public bool ClearsLeftovers => true;

        public bool Validate(DrillOptions options, out string error)
        {
            if (!_kit.HasArena(options, out error)) return false;

            if (!_book.TryFind(options.GetName(), out CustomDrill drill)) error = $"No custom drill '{options.GetName()}'. rc drill lists them.";
            else if (drill.Groups.Count == 0) error = $"{drill.Name} has no groups yet. Add one with rc drill add {drill.Name} {DrillLineParser.GroupFormat}";
            return error.Length == 0;
        }

        // The fit is checked before the pick, so a refused drill leaves Next and Repeat where they were.
        public bool TryStart(DrillOptions options, out DrillRun run, out string error)
        {
            run = null;
            _book.TryFind(options.GetName(), out CustomDrill drill);
            CustomPlacement placement = PlacementFor(drill, options);
            error = placement.Error;
            if (!placement.Fits) return false;

            PickResult pick = drill.Selection.Pick(options.GetStrategy(drill.Strategy), drill.Count);
            error = pick.Error;
            if (!pick.Succeeded) return false;

            run = new DrillRun($"{Name} {drill.Name}", _kit.ArenaFor(options)) { Note = placement.Note };
            Place(run, pick.Lineup, placement);
            return true;
        }

        public bool TryPreview(DrillOptions options, out DrillPreview preview, out string error)
        {
            preview = null;
            _book.TryFind(options.GetName(), out CustomDrill drill);
            CustomPlacement placement = PlacementFor(drill, options);
            error = placement.Error;
            if (!placement.Fits) return false;

            preview = new DrillPreview { IsSample = placement.IsSample, Detail = drill.Name, Note = placement.Note };
            foreach (PlacedGroup group in placement.Groups) (group.Side == TeamSide.Attacking ? preview.Attackers : preview.Defenders).AddRange(group.Spots);
            return true;
        }

        private static List<IPlayer> Take(List<IPlayer> from, int count)
        {
            List<IPlayer> taken = from.GetRange(0, Mathf.Min(count, from.Count));
            from.RemoveRange(0, taken.Count);
            return taken;
        }

        private CustomPlacement PlacementFor(CustomDrill drill, DrillOptions options)
        {
            Facing turn = options.GetTurn(drill.Turn);
            CustomPlacement placement = _layout.Place(drill, _kit.ArenaFor(options), turn.Resolve(_kit.Random));
            placement.IsSample |= turn.IsRandom;
            return placement;
        }

        private void Place(DrillRun run, Lineup lineup, CustomPlacement placement)
        {
            var attackers = new List<IPlayer>(lineup.Attackers);
            var defenders = new List<IPlayer>(lineup.Defenders);
            var spots = new List<Vector2>();
            int moved = 0;
            foreach (PlacedGroup group in placement.Groups)
            {
                moved += _kit.Teleporter.MoveAll(Take(group.Side == TeamSide.Attacking ? attackers : defenders, group.Spots.Count), group.Spots);
                spots.AddRange(group.Spots);
            }
            run.Placed(lineup, spots);
            _kit.Log.Info($"{run.Drill} in arena {run.Arena.Number}: {lineup.Attackers.Count} attackers vs {lineup.Defenders.Count} defenders; {moved} moved. {run.Note}".TrimEnd());
        }
    }
}
