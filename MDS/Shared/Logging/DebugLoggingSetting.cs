using MDS.Shared.Game;
using MDS.Shared.Settings;

namespace MDS.Shared.Logging
{
    public sealed class DebugLoggingSetting : ISetting
    {
        private readonly ModLog _log;
        private readonly Replies _replies;

        public DebugLoggingSetting(ModLog log, Replies replies)
        {
            _log = log;
            _replies = replies;
        }

        public string Key => "MDSDebugLogging";

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            bool valid = args.Length == 1 && bool.TryParse(args[0], out _);
            errorMessage = valid ? string.Empty : $"Usage: rc set {Key} <true|false>";
            return valid;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = args.Length == 0 ? string.Empty : $"Usage: rc get {Key}";
            return args.Length == 0;
        }

        public void Set(int playerId, string[] args)
        {
            _log.DebugEnabled = bool.Parse(args[0]);
            Get(playerId, args);
        }

        public void Get(int playerId, string[] args)
        {
            _replies.Send(playerId, $"{Key} is {_log.DebugEnabled}.");
        }
    }
}
