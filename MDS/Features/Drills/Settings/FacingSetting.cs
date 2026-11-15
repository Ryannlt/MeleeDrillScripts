using MDS.Shared.Game;

namespace MDS.Features.Drills.Settings
{
    public sealed class FacingSetting : DrillSetting<Facing>
    {
        public FacingSetting(Replies replies)
            : base("DrillOrientation", Facing.NorthSouth, replies)
        {
        }

        protected override string Accepted => "NorthSouth|EastWest|SouthNorth|WestEast|Random|0-359";

        protected override bool TryRead(string text, out Facing value) => Facing.TryParse(text, out value);
    }
}
