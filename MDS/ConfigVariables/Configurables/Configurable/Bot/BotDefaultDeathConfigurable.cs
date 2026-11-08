using System;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    // Default death policy assigned to bots at spawn when none is specified inline.
    // rc get/set botDefaultDeathPolicy.
    public class BotDefaultDeathConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.BotDefaultDeathPolicy;

        public BotDeathPolicy DefaultPolicy { get; set; } = BotDeathPolicy.Kick;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !EnumParser.TryParseEnumStrict(args[0], out BotDeathPolicy _))
            {
                errorMessage = $"Invalid policy. Valid: {string.Join(", ", Enum.GetNames(typeof(BotDeathPolicy)))}. Usage: rc set botDefaultDeathPolicy <deathPolicyEnum>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length > 0)
            {
                errorMessage = "botDefaultDeathPolicy takes no arguments for 'get'. Usage: rc get botDefaultDeathPolicy";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            EnumParser.TryParseEnumStrict(args[0], out BotDeathPolicy policy);
            DefaultPolicy = policy;

            string message = $"Bot default death policy set to {DefaultPolicy}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Bot default death policy is {DefaultPolicy}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }
    }
}
