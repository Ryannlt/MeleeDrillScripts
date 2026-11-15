namespace MDS.Shared.Commands
{
    // One rc command (the Command pattern). Validate checks the parameters, Execute does the work.
    public interface IConsoleCommand
    {
        string Name { get; }

        bool Validate(string[] parameters, out string errorMessage);

        void Execute(int playerId, string[] parameters);
    }
}
