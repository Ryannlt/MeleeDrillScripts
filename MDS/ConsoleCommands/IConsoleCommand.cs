namespace MDS.ConsoleCommands
{
    public interface IConsoleCommand
    {
        ConsoleCommandEnum CommandName { get; }
        bool Validate(string[] parameters, out string errorMessage);
        void Execute(int playerId, string[] parameters);
    }
}
