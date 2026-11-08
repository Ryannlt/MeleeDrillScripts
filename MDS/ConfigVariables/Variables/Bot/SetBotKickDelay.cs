namespace MDS.ConfigVariables
{
    public class SetBotKickDelay : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetBotKickDelay;

        public bool Validate(string value)
        {
            return float.TryParse(value, out float parsed) && parsed >= 0f;
        }

        public void Execute(string value)
        {
            if (float.TryParse(value, out float parsed))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.BotKickDelay, out var configurable)
                    && configurable is BotKickDelayConfigurable delayConfig)
                {
                    delayConfig.KickDelay = parsed;
                    Logger.Log($"Set bot kick delay to {parsed:F2}s", LogLevel.INFO);
                }
            }
        }
    }
}
