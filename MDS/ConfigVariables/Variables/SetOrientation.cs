using System;

namespace MDS.ConfigVariables
{
    public class SetOrientation : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetOrientation;

        public bool Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (int.TryParse(value, out _))
            {
                // Integers are not allowed as enum names; must be string or proper float
                return false;
            }

            if (Enum.TryParse(value, true, out OrientationEnum _))
            {
                return true;
            }

            if (float.TryParse(value, out float parsedAngle))
            {
                return (parsedAngle >= 0 && parsedAngle < 360) || Math.Abs(parsedAngle - (-1f)) < 0.001f;
            }

            return false;
        }


        public void Execute(string value)
        {
            if (Enum.TryParse(value, true, out OrientationEnum orientationEnum))
            {
                if (ConfigurableRegistry.TryGet(ConfigurableEnum.Orientation, out var configurable)
                    && configurable is OrientationConfigurable orientationConfig)
                {
                    orientationConfig.OrientationAngle = orientationEnum == OrientationEnum.Random ? -1f : (float)orientationEnum;
                    Logger.Log($"Set Orientation to {orientationEnum} ({orientationConfig.OrientationAngle}°)", LogLevel.INFO);
                }
                return;
            }

            if (float.TryParse(value, out float parsedAngle))
            {
                if ((parsedAngle >= 0 && parsedAngle < 360) || Math.Abs(parsedAngle - (-1f)) < 0.001f)
                {
                    if (ConfigurableRegistry.TryGet(ConfigurableEnum.Orientation, out var configurable)
                        && configurable is OrientationConfigurable orientationConfig)
                    {
                        orientationConfig.OrientationAngle = parsedAngle;
                        Logger.Log($"Set Orientation to {parsedAngle:F2}°", LogLevel.INFO);
                    }
                }
                else
                {
                    Logger.Log($"Invalid angle value '{value}'. Must be between 0 and 359 or -1 for random.", LogLevel.WARNING);
                }
            }
            else
            {
                Logger.Log($"Invalid orientation input '{value}'. Expected enum name or degree value.", LogLevel.WARNING);
            }
        }
    }
}
