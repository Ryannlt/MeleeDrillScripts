namespace MDS.Shared.Game
{
    // A seam, so timed code runs in unit tests on a clock the test moves.
    public interface IClock
    {
        // Unity's Time.time: scaled game time.
        float GameSeconds { get; }

        // Unity's Time.realtimeSinceStartup: wall time, untouched by time scale.
        float RealSeconds { get; }
    }
}
