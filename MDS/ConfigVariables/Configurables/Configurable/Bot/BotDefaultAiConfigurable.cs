using System;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    // Default AI assigned to bots at spawn when none is specified inline. rc get/set botDefaultAi.
    public class BotDefaultAiConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.BotDefaultAi;

        public BotAiEnum DefaultAi { get; set; } = BotAiEnum.None;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 1 || !EnumParser.TryParseEnumStrict(args[0], out BotAiEnum _))
            {
                errorMessage = $"Invalid AI. Valid: {string.Join(", ", Enum.GetNames(typeof(BotAiEnum)))}. Usage: rc set botDefaultAi <type>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length > 0)
            {
                errorMessage = "botDefaultAi takes no arguments for 'get'. Usage: rc get botDefaultAi";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            EnumParser.TryParseEnumStrict(args[0], out BotAiEnum ai);
            DefaultAi = ai;

            string message = $"Bot default AI set to {DefaultAi}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = $"Bot default AI is {DefaultAi}.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }
    }
}
