namespace MDS.ConfigVariables
{
    public class SetGroupfightSpacing : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetGroupfightSpacing;

        public bool Validate(string value)
        {
            return float.TryParse(value, out float parsed) && parsed > 0f;
        }

        public void Execute(string value)
        {
            if (float.TryParse(value, out float parsed))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.GroupfightSpacing, out var configurable)
                    && configurable is GroupfightSpacingConfigurable spacingConfig)
                {
                    spacingConfig.GroupfightSpacing = parsed;
                    Logger.Log($"Set Groupfight spacing to {parsed:F2}", LogLevel.INFO);
                }
            }
        }
    }
}
