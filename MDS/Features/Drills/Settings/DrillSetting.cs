using MDS.Shared.Config;
using MDS.Shared.Game;
using MDS.Shared.Settings;

namespace MDS.Features.Drills.Settings
{
    // A drill default that rc set/get and a config line both change, under one name: rc set xvxDistance 5 and
    // MDS:XvXDistance:5 are the same setting. Subclasses only say how to read and show a value.
    public abstract class DrillSetting<T> : ISetting, IConfigVariable
    {
        private readonly Replies _replies;

        protected DrillSetting(string key, T value, Replies replies)
        {
            Key = key;
            Value = value;
            _replies = replies;
        }

        public string Key { get; }

        public T Value { get; private set; }

        public bool Validate(string value) => TryRead(value, out _);

        public void Apply(string value)
        {
            if (TryRead(value, out T read)) Value = read;
        }

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            bool valid = args.Length == 1 && TryRead(args[0], out _);
            errorMessage = valid ? string.Empty : $"Usage: rc set {Key} <{Accepted}>";
            return valid;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = args.Length == 0 ? string.Empty : $"Usage: rc get {Key}";
            return args.Length == 0;
        }

        public void Set(int playerId, string[] args)
        {
            Apply(args[0]);
            Get(playerId, args);
        }

        public void Get(int playerId, string[] args) => _replies.Send(playerId, $"{Key} is {Show(Value)}.");

        // What the usage line lists as valid values.
        protected abstract string Accepted { get; }

        protected abstract bool TryRead(string text, out T value);

        protected virtual string Show(T value) => value.ToString();
    }
}
