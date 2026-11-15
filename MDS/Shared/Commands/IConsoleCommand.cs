namespace MDS.Shared.Commands
{
    public interface IConsoleCommand
    {
        string Name { get; }

        bool Validate(string[] parameters, out string errorMessage);

        void Execute(int playerId, string[] parameters);
    }
}
