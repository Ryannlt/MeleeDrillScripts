namespace MDS.Features.Drills.Picking
{
    public interface ISelectionStrategy
    {
        PickResult Pick(Headcount count, PickScope scope);
    }
}
