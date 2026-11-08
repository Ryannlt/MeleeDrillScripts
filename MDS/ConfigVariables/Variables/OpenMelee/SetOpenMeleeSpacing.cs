namespace MDS.ConfigVariables
{
    public class SetOpenMeleeSpacing : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetOpenMeleeSpacing;

        public bool Validate(string value)
        {
            return float.TryParse(value, out float parsed) && parsed > 0f;
        }

        public void Execute(string value)
        {
            if (float.TryParse(value, out float parsed))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.OpenMeleeSpacing, out var configurable)
                    && configurable is OpenMeleeSpacingConfigurable spacingConfig)
                {
                    spacingConfig.OpenMeleeSpacing = parsed;
                    Logger.Log($"Set Open Melee default spacing to {parsed:F2}", LogLevel.INFO);
                }
            }
        }
    }
}
