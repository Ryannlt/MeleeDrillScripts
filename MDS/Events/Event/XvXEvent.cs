using System.Collections.Generic;
using UnityEngine;
using MDS.Core;
using MDS.Systems;

namespace MDS.Events
{
    public class XvXEvent : IEvent
    {
        public EventEnum EventName => EventEnum.XvX;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length != 6 ||
                !(parameters[0] is List<IPlayer> attackers) ||
                !(parameters[1] is List<IPlayer> defenders) ||
                !(parameters[2] is Vector2 center) ||
                !(parameters[3] is float lineSpacing) ||
                !(parameters[4] is float interSpacing) ||
                !(parameters[5] is float orientationDegrees))
            {
                errorMessage = "Invalid parameters. Expected: List<IPlayer>, List<IPlayer>, Vector2 center, lineSpacing, interSpacing, orientation (degrees).";
                return false;
            }

            if (attackers.Count == 0 || defenders.Count == 0)
            {
                errorMessage = "One or both player groups are empty.";
                return false;
            }

            if (attackers.Exists(p => p == null) || defenders.Exists(p => p == null))
            {
                errorMessage = "One or more players are null.";
                return false;
            }

            if (lineSpacing <= 0)
            {
                errorMessage = "Line spacing must be greater than 0.";
                return false;
            }

            if (interSpacing <= 0)
            {
                errorMessage = "Inter spacing must be greater than 0.";
                return false;
            }

            if (orientationDegrees < -1 || orientationDegrees >= 360)
            {
                errorMessage = "Orientation must be between 0 and 359 degrees, or -1 for random.";
                return false;
            }

            return true;
        }

        public void Trigger(object[] parameters)
        {
            var attackers = (List<IPlayer>)parameters[0];
            var defenders = (List<IPlayer>)parameters[1];
            var center = (Vector2)parameters[2];
            var lineSpacing = (float)parameters[3];
            var interSpacing = (float)parameters[4];
            var orientationDegrees = (float)parameters[5];

            // Handle Random case
            if (Mathf.Approximately(orientationDegrees, -1f))
            {
                orientationDegrees = UnityEngine.Random.Range(0f, 360f);
            }

            float radians = orientationDegrees * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 right = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));

            Vector2 attackerCenter = center - (forward * (lineSpacing / 2f));
            Vector2 defenderCenter = center + (forward * (lineSpacing / 2f));

            PlaceLine(attackers, attackerCenter, interSpacing, right);
            PlaceLine(defenders, defenderCenter, interSpacing, right);

            Logger.Log($"XvXEvent triggered. {attackers.Count} attackers vs {defenders.Count} defenders at orientation {orientationDegrees:F2} degrees.", LogLevel.INFO);
        }

        private void PlaceLine(List<IPlayer> players, Vector2 lineCenter, float interSpacing, Vector2 rightDirection)
        {
            int count = players.Count;
            float startOffset = -((count - 1) * interSpacing) / 2f;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = rightDirection * (startOffset + i * interSpacing);
                Vector2 position2D = lineCenter + offset;
                float y = TerrainSampler.GetYAt(position2D);

                string command = $"teleport {players[i].PlayerId} {position2D.x:F2},{y:F2},{position2D.y:F2}";
                CommandExecutor.ExecuteCommand(command);
            }
        }
    }
}
