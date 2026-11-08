using UnityEngine;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class AddArena : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.AddArena;

        public bool Validate(string value)
        {
            // Expecting format: (x1,z1),(x2,z2)
            var parts = value.Split("),(");
            if (parts.Length != 2) return false;

            parts[0] = parts[0].TrimStart('(');
            parts[1] = parts[1].TrimEnd(')');

            var first = parts[0].Split(',');
            var second = parts[1].Split(',');

            return first.Length == 2 && second.Length == 2 &&
                   float.TryParse(first[0], out _) && float.TryParse(first[1], out _) &&
                   float.TryParse(second[0], out _) && float.TryParse(second[1], out _);
        }

        public void Execute(string value)
        {
            var parts = value.Split("),(");
            parts[0] = parts[0].TrimStart('(');
            parts[1] = parts[1].TrimEnd(')');

            var first = parts[0].Split(',');
            var second = parts[1].Split(',');

            float x1 = float.Parse(first[0]);
            float z1 = float.Parse(first[1]);
            float x2 = float.Parse(second[0]);
            float z2 = float.Parse(second[1]);

            Vector2 corner1 = new Vector2(x1, z1);
            Vector2 corner2 = new Vector2(x2, z2);

            ArenaManager.StageArena(corner1, corner2, false);
            Logger.Log($"Staged new arena: ({x1:F2}, {z1:F2}) -> ({x2:F2}, {z2:F2})", LogLevel.INFO);
        }
    }
}
