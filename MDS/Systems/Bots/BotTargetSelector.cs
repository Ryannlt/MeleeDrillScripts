using System;
using System.Collections.Generic;
using System.Linq;
using Holdfast.Modding;

// Resolves a bot-target token (all, attacking, defending, faction, playerId) to tracked bots.

namespace MDS.Systems
{
    public static class BotTargetSelector
    {
        public static bool IsValidToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            if (int.TryParse(token, out _)) return true;   // playerId
            if (token.Equals("all", StringComparison.OrdinalIgnoreCase)) return true;

            return FactionTokens.IsToken(token);           // attacking | defending | FactionCountry name
        }

        // Returns the playerIds of tracked bots matching the token (empty if none match / token invalid).
        public static List<int> Resolve(string token)
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(token)) return result;

            if (int.TryParse(token, out int id))
            {
                if (BotManager.Bots.Any(b => b.PlayerId == id))
                    result.Add(id);
                return result;
            }

            if (token.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var b in BotManager.Bots)
                    result.Add(b.PlayerId);
                return result;
            }

            if (FactionTokens.TryResolve(token, out FactionCountry faction))
            {
                foreach (var b in BotManager.Bots)
                    if (b.Bot.Faction == faction)
                        result.Add(b.PlayerId);
            }

            return result;
        }
    }
}
