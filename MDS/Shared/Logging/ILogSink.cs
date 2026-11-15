namespace MDS.Shared.Logging
{
    // A seam: the Unity log in game, a list in the unit tests.
    public interface ILogSink
    {
        void Write(LogLevel level, string line);
    }
}
