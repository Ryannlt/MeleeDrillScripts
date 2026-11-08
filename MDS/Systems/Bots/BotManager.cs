using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MDS.ConfigVariables;
using MDS.Core;

// Central bot subsystem: spawns, tracks, assigns AI and death policy, and runs the one tick coroutine.

namespace MDS.Systems
{
    public static class BotManager
    {
        private const float TickInterval = 0.05f;                 // 20 Hz - tunable
        private const float GhostTimeoutSeconds = 5f;             // drop bots that joined but never spawned
        // Give up on a spawn the game never delivered, or it hands the next joining bot the wrong placement.
        private const float PendingSpawnTimeoutSeconds = 15f;

        // Defaults + timings read live from configurables (settable via rc set / map config variables).
        private static BotAiEnum DefaultAi =>
            ((BotDefaultAiConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotDefaultAi)).DefaultAi;
        private static BotDeathPolicy DefaultDeathPolicy =>
            ((BotDefaultDeathConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotDefaultDeathPolicy)).DefaultPolicy;
        private static float KickDelaySeconds =>
            ((BotKickDelayConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotKickDelay)).KickDelay;
        private static float ReplaceDelaySeconds =>
            ((BotReplaceDelayConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.BotReplaceDelay)).ReplaceDelay;

        private static readonly List<BotController> _bots = new();
        private static readonly Queue<PendingBotSpawn> _pending = new();
        private static ModRoutine _tickRoutine;

        // Bumped whenever tracking is torn down, so delayed work from an earlier round cannot spawn into this one.
        private static int _generation;

        // How long a held replacement will wait for its group's bout to finish, and how often it checks. The cap
        // matters: a drill nobody ever finishes would otherwise swallow the bot permanently.
        private const float ReplacementHoldTimeout = 120f;
        private const float ReplacementHoldPoll = 0.5f;

        // Bots whose death was a casualty of their own group's bout. Only these hold their replacement back.
        private static readonly HashSet<int> _boutCasualties = new();

        // Handed out one per spawn batch, so bots summoned together can be recognised as a formation later. Never
        // reused within a session, so a stale id can't quietly adopt a bot into a group it was never part of.
        private static int _nextGroupId = 1;

        public static IReadOnlyList<BotController> Bots => _bots;

        // Read by other systems that schedule delayed spawns (see LineManager) so they can drop stale work.
        public static int Generation => _generation;

        // Command surface.

        // spec null means a fully random spawn. Requests are queued and paired with joins in arrival order.
        public static void SpawnBots(int count, BotSpawnSpec spec, BotAiEnum ai, BotDeathPolicy death, BotPlacement? placement, IBotAi predecessor = null, int? guardTargetId = null, int groupId = 0)
        {
            float requestedAt = Time.realtimeSinceStartup;
            if (groupId == 0) groupId = _nextGroupId++;

            for (int i = 0; i < count; i++)
                _pending.Enqueue(new PendingBotSpawn { Spec = spec, Ai = ai, Death = death, Placement = placement, Predecessor = predecessor, RequestedAt = requestedAt, GuardTargetId = guardTargetId, GroupId = groupId });

            if (spec == null)
            {
                CarbonPlayerCommands.Spawn(count);
            }
            else
            {
                for (int i = 0; i < count; i++)
                    CarbonPlayerCommands.SpawnSpecific(spec);
            }

            Logger.Log($"Requested {count} bot(s). Spec: {(spec == null ? "random" : $"{FactionTokens.DisplayName(spec.Faction)}/{spec.Class}")}, AI {ai}, death {death}.", LogLevel.INFO);
        }

