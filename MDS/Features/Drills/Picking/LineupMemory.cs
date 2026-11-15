namespace MDS.Features.Drills.Picking
{
    // The last lineup a drill placed, for Repeat.
    public sealed class LineupMemory
    {
        // Null until the drill has run once this round.
        public Lineup Last { get; private set; }

        public void Remember(Lineup lineup) => Last = lineup;

        public void Clear() => Last = null;
    }
}
