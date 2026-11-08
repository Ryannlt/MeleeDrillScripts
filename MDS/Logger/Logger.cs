namespace MDS
{
    public static class Logger
    {
        public static bool EnableLogging { get; private set; } = true;
        public static bool EnableDebugLogging { get; private set; } = false;

        private static readonly ILogger loggerInstance = LoggerFactory.CreateLogger(); // Console Logger hardcoded for now

        public static void Log(string message, LogLevel level = LogLevel.INFO)
        {
            if (!EnableLogging) return;
            loggerInstance.Log(message, level);
        }

        public static void SetEnableLogging(bool enabled)
        {
            EnableLogging = enabled;
            Log($"Logging {(enabled ? "enabled" : "disabled")}.", LogLevel.INFO);
        }

        public static void SetEnableDebugLogging(bool enabled)
        {
            EnableDebugLogging = enabled;
            Log($"Debug Logging {(enabled ? "enabled" : "disabled")}.", LogLevel.INFO);
        }
    }
}