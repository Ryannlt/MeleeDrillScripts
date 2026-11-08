using Holdfast.Modding;

namespace MDS.Systems
{
    // A bot-line spec parsed from a SpawnLine config variable.
    public struct LineSpec
    {
        public int Count;
        public string FactionToken;   // attacking | defending | FactionCountry name | extension name
        public PlayerClass Class;
        public string Name;           // null/empty => omitted
        public string RegTag;         // null/empty => omitted
        public int? UniformId;        // null => omitted
        public BotAiEnum Ai;
        public BotDeathPolicy Death;
    }
}
