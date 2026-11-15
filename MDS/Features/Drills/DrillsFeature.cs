using System;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Settings;
using MDS.Shared.Rounds;

namespace MDS.Features.Drills
{
    public sealed class DrillsFeature : IDisposable
    {
        private readonly RoundTracker _round;
        private readonly Selection _xvx;
        private readonly Selection _groupfight;

        public DrillsFeature(ModContext context, ArenaStore arenas)
        {
            _round = context.Round;
            Random random = context.Runtime.CreateRandom();
            _xvx = Selection.ForXvX(context.Players, random);
            _groupfight = Selection.ForGroupfight(context.Players, random);
            var settings = new DrillSettings(context.Replies, _xvx, _groupfight);
            settings.RegisterWith(context.Settings, context.Config);

            var kit = new DrillKit(context, arenas, settings);
            context.Commands.Register(new XvXCommand(kit, _xvx));
            context.Commands.Register(new GroupfightCommand(kit, _groupfight));
            context.Commands.Register(new OpenMeleeCommand(kit, new OpenMeleeDrill(kit, context.Routines)));
            _round.RoundStarted += OnRoundStarted;
        }

        public void Dispose() => _round.RoundStarted -= OnRoundStarted;

        private void OnRoundStarted(RoundInfo round)
        {
            _xvx.Reset();
            _groupfight.Reset();
        }
    }
}
