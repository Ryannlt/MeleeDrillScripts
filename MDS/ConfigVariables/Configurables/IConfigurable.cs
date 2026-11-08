namespace MDS.ConfigVariables
{
    public interface IConfigurable
    {
        ConfigurableEnum ConfigurableName { get; }

        bool ValidateSet(string[] args, out string errorMessage);
        bool ValidateGet(string[] args, out string errorMessage);
        void Set(int playerId, string[] args);
        void Get(int playerId, string[] args);
    }
}
