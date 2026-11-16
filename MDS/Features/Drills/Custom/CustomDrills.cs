using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Preview;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // Runs a custom drill by name or number.
    // The drill's pick fills the static spots first, attackers in the order they were written and defenders likewise, and the formation takes everyone else.
    public sealed class CustomDrills : IDrill
    {
        private readonly DrillKit _kit;
        private readonly CustomDrillBook _book;
        private readonly DrillCountdown _countdown;
        private readonly CustomLayout _layout;

        public CustomDrills(DrillKit kit, CustomDrillBook book, DrillCountdown countdown)
        {
            _kit = kit;
            _book = book;
            _countdown = countdown;
            _layout = new CustomLayout(kit);
        }

        public string Name => "customdrill";

        public DrillSyntax Syntax { get; } = new DrillSyntax(
            "Usage: rc customdrill <name|number> [strategy] [arena], or by name: strategy=next turn=90 arena=2. A turn goes by name only. rc customdrill alone lists them",
            1, DrillOption.Name, DrillOption.Strategy, DrillOption.Turn);

        // Anyone standing where the drill puts players who isn't on a spot or in the formation is sent back to a spawn.
        public bool ClearsLeftovers => true;

        public bool Validate(DrillOptions options, out string error)
        {
            if (!_kit.HasArena(options, out error)) return false;

            if (!_book.TryFind(options.GetName(), out CustomDrill drill)) error = $"No custom drill '{options.GetName()}'. rc customdrill lists them.";
            else if (drill.IsEmpty) error = $"{drill.Name} has nothing to place yet. Add a spot or a formation with rc customdrill add {drill.Name} <line>.";
            return error.Length == 0;
        }

        // The room check runs on the field as it stands, before the pick, so a refused drill leaves Next and Repeat where they were.
        public bool TryStart(DrillOptions options, out DrillRun run, out string error)
        {
            run = null;
            CustomRun custom = RunFor(options);
            error = Refusal(custom);
            if (error.Length > 0) return false;

            PickResult pick = custom.Drill.Selection.Pick(options.GetStrategy(custom.Drill.Strategy), custom.Drill.Count);
            error = pick.Error;
            if (!pick.Succeeded) return false;

            custom.Picked = pick.Lineup;
            run = custom.Run = new DrillRun($"{Name} {custom.Drill.Name}", custom.Arena);
            if (custom.Drill.Timing.IsInstant) Place(custom);
            else _countdown.Start(custom.Drill.Timing, () => Place(custom));
            return true;
        }

        public bool TryPreview(DrillOptions options, out DrillPreview preview, out string error)
        {
            preview = null;
            CustomRun custom = RunFor(options);
            CustomPlacement placement = _layout.Place(custom, RestAfter(custom.Drill.Count));
            error = placement.Error;
            if (!placement.Fits) return false;

            preview = new DrillPreview { Area = placement.Formation.Area, IsSample = placement.Formation.IsSample || custom.Turn.IsRandom, Detail = custom.Drill.Name, Note = placement.Note };
            preview.Attackers.AddRange(placement.Attackers);
            preview.Defenders.AddRange(placement.Defenders);
            preview.AddSpots(_kit.Field(), placement.Formation.Plan);
            return true;
        }

        private static List<IPlayer> Without(IReadOnlyList<IPlayer> players, Lineup picked) => new List<IPlayer>(players).FindAll(player => !picked.Includes(player));

        private static List<IPlayer> After(IReadOnlyList<IPlayer> players, int count) => new List<IPlayer>(players).GetRange(Mathf.Min(count, players.Count), Mathf.Max(0, players.Count - count));

        private CustomRun RunFor(DrillOptions options)
        {
            _book.TryFind(options.GetName(), out CustomDrill drill);
            Facing turn = options.GetTurn(drill.Turn);
            return new CustomRun { Drill = drill, Arena = _kit.ArenaFor(options), Turn = turn, Degrees = turn.Resolve(_kit.Random) };
        }

        private string Refusal(CustomRun custom)
        {
            if (!custom.Drill.Timing.IsInstant && _countdown.IsRunning) return _countdown.Busy;

            return _layout.Place(custom, RestAfter(custom.Drill.Count)).Error;
        }

        // Before the pick, the field less as many of each team as the static spots take stands in for the rest.
        private Lineup RestAfter(Headcount spots) => new Lineup(After(_kit.Players.Attacking, spots.Attackers), After(_kit.Players.Defending, spots.Defenders));

        // Whoever is on the field at the move: the picked players on their spots, everyone else of each team in the formation.
        private void Place(CustomRun custom)
        {
            var rest = new Lineup(Without(_kit.Players.Attacking, custom.Picked), Without(_kit.Players.Defending, custom.Picked));
            CustomPlacement placement = _layout.Place(custom, rest);
            int moved = _kit.Teleporter.MoveAll(custom.Picked.Attackers, placement.Attackers) + _kit.Teleporter.MoveAll(custom.Picked.Defenders, placement.Defenders);
            moved += _kit.Teleporter.MoveAll(placement.Formation.Plan.Players, placement.Formation.Plan.Spots);

            var placed = new List<IPlayer>(custom.Picked.Attackers);
            placed.AddRange(placement.Formation.Plan.Players);
            custom.Run.Note = placement.Note;
            custom.Run.Placed(new Lineup(placed, custom.Picked.Defenders), placement.AllSpots());
            _kit.Log.Info($"{custom.Run.Drill} in arena {custom.Arena.Number}: {custom.Picked.Attackers.Count + custom.Picked.Defenders.Count} on spots, {placement.Formation.Plan.Players.Count} in the formation; {moved} moved. {placement.Note}".TrimEnd());
            Tell(custom.Run, placement);
        }

        // At once the runner replies with the note; after a countdown it goes out as a notice.
        private void Tell(DrillRun run, CustomPlacement placement)
        {
            if (!placement.Fits) _kit.Warn(run, $"{placement.Error} Whoever had no room stays where they are.");
            else if (placement.Note.Length > 0) run.Notice(placement.Note);
        }
    }
}
