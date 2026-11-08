using MDS.Systems;

namespace MDS.Events
{
    // Sets the AI of all tracked bots matching a target token.
    public class SetBotAiEvent : IEvent
    {
        public EventEnum EventName => EventEnum.SetBotAi;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length != 2 ||
                parameters[0] is not string target ||
                parameters[1] is not BotAiEnum ai)
            {
                errorMessage = "Invalid parameters. Expected: (string target, BotAiEnum ai).";
                return false;
            }

            if (!BotTargetSelector.IsValidToken(target))
            {
                errorMessage = $"Invalid target '{target}'. Use a playerId, all, attacking, defending, or a faction name.";
                return false;
            }

            if (!BotAiFactory.IsRegistered(ai))
            {
                errorMessage = $"AI type '{ai}' is not registered.";
                return false;
            }

            return true;
        }

        public void Trigger(object[] parameters)
        {
            string target = (string)parameters[0];
            var ai = (BotAiEnum)parameters[1];

            var ids = BotTargetSelector.Resolve(target);
            foreach (int id in ids)
                BotManager.SetAi(id, ai);

            Logger.Log($"SetBotAiEvent: set AI '{ai}' on {ids.Count} bot(s) matching '{target}'.", LogLevel.INFO);
        }
    }
}
