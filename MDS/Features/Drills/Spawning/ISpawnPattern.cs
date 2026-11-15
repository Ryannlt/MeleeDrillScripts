using MDS.Features.Drills.Picking;

namespace MDS.Features.Drills.Spawning
{
    public interface ISpawnPattern
    {
        SpawnPlan Plan(Lineup field, SpawnRequest request);
    }
}
