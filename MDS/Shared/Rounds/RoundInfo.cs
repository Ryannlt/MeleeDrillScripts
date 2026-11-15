using Holdfast.Modding;

namespace MDS.Shared.Rounds
{
    // The round as OnRoundDetails describes it.
    public sealed class RoundInfo
    {
        public int RoundId { get; set; }

        public string ServerName { get; set; } = string.Empty;

        public string MapName { get; set; } = string.Empty;

        public FactionCountry AttackingFaction { get; set; }

        public FactionCountry DefendingFaction { get; set; }

        public GameplayMode GameMode { get; set; }

        public GameType GameType { get; set; }
    }
}
