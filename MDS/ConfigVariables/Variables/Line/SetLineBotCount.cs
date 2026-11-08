namespace MDS.ConfigVariables
{
    public class SetLineBotCount : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetLineBotCount;

        public bool Validate(string value)
        {
            return int.TryParse(value, out int parsed) && parsed > 0;
        }

        public void Execute(string value)
        {
            if (int.TryParse(value, out int parsed)
                && ConfigurableRegistry.TryGet(ConfigurableEnum.LineBotCount, out var configurable)
                && configurable is LineBotCountConfigurable config)
            {
                config.LineBotCount = parsed;
                Logger.Log($"Set line bot count to {parsed}", LogLevel.INFO);
            }
        }
    }
}
