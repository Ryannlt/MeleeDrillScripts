using MDS.Features.Drills.Picking;

namespace MDS.Features.Drills.Spawning
{
    public interface ISpawnPattern
    {
        SpawnPlan Plan(Lineup field, SpawnRequest request);

        // Checked before any spot is drawn, since a grid too large to build is refused rather than tried.
        bool CanCover(SpawnRequest request);

        // How the pattern spaces players, for the reply when the area runs out of room.
        string DescribeSpacing(SpawnRequest request);
    }
}
