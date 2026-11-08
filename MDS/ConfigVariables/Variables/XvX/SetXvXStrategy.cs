using System;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class SetXvXStrategy : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetXvXStrategy;

        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

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

            if (ConfigurableRegistry.TryGet(ConfigurableEnum.XvXStrategy, out var configurable) &&
                configurable is XvXStrategyConfigurable strategyConfig)
            {
                strategyConfig.Strategy = parsed;
                Logger.Log($"XvX strategy set to: {parsed}", LogLevel.INFO);
            }
        }
    }
}
