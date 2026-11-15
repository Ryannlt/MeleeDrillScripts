namespace MDS.Shared.Config
{
    public interface IConfigVariable
    {
        string Key { get; }

        bool Validate(string value);

        void Apply(string value);
    }
}
