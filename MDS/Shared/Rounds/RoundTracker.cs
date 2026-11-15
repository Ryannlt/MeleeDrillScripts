using System;

namespace MDS.Shared.Rounds
{
    // The round as the game last described it. Anything that resets per round subscribes to RoundStarted, in the order
    // it was built, so the player registry (built first) is ready before any feature hears of the round.
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
