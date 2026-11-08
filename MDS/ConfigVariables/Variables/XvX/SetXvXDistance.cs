namespace MDS.ConfigVariables
{
    public class SetXvXDistance : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetXvXDistance;

        public bool Validate(string value)
        {
            return float.TryParse(value, out float parsed) && parsed > 0f;
        }

        public void Execute(string value)
        {
            if (float.TryParse(value, out float parsed))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.XvXDistance, out var configurable)
                    && configurable is XvXDistanceConfigurable distanceConfig)
                {
                    distanceConfig.XvXDistance = parsed;
                    Logger.Log($"Set XvX distance to {parsed:F2}", LogLevel.INFO);
                }
            }
        }
    }
}
