namespace MDS.Shared.Config
{
    public interface IConfigVariable
    {
        string Key { get; }

        // The reason goes into the log line for a refused value, and may be empty.
        bool Validate(string value, out string reason);

        void Apply(string value);
    }
}
