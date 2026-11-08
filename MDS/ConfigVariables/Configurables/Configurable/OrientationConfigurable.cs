using System;
using UnityEngine;
using MDS.Core;

namespace MDS.ConfigVariables
{
    public class OrientationConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.Orientation;

        public float OrientationAngle { get; set; } = 90f; // Default: NorthSouth (90 degrees)

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1)
            {
                errorMessage = "Incorrect Arguments. Usage: rc set orientation <OrientationEnum|angle>";
                return false;
            }

            if (Enum.TryParse(args[0], true, out OrientationEnum orientationEnum))
            {
                OrientationAngle = orientationEnum == OrientationEnum.Random ? -1f : (float)orientationEnum;
                return true;
            }

            if (float.TryParse(args[0], out float parsedAngle))
            {
                if ((parsedAngle >= 0 && parsedAngle < 360) || Mathf.Approximately(parsedAngle, -1f))
                {
                    OrientationAngle = parsedAngle; // Set AFTER validating
                    return true;
                }
                else
                {
                    errorMessage = "Angle must be between 0 and 359 (inclusive) or -1 for random.";
                    return false;
                }
            }

            errorMessage = $"Invalid orientation value '{args[0]}'. Expected enum name or numeric degree.";
            return false;
        }


        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 0)
            {
                errorMessage = "Too many arguments. Usage: rc get orientation";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            if (ValidateSet(args, out string errorMessage))
            {
                string message = OrientationAngle == -1f
                    ? "Orientation set to: Random (-1)"
                    : $"Orientation set to: {FormatOrientation(OrientationAngle)}";

                Logger.Log(message, LogLevel.INFO);
                Feedback.Reply(playerId, message);
            }
            else
            {
                Feedback.Reply(playerId, errorMessage);
            }
        }

        public void Get(int playerId, string[] args)
        {
            string message = OrientationAngle == -1f
                ? "Current Orientation: Random (-1)"
                : $"Current Orientation: {FormatOrientation(OrientationAngle)}";

            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        private string FormatOrientation(float angle)
        {
            if (angle == -1f)
                return "Random (-1)";

            foreach (OrientationEnum known in Enum.GetValues(typeof(OrientationEnum)))
            {
                if ((float)known == angle)
                {
                    return $"{known} ({angle:F2}°)";
                }
            }

            return $"{angle:F2}°";
        }
    }
}
