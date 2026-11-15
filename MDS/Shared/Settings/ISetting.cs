namespace MDS.Shared.Settings
{
    // Keys must be unique across every mod loaded together.
    public interface ISetting
    {
        string Key { get; }

        bool ValidateSet(string[] args, out string errorMessage);

        bool ValidateGet(string[] args, out string errorMessage);

        void Set(int playerId, string[] args);

        void Get(int playerId, string[] args);
    }
}
