namespace MDS.Systems
{
    // An IBotAi that escorts a friendly player rather than picking its own fight.
    public interface IGuardianAi
    {
        void SetGuardTarget(int playerId);
    }
}
