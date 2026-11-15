using System;

namespace MDS.Shared.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    // Tagged, timestamped lines, handed to the sink the runtime made. A feature logs through its own child from For,
    // which shares the sink and the debug switch and names the feature after the level.
    public sealed class ModLog
    {
        private readonly string _tag;
        private readonly ILogSink _sink;
        private readonly string _system;
        private readonly ModLog _root;
        private bool _debugEnabled;

        public ModLog(string tag, ILogSink sink)
        {
            _tag = tag;
            _sink = sink;
            _system = string.Empty;
        }

        private ModLog(ModLog root, string system)
        {
            _tag = root._tag;
            _sink = root._sink;
            _system = system;
            _root = root;
        }

        public bool DebugEnabled
        {
            get => _root?.DebugEnabled ?? _debugEnabled;
            set
            {
                if (_root != null) _root.DebugEnabled = value;
                else _debugEnabled = value;
            }
        }

        public ModLog For(string system) => new ModLog(_root ?? this, system);

        public void Debug(string message) => Write(LogLevel.Debug, message);

        public void Info(string message) => Write(LogLevel.Info, message);

        public void Warning(string message) => Write(LogLevel.Warning, message);

        public void Error(string message) => Write(LogLevel.Error, message);

        private void Write(LogLevel level, string message)
        {
            if (level == LogLevel.Debug && !DebugEnabled) return;

            string system = _system.Length == 0 ? string.Empty : $"[{_system}] ";
            _sink.Write(level, $"[{_tag}] [{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpperInvariant()}] {system}{message}");
        }
    }
}
