namespace MDS.ConfigVariables
{
    public class SetBotReplaceDelay : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetBotReplaceDelay;

        public bool Validate(string value)
        {
            return float.TryParse(value, out float parsed) && parsed >= 0f;
        }

        public void Execute(string value)
        {
            if (float.TryParse(value, out float parsed))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.BotReplaceDelay, out var configurable)
                    && configurable is BotReplaceDelayConfigurable delayConfig)
                {
                    delayConfig.ReplaceDelay = parsed;
                    Logger.Log($"Set bot replace delay to {parsed:F2}s", LogLevel.INFO);
                }
            }
        }
    }
}
