namespace MDS
{
    public interface ILogger
    {
        void Log(string message, LogLevel level);
    }
}