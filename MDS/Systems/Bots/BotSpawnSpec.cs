using Holdfast.Modding;

namespace MDS.Systems
{
    // What a bot IS. Faction and Class are required by carbonPlayers; the rest are optional positional args.
    public class BotSpawnSpec
    {
        public FactionCountry Faction { get; }
        public PlayerClass Class { get; }
        public string Name { get; }        // null/empty => omitted
        public string RegTag { get; }      // null/empty => omitted
        public int? UniformId { get; }     // null => omitted

        public BotSpawnSpec(FactionCountry faction, PlayerClass playerClass, string name = null, string regTag = null, int? uniformId = null)
        {
            Faction = faction;
            Class = playerClass;
            Name = name;
            RegTag = regTag;
            UniformId = uniformId;
        }
    }
}
