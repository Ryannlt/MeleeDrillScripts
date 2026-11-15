using MDS.Shared.Game;

namespace MDS.Features.ShootingTraining
{
    // Infinite firearm ammo and drawn bullet trajectories, switched together for the current map only.
    public sealed class ShootingTrainingMode
    {
        private readonly GameConsole _console;

        public ShootingTrainingMode(GameConsole console)
        {
            _console = console;
        }

        public bool IsOn { get; private set; }

        // The state only changes when the game takes both settings.
        public bool TrySet(bool on)
        {
            string value = on ? "true" : "false";
            bool ammo = _console.ExecuteForCurrentRound($"set characterInfiniteFirearmAmmo {value}");
            bool trajectories = _console.ExecuteForCurrentRound($"set drawFirearmTrajectories {value}");
            if (!ammo || !trajectories) return false;

            IsOn = on;
            return true;
        }
    }
}