        // Spawns one bot per placement, all sharing the same spec/ai/death (used by line formations).
        // guardTargetId is set when the line was summoned onto a player, so a guardian AI escorts them.
        public static void SpawnBotsAt(IReadOnlyList<BotPlacement> placements, BotSpawnSpec spec, BotAiEnum ai, BotDeathPolicy death, int? guardTargetId = null)
        {
            if (spec == null)
            {
                Logger.Log("SpawnBotsAt requires a spec (formations can't use random spawn).", LogLevel.WARNING);
                return;
            }

            float requestedAt = Time.realtimeSinceStartup;
            int groupId = _nextGroupId++;   // a line is one formation

            foreach (var placement in placements)
            {
                _pending.Enqueue(new PendingBotSpawn { Spec = spec, Ai = ai, Death = death, Placement = placement, RequestedAt = requestedAt, GuardTargetId = guardTargetId, GroupId = groupId });
                CarbonPlayerCommands.SpawnSpecific(spec);
            }

            Logger.Log($"Requested {placements.Count} bot(s) in formation. Spec: {FactionTokens.DisplayName(spec.Faction)}/{spec.Class}, AI {ai}, death {death}.", LogLevel.INFO);
        }

        public static bool SetAi(int playerId, BotAiEnum ai)
        {
            var bot = _bots.FirstOrDefault(b => b.PlayerId == playerId);
            if (bot == null) return false;

            bot.SetAi(BotAiFactory.Create(ai));
            return true;
        }

        public static bool SetDeath(int playerId, BotDeathPolicy policy)
        {
            var bot = _bots.FirstOrDefault(b => b.PlayerId == playerId);
            if (bot == null) return false;

            bot.SetDeathPolicy(policy);
            return true;
        }

        public static bool RemoveBot(int playerId)
        {
            if (_bots.All(b => b.PlayerId != playerId)) return false;

            CarbonPlayerCommands.Despawn(playerId); // serverAdmin kick; disconnect callback also untracks (idempotent)
            Untrack(playerId);
            return true;
        }

        public static void RemoveAll()
        {
            foreach (var bot in _bots.ToList())
                CarbonPlayerCommands.Despawn(bot.PlayerId);

            _generation++; // drop any scheduled replacement, so a removed bot can't come back
            _bots.Clear();
            _pending.Clear();
            SquadCoordinator.Reset();   // the formations went with the bots; Reset() does the same on a new round
            StopTicking();
        }

        // Lifecycle hooks (called by StateTracker).

        public static void OnBotJoined(IPlayer bot)
        {
            if (_bots.Any(b => b.PlayerId == bot.PlayerId)) return;

            // Clear out expired requests first: this bot must never be paired with one from minutes ago.
            PrunePendingSpawns();

            // Joining bots are paired with spawn requests in arrival order.
            if (_pending.Count == 0)
                Logger.Log($"Bot {bot.PlayerId} joined with no pending spawn request; using defaults (AI {DefaultAi}).", LogLevel.WARNING);

            PendingBotSpawn p = _pending.Count > 0
                ? _pending.Dequeue()
                : new PendingBotSpawn { Spec = null, Ai = DefaultAi, Death = DefaultDeathPolicy, Placement = null };

            IBotAi ai = BotAiFactory.Create(p.Ai);

            // A Replace replacement resumes the standing order of the bot it replaces; without this it would
            // come back identical in every respect except that it had forgotten what it was doing.
            if (p.Predecessor != null)
                ai.InheritFrom(p.Predecessor);

            // A bot summoned onto a player escorts them, if its AI knows how to.
            if (p.GuardTargetId is int wardId && ai is IGuardianAi guardian)
                guardian.SetGuardTarget(wardId);

            _bots.Add(new BotController(bot, ai, p.Spec, p.Death, p.Placement, p.GroupId));
            Logger.Log($"Bot {bot.PlayerId} tracked (AI {p.Ai}, death {p.Death}). Active bots: {_bots.Count}.", LogLevel.INFO);

            EnsureTicking();
        }

        public static void OnBotSpawned(IPlayer bot)
        {
            var controller = _bots.FirstOrDefault(b => b.PlayerId == bot.PlayerId);
            controller?.OnSpawned();
        }

