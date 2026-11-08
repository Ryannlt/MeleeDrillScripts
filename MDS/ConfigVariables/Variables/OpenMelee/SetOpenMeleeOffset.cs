namespace MDS.ConfigVariables
{
    public class SetOpenMeleeOffset : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetOpenMeleeOffset;

        public bool Validate(string value)
        {
            return float.TryParse(value, out float parsed) && parsed >= 0f;
        }

        public void Execute(string value)
        {
            if (float.TryParse(value, out float parsed))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.OpenMeleeOffset, out var configurable)
                    && configurable is OpenMeleeOffsetConfigurable offsetConfig)
                {
                    offsetConfig.OpenMeleeOffset = parsed;
                    Logger.Log($"Set Open Melee default offset to {parsed:F2}", LogLevel.INFO);
                }
            }
        }
    }
}
