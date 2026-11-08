namespace MDS.Systems
{
    // An IBotAi whose target can be set from outside, a seam for a supervisor or a command.
    public interface ITargetableAi
    {
        // Pin a preferred target by player id, or null to clear the pin and auto-acquire the nearest enemy.
        void SetTarget(int? playerId);

        // Whoever the AI settled on last tick, or null if it has nobody.
        int? CurrentTargetId { get; }
    }
}
