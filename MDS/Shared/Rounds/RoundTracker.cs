using System;

namespace MDS.Shared.Rounds
{
    // Subscribers hear of a new round in the order they were built, so the player registry (built first) is ready before any feature.
    public sealed class RoundTracker
    {
        public event Action<RoundInfo> RoundStarted;

        public RoundInfo Current { get; private set; } = new RoundInfo();

        public void OnRoundDetails(RoundInfo round)
        {
            Current = round;
            RoundStarted?.Invoke(round);
        }
    }
}
