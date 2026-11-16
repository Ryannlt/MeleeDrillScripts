namespace MDS.Features.Drills
{
    // How a drill starts: with an on-screen countdown or not, and how long god mode covers the move, 0 for none.
    public sealed class DrillTiming
    {
        public DrillTiming(string title, bool countsDown, float godModeSeconds)
        {
            Title = title;
            CountsDown = countsDown;
            GodModeSeconds = godModeSeconds;
        }

        public string Title { get; }

        public bool CountsDown { get; }

        public float GodModeSeconds { get; }

        // Neither needs a routine, so the drill places everyone at once.
        public bool IsInstant => !CountsDown && GodModeSeconds <= 0f;
    }
}
