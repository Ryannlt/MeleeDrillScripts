namespace MDS.Systems
{
    // The AI: pure decision logic. Observes the world and returns an intent, never touches the engine.
    public interface IBotAi
    {
        BotAiEnum AiType { get; }

        // Decide what the bot should do this tick. 'self' exposes the bot's identity/position;
        // implementations may also read StateTracker for targets. deltaTime is seconds since last tick.
        BotIntent Decide(BotController self, float deltaTime);

        // Called on a Replace replacement with the previous bot's AI, so it can carry over what should persist.
        void InheritFrom(IBotAi previous);
    }
}