        public static void OnBotDied(IPlayer bot)
        {
            var controller = _bots.FirstOrDefault(b => b.PlayerId == bot.PlayerId);
            if (controller == null) return;

            // A kick is already scheduled (e.g. it was killed again during the delay) - ignore.
            if (controller.IsAwaitingKick) return;

            // Capture state at the moment of death - position/GameObject may be gone moments later.
            BotDeathPolicy policy = controller.DeathPolicy;
            Vector3? deathPos = controller.Position;
            float? deathHeading = controller.Heading;
            BotSpawnSpec spec = BuildReplacementSpec(controller);
            BotAiEnum ai = controller.AiType;
            IBotAi predecessorAi = controller.Ai;   // Replace: lets the replacement resume its standing order
            int groupId = controller.GroupId;       // Replace: keeps the replacement in its formation

            // Whether the replacement is allowed to wait out the bout; whether it does is decided once the killer is known.
            bool holdReplacement = groupId != 0 && controller.Ai is MeleeAi melee && melee.HoldReplacement;

            switch (policy)
            {
                case BotDeathPolicy.None:
                    // Do nothing - the game auto-respawns it and the bot stays tracked.
                    Logger.Log($"Bot {bot.PlayerId} died (policy: None). No action taken.", LogLevel.DEBUG);
                    break;

                case BotDeathPolicy.Kick:
                    // Keep it tracked until the kick fires, so an auto-respawn during the delay re-joins
                    // as an already-tracked bot (guarded in OnBotJoined) rather than resetting to defaults.
                    Logger.Log($"Bot {bot.PlayerId} died (policy: Kick). Kicking in {KickDelaySeconds}s.", LogLevel.INFO);
                    controller.MarkAwaitingKick();
                    ScheduleDeathKick(bot.PlayerId, null, ai, policy, null);
                    break;

                case BotDeathPolicy.Replace:
                    controller.MarkAwaitingKick();

                    if (spec == null || !deathPos.HasValue)
                    {
                        string reason = spec == null ? "has no spawn spec (random bot)" : "position unavailable";
                        Logger.Log($"Bot {bot.PlayerId} died (policy: Replace) but {reason}. Kicking only (in {KickDelaySeconds}s).", LogLevel.WARNING);
                        ScheduleDeathKick(bot.PlayerId, null, ai, policy, null);
                        break;
                    }

                    Logger.Log($"Bot {bot.PlayerId} died (policy: Replace). Kicking in {KickDelaySeconds}s and respawning at {deathPos.Value}.", LogLevel.INFO);
                    ScheduleDeathKick(bot.PlayerId, spec, ai, policy, new BotPlacement(deathPos.Value, deathHeading), predecessorAi, groupId, holdReplacement);
                    break;
            }
        }

        // A player killed someone. Only interesting when the victim was one of our bots in a formation.
        public static void OnPlayerKilled(int killerPlayerId, int victimPlayerId)
        {
            var victim = _bots.FirstOrDefault(b => b.PlayerId == victimPlayerId);
            if (victim == null) return;

            // A bot killing a bot of its own faction is the friendly-fire case. Unknown factions count as a match.
            var killer = _bots.FirstOrDefault(b => b.PlayerId == killerPlayerId);
            bool sameSide = killer != null
                && (killer.Bot.Faction == null || victim.Bot.Faction == null
                    || killer.Bot.Faction == victim.Bot.Faction);

            if (sameSide && killer.Position is Vector3 kp && victim.Position is Vector3 vp && killer.Heading is float kh)
                MeleeProbe.LogFriendlyFire(killerPlayerId, victimPlayerId, killer.AiType,
                    new Vector2(kp.x, kp.z), kh, new Vector2(vp.x, vp.z));

            SquadCoordinator.OnMemberKilled(victim.GroupId, victimPlayerId, killerPlayerId);

            // Only a death at the hands of the bout's own opponent holds a replacement back.
            if (SquadCoordinator.IsBoutOpponent(victim.GroupId, killerPlayerId))
                _boutCasualties.Add(victimPlayerId);
        }

        // A player respawned, so anyone fighting them was fighting a body that no longer exists.
        public static void OnTargetRespawned(int playerId)
        {
            SquadCoordinator.OnTargetRespawned(playerId);

            // The coordinator covers formations; a bot with squad and post both off is not in one.
            for (int i = 0; i < _bots.Count; i++)
                if (_bots[i].Ai is ISquadMember member && member.ProvokedBy == playerId)
                    member.StandDown();
        }

        public static void OnBotDisconnected(int playerId)
        {
            Untrack(playerId);
        }

