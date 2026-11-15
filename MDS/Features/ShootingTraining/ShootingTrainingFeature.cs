using System;

namespace MDS.Features.ShootingTraining
{
    // rc shootingTraining.
    public sealed class ShootingTrainingFeature : IDisposable
    {
        public ShootingTrainingFeature(ModContext context)
        {
            context.Commands.Register(new ShootingTrainingCommand(new ShootingTrainingMode(context.Console), context.Replies));
        }

        // It subscribes to nothing, so there is nothing to release.
        public void Dispose()
        {
        }
    }
}
