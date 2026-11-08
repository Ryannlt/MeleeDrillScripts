using System;

namespace MDS.ConfigVariables
{
    // Config-file default for a bot-AI lever, the persistent form of 'rc set globalAI'.
    public class SetGlobalAi : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetGlobalAi;

        public bool Validate(string value) => Parse(value, out _, out _, out _);

        public void Execute(string value)
        {
            if (!Parse(value, out string aiType, out string lever, out string leverValue)) return;

            if (ConfigurableRegistry.TryGet(ConfigurableEnum.GlobalAi, out var configurable)
                && configurable is GlobalAiConfigurable global)
            {
                global.SetDefault(aiType, lever, leverValue);
                Logger.Log($"Set global AI default '{aiType} {lever}' to '{leverValue}'", LogLevel.INFO);
            }
        }

        // "<AiType>,<lever>,<value>" (comma or space delimited); AiType/lever/value hold neither, so either works.
        private static bool Parse(string value, out string aiType, out string lever, out string leverValue)
        {
            aiType = lever = leverValue = null;
            if (string.IsNullOrWhiteSpace(value)) return false;

            string[] parts = value.Split(new[] { ' ', ',' }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return false;

            aiType = parts[0];
            lever = parts[1];
            leverValue = parts[2];
            return true;
        }
    }
}
