namespace MDS.ConfigVariables
{
    public interface IConfigVariables
    {
        ConfigCommandEnum CommandName { get; }
        bool Validate(string value);
        void Execute(string value);
    }
}