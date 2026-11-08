namespace MDS.ConfigVariables
{
    public class SetGroupfightDistance : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetGroupfightDistance;

        public bool Validate(string value)
        {
            return float.TryParse(value, out float parsed) && parsed > 0f;
        }

        public void Execute(string value)
        {
            if (float.TryParse(value, out float parsed))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.GroupfightDistance, out var configurable)
                    && configurable is GroupfightDistanceConfigurable distanceConfig)
                {
                    distanceConfig.GroupfightDistance = parsed;
                    Logger.Log($"Set Groupfight distance to {parsed:F2}", LogLevel.INFO);
                }
            }
        }
    }
}
