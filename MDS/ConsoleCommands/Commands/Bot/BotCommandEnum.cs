namespace MDS.ConsoleCommands
{
    // Subcommands of 'rc bot'. Parsed by BotCommand, handled by IBotSubCommand implementations.
    public enum BotCommandEnum
    {
        Spawn,
        SpawnRandom,
        Summon,
        SummonAt,
        SetBotAi,
        SetBotDeathPolicy,
        Remove,
        List,
        Move,
        Probe,
        Act,
        Cfg
        // Add more bot subcommands here as needed
    }
}
