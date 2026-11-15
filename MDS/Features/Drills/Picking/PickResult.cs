namespace MDS.Features.Drills.Picking
{
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
