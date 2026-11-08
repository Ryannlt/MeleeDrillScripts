using System.Collections.Generic;
using Holdfast.Modding;
using MDS.Core;

namespace MDS.Systems
{
    
    public static class FactionTokens
    {
        private static readonly Dictionary<string, FactionCountry> ExtensionFactions =
            new(System.StringComparer.OrdinalIgnoreCase)
            {
                { "ARBritish", (FactionCountry)11 },
                { "ARAmerican", (FactionCountry)12 },
            };

        public static bool IsToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            if (token.Equals("attacking", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (token.Equals("defending", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (ExtensionFactions.ContainsKey(token)) return true;
            return EnumParser.TryParseEnumStrict(token, out FactionCountry _);
        }

        public static bool TryResolve(string token, out FactionCountry faction)
        {
            faction = default;
            if (string.IsNullOrEmpty(token)) return false;

            if (token.Equals("attacking", System.StringComparison.OrdinalIgnoreCase))
            {
                faction = StateTracker.AttackingFaction;
                return true;
            }
            if (token.Equals("defending", System.StringComparison.OrdinalIgnoreCase))
            {
                faction = StateTracker.DefendingFaction;
                return true;
            }

            // A named or extension faction. Either form gives a concrete FactionCountry value.
            if (!ExtensionFactions.TryGetValue(token, out faction) &&
                !EnumParser.TryParseEnumStrict(token, out faction))
                return false;

            // Valid, but maybe not active this round. If it's not one of the two active factions the game
            // assigns the bot arbitrarily, so fall back to attacking.
            if (!IsActive(faction))
            {
                Logger.Log($"Faction '{token}' is not active this round; defaulting to attacking ({DisplayName(StateTracker.AttackingFaction)}).", LogLevel.WARNING);
                faction = StateTracker.AttackingFaction;
            }
            return true;
        }

        // The human-readable faction name for logs and messages.
        public static string DisplayName(FactionCountry faction)
        {
            if (System.Enum.IsDefined(typeof(FactionCountry), faction))
                return faction.ToString();

            foreach (var kvp in ExtensionFactions)
                if (kvp.Value == faction)
                    return kvp.Key;

            return faction.ToString();
        }

        private static bool IsActive(FactionCountry faction) =>
            faction == StateTracker.AttackingFaction || faction == StateTracker.DefendingFaction;
    }
}
