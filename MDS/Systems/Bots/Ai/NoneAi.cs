namespace MDS.Systems
{
    // The "does nothing" AI. Bot stands in place; emits no movement, look, or actions.
    public class NoneAi : IBotAi
    {
        public BotAiEnum AiType => BotAiEnum.None;

        public BotIntent Decide(BotController self, float deltaTime)
        {
            return BotIntent.Idle;
        }

        // Nothing to carry across a Replace - this AI holds no instruction.
        public void InheritFrom(IBotAi previous) { }
    }
}
