using System;
using UnityEngine;
using MDS.ConsoleCommands;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    // mod_variable MDS:SpawnBot:x,z,rotation[,faction][,class][,ai][,death] - one bot placed at map load.
    public class SpawnBot : IConfigVariables
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.SpawnBot;

        public bool Validate(string value) => TryParse(value, out _, out _);

        public void Execute(string value)
        {
            if (!TryParse(value, out StagedLine bot, out string error))
            {
                Logger.Log($"Invalid SpawnBot config '{value}': {error}", LogLevel.WARNING);
                return;
            }

            LineManager.StageLine(bot);
        }

        private static bool TryParse(string value, out StagedLine bot, out string error)
        {
            bot = default;
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

            // The shared line parser would take a leading number as a count, which SpawnBot has no use for.
            // Reject it instead of silently ignoring it.
            if (tail.Length > 0 && int.TryParse(tail[0], out _))
            {
                error = "SpawnBot takes no count. Use SpawnLine to spawn more than one bot.";
                return false;
            }

            if (!BotSpawnArgs.TryResolveLine(tail, out LineSpec spec, out error))
                return false;

            spec.Count = 1;
            bot = new StagedLine(new Vector2(x, z), rotation, spec);
            return true;
        }
    }
}
