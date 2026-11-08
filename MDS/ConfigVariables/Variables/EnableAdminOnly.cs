namespace MDS.ConfigVariables
{
    public class EnableAdminOnly : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.EnableAdminOnly;

        public bool Validate(string value)
        {
            return bool.TryParse(value, out _);
        }

        public void Execute(string value)
        {
            if (bool.TryParse(value, out bool parsed))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.AdminOnly, out var configurable)
                    && configurable is AdminOnlyConfigurable)
                {
                    AdminOnlyConfigurable.IsAdminOnlyEnabled = parsed;

                    Logger.Log($"Set EnableAdminOnly to {parsed}", LogLevel.INFO);
                }
            }
        }
    }
}
