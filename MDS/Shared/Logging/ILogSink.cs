namespace MDS.Shared.Logging
{
    // Where finished log lines go: the Unity log in game, a list in the unit tests.
    public interface ILogSink
    {
        void Write(LogLevel level, string line);
    }
}
