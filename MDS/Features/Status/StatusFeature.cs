using System;
using MDS.Features.Arenas;
using MDS.Shared.Logging;
using MDS.Shared.Players;
using MDS.Shared.Rounds;

namespace MDS.Features.Status
{
    // rc mds, and a line in the server log for each round.
    public sealed class StatusFeature : IDisposable
    {
        private readonly RoundTracker _round;
        private readonly ModLog _log;

        public StatusFeature(ModContext context, ArenaStore arenas)
        {
            _round = context.Round;
            _log = context.Log.For("Status");
            context.Commands.Register(new MdsCommand(new StatusReport(context, arenas), context.Players, context.Replies));
            _round.RoundStarted += OnRoundStarted;
        }

        public void Dispose() => _round.RoundStarted -= OnRoundStarted;

        private void OnRoundStarted(RoundInfo round)
        {
            string attacking = FactionResolver.DisplayName(round.AttackingFaction);
            string defending = FactionResolver.DisplayName(round.DefendingFaction);
            _log.Info($"Round {round.RoundId} on {round.MapName}, {round.GameMode}. Attacking {attacking}, defending {defending}.");
        }
    }
}
