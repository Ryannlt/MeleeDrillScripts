using MDS.Systems;

namespace MDS.Events
{
    // Sets the death policy of all tracked bots matching a target token.
    public class SetBotDeathPolicyEvent : IEvent
    {
        public EventEnum EventName => EventEnum.SetBotDeathPolicy;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length != 2 ||
                parameters[0] is not string target ||
                parameters[1] is not BotDeathPolicy)
            {
                errorMessage = "Invalid parameters. Expected: (string target, BotDeathPolicy policy).";
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
            var policy = (BotDeathPolicy)parameters[1];

            var ids = BotTargetSelector.Resolve(target);
            foreach (int id in ids)
                BotManager.SetDeath(id, policy);

            Logger.Log($"SetBotDeathPolicyEvent: set policy '{policy}' on {ids.Count} bot(s) matching '{target}'.", LogLevel.INFO);
        }
    }
}
