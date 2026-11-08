using System;
using UnityEngine;
using MDS.ConsoleCommands;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    // mod_variable MDS:SpawnLine:x,z,rotation[,count][,faction][,class][,ai][,death] - a line at map load.
    public class SpawnLine : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SpawnLine;

        public bool Validate(string value) => TryParse(value, out _, out _);

        public void Execute(string value)
        {
            if (!TryParse(value, out StagedLine line, out string error))
            {
                Logger.Log($"Invalid SpawnLine config '{value}': {error}", LogLevel.WARNING);
                return;
            }

            LineManager.StageLine(line);
        }

        private static bool TryParse(string value, out StagedLine line, out string error)
        {
            line = default;
            error = string.Empty;

            string[] tokens = value.Split(',');
            if (tokens.Length < 3)
            {
                error = "Expected at least x,z,rotation.";
                return false;
            }

            if (!float.TryParse(tokens[0], out float x) ||
                !float.TryParse(tokens[1], out float z) ||
                !float.TryParse(tokens[2], out float rotation))
            {
                error = "x, z, and rotation must be numbers.";
                return false;
            }

            string[] tail = tokens.Length > 3 ? tokens[3..] : Array.Empty<string>();

            if (!BotSpawnArgs.TryResolveLine(tail, out LineSpec spec, out error))
                return false;

            line = new StagedLine(new Vector2(x, z), rotation, spec);
            return true;
        }
    }
}
