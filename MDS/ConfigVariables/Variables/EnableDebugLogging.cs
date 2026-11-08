using System;

namespace MDS.ConfigVariables
{
    public class EnableDebugLogging : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.EnableDebugLogging;

        public bool Validate(string value)
        {
            return bool.TryParse(value, out _);
        }

        public void Execute(string value)
        {
            Logger.SetEnableDebugLogging(value.Equals("true", StringComparison.OrdinalIgnoreCase));
            Logger.Log($"EnableDebugLogging set to {value}.", LogLevel.DEBUG);
        }
    }
}
