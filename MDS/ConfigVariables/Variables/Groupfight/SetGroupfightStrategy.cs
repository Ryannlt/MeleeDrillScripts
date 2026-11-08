using System;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class SetGroupfightStrategy : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetGroupfightStrategy;

        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            // Disallow integers
            if (int.TryParse(value, out _)) return false;

            return Enum.TryParse(value, true, out SelectionStrategyType parsed);
        }

        public void Execute(string value)
        {
            if (!Enum.TryParse(value, true, out SelectionStrategyType parsed))
            {
                Logger.Log($"Invalid strategy '{value}'", LogLevel.WARNING);
                return;
            }

            if (ConfigurableRegistry.TryGet(ConfigurableEnum.GroupfightStrategy, out var configurable) &&
                configurable is GroupfightStrategyConfigurable strategyConfig)
            {
                strategyConfig.Strategy = parsed;
                Logger.Log($"Groupfight strategy set to: {parsed}", LogLevel.INFO);
            }
        }
    }
}
