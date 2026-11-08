using System;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    // MDS:EnableMeleeLog:true|false|frames. Set for a server that should log melee from the first hit of a map.
    public class EnableMeleeLog : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.EnableMeleeLog;

        public bool Validate(string value)
        {
            return bool.TryParse(value, out _) || IsFrames(value);
        }

        public void Execute(string value)
        {
            bool frames = IsFrames(value);
            MeleeDebug.SetFramesEnabled(frames);
            MeleeDebug.SetEnabled(frames || value.Equals("true", StringComparison.OrdinalIgnoreCase));
            Logger.Log($"EnableMeleeLog set to {value}.", LogLevel.INFO);
        }

        private static bool IsFrames(string value) => value.Equals("frames", StringComparison.OrdinalIgnoreCase);
    }
}
