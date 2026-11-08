using System;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class SetBotDefaultAi : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetBotDefaultAi;

        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (int.TryParse(value, out _)) return false;

            return Enum.TryParse(value, true, out BotAiEnum _);
        }

        public void Execute(string value)
        {
            if (!Enum.TryParse(value, true, out BotAiEnum parsed))
            {
                Logger.Log($"Invalid bot AI '{value}'", LogLevel.WARNING);
                return;
            }

            if (ConfigurableRegistry.TryGet(ConfigurableEnum.BotDefaultAi, out var configurable) &&
                configurable is BotDefaultAiConfigurable aiConfig)
            {
                aiConfig.DefaultAi = parsed;
                Logger.Log($"Bot default AI set to: {parsed}", LogLevel.INFO);
            }
        }
    }
}
