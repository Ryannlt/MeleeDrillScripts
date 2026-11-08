using UnityEngine;
using MDS.Core;

namespace MDS.ConsoleCommands
{
    // rc spawnLine <x> <z> <rotation> [count] [faction class] [ai] [death] [name [regtag [uniformId]]]
    // Forms a shoulder-to-shoulder line of bots at world (x,z) facing 'rotation' (degrees from North).
    public class SpawnLineCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.SpawnLine;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length < 3)
            {
                errorMessage = "Usage: rc spawnLine <x> <z> <rotation> [count] [faction class] [ai] [death] [name [regtag [uniformId]]]";
                return false;
            }

            if (!float.TryParse(parameters[0], out _) || !float.TryParse(parameters[1], out _) || !float.TryParse(parameters[2], out _))
            {
                errorMessage = "x, z, and rotation must be numbers.";
                return false;
            }

            return LineArgs.ValidateTail(parameters[3..], out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            float x = float.Parse(parameters[0]);
            float z = float.Parse(parameters[1]);
            float rotation = float.Parse(parameters[2]);

            if (!LineArgs.ResolveTail(parameters[3..], playerId, out int count, out var spec, out string error))
            {
                Feedback.Reply(playerId, error);
                return;
            }

            LineArgs.Trigger(playerId, new Vector2(x, z), rotation, count, spec);
        }
    }
}
