using System.Collections.Generic;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConfigVariables
{
    // Global default values for bot-AI levers, set with 'rc set globalAI'. New bots of that AI start from them.
    public class GlobalAiConfigurable : IConfigurable
    {
        public ConfigurableEnum ConfigurableName => ConfigurableEnum.GlobalAi;

        private readonly Dictionary<string, string> _defaults = new(System.StringComparer.OrdinalIgnoreCase);

        public GlobalAiConfigurable()
        {
            // Seed the built-in defaults for every configurable AI. Add a Seed line when a new AI gets levers.
            Seed(BotAiEnum.StabbingDummy, MeleeDummy.DefaultLevers);
            Seed(BotAiEnum.RiposteDummy, MeleeAi.DefaultLeversFor(BotAiEnum.RiposteDummy));
            Seed(BotAiEnum.SparringEasy, MeleeAi.DefaultLeversFor(BotAiEnum.SparringEasy));
            Seed(BotAiEnum.SparringNormal, MeleeAi.DefaultLeversFor(BotAiEnum.SparringNormal));
            Seed(BotAiEnum.Sparring, MeleeAi.DefaultLeversFor(BotAiEnum.Sparring));
            Seed(BotAiEnum.Dueling, MeleeAi.DefaultLeversFor(BotAiEnum.Dueling));
            Seed(BotAiEnum.Guardian, MeleeAi.DefaultLeversFor(BotAiEnum.Guardian));
            Seed(BotAiEnum.Test, MeleeAi.DefaultLeversFor(BotAiEnum.Test));
        }

        private void Seed(BotAiEnum aiType, IEnumerable<(string name, string value)> levers)
        {
            string ai = aiType.ToString();
            foreach (var (name, value) in levers)
                _defaults[Key(ai, name)] = value;
        }

        private static string Key(string aiType, string lever) => $"{aiType}.{lever}";

        // The stored default for an AI's lever, or the caller's fallback if none is set. Instance accessor.
        public string GetDefault(string aiType, string lever, string fallback) =>
            _defaults.TryGetValue(Key(aiType, lever), out string v) ? v : fallback;

        // Set/overwrite a global default. Used by the config-file path (SetGlobalAi); the live path is Set().
        public void SetDefault(string aiType, string lever, string value) =>
            _defaults[Key(aiType, lever)] = value;

        // Convenience so an AI (in MDS.Systems) can read a default without repeating the registry cast.
        public static string Default(string aiType, string lever, string fallback) =>
            ConfigurableRegistry.TryGet(ConfigurableEnum.GlobalAi, out var c) && c is GlobalAiConfigurable g
                ? g.GetDefault(aiType, lever, fallback)
                : fallback;

        public bool ValidateSet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 3)
            {
                errorMessage = "Usage: rc set globalAI <AiType> <lever> <value>";
                return false;
            }

            return true;
        }

        public bool ValidateGet(string[] args, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (args.Length != 2)
            {
                errorMessage = "Usage: rc get globalAI <AiType> <lever>";
                return false;
            }

            return true;
        }

        public void Set(int playerId, string[] args)
        {
            _defaults[Key(args[0], args[1])] = args[2];

            string message = $"Global AI default '{args[0]} {args[1]}' set to '{args[2]}'.";
            Logger.Log(message, LogLevel.INFO);
            Feedback.Reply(playerId, message);
        }

        public void Get(int playerId, string[] args)
        {
            string message = _defaults.TryGetValue(Key(args[0], args[1]), out string v)
                ? $"Global AI default '{args[0]} {args[1]}' is '{v}'."
                : $"Global AI default '{args[0]} {args[1]}' is not set (AIs use their built-in default).";
            Feedback.Reply(playerId, message);
        }
    }
}
