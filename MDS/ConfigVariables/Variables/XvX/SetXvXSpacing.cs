namespace MDS.ConfigVariables
{
    public class SetXvXSpacing : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetXvXSpacing;

        public bool Validate(string value)
        {
            return float.TryParse(value, out float parsed) && parsed > 0f;
        }

        public void Execute(string value)
        {
            if (float.TryParse(value, out float parsed))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.XvXSpacing, out var configurable)
                    && configurable is XvXSpacingConfigurable spacingConfig)
                {
                    spacingConfig.XvXSpacing = parsed;
                    Logger.Log($"Set XvX spacing to {parsed:F2}", LogLevel.INFO);
                }
            }
        }
    }
}
