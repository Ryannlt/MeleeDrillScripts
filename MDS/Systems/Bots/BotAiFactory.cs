using System;
using System.Collections.Generic;

// Creates bot AI instances by type. Mirrors the registration list in BotAiEnum.

namespace MDS.Systems
{
    public static class BotAiFactory
    {
        private static readonly Dictionary<BotAiEnum, Func<IBotAi>> _factories = new();

        static BotAiFactory()
        {
            Register(BotAiEnum.None, () => new NoneAi());
            Register(BotAiEnum.Manual, () => new ManualAi());
            Register(BotAiEnum.StabbingDummy, () => new MeleeDummy());
            Register(BotAiEnum.RiposteDummy, () => new MeleeAi(BotAiEnum.RiposteDummy));
            Register(BotAiEnum.SparringEasy, () => new MeleeAi(BotAiEnum.SparringEasy));
            Register(BotAiEnum.SparringNormal, () => new MeleeAi(BotAiEnum.SparringNormal));
            Register(BotAiEnum.Sparring, () => new MeleeAi(BotAiEnum.Sparring));
            Register(BotAiEnum.Dueling, () => new MeleeAi(BotAiEnum.Dueling));
            Register(BotAiEnum.Feinting, () => new MeleeAi(BotAiEnum.Feinting));
            Register(BotAiEnum.Guardian, () => new MeleeAi(BotAiEnum.Guardian));
            Register(BotAiEnum.GroupEasy, () => new MeleeAi(BotAiEnum.GroupEasy));
            Register(BotAiEnum.GroupNormal, () => new MeleeAi(BotAiEnum.GroupNormal));
            Register(BotAiEnum.Group, () => new MeleeAi(BotAiEnum.Group));
            Register(BotAiEnum.GroupHard, () => new MeleeAi(BotAiEnum.GroupHard));
            Register(BotAiEnum.Test, () => new MeleeAi(BotAiEnum.Test));

            Logger.Log($"Registered {_factories.Count} bot AI type(s).", LogLevel.INFO);
        }

        public static void Register(BotAiEnum type, Func<IBotAi> factory)
        {
            _factories[type] = factory;
        }

        public static bool IsRegistered(BotAiEnum type) => _factories.ContainsKey(type);

        // Returns a fresh AI instance for the given type, falling back to Idle if unregistered.
        public static IBotAi Create(BotAiEnum type)
        {
            if (_factories.TryGetValue(type, out var factory))
            {
                return factory();
            }

            Logger.Log($"No AI registered for '{type}'. Falling back to Idle.", LogLevel.WARNING);
            return new NoneAi();
        }
    }
}
