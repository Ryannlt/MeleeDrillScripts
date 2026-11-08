namespace MDS.ConfigVariables
{
    public class SetLineSpacing : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetLineSpacing;

        public bool Validate(string value)
        {
            return float.TryParse(value, out float parsed) && parsed > 0f;
        }

        public void Execute(string value)
        {
            if (float.TryParse(value, out float parsed)
                && ConfigurableRegistry.TryGet(ConfigurableEnum.LineSpacing, out var configurable)
                && configurable is LineSpacingConfigurable config)
            {
                config.LineSpacing = parsed;
                Logger.Log($"Set line spacing to {parsed:F2}", LogLevel.INFO);
            }
        }
    }
}
