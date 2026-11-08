using UnityEngine;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    public class ArenaCorner2Configurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.ArenaCorner2;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length == 0) return true;

            if (args.Length < 2)
            {
                errorMessage = "Missing coordinates. Usage: rc set ArenaCorner2 <x> <z>";
                return false;
            }

            if (!float.TryParse(args[0], out _) || !float.TryParse(args[1], out _))
            {
                errorMessage = $"Invalid coordinate values: '{args[0]}' '{args[1]}'. Expected numbers.";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length > 0)
            {
                errorMessage = "ArenaCorner2 does not accept arguments. Usage: rc get ArenaCorner2";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            if (args.Length == 0)
            {
                if (!StateTracker.TryGetPosition(playerId, out Vector3 position))
                {
                    Feedback.Reply(playerId, "Cannot read your position. Spawn in first, or give the corner as x z.");
                    return;
                }

                Vector2 pos = new Vector2(position.x, position.z);
                ArenaManager.SetArenaCorner2(pos);

                string msg = $"ArenaCorner2 set to your position ({pos.x:F2}, {pos.y:F2})";
                Logger.Log(msg, LogLevel.INFO);
                Feedback.Reply(playerId, msg);
                return;
            }

            float x = float.Parse(args[0]);
            float z = float.Parse(args[1]);

            ArenaManager.SetArenaCorner2(new Vector2(x, z));

            string message = $"ArenaCorner2 set to ({x:F2}, {z:F2})";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            if (ArenaManager.Arena == null)
            {
                string message = "Arena has not been initialized.";
                Logger.Log(message, LogLevel.WARNING);
                Feedback.Reply(playerId, message);
                return;
            }

            Vector2 c2 = ArenaManager.Arena.Corner2;
            string info = $"ArenaCorner2 is at ({c2.x:F2}, {c2.y:F2})";
            Logger.Log(info, LogLevel.INFO);
            Feedback.Reply(playerId, info);
        }
    }
}
