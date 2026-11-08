using UnityEngine;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class SetArenaCorner2 : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SetArenaCorner2;

        public bool Validate(string value)
        {
            var parts = value.Split(',');
            return parts.Length == 2 &&
                   float.TryParse(parts[0], out _) &&
                   float.TryParse(parts[1], out _);
        }

        public void Execute(string value)
        {
            var parts = value.Split(',');
            float x = float.Parse(parts[0]);
            float z = float.Parse(parts[1]);

            ArenaManager.SetArenaCorner2(new Vector2(x, z));
            Logger.Log($"Arena Corner 2 set to ({x}, {z})", LogLevel.INFO);
        }
    }
}