        // Called on new round (StateTracker.NewRoundCleanup); bots are dropped from tracking.
        public static void Reset()
        {
            _generation++;
            _bots.Clear();
            _pending.Clear();
            _boutCasualties.Clear();
            CharacterTracker.Reset();
            MeleeProbe.Reset();
            MeleeDebug.Reset();
            CombatTracker.Reset();
            SquadCoordinator.Reset();
            StopTicking();
            Logger.Log("BotManager reset.", LogLevel.DEBUG);
        }

        // Internals.

        // Drops spawn requests the game never delivered a bot for.
        private static List<BotController> ShuffledBots()
        {
            List<BotController> order = _bots.ToList();

            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                BotController swap = order[i];
                order[i] = order[j];
                order[j] = swap;
            }

            return order;
        }

        private static void PrunePendingSpawns()
        {
            float now = Time.realtimeSinceStartup;

            while (_pending.Count > 0 && now - _pending.Peek().RequestedAt > PendingSpawnTimeoutSeconds)
            {
                PendingBotSpawn stale = _pending.Dequeue();
                Logger.Log($"Spawn request (spec {(stale.Spec == null ? "random" : $"{FactionTokens.DisplayName(stale.Spec.Faction)}/{stale.Spec.Class}")}, AI {stale.Ai}) never produced a bot within {PendingSpawnTimeoutSeconds}s - discarded.", LogLevel.WARNING);
            }
        }

        private static void Untrack(int playerId)
        {
            int groupId = _bots.FirstOrDefault(b => b.PlayerId == playerId)?.GroupId ?? 0;

            int removed = _bots.RemoveAll(b => b.PlayerId == playerId);
            if (removed == 0) return;

            // That may have been the group's last member.
            if (groupId != 0 && _bots.All(b => b.GroupId != groupId))
                SquadCoordinator.OnGroupEmptied(groupId);

            Logger.Log($"Bot {playerId} untracked. Active bots: {_bots.Count}.", LogLevel.INFO);
            if (_bots.Count == 0) StopTicking();
        }

        // The kick is delayed so the game can credit the killer and play the death out first.
        private static void ScheduleDeathKick(int playerId, BotSpawnSpec replacementSpec, BotAiEnum ai, BotDeathPolicy death, BotPlacement? placement, IBotAi predecessor = null, int groupId = 0, bool holdReplacement = false)
        {
            if (!UnityEngine.Application.isPlaying)
            {
                // Edit-mode (tests): nothing steps routines - do it synchronously, no delays.
                KickBot(playerId);
                SpawnReplacement(replacementSpec, ai, death, placement, predecessor, groupId);
                return;
            }

            ModRoutines.Start(DeathKickRoutine(playerId, replacementSpec, ai, death, placement, predecessor, groupId, holdReplacement), "bot death kick");
        }

        private static IEnumerator DeathKickRoutine(int playerId, BotSpawnSpec replacementSpec, BotAiEnum ai, BotDeathPolicy death, BotPlacement? placement, IBotAi predecessor, int groupId, bool holdReplacement)
        {
            // A new round can start during these waits; drop the work if it did.
            int generation = _generation;

            // 1) Wait so the killer is credited and the death plays out before the bot is removed.
            yield return ModRoutines.Wait(KickDelaySeconds);
            if (generation != _generation) yield break;
            KickBot(playerId);

            // 2) Spawn the replacement only after a short gap, so the kick fully frees the bot slot
            //    first (kicking and respawning back-to-back can make the spawnSpecific fail).
            yield return ModRoutines.Wait(ReplaceDelaySeconds);
            if (generation != _generation) yield break;

            // Sit out the rest of the bout, but only for a bot the bout itself killed. Capped either way.
            bool boutCasualty = _boutCasualties.Remove(playerId);
            if (holdReplacement && !boutCasualty)
                Logger.Log($"Bot {playerId} was not killed by its group's opponent; replacing without holding.", LogLevel.DEBUG);

            if (holdReplacement && boutCasualty)
            {
                float waitUntil = Time.realtimeSinceStartup + ReplacementHoldTimeout;
                while (!GroupBetweenBouts(groupId) && Time.realtimeSinceStartup < waitUntil)
                {
                    yield return ModRoutines.Wait(ReplacementHoldPoll);
                    if (generation != _generation) yield break;
                }
            }

            SpawnReplacement(replacementSpec, ai, death, placement, predecessor, groupId);
        }

