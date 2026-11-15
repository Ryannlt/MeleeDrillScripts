using MDS.Shared.Game;

namespace MDS.Features.ShootingTraining
{
    public sealed class ShootingTrainingMode
    {
        private const string Ammo = "characterInfiniteFirearmAmmo";
        private const string Trajectories = "drawFirearmTrajectories";

        private readonly GameConsole _console;

        public ShootingTrainingMode(GameConsole console)
        {
            _console = console;
        }

        public bool IsOn { get; private set; }

        // Infinite ammo without the trajectories, or the reverse, would stay out of step for the rest of the map.
        public bool TrySet(bool on)
        {
            if (!Set(Ammo, on)) return false;

            if (!Set(Trajectories, on))
            {
                Set(Ammo, IsOn);
                return false;
            }

            IsOn = on;
            return true;
        }

        private bool Set(string setting, bool on) => _console.ExecuteForCurrentRound($"set {setting} {(on ? "true" : "false")}");
    }
}
