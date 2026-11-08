namespace MDS
{
    public static class LoggerFactory
    {
        public static ILogger CreateLogger()
        {
            return new ConsoleLogger();
        }
    }
}
