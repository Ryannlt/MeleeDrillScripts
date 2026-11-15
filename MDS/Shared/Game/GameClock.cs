using UnityEngine;

namespace MDS.Shared.Game
{
    public sealed class GameClock : IClock
    {
        public float GameSeconds => Time.time;

        public float RealSeconds => Time.realtimeSinceStartup;
    }
}
