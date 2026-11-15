namespace MDS.Features.Drills.Picking
{
    public sealed class LineupMemory
    {
        public Lineup Last { get; private set; }

        public void Remember(Lineup lineup) => Last = lineup;

        public void Clear() => Last = null;
    }
}
