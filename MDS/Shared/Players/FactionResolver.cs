using System;
using System.Collections.Generic;
using Holdfast.Modding;
using MDS.Shared.Logging;
using MDS.Shared.Rounds;

namespace MDS.Shared.Players
{
    // ARBritish and ARAmerican are real factions the FactionCountry enum doesn't name.
    public sealed class FactionResolver
    {
        private const FactionCountry ArBritish = (FactionCountry)11;
        private const FactionCountry ArAmerican = (FactionCountry)12;

        private static readonly Dictionary<string, FactionCountry> ExtensionFactions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ARBritish", ArBritish },
            { "ARAmerican", ArAmerican },
        };

        private readonly RoundTracker _round;
        private readonly ModLog _log;

        public FactionResolver(RoundTracker round, ModLog log)
        {
            _round = round;
            _log = log;
        }

        public static string DisplayName(FactionCountry faction)
        {
            if (Enum.IsDefined(typeof(FactionCountry), faction)) return faction.ToString();

            foreach (var pair in ExtensionFactions)
            {
                if (pair.Value == faction) return pair.Key;
            }
            return faction.ToString();
        }

        public bool TryResolve(string token, out FactionCountry faction)
        {
            faction = default;
            if (string.IsNullOrEmpty(token)) return false;
            if (TryResolveSide(token, out faction)) return true;
            if (!ExtensionFactions.TryGetValue(token, out faction) && !EnumParser.TryParseStrict(token, out faction)) return false;
            if (IsActive(faction)) return true;

            // Not one of this round's factions: the game would place it arbitrarily, so attacking is used instead.
            _log.Warning($"Faction '{token}' is not playing this round; using attacking ({DisplayName(_round.Current.AttackingFaction)}).");
            faction = _round.Current.AttackingFaction;
            return true;
        }

        private bool TryResolveSide(string token, out FactionCountry faction)
        {
            bool attacking = token.Equals("attacking", StringComparison.OrdinalIgnoreCase);
            bool defending = token.Equals("defending", StringComparison.OrdinalIgnoreCase);
            faction = attacking ? _round.Current.AttackingFaction : _round.Current.DefendingFaction;
            return attacking || defending;
        }

        private bool IsActive(FactionCountry faction)
        {
            return faction == _round.Current.AttackingFaction || faction == _round.Current.DefendingFaction;
        }
    }
}