        private static void KickBot(int playerId)
        {
            // Untrack only now (not at death time) so the bot stays tracked through the delay.
            Untrack(playerId);
            CarbonPlayerCommands.Despawn(playerId);
            Logger.Log($"Bot {playerId} kicked; replacement (if any) in {ReplaceDelaySeconds}s.", LogLevel.DEBUG);
        }

        // Whether a held replacement may appear yet. The live-member check has to happen here, not in the coordinator.
        private static bool GroupBetweenBouts(int groupId) =>
            _bots.All(b => b.GroupId != groupId) || SquadCoordinator.IsBoutOver(groupId);

        private static void SpawnReplacement(BotSpawnSpec replacementSpec, BotAiEnum ai, BotDeathPolicy death, BotPlacement? placement, IBotAi predecessor, int groupId)
        {
            if (replacementSpec != null && placement.HasValue)
                SpawnBots(1, replacementSpec, ai, death, placement, predecessor, groupId: groupId);
        }

        // Builds the Replace spec: the requested faction, class, name and regtag, but the uniform it actually wore.
        // The game never reports a bot's own name, so an unnamed bot's replacement is given a new one.
        private static BotSpawnSpec BuildReplacementSpec(BotController controller)
        {
            var spec = controller.Spec;
            if (spec == null) return null;

            var bot = controller.Bot;
            return new BotSpawnSpec(
                spec.Faction,
                spec.Class,
                spec.Name,
                spec.RegTag,
                bot.UniformId);
        }

        private static void EnsureTicking()
        {
            if ((_tickRoutine == null || _tickRoutine.IsFinished) && _bots.Count > 0)
                _tickRoutine = ModRoutines.Start(TickLoop(), "bot tick");
        }

        private static void StopTicking()
        {
            if (_tickRoutine == null) return;

            ModRoutines.Stop(_tickRoutine);
            _tickRoutine = null;
        }

        private static IEnumerator TickLoop()
        {
            int generation = _generation;

            while (_bots.Count > 0)
            {
                if (generation != _generation) yield break;

                float now = Time.realtimeSinceStartup;

                // Report an unanswered spawn request promptly, even when no further bots join to trigger it.
                PrunePendingSpawns();

                // One shared snapshot of all spawned players/bots per tick, so neighbour-aware steering
                // (separation, collision avoidance) sees everyone without an O(n) gather per bot.
                CharacterTracker.Refresh(TickInterval);

                // Lay out the squads before the bots decide, so each one reads a slot built from this tick.
                SquadCoordinator.Refresh(_bots, TickInterval);

                // Shuffled, so bots deciding on the same tick do not always resolve in the same order.
                foreach (var bot in ShuffledBots())
                {
                    // Self-heal: a replacement that joined but never spawned (game rejected it, e.g. a
                    // carbon-bot limit) would otherwise stay tracked forever as a ghost.
                    if (bot.IsUnspawnedGhost(now, GhostTimeoutSeconds))
                    {
                        Logger.Log($"Bot {bot.PlayerId} joined but never spawned within {GhostTimeoutSeconds}s - dropping as ghost.", LogLevel.WARNING);
                        Untrack(bot.PlayerId);
                        continue;
                    }

                    bot.Tick(TickInterval);
                }

                yield return ModRoutines.Wait(TickInterval);
            }

            _tickRoutine = null;
        }

        private struct PendingBotSpawn
        {
            public BotSpawnSpec Spec;
            public BotAiEnum Ai;
            public BotDeathPolicy Death;
            public BotPlacement? Placement;
            public IBotAi Predecessor;   // Replace only: AI of the bot being replaced, for InheritFrom
            public float RequestedAt;    // realtime the spawn was asked for, so a request that never lands expires
            public int? GuardTargetId;   // the player a guardian AI should escort, from the summon that placed it
            public int GroupId;          // the spawn batch, so bots summoned together fight as one formation
        }
    }
}
