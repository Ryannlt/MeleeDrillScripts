using System;
using MDS.Systems;

namespace MDS.Events
{
    // Removes all tracked bots matching a target token.
    public class RemoveBotsEvent : IEvent
    {
        public EventEnum EventName => EventEnum.RemoveBots;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length != 1 || parameters[0] is not string target)
            {
                errorMessage = "Invalid parameters. Expected: (string target).";
                return false;
            }

            if (!BotTargetSelector.IsValidToken(target))
            {
                errorMessage = $"Invalid target '{target}'. Use a playerId, all, attacking, defending, or a faction name.";
                return false;
            }

            return true;
        }

        public void Trigger(object[] parameters)
        {
            string target = (string)parameters[0];

            if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                BotManager.RemoveAll();   // kicks all + cancels pending spawns
                Logger.Log("RemoveBotsEvent: removed all bots.", LogLevel.INFO);
                return;
            }

            var ids = BotTargetSelector.Resolve(target);
            foreach (int id in ids)
                BotManager.RemoveBot(id);

            Logger.Log($"RemoveBotsEvent: removed {ids.Count} bot(s) matching '{target}'.", LogLevel.INFO);
        }
    }
}
