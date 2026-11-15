namespace MDS.Features.Drills.Picking
{
    public readonly struct Headcount
    {
        private const int WholeTeam = -1;

        public Headcount(int attackers, int defenders)
        {
            Attackers = attackers;
            Defenders = defenders;
        }

        public static Headcount Everyone => new Headcount(WholeTeam, WholeTeam);

        public int Attackers { get; }

        public int Defenders { get; }

        public bool IsEveryone => Attackers == WholeTeam;

        public override string ToString() => IsEveryone ? "everyone" : $"{Attackers}v{Defenders}";
    }
}
