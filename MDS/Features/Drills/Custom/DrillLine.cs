using MDS.Features.Drills.Picking;

namespace MDS.Features.Drills.Custom
{
    // One line of a custom drill, from the config or rc drill add: a group, the drill's default pick, or its default turn.
    public sealed class DrillLine
    {
        public DrillGroup Group { get; set; }

        public PickStrategy? Strategy { get; set; }

        public Facing? Turn { get; set; }
    }
}
