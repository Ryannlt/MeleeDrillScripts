using MDS.Systems;

namespace MDS.Events
{
    // Applies a MoveOrder to tracked bots matching a target token.
    public class SetBotMoveOrderEvent : IEvent
    {
        public EventEnum EventName => EventEnum.SetBotMoveOrder;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length != 2 ||
                parameters[0] is not string target ||
                parameters[1] is not MoveOrder)
            {
                errorMessage = "Invalid parameters. Expected: (string target, MoveOrder order).";
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
            var order = (MoveOrder)parameters[1];

            int applied = 0, skipped = 0;
            foreach (int id in BotTargetSelector.Resolve(target))
            {
                BotController controller = FindController(id);
                if (controller?.Ai is ManualAi manual)
                {
                    manual.SetOrder(order); // ManualAi establishes the run mode itself on its next tick
                    applied++;
                }
                else
                {
                    skipped++;
                }
            }

            Logger.Log($"SetBotMoveOrderEvent: order '{order.Kind}' applied to {applied} Manual-AI bot(s), {skipped} skipped (other AI) for target '{target}'.", LogLevel.INFO);
        }

        private static BotController FindController(int id)
        {
            foreach (var c in BotManager.Bots)
                if (c.PlayerId == id)
                    return c;
            return null;
        }
    }
}
