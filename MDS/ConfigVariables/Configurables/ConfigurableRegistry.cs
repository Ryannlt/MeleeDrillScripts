using System.Collections.Generic;

namespace MDS.ConfigVariables
{
    public static class ConfigurableRegistry
    {
        private static readonly Dictionary<ConfigurableEnum, IConfigurable> registry = new();

        static ConfigurableRegistry()
        {
            RegisterAllConfigurables();
        }

        public static void RegisterConfigurable(IConfigurable configurable)
        {
            ConfigurableEnum key = configurable.ConfigurableName;

            if (!registry.ContainsKey(key))
            {
                registry[key] = configurable;
                Logger.Log($"Registered configurable: {key}", LogLevel.DEBUG);
            }
            else
            {
                Logger.Log($"Configurable '{key}' already registered. Skipping duplicate.", LogLevel.WARNING);
            }
        }

        public static IConfigurable Get(ConfigurableEnum key)
        {
            if (registry.TryGetValue(key, out var configurable))
            {
                return configurable;
            }

            throw new KeyNotFoundException($"Configurable '{key}' is not registered.");
        }

        public static bool TryGet(ConfigurableEnum key, out IConfigurable configurable)
        {
            return registry.TryGetValue(key, out configurable);
        }

        public static void RegisterAllConfigurables()
        {
            RegisterConfigurable(new DebugLoggingConfigurable());
            RegisterConfigurable(new AdminOnlyConfigurable());
            RegisterConfigurable(new RoundConfigurable());
            RegisterConfigurable(new PlayerConfigurable());
            RegisterConfigurable(new PlayersConfigurable());
            RegisterConfigurable(new OpenMeleeSpacingConfigurable());
            RegisterConfigurable(new OpenMeleeOffsetConfigurable());
            RegisterConfigurable(new ArenaCorner1Configurable());
            RegisterConfigurable(new ArenaCorner2Configurable());
            RegisterConfigurable(new XvXDistanceConfigurable());
            RegisterConfigurable(new XvXSpacingConfigurable());
            RegisterConfigurable(new XvXStrategyConfigurable());
            RegisterConfigurable(new GroupfightDistanceConfigurable());
            RegisterConfigurable(new GroupfightSpacingConfigurable());
            RegisterConfigurable(new GroupfightStrategyConfigurable());
            RegisterConfigurable(new OrientationConfigurable());

            Logger.Log($"Total configurables registered: {registry.Count}", LogLevel.INFO);
        }
    }
}
