using Holdfast.Modding;
using UnityEngine;

namespace MDS.Shared.Players
{
    // Built from the game's PlayerSpawnDetails, which tests can't create.
    public sealed class SpawnState
    {
        public int SpawnSectionId { get; set; }

        public FactionCountry Faction { get; set; }

        public PlayerClass PlayerClass { get; set; }

        public int UniformId { get; set; }

        public GameObject PlayerObject { get; set; }
    }
}
