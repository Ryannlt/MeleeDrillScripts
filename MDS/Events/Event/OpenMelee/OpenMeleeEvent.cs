using System.Collections;
using System.Linq;
using UnityEngine;
using MDS.Core;
using MDS.Systems;

namespace MDS.Events
{
    public class OpenMeleeEvent : IEvent
    {
        public EventEnum EventName => EventEnum.OpenMelee;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length != 2 || !(parameters[0] is float spacing) || !(parameters[1] is float offset))
            {
                errorMessage = "Invalid parameters. Expected: spacing (float), offset (float)";
                return false;
            }

            if (ArenaManager.Arena == null)
            {
                errorMessage = "Arena is not defined. Use 'rc set ArenaCorner1' and 'rc set ArenaCorner2' or set it via config.";
                return false;
            }

            var players = StateTracker.AttackingPlayers.Concat(StateTracker.DefendingPlayers).ToList();
            if (players.Count == 0)
            {
                errorMessage = "No players available to teleport for Open Melee.";
                return false;
            }

            Vector2 min = ArenaManager.Arena.Min;
            Vector2 max = ArenaManager.Arena.Max;

            min.x += offset;
            min.y += offset;
            max.x -= offset;
            max.y -= offset;

            if (min.x >= max.x || min.y >= max.y)
            {
                errorMessage = "Arena is too small and cannot generate a valid spawn area.";
                return false;
            }

            Rect testArea = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            var testPoints = OrganicPoissonSampler.Generate(testArea, spacing, players.Count, offset);

            if (testPoints.Count < players.Count)
            {
                errorMessage = $"Failed to generate enough spawn points. Needed {players.Count}, got {testPoints.Count}.";
                return false;
            }

            return true;
        }

        public void Trigger(object[] parameters)
        {
            float spacing = (float)parameters[0];
            float offset = (float)parameters[1];

            ModRoutines.Start(Run(spacing, offset), "open melee");
        }

        private IEnumerator Run(float spacing, float offset)
        {
            CommandExecutor.ExecuteCommand("broadcast Open Melee starting in 3...");
            yield return ModRoutines.Wait(1f);

            CommandExecutor.ExecuteCommand("broadcast 2...");
            yield return ModRoutines.Wait(1f);

            CommandExecutor.ExecuteCommand("broadcast 1...");
            yield return ModRoutines.Wait(1f);

            CommandExecutor.ExecuteForCurrentRound("set characterGodMode true");

            RunOpenMeleeLogic(spacing, offset);

            yield return ModRoutines.Wait(2f);
            CommandExecutor.ExecuteForCurrentRound("set characterGodMode false");

            Logger.Log("Open Melee drill completed.", LogLevel.INFO);
        }

        private void RunOpenMeleeLogic(float spacing, float offset)
        {
            var players = StateTracker.AttackingPlayers.Concat(StateTracker.DefendingPlayers).ToList();

            Vector2 min = ArenaManager.Arena.Min;
            Vector2 max = ArenaManager.Arena.Max;

            min.x += offset;
            min.y += offset;
            max.x -= offset;
            max.y -= offset;

            Rect spawnArea = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            var spawnPoints = OrganicPoissonSampler.Generate(spawnArea, spacing, players.Count, offset);

            int spawned = Mathf.Min(players.Count, spawnPoints.Count);

            for (int i = 0; i < spawned; i++)
            {
                var pos = spawnPoints[i];
                float y = TerrainSampler.GetYAt(pos);
                string command = $"teleport {players[i].PlayerId} {pos.x:F2},{y:F2},{pos.y:F2}";
                CommandExecutor.ExecuteCommand(command);
            }

            Logger.Log($"Attempted to teleport {players.Count} players. Successfully teleported {spawned}.");
        }
    }
}
