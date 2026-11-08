using System.Collections.Generic;

// Registers all 'rc bot' subcommand handlers, keyed by BotCommandEnum. Mirrors ConfigurableRegistry.
// UMod disallows reflection, so subcommands are registered manually.

namespace MDS.ConsoleCommands
{
    public static class BotSubCommandRegistry
    {
        private static readonly Dictionary<BotCommandEnum, IBotSubCommand> registry = new();

        static BotSubCommandRegistry()
        {
            RegisterAll();
        }

        public static void Register(IBotSubCommand subCommand)
        {
            BotCommandEnum key = subCommand.SubCommandName;

            if (!registry.ContainsKey(key))
            {
                registry[key] = subCommand;
                Logger.Log($"Registered bot sub-command: {key}", LogLevel.DEBUG);
            }
            else
            {
                Logger.Log($"Bot sub-command '{key}' already registered. Skipping duplicate.", LogLevel.WARNING);
            }
        }

        public static bool TryGet(BotCommandEnum key, out IBotSubCommand subCommand)
        {
            return registry.TryGetValue(key, out subCommand);
        }

        private static void RegisterAll()
        {
            Register(new SpawnSubCommand());
            Register(new SpawnRandomSubCommand());
            Register(new SummonSubCommand());
            Register(new SummonAtSubCommand());
            Register(new SetBotAiSubCommand());
            Register(new SetBotDeathPolicySubCommand());
            Register(new RemoveSubCommand());
            Register(new ListSubCommand());
            Register(new MoveSubCommand());
            Register(new ProbeSubCommand());
            Register(new ActSubCommand());
            Register(new CfgSubCommand());

            Logger.Log($"Total bot sub-commands registered: {registry.Count}", LogLevel.INFO);
        }
    }
}
