namespace MDS.Features.Drills.Picking
{
    // A lineup, or the reason there isn't one, worded for the admin who ran the drill.
    public sealed class PickResult
    {
        private PickResult(Lineup lineup, string error)
        {
            Lineup = lineup;
            Error = error;
        }

        public Lineup Lineup { get; }

        public string Error { get; }

        public bool Succeeded => Lineup != null;

        public static PickResult Ok(Lineup lineup) => new PickResult(lineup, string.Empty);

        public static PickResult Failed(string error) => new PickResult(null, error);
    }
}
