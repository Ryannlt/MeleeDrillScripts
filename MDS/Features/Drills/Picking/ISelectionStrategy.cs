namespace MDS.Features.Drills.Picking
{
    // One way to pick a drill's players (the Strategy pattern). A drill command chooses one by name.
    public interface ISelectionStrategy
    {
        PickResult Pick(Headcount count);
    }
}
