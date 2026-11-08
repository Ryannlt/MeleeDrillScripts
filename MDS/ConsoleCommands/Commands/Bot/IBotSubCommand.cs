namespace MDS.ConsoleCommands
{
    // A 'rc bot <subcommand>' handler. Mirrors IConfigurable so registration is uniform.
    public interface IBotSubCommand
    {
        BotCommandEnum SubCommandName { get; }

        bool Validate(string[] args, out string errorMessage);
        void Execute(int playerId, string[] args);
    }
}
