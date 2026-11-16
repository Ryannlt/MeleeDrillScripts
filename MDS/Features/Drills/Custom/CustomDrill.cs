using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared;

namespace MDS.Features.Drills.Custom
{
    // A drill made of static spots, filled first by the drill's pick, and one formation that takes everyone else on the field.
    // It keeps its own selection, so Next and Repeat carry on from this drill's last run only.
    public sealed class CustomDrill
    {
        // Open melee's.
        public const float DefaultGodModeSeconds = 2f;

        private readonly List<string> _spotLines = new();

        public CustomDrill(string name, Selection selection)
        {
            Name = name;
            Selection = selection;
        }

        public string Name { get; }

        public Selection Selection { get; }

        public List<StaticSpot> Spots { get; } = new();

        // A later formation replaces an earlier one, apart from a line for the other team, which joins it.
        public IFormation Formation { get; set; }

        public PickStrategy Strategy { get; set; } = PickStrategy.Random;

        // 0 degrees runs the drill as written.
        public Facing Turn { get; set; } = Facing.WestEast;

        public bool CountsDown { get; set; }

        public bool GodMode { get; set; }

        public float GodModeSeconds { get; set; } = DefaultGodModeSeconds;

        public Headcount Count => new Headcount(CountOf(TeamSide.Attacker), CountOf(TeamSide.Defender));

        public bool IsEmpty => Spots.Count == 0 && Formation == null;

        public DrillTiming Timing => new DrillTiming(Name, CountsDown, GodMode ? GodModeSeconds : 0f);

        public string Summary => $"{Name} ({Count}{(Formation != null ? $" + {Formation.Name}" : string.Empty)}, {Strategy.ToString().ToLowerInvariant()})";

        public void AddSpots(IEnumerable<StaticSpot> spots, string written)
        {
            Spots.AddRange(spots);
            _spotLines.Add(written);
        }

        public void SetLine(FormationLine line)
        {
            if (Formation is LineFormation lines) lines.Set(line);
            else Formation = new LineFormation(line);
        }

        // The drill as lines the config or rc customdrill add would take back.
        public List<string> Lines()
        {
            var lines = new List<string>(_spotLines);
            if (Formation != null) lines.AddRange(Formation.Lines);
            lines.Add($"strategy,{Strategy.ToString().ToLowerInvariant()}");
            lines.Add($"turn,{(Turn.IsRandom ? "random" : Numbers.Format(Turn.Degrees))}");
            lines.Add($"countdown,{(CountsDown ? "on" : "off")}");
            lines.Add($"godmode,{(GodMode ? "on" : "off")}");
            lines.Add($"godModeSeconds,{Numbers.Format(GodModeSeconds)}");
            return lines;
        }

        private int CountOf(TeamSide side) => Spots.FindAll(spot => spot.Side == side).Count;
    }
}
