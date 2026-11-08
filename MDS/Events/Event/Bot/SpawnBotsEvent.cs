using MDS.Systems;

namespace MDS.Events
{
    // Spawns one or more bots. The command layer resolves caller context; this takes explicit data only.
    public class SpawnBotsEvent : IEvent
    {
        public EventEnum EventName => EventEnum.SpawnBots;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if ((parameters.Length != 5 && parameters.Length != 6) ||
                !(parameters[0] is null || parameters[0] is BotSpawnSpec) ||
                parameters[1] is not int count ||
                parameters[2] is not BotAiEnum ai ||
                parameters[3] is not BotDeathPolicy ||
                !(parameters[4] is null || parameters[4] is BotPlacement) ||
                (parameters.Length == 6 && !(parameters[5] is null || parameters[5] is int)))
            {
                errorMessage = "Invalid parameters. Expected: (BotSpawnSpec|null, int count, BotAiEnum, BotDeathPolicy, BotPlacement? placement, [int? guardTargetId]).";
                return false;
            }

            if (count <= 0)
            {
                errorMessage = "Count must be greater than 0.";
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
            var spec = parameters[0] as BotSpawnSpec;
            int count = (int)parameters[1];
            var ai = (BotAiEnum)parameters[2];
            var death = (BotDeathPolicy)parameters[3];
            BotPlacement? placement = parameters[4] as BotPlacement?;
            int? guardTargetId = parameters.Length == 6 ? parameters[5] as int? : null;

            BotManager.SpawnBots(count, spec, ai, death, placement, guardTargetId: guardTargetId);
            Logger.Log($"SpawnBotsEvent: {count}x {(spec == null ? "random" : $"{FactionTokens.DisplayName(spec.Faction)}/{spec.Class}")}, AI {ai}, death {death}.", LogLevel.INFO);
        }
    }
}
