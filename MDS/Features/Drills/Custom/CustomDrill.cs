using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared;

namespace MDS.Features.Drills.Custom
{
    // A drill made of groups from the config or rc drill add.
    // It keeps its own selection, so Next and Repeat carry on from this drill's last run only.
    public sealed class CustomDrill
    {
        public CustomDrill(string name, Selection selection)
        {
            Name = name;
            Selection = selection;
        }

        public string Name { get; }

        public Selection Selection { get; }

        public List<DrillGroup> Groups { get; } = new();

        public PickStrategy Strategy { get; private set; } = PickStrategy.Random;

        // 0 degrees runs the drill as written.
        public Facing Turn { get; private set; } = Facing.WestEast;

        public Headcount Count => new Headcount(CountOf(TeamSide.Attacking), CountOf(TeamSide.Defending));

        public string Summary => $"{Name} ({Count}, {Strategy.ToString().ToLowerInvariant()})";

        public void Apply(DrillLine line)
        {
            if (line.Group != null) Groups.Add(line.Group);
            if (line.Strategy.HasValue) Strategy = line.Strategy.Value;
            if (line.Turn.HasValue) Turn = line.Turn.Value;
        }

        // The drill as lines the config or rc drill add would take back.
        public List<string> Lines()
        {
            List<string> lines = Groups.ConvertAll(group => group.Text);
            lines.Add($"strategy {Strategy.ToString().ToLowerInvariant()}");
            lines.Add($"turn {(Turn.IsRandom ? "random" : Numbers.Format(Turn.Degrees))}");
            return lines;
        }

        public int CountOf(TeamSide side)
        {
            int count = 0;
            foreach (DrillGroup group in Groups)
            {
                if (group.Side == side) count += group.Count;
            }
            return count;
        }
    }
}
