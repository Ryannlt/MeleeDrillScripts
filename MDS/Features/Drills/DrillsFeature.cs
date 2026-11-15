using System;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Popcorn;
using MDS.Features.Drills.Preview;
using MDS.Features.Drills.Settings;
using MDS.Shared.Rounds;

namespace MDS.Features.Drills
{
    public sealed class DrillsFeature : IDisposable
    {
        private readonly RoundTracker _round;
        private readonly Selection _xvx;
        private readonly Selection _groupfight;
        private readonly SpotPreviewOverlay _preview;

        public DrillsFeature(ModContext context, ArenaStore arenas)
        {
            _round = context.Round;
            Random random = context.Runtime.CreateRandom();
            _xvx = Selection.ForXvX(context.Players, random);
            _groupfight = Selection.ForGroupfight(context.Players, random);
            var settings = new DrillSettings(context.Replies, _xvx, _groupfight);
            settings.RegisterWith(context.Settings, context.Config);

            var kit = new DrillKit(context, arenas, settings);
            var runner = new DrillRunner(new LeftoverClearing(kit));
            IDrill[] drills = DrillsOf(kit, new DrillCountdown(context.Console, context.Routines));
            foreach (IDrill drill in drills) context.Commands.Register(new DrillCommand(drill, runner, context.Replies));
            context.Commands.Register(new PreviewCommand(drills, context.Replies));
            _round.RoundStarted += OnRoundStarted;
            if (context.IsServer) return;

            _preview = new SpotPreviewOverlay(context.Runtime.CreateOverlayDrawer(kit.Log), context.Runtime.CreateGround(kit.Log));
            context.ClientMessages.Register(SpotViewMessage.Name, _preview.OnMessage);
        }

        // Open melee and popcorn share one countdown, so they never run at once.
        private IDrill[] DrillsOf(DrillKit kit, DrillCountdown countdown)
        {
            return new IDrill[] { new XvXDrill(kit, _xvx), new GroupfightDrill(kit, _groupfight), new OpenMeleeDrill(kit, countdown), new PopcornDrill(kit, countdown) };
        }

        public void Dispose()
        {
            _round.RoundStarted -= OnRoundStarted;
            _preview?.Dispose();
        }

        private void OnRoundStarted(RoundInfo round)
        {
            _xvx.Reset();
            _groupfight.Reset();
        }
    }
}
