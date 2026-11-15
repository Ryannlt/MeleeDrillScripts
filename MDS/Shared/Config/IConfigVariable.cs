namespace MDS.Shared.Config
{
    // One mod_variable key: MDS:<Key>:<Value> in a server config.
    public interface IConfigVariable
    {
        string Key { get; }

        bool Validate(string value);

        void Apply(string value);
    }
}
