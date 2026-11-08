namespace MDS.Systems
{
    // What happens when a bot dies. Orthogonal to AI. Executed in Slice B (death detection).
    public enum BotDeathPolicy
    {
        None,          // do nothing - the game auto-respawns it (faction/class will randomize)
        Kick,          // kick on death; the bot is gone
        Replace        // kick + respawn an identical bot (same spec) at the death location
    }
}
