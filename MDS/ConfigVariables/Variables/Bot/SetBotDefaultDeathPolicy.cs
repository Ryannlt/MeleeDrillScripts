using System;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class SetBotDefaultDeathPolicy : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetBotDefaultDeathPolicy;

        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (int.TryParse(value, out _)) return false;

            return Enum.TryParse(value, true, out BotDeathPolicy _);
        }

        public void Execute(string value)
        {
            if (!Enum.TryParse(value, true, out BotDeathPolicy parsed))
            {
                Logger.Log($"Invalid bot death policy '{value}'", LogLevel.WARNING);
                return;
            }

            if (ConfigurableRegistry.TryGet(ConfigurableEnum.BotDefaultDeathPolicy, out var configurable) &&
                configurable is BotDefaultDeathConfigurable deathConfig)
            {
                deathConfig.DefaultPolicy = parsed;
                Logger.Log($"Bot default death policy set to: {parsed}", LogLevel.INFO);
            }
        }
    }
}
