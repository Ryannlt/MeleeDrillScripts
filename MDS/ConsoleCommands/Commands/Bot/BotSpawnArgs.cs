using System;
using Holdfast.Modding;
using MDS.ConfigVariables;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // Parses the shared positional spawn grammar used by spawn, summon and the line commands.
    public class BotSpawnArgs
    {
        public int Count { get; private set; } = 1;
        public BotSpawnSpec Spec { get; private set; }
        public BotAiEnum Ai { get; private set; }
        public BotDeathPolicy Death { get; private set; }

        // Structural validation only (no caller resolution); used by Validate which has no playerId.
        // allowCount: spawn accepts a leading count; summon does not (it's always a single bot).
        public static bool ValidateShape(string[] args, bool allowCount, out string error) =>
            ParseTokens(args, allowCount, out _, out error);

        // Map-load line resolution with no caller: count, faction, class, ai and death all optional.
        public static bool TryResolveLine(string[] args, out LineSpec spec, out string error)
        {
            spec = default;
            error = string.Empty;

            const string order = "[count] [faction class] [ai] [death] [name [regtag [uniformId]]]";
            int i = 0;

            int count = ((LineBotCountConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.LineBotCount)).LineBotCount;
            if (i < args.Length && int.TryParse(args[i], out int c))
            {
                if (c <= 0) { error = "Count must be a positive integer."; return false; }
                count = c;
                i++;
            }

            string factionToken = "attacking";
            if (i < args.Length && FactionTokens.IsToken(args[i]))
            {
                factionToken = args[i];
                i++;
            }

            PlayerClass playerClass = PlayerClass.ArmyLineInfantry;
            if (i < args.Length && EnumParser.TryParseEnumStrict(args[i], out PlayerClass pc))
            {
                playerClass = pc;
                i++;
            }

            // name is always allowed here (faction always resolves to a default, so the "needs explicit
            // faction" rule from the runtime grammar doesn't apply).
            if (!ParseTail(args, ref i, allowName: true, order,
                    out BotAiEnum? ai, out BotDeathPolicy? death, out string name, out string regTag, out int? uniformId, out error))
                return false;

            spec = new LineSpec
            {
                Count = count,
                FactionToken = factionToken,
                Class = playerClass,
                Name = name,
                RegTag = regTag,
                UniformId = uniformId,
                Ai = ai ?? ((BotDefaultAiConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotDefaultAi)).DefaultAi,
                Death = death ?? ((BotDefaultDeathConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotDefaultDeathPolicy)).DefaultPolicy
            };
            return true;
        }

        // Full resolution incl. caller defaults; used by Execute which has the caller's playerId.
        public static bool TryResolve(string[] args, int callerPlayerId, bool allowCount, out BotSpawnArgs result, out string error)
        {
            result = null;

            if (!ParseTokens(args, allowCount, out Parsed p, out error))
                return false;

            FactionCountry faction;
            PlayerClass playerClass;

            var caller = StateTracker.GetPlayerById(callerPlayerId);

            if (p.HasSpec)
            {
                // Both explicit.
                faction = p.Faction;
                playerClass = p.Class;
            }
            else if (p.HasFaction)
            {
                // Faction explicit, class from caller.
                faction = p.Faction;
                if (caller?.PlayerClass == null)
                {
                    error = "You have no class to copy. Specify one explicitly: rc bot spawn [count] <faction> <class>";
                    return false;
                }
                playerClass = caller.PlayerClass.Value;
            }
            else
            {
                // Both from caller.
                if (caller?.Faction == null || caller.PlayerClass == null)
                {
                    error = "You have no faction/class to copy. Specify them explicitly: rc bot spawn [count] <faction> [class]";
                    return false;
                }
                faction = caller.Faction.Value;
                playerClass = caller.PlayerClass.Value;
            }

            result = new BotSpawnArgs
            {
                Count = p.Count,
                Spec = new BotSpawnSpec(faction, playerClass, p.Name, p.RegTag, p.UniformId),
                Ai = p.Ai ?? ((BotDefaultAiConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotDefaultAi)).DefaultAi,
                Death = p.Death ?? ((BotDefaultDeathConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotDefaultDeathPolicy)).DefaultPolicy
            };
            return true;
        }

        private static bool ParseTokens(string[] args, bool allowCount, out Parsed p, out string error)
        {
            p = new Parsed { Count = 1 };
            error = string.Empty;

            string order = allowCount
                ? "[count] [faction class] [ai] [death] [name [regtag [uniformId]]]"
                : "[faction class] [ai] [death] [name [regtag [uniformId]]]";

            int i = 0;

            // optional leading count (spawn only)
            if (allowCount && i < args.Length && int.TryParse(args[i], out int count))
            {
                if (count <= 0) { error = "Count must be a positive integer."; return false; }
                p.Count = count;
                i++;
            }

            // Optional faction, and an optional class after it. Accepts attacking/defending as well as names.
            if (i < args.Length && FactionTokens.TryResolve(args[i], out FactionCountry faction))
            {
                p.Faction = faction;
                p.HasFaction = true;
                i++;

                // class is optional after faction - if the next token parses as one, consume it
                if (i < args.Length && EnumParser.TryParseEnumStrict(args[i], out PlayerClass playerClass))
                {
                    p.Class = playerClass;
                    p.HasSpec = true;
                    i++;
                }
            }

            if (!ParseTail(args, ref i, allowName: p.HasFaction, order,
                    out BotAiEnum? ai, out BotDeathPolicy? death, out string name, out string regTag, out int? uniformId, out error))
                return false;

            p.Ai = ai;
            p.Death = death;
            p.Name = name;
            p.RegTag = regTag;
            p.UniformId = uniformId;

            return true;
        }

        // Parses the shared positional tail from args[i]: ai, death, name, regtag, uniform.
        private static bool ParseTail(string[] args, ref int i, bool allowName, string order,
            out BotAiEnum? ai, out BotDeathPolicy? death, out string name, out string regTag, out int? uniformId, out string error)
        {
            ai = null;
            death = null;
            name = null;
            regTag = null;
            uniformId = null;
            error = string.Empty;

            // ai slot
            if (i < args.Length)
            {
                if (!EnumParser.TryParseEnumStrict(args[i], out BotAiEnum a))
                {
                    error = $"Unknown AI '{args[i]}'. Valid: {string.Join(", ", Enum.GetNames(typeof(BotAiEnum)))}. Order: {order}.";
                    return false;
                }
                ai = a;
                i++;
            }

            // death slot
            if (i < args.Length)
            {
                if (!EnumParser.TryParseEnumStrict(args[i], out BotDeathPolicy d))
                {
                    error = $"Unknown death policy '{args[i]}'. Valid: {string.Join(", ", Enum.GetNames(typeof(BotDeathPolicy)))}. Order: {order}.";
                    return false;
                }
                death = d;
                i++;
            }

            // name / regtag / uniformId
            if (i < args.Length)
            {
                if (!allowName)
                {
                    error = $"Unexpected argument '{args[i]}'. Order: {order}.";
                    return false;
                }

                name = DecodeSpaces(args[i++]);
                if (i < args.Length) regTag = DecodeSpaces(args[i++]);
                if (i < args.Length)
                {
                    if (!int.TryParse(args[i], out int uid))
                    {
                        error = $"Invalid uniformId '{args[i]}'. Must be an integer.";
                        return false;
                    }
                    uniformId = uid;
                    i++;
                }
                if (i < args.Length)
                {
                    error = $"Too many arguments. Order: {order}.";
                    return false;
                }
            }

            return true;
        }

        // Names and regtags are single positional args, so an underscore stands in for a space.
        private const char SpacePlaceholder = '_';
        private const char SpaceChar = '\u2002';   // EN SPACE (U+2002)

        private static string DecodeSpaces(string token) =>
            token?.Replace(SpacePlaceholder, SpaceChar).Replace(' ', SpaceChar);

        private struct Parsed
        {
            public int Count;
            public FactionCountry Faction;
            public PlayerClass Class;
            public bool HasFaction;  // faction was explicitly provided
            public bool HasSpec;     // both faction AND class were explicitly provided
            public BotAiEnum? Ai;
            public BotDeathPolicy? Death;
            public string Name;
            public string RegTag;
            public int? UniformId;
        }
    }
}
