using System.Globalization;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Settings
{
    // How many seconds open melee, popcorn and custom drills count down on screen before they start.
    public sealed class CountdownSetting : DrillSetting<int>
    {
        public const int Default = 3;
        private const int Longest = 30;

        public CountdownSetting(Replies replies)
            : base("countdownSeconds", Default, replies)
        {
        }

        protected override string Accepted => $"whole seconds, 1 to {Longest}";

        protected override bool TryRead(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 1 && value <= Longest;
        }
    }
}
