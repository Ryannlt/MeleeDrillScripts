using System;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Custom;
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
        private readonly CustomDrillBook _book;
        private readonly SpotPreviewOverlay _preview;

        // Programs start drills through this.
        public DrillCatalog Catalog { get; private set; }

        public DrillsFeature(ModContext context, ArenaStore arenas)
        {
            _round = context.Round;
            Random random = context.Runtime.CreateRandom();
            _xvx = Selection.ForXvX(context.Players, random);
            _groupfight = Selection.ForGroupfight(context.Players, random);
            _book = new CustomDrillBook(context.Players, random);
            var settings = new DrillSettings(context.Replies, _xvx, _groupfight);
            settings.RegisterWith(context.Settings, context.Config);

            var kit = new DrillKit(context, arenas, settings);
            context.Config.Register(new CustomDrillVariable(_book));
            RegisterCommands(context, kit);
            _round.RoundStarted += OnRoundStarted;
            if (context.IsServer) return;

            _preview = new SpotPreviewOverlay(context.Runtime.CreateOverlayDrawer(kit.Log), context.Runtime.CreateGround(kit.Log));
            context.ClientMessages.Register(SpotViewMessage.Name, _preview.OnMessage);
        }

        private IDrill[] DrillsOf(DrillKit kit, DrillCountdown countdown)
        {
            return new IDrill[] { new XvXDrill(kit, _xvx), new GroupfightDrill(kit, _groupfight), new OpenMeleeDrill(kit, countdown), new PopcornDrill(kit, countdown) };
        }

        // Open melee, popcorn and custom drills share one countdown, so they never run at once.
        // rc customdrill also lists and edits the custom drills, so its command wraps the plain drill command.
        private void RegisterCommands(ModContext context, DrillKit kit)
        {
            var runner = new DrillRunner(new LeftoverClearing(kit));
            var countdown = new DrillCountdown(context.Console, context.Routines);
            var drills = new List<IDrill>(DrillsOf(kit, countdown));
            foreach (IDrill drill in drills) context.Commands.Register(new DrillCommand(drill, runner, context.Replies));

            var custom = new CustomDrills(kit, _book, countdown);
            context.Commands.Register(new CustomDrillCommand(_book, new DrillCommand(custom, runner, context.Replies), context.Replies));
            drills.Add(custom);
            context.Commands.Register(new PreviewCommand(drills, context.Replies));
            Catalog = new DrillCatalog(drills, runner, kit);
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
            _book.ResetSelections();
        }
    }
}
