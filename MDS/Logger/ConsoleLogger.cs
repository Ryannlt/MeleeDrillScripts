using UnityEngine;
using System;

namespace MDS
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message, LogLevel level)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[MDS] [{timestamp}] [{level}] {message}";

            switch (level)
            {
                case LogLevel.WARNING:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.ERROR:
                    Debug.LogError(formattedMessage);
                    break;
                case LogLevel.DEBUG:
                    if (Logger.EnableDebugLogging)
                        Debug.Log(formattedMessage);
                    break;
                default:
                    Debug.Log(formattedMessage);
                    break;
            }
        }
    }
}