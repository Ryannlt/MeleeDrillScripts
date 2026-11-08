using MDS.Systems;

namespace MDS.Events
{
    // Rotates all bots matching a target to face a heading.
    public class RotateBotsEvent : IEvent
    {
        public EventEnum EventName => EventEnum.RotateBots;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length != 2 ||
                parameters[0] is not string target ||
                parameters[1] is not float)
            {
                errorMessage = "Invalid parameters. Expected: (string target, float heading).";
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
            float heading = (float)parameters[1];

            var ids = BotTargetSelector.Resolve(target);
            foreach (int id in ids)
                CarbonPlayerCommands.SetInputRotation(id, heading);

            Logger.Log($"RotateBotsEvent: rotated {ids.Count} bot(s) matching '{target}' to {heading:F0} deg.", LogLevel.INFO);
        }
    }
}
