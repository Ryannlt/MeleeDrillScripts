namespace MDS.Shared.Logging
{
    // The only caller of Debug.Log: the game copies the Unity log into the server or client log.
    public sealed class UnityLogSink : ILogSink
    {
        public void Write(LogLevel level, string line)
        {
            if (level == LogLevel.Error) UnityEngine.Debug.LogError(line);
            else if (level == LogLevel.Warning) UnityEngine.Debug.LogWarning(line);
            else UnityEngine.Debug.Log(line);
        }
    }
}
