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
            "Usage: rc customdrill <name|number> [strategy] [mirror] [arena], or by name: strategy=next turn=90 arena=2. A turn goes by name only. rc customdrill alone lists them",
            1, DrillOption.Name, DrillOption.Strategy, DrillOption.Turn, DrillOption.Mirror);

        // Anyone standing where the drill puts players who isn't on a spot or in the formation is sent back to a spawn.
        public bool ClearsLeftovers => true;

        // Only the static spots have a set number of players, so a drill with a formation takes everyone.
        // A drill not made yet, such as one a later config line adds, is checked again when it runs.
        public bool RunsInParallel(DrillOptions options) => !_book.TryFind(options.GetName(), out CustomDrill drill) || drill.Formation == null;

        public Headcount Needs(DrillOptions options)
        {
            if (!_book.TryFind(options.GetName(), out CustomDrill drill)) return new Headcount(0, 0);

            return drill.Formation == null ? drill.Count : Headcount.Everyone;
        }

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

            PickResult pick = custom.Drill.Selection.Pick(options.GetStrategy(custom.Drill.Strategy), custom.Count, custom.Scope);
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
            CustomPlacement placement = _layout.Place(custom, RestAfter(custom));
            error = placement.Error;
            if (!placement.Fits) return false;

            preview = new DrillPreview { Area = placement.Formation.Area, IsSample = placement.Formation.IsSample || custom.Turn.IsRandom, Detail = custom.Drill.Name, Note = placement.Note };
            preview.Attackers.AddRange(placement.Attackers);
            preview.Defenders.AddRange(placement.Defenders);
            preview.AddSpots(_kit.Field(custom.Scope), placement.Formation.Plan);
            return true;
        }

        private static List<IPlayer> Without(IReadOnlyList<IPlayer> players, Lineup picked) => new List<IPlayer>(players).FindAll(player => !picked.Includes(player));

        private static List<IPlayer> After(IReadOnlyList<IPlayer> players, int count) => new List<IPlayer>(players).GetRange(Mathf.Min(count, players.Count), Mathf.Max(0, players.Count - count));

        // The formation's players join the side of their own team, so a program can tell when one side has all fallen.
        private Lineup Everyone(Lineup picked, List<IPlayer> formation)
        {
            var attackers = new List<IPlayer>(picked.Attackers);
            var defenders = new List<IPlayer>(picked.Defenders);
            var attacking = new List<IPlayer>(_kit.Players.Attacking);
            foreach (IPlayer player in formation) (attacking.Contains(player) ? attackers : defenders).Add(player);
            return new Lineup(attackers, defenders);
        }

        private CustomRun RunFor(DrillOptions options)
        {
            _book.TryFind(options.GetName(), out CustomDrill drill);
            Facing turn = options.GetTurn(drill.Turn);
            return new CustomRun { Drill = drill, Arena = _kit.ArenaFor(options), Turn = turn, Degrees = turn.Resolve(_kit.Random), Scope = options.Scope, Mirrored = options.IsMirrored };
        }

        private string Refusal(CustomRun custom)
        {
            if (!custom.Drill.Timing.IsInstant && _countdown.IsRunning) return _countdown.Busy;

            return _layout.Place(custom, RestAfter(custom)).Error;
        }

        // Before the pick, the free field less as many of each team as the static spots take stands in for the rest.
        private Lineup RestAfter(CustomRun custom)
        {
            Lineup free = _kit.Field(custom.Scope);
            return new Lineup(After(free.Attackers, custom.Count.Attackers), After(free.Defenders, custom.Count.Defenders));
        }

        // Whoever is on the field at the move: the picked players on their spots, everyone else of each team in the formation.
        private void Place(CustomRun custom)
        {
            Lineup free = _kit.Field(custom.Scope);
            var rest = new Lineup(Without(free.Attackers, custom.Picked), Without(free.Defenders, custom.Picked));
            CustomPlacement placement = _layout.Place(custom, rest);
            int moved = _kit.Teleporter.MoveAll(custom.Picked.Attackers, placement.Attackers) + _kit.Teleporter.MoveAll(custom.Picked.Defenders, placement.Defenders);
            moved += _kit.Teleporter.MoveAll(placement.Formation.Plan.Players, placement.Formation.Plan.Spots);

            custom.Run.Note = placement.Note;
            custom.Run.Placed(Everyone(custom.Picked, placement.Formation.Plan.Players), placement.AllSpots());
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
