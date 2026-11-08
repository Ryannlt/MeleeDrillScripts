using System.Collections.Generic;
using Holdfast.Modding;
using UnityEngine;

namespace MDS.Systems
{
    // Melee combat AI: faces its target, holds spacing, blocks, and throws stabs. Levers in MeleeAi.Levers.cs.
    public partial class MeleeAi : IBotAi, IConfigurableAi, ITargetableAi, IGuardianAi, ISquadMember
    {
        // How hard the push away from other bots counts next to the movement it is blended into.
        private const float SeparationWeight = 1.5f;

        // A reserve is spread by pushback rather than placed, so its comfort zone is wider and pushes harder.
        // This is what turns a handful of extras into a ring around the enemy instead of a queue on one side.
        private const float LooseSeparationScale = 3.5f;
        private const float LooseSeparationBoost = 2f;

        // Push below this is dropped. The falloff never quite reaches zero, so without a floor a reserve creeps
        // at a few percent throttle forever, dragging the loose slot along behind it and never settling. The
        // floor also turns the soft falloff into a real spacing: pushback only starts inside about two thirds
        // of the comfort zone.
        private const float LooseSeparationFloor = 0.35f;

        // Reused when gathering neighbour positions for Steering.Separation, so a 20 Hz tick does not allocate.
        private static readonly List<Vector2> _separationNeighbours = new();

        // Strike-mechanic timings, measured from the engine. Not levers.
        private const float WindupSeconds = 0.15f;    // hold the windup this long (one MeleeStrike) before releasing
        // How often a held chamber is refreshed. Re-sending MeleeStrike restarts the windup, which is what keeps
        // the pose up; provisional until measured in game.
        private const float HoldSustainInterval = 0.1f;
        // How long a thrown-away chamber stays worth punishing. Holding through it hands the opening back.
        private const float FeintPunishWindow = 1f;
        // A committed stab occupies the bot this long whether it misses or is blocked. Timed from release.
        private const float MissedStabDuration = 1.5f;
        // A chamber stuns both players for characterMeleeBlockStunTime, 0.8 by default. The next windup starts just before that ends.
        private const float ClashStunSeconds = 0.75f;
        // Extra time we refuse to block after throwing first. Nerve, not geometry - not part of blade-live time.
        private const float FirstStrikeCommitBonus = 0.4f; // extra commit time when we threw first, so we back our stab instead of flinching into a guard
        private const float MinBlockHold = 0.35f;      // keep a raised guard up at least this long so it reads and animates
        private const float AimOffset = 0.3f;          // sideways aim shift while striking to centre a right-hand stab, metres

        // Striking on the first tick out of a spawn plays the animation wrong.
        private const float SpawnStrikeDelay = 1.0f;

        // Blade geometry, resolved from the game's baked table against aimPitch. These are the pitch-0 values.
        private float BladeReach = 2.065f;   // how far the blade line extends from the body, metres
        private float BladeBearing = 16.1f;  // degrees the blade sits off the facing, positive to the bot's right

        // How long after our release the blade is treated as out. Measured in our frame, not the game's.
        private const float StrikeCommitWindow = 0.9f;

        // Movement feel. Kept const for now.
        private const float RangeTolerance = 0.3f;     // slop band around the hold range where the bot just stands
        private const float BackoffThrottle = 1.0f;    // back off at full speed so an approaching attacker can't just fill the gap
        private const float MoveChangeDelay = 0.2f;    // reaction beat before adopting a closer hold range; retreating is immediate
        private const float MoveHysteresis = 0.5f;     // ignore range jitter smaller than this when deciding to advance
        private const float StrikerLockRange = 3f;     // attacker-lock only triggers for strikers within this when targetRange is unlimited
        private const float SlotDeadband = 0.15f;      // close enough to the slot to stop rather than shuffle
        private const float LooseSlotDeadband = 0.5f;  // a reserve holds a rough station, not a place to the centimetre

        // The engine refuses a melee hit past this vertical gap, before any of its raycasts run.
        private const float MateVerticalReach = 1.5f;

        // How often a bot picks a new personal misplacement, and how fast it slides to it.
        private const float SlotBiasRerollMin = 2f;    // seconds
        private const float SlotBiasRerollMax = 5f;
        private const float SlotBiasDriftRate = 0.35f; // metres per second

        // The tuning levers, the per-preset defaults and the IConfigurableAi plumbing all live in
        // MeleeAi.Levers.cs. Everything below is runtime state: what this bot is doing right now.

        // While guarding, sides are judged from the ward rather than from the bot itself.
        private FactionCountry? _wardFaction;

        private readonly BotAiEnum _aiType;
        private float _offensiveRange;                // close spacing actually in use, re-rolled for jitter
        private float _defensiveRange;                // further spacing actually in use, re-rolled for jitter
        private float _appliedRange;                  // the hold range actually driving movement (lags on advancing)
        private float _advanceWantedSince = -1f;      // realtime we first wanted to advance to a closer range (-1 = not)

        private int? _assignedTargetId;       // pin from ITargetableAi; preferred over nearest while alive
        private int? _targetId;               // last resolved target (sticky or closest, per _stickyTarget)
        private int? _lockTargetId;           // attacker-lock: a mid-strike player we stick to through the exchange
        private float _lockUntil;             // realtime the attacker-lock expires
        private bool _engaged;                // engageOnAttack runtime: provoked and fighting. Not inherited, so a replacement starts passive.
        private int? _engagedTargetId;        // engageOnAttack runtime: the attacker we fight until it (or we) die
        private float _lastEngageBlock;       // engageOnAttack runtime: block time we last engaged off (dedupe)

        // Who last hit our guard. Tracked for every preset, since a station wakes its group from it.
        private int? _provokedBy;
        private float _lastProvokeBlock;
        private bool _stancePending = true;   // issue EnableCombatStance once, on the first spawned tick
        private bool _runPending = true;      // establish the sticky run toggle once, on first engagement
        private string _blockToken;           // the block playerAction we're currently holding (null = not blocking)

        // Attack sequencer state (see StepAttack).
        private enum AttackPhase { None, Chamber, Swing, Feint }

        // What the bot is doing, as opposed to where its swing is. Keeping the two apart is the point: a stance
        // added here does not have to be crossed with every strike-state flag in Decide.
        private enum Posture
        {
            Waiting,     // engageOnAttack and not yet provoked: face and block, throw nothing
            BackingOff,  // provoked, re-establishing range and formation before it will swing
            Holding,     // set and in range, but the group's engage delay has not run out yet
            Withdrawing, // breaking off from a live enemy: still blocks and counters, but gives ground and won't press
            Fighting
        }
        private AttackPhase _attackPhase;
        private string _attackDir;            // "High" or "Low" for the strike in progress
        private float _attackCooldownUntil;   // realtime before which we won't start another strike
        private float _strikeReadyAt;         // realtime the bot may first swing at all, set on spawn (SpawnStrikeDelay)
        private bool _releasePending;         // a dropped windup still needs releasing, or the engine keeps cycling it
        private float _chamberStartedAt;      // realtime our windup began (for the "I threw first" read)
        private float _executeAt;             // realtime to release the held windup
        private float _holdUntil;             // realtime a deliberate hold gives up and throws anyway
        private float _holdMinUntil;          // realtime before which a hold stands regardless of their guard
        private float _nextSustainAt;         // realtime to re-send MeleeStrike so the chamber stays up
        private bool _holdChosen;             // the read said hold this chamber, rather than throw on the beat
        private float _strikeThrownAt;        // realtime the last stab was released, which a feint cancels from
        private bool _threwFirst;             // this swing out-timed the enemy's, so commit to it harder

        // Stab-priority state: after our guard absorbs an attack we get a brief riposte window (Decide).
        private bool _blockBaselinePending = true; // take the current block history as the baseline on the first tick
        private float _lastConsumedBlock;     // last block we reacted to as defender (dedupe)
        private float _lastConsumedClash;     // last chamber we reacted to (dedupe)
        private float _riposteReadyAt;        // realtime before which we hold the guard (reaction beat) before countering
        private float _priorityUntil;         // while now < this: riposte immediately, don't re-block
        private float _strikeCommittedUntil;  // while now < this: don't block (it'd cancel our own swing). Includes the first-strike nerve bonus.
        private float _bladeLiveUntil;        // while now < this: the blade is actually out, so the aim cap and mate clamp apply

        // Diagnostics only, read by MeleeProbe when a bot kills a bot. The aim the swing wanted versus the aim it
        // was allowed, which is what tells a clamp that never engaged from a clamp that engaged and was not enough.
        private float _lastAimDesired;
        private float _lastAimClamped;
        private int _selfId = -1;
        private float _blockStartedAt;        // realtime the current guard went up (for MinBlockHold)
        private bool _blockIsFeint;           // the guard up right now is a feint's cancel, so MinBlockHold is off
        private float _blockDesiredSince = -1f; // realtime we first wanted this guard (for block reaction; -1 = not)
        private float _blockReadyAt;          // realtime the guard may go up (start plus block reaction beat)
        private float _rerollAt;              // realtime to next re-roll the hold distance

        // How badly this bot is holding the line right now (see slotError / formationLag).
        private bool _wasInSquad;             // formed up last tick, so we can tell when a bout's formation begins
        private Vector2 _slotBias;            // its personal misplacement right now, drifting toward _slotBiasTarget
        private Vector2 _slotBiasTarget;      // the misplacement it is currently sliding toward
        private float _slotBiasRerollAt;      // realtime it next picks a new one
        private Vector2 _slotSeen;            // the slot position it is working from, which may be out of date
        private bool _slotSeenValid;          // guards against chasing world origin before the first sample
        private float _resampleAt;            // realtime it next looks at where it is actually supposed to be

        public MeleeAi(BotAiEnum aiType)
        {
            _aiType = aiType;
            SeedLevers(aiType);
            RollHoldRange();
        }

        public BotAiEnum AiType => _aiType;

        public BotIntent Decide(BotController self, float deltaTime)
        {
            if (!self.TryGetPose(out BotPose pose))
                return BotIntent.Idle; // not spawned, issue nothing

            _selfId = self.PlayerId;   // StepAttack records its strike against it and has no self of its own

            // Player ids are recycled, so take whatever is on record as the baseline on the first tick.
            if (_blockBaselinePending)
            {
                _blockBaselinePending = false;
                _lastConsumedBlock = CombatTracker.LastBlockTime(self.PlayerId);
                _lastConsumedClash = CombatTracker.LastClashTime(self.PlayerId);
                _lastEngageBlock = _lastConsumedBlock;
                _lastProvokeBlock = _lastConsumedBlock;
            }

            // Note a fresh hit on our guard. Kept outside the engageOnAttack path so every preset reports being
            // attacked, which is the signal a drill station wakes its group from.
            float provokeBlock = CombatTracker.LastBlockTime(self.PlayerId);
            if (provokeBlock > _lastProvokeBlock)
            {
                _lastProvokeBlock = provokeBlock;
                if (CombatTracker.LastBlockAttacker(self.PlayerId) is int attacker)
                    _provokedBy = attacker;
            }

            // A chamber ended our swing, so the next stab waits on the stun rather than on a full swing. The game
            // reports a block alongside it, which was no guard of ours and must not open a riposte: nobody has
            // priority, because both sides recover together. It still counts as being attacked, above.
            float clash = CombatTracker.LastClashTime(self.PlayerId);
            if (clash > _lastConsumedClash)
            {
                _lastConsumedClash = clash;
                _attackCooldownUntil = clash + ClashStunSeconds + Random.Range(0f, _attackReadBeat);
                _lastConsumedBlock = Mathf.Max(_lastConsumedBlock, CombatTracker.LastBlockTime(self.PlayerId));
                _strikeCommittedUntil = 0f;
                _bladeLiveUntil = 0f;
            }

            // A provocation lasts only as long as the player who made it.
            if (_provokedBy is int provoker && !IsCandidate(self, StateTracker.GetPlayerById(provoker), ignoreRange: true))
                _provokedBy = null;

            // Let go of a windup that was dropped rather than thrown; a raw MeleeStrike latches the attack loop.
            if (_releasePending)
            {
                _releasePending = false;
                float releasedAt = Time.realtimeSinceStartup;
                _attackCooldownUntil = releasedAt + MissedStabDuration;

                // A dropped windup still puts the blade out, so the mid-swing hold has to know about it.
                _bladeLiveUntil = releasedAt + StrikeCommitWindow;

                return new BotIntent { Action = "ExecuteMeleeWeaponStrike", MoveAxis = Vector2.zero };
            }

            // Enter combat stance once so the bot can block and strike. Consumed only once actually spawned, which
            // makes it the moment the bot enters the world and so where the no-swinging-yet window starts.
            if (_stancePending)
            {
                _stancePending = false;
                _strikeReadyAt = Time.realtimeSinceStartup + SpawnStrikeDelay;
                return new BotIntent { Action = "EnableCombatStance" };
            }

            float now = Time.realtimeSinceStartup;

            // Orders from the coordinator for this tick. Resolved before targeting because they also say where to
            // stand while there is nobody to fight at all, which is how a station holds its post.
            SquadSlot slot = default;
            bool hasSlot = (_squad || _post) && SquadCoordinator.TryGetSlot(self.PlayerId, out slot);

            // Standing in a formation, which needs both the lever and someone to form up with. A lone bot has no
            // partner to divide the work with, so it fights as itself even while it holds a slot.
            bool inSquad = _squad && hasSlot && slot.Members > 1;

            // Freshly formed up: take up a place rather than sliding into one from the last bout.
            if (inSquad && !_wasInSquad)
            {
                RollFormationError();
                _slotBias = _slotBiasTarget;   // take up a place, rather than sliding into one from the last bout
                _slotSeenValid = false;        // and look at where it really should be
                _resampleAt = 0f;
            }
            else if (inSquad)
            {
                StepFormationError(now, deltaTime);
            }

            _wasInSquad = inSquad;

            // A slot is the whole movement decision for a formation; for a lone station, only the walk home.
            bool slotDrivesMovement = hasSlot && (inSquad || slot.Phase != SquadPhase.Engaged);

            // The ward is resolved first because targeting reads sides from them while we are guarding.
            IPlayer ward = ResolveWard(self);
            _wardFaction = ward?.Faction;

            IPlayer target = ResolveTarget(self, now);

            // Escorting someone takes precedence over picking a fight: unless the enemy we found is actually a
            // threat to them, hold station at their side instead.
            if (ward != null && !ThreatensWard(ward, target, now))
                return GuardIntent(self, pose, ward);

            if (target?.PlayerObject == null)
            {
                AbandonChamber(); // don't resume a stale chamber when a target is reacquired

                // No slot and nothing to fight: issue no movement at all.
                if (!hasSlot)
                    return DropBlock(new BotIntent { MoveAxis = Vector2.zero, LookPitch = _aimPitch });

                // Exact slot, no bias and no lag: holding a fighting line badly is the drill, walking home badly
                // is just sluggish.
                Vector2 idleMove = _move ? EngageVelocity(pose, slot.Position, true, slot.Position, SlotDeadband, 0f, false) : Vector2.zero;

                // Look where it is going while it is going there, and take the formation's bearing on arrival.
                float idleHeading = idleMove.sqrMagnitude > 1e-4f
                    ? MovementSolver.HeadingOf(idleMove)
                    : slot.Facing;

                // Unless the blade is still out, in which case hold what we have until it is back in.
                if (BladeLive(now))
                {
                    idleHeading = pose.Heading;

                    if (MeleeProbe.IsProbing(self.PlayerId))
                        MeleeProbe.LogBladeHold(self.PlayerId, pose.Heading, slot.Facing, "target gone");
                }

                // Carried on every path, not just the fighting one, or the lever looks inert until something provokes.
                var idleIntent = new BotIntent { MoveAxis = ToAxis(pose, idleMove), LookHeading = idleHeading, LookPitch = _aimPitch };

                // Running is a sticky engine toggle, established once.
                if (_runPending)
                {
                    idleIntent.Running = true;
                    _runPending = false;
                }

                return DropBlock(idleIntent);
            }

            Vector3 tp = target.PlayerObject.transform.position;
            Vector2 targetPos = new Vector2(tp.x, tp.z);
            CombatTracker.TryGet(target.PlayerId, out CombatTracker.MeleeState enemy);
            StepRead(enemy, now, target.PlayerId);

            // What the bot is doing this tick. Its own axis, separate from where its swing is.
            Posture posture =
                hasSlot && slot.Phase == SquadPhase.Breaking    ? Posture.BackingOff  // re-forming, guard up, no swings
                : hasSlot && slot.Phase == SquadPhase.Withdrawing ? Posture.Withdrawing
                : hasSlot && now < slot.AttackAllowedAt         ? Posture.Holding     // set, but not cleared to swing yet
                : _engageOnAttack && !_engaged                  ? Posture.Waiting     // provoked-on-attack, not yet provoked
                : Posture.Fighting;

            // Only a fighting bot presses, counters or chases. Every posture still blocks.
            bool fighting = posture == Posture.Fighting;
            bool press = _press && fighting;
            bool riposte = _riposte && fighting;
            bool pursue = _pursue && fighting;

            // While striking, nudge the aim sideways so the right-hand stab lands on centre of mass.
            bool swingLive = BladeLive(now);

            Vector2 aimPos = targetPos;
            if (swingLive)
            {
                Vector2 toTarget = targetPos - pose.Position;
                if (toTarget.sqrMagnitude > 1e-4f)
                {
                    Vector2 dir = toTarget.normalized;
                    Vector2 botLeft = new Vector2(-dir.y, dir.x); // shift aim to the bot's left to centre a right-hand stab
                    aimPos = targetPos + botLeft * AimOffset;
                }
            }

            // A swing follows the aim for its whole length, so only the aim is constrained, and only while live.
            float crowdDistance = _mateCrowdRatio * (hasSlot && slot.Spacing > 0f ? slot.Spacing : _squadSpacing);

            bool mateAcrossBlade = false;
            float aimHeading = swingLive
                ? ClampAimAroundMates(self, pose, aimPos, crowdDistance, out mateAcrossBlade)
                : MovementSolver.HeadingTo(pose.Position, aimPos);

            // A long feint is held out past the range its stab could connect at, so it is aimed to miss on
            // purpose. Added after the mate clamp, which is about not hitting friends and still applies.
            aimHeading += FeintAimOffset();

            // How far the commanded heading actually moves this tick. The engine joins its frames with rays.
            float turned = swingLive ? Mathf.DeltaAngle(pose.Heading, aimHeading) : 0f;

            if (swingLive && MeleeProbe.IsProbing(self.PlayerId))
                MeleeProbe.LogSwingTick(self.PlayerId, pose.Heading, _lastAimDesired, aimHeading, turned,
                    mateAcrossBlade, _gateRadius, _clampRadius, DescribeMates(self, pose));

            BotIntent intent = new BotIntent { LookHeading = aimHeading, LookPitch = _aimPitch };


            // Stab priority: our guard absorbing a stab leaves the thrower recovering, so we counter at once.
            bool priority = false;
            if (riposte)
            {
                // A coordinated formation shares its guard: a stab any member turns aside is spent.
                float myBlock = CombatTracker.LastBlockTime(self.PlayerId);
                float shareChance = Mathf.Max(0f, (_coordinate - 0.5f) * 2f);
                if (inSquad && Random.value < shareChance) myBlock = Mathf.Max(myBlock, slot.BlockTime);

                if (myBlock > _lastConsumedBlock)
                {
                    _lastConsumedBlock = myBlock;
                    _riposteReadyAt = now + Random.Range(_riposteReactionMin, _riposteReactionMax); // reaction beat before we counter
                    _priorityUntil = _riposteReadyAt + _riposteWindow;                              // window runs after the beat
                }

                // Only riposte if the enemy has not readied a fresh stab since the block we absorbed.
                priority = now >= _riposteReadyAt && now < _priorityUntil && !enemy.WindingUp && enemy.WindupTime <= _lastConsumedBlock;
            }

            // Vary the spacing over time so the bot isn't pinned to one radius.
            if (now >= _rerollAt) { RollHoldRange(); _rerollAt = now + Random.Range(1.5f, 3.5f); }

            // While our own strike is still flying we must not block: a block cancels the swing before it lands.
            bool committed = now < _strikeCommittedUntil;

            // Asked before anything else reads the phase, so a cancel that starts this tick is seen by all of it.
            string feintBlock = FeintBlock(now, enemy);

            // I threw first: our windup began before theirs, so commit rather than bailing into a guard.
            bool chamberCommit = (press || riposte) && _attackPhase == AttackPhase.Chamber
                                 && enemy.IsThreat(now) && _chamberStartedAt <= enemy.WindupTime;
            if (chamberCommit) _threwFirst = true; // out-timed them, so commit harder to this swing (see StepAttack)

            // A deliberate hold never bails into a guard. A swing we have already blocked still counts as a threat
            // for LethalWindowSeconds afterwards, and guarding against that ghost cancelled the chamber and started
            // the hold over. A real new windup is answered by releasing the stab, which beats it anyway.
            bool holding = _attackPhase == AttackPhase.Chamber && _holdMax > 0f && now < _holdUntil;

            // A feint's cancel is a guard raised on purpose, so it outranks every reason not to raise one.
            string desiredBlock = feintBlock
                ?? ((priority || committed || chamberCommit || holding) ? null : DesiredBlockToken(enemy, now));

            // Block reaction: a real player takes a beat to raise the guard after reading the attack. It applies
            // only to the initial raise; switching guard direction once up stays instant. min = max = 0 is instant.
            if (desiredBlock != null)
            {
                if (_blockDesiredSince < 0f)
                {
                    _blockDesiredSince = now;

                    // Not fighting means the guard goes up on the passive beat, instant by default.
                    _blockReadyAt = now + (fighting
                        ? Random.Range(_blockReactionMin, _blockReactionMax)
                        : _passiveBlockReaction);
                }
                if (_blockToken == null && now < _blockReadyAt)
                    desiredBlock = null; // still reacting, guard not up yet
            }
            else
            {
                _blockDesiredSince = -1f;
            }

            // Minimum block hold: once the guard is up, keep it up briefly even if we'd now drop it to riposte, so
            // it reads and its animation completes. Never overrides committing our own in-flight strike.
            // A feint's cancel is exempt: that constant is here so a defensive guard reads and animates, which is
            // the opposite of what a cancel wants. Without this the block runs to 0.35s whatever feintDwell says.
            if (desiredBlock == null && _blockToken != null && !committed && !chamberCommit && !_blockIsFeint
                && now - _blockStartedAt < MinBlockHold)
                desiredBlock = _blockToken;

            // Last resort: block to cancel our own stab when no aim clamp can save the mate. Unreliable.
            if (_abortOnMate && mateAcrossBlade && now < _bladeLiveUntil && desiredBlock == null)
            {
                desiredBlock = "MeleeBlockHigh";
                if (MeleeProbe.IsProbing(self.PlayerId))
                    Logger.Log($"MeleeSwing[{self.PlayerId}]: aborting own stab, mate across the blade.", LogLevel.INFO);
            }

            Vector2 worldMove = Vector2.zero;

            if (desiredBlock != null)
            {
                // Under threat: block. Abort any in-progress strike, since raising a block cancels our own windup.
                // A feint is the exception: there the cancel is the point, and the phase is keeping the chain's place.
                if (_attackPhase != AttackPhase.Feint) _attackPhase = AttackPhase.None;
                if (_blockToken != desiredBlock)
                {
                    if (_blockToken == null) _blockStartedAt = now; // this guard just went up
                    intent.Action = desiredBlock;                  // start, or switch direction (no StopMeleeBlock needed)
                    _blockToken = desiredBlock;
                }
                // Guarding: hold the further defensive distance to make space to read, following a circling player
                // instead of freezing. While waiting (passive) hold the closer passiveRange instead.
                if (_move)
                    worldMove = EngageVelocity(pose, targetPos, slotDrivesMovement, SlotTarget(slot, now), slot.Loose ? LooseSlotDeadband : SlotDeadband, posture == Posture.Waiting ? _passiveRange : MovementRange(false, now), pursue);
            }
            else
            {
                // Not threatened, or riposting with priority: lower the guard and keep melee spacing.
                bool droppedBlock = false;
                if (_blockToken != null)
                {
                    intent.Action = "StopMeleeBlock";
                    _blockToken = null;
                    _blockIsFeint = false;
                    droppedBlock = true;
                }
                // Free: press closes to the offensive range, otherwise hold the reading distance. While waiting
                // (passive) hold the closer passiveRange so it doesn't back off far from an approaching player.
                if (_move)
                    worldMove = EngageVelocity(pose, targetPos, slotDrivesMovement, SlotTarget(slot, now), slot.Loose ? LooseSlotDeadband : SlotDeadband, posture == Posture.Waiting ? _passiveRange : MovementRange(press, now), pursue);

                // Attack when the enemy is not threatening. Priority bypasses press and the cooldown.
                if ((press || riposte || _attackPhase == AttackPhase.Chamber || _feintsDone > 0) && !droppedBlock)
                    // Range is judged from the slot, so a bot's own jitter cannot veto its offence.
                    StepAttack(ref intent, pose, targetPos, slotDrivesMovement ? slot.Position : pose.Position,
                        priority, press,
                        (!inSquad || slot.LaneClear)
                            && !(_gateOnMate && (MateInBladeBand(self, pose) || TargetBehindMate(self, pose, aimPos))),
                        inSquad ? self.GroupId : 0,
                        inSquad ? (slot.StabHigh ? "High" : "Low") : null,
                        inSquad ? (slot.SharedHigh ? "High" : "Low") : null,
                        enemy);
            }

            // A line slot already guarantees the spacing, so separation could only pull against it. A loose one
            // does not: there the pushback is the only thing holding reserves off each other, and off the line.
            if (!slotDrivesMovement || slot.Loose)
                worldMove = WithSeparation(self, pose, worldMove, slot.Loose);

            intent.MoveAxis = ToAxis(pose, worldMove);

            // Establish run once, on the first tick we actually engage a target (sticky engine toggle).
            if (_runPending)
            {
                intent.Running = true;
                _runPending = false;
            }

            return intent;
        }

        // The block playerAction to hold this tick, or null to lower the guard. We block whenever the enemy is a
        // melee threat: winding up, or a committed swing still in its lethal window.
        private static string DesiredBlockToken(CombatTracker.MeleeState enemy, float now)
        {
            if (string.IsNullOrEmpty(enemy.WindupDir)) return null;
            if (!enemy.IsThreat(now)) return null;
            return BlockTokenFor(enemy.WindupDir);
        }

        // Maps the enemy's attack direction into the block token that stops it.
        private static string BlockTokenFor(string windupDir)
        {
            switch (windupDir)
            {
                case "Left":  return "MeleeBlockRight";
                case "Right": return "MeleeBlockLeft";
                default:      return "MeleeBlock" + windupDir; // High / Low
            }
        }

        // Attack sequencer: one MeleeStrike to chamber, silence while it holds, one Execute to release.
        private void StepAttack(ref BotIntent intent, BotPose pose, Vector2 targetPos, Vector2 reachFrom, bool priority, bool press, bool laneClear, int groupId, string assignedDir, string matchDir, CombatTracker.MeleeState enemy)
        {
            float now = Time.realtimeSinceStartup;

            if (_attackPhase == AttackPhase.Chamber)
            {
                // A deliberate hold: keep the chamber up and wait for a reason to throw, rather than releasing on
                // a fixed beat. Restarting the windup is what sustains the pose, so re-sending is the mechanism.
                if (_holdMax > 0f && now < _holdUntil && ReasonToHold(enemy, now))
                {
                    if (now >= _nextSustainAt)
                    {
                        intent.Action = "MeleeStrike" + _attackDir;
                        _nextSustainAt = now + HoldSustainInterval;
                    }
                    return;
                }

                // Re-sending MeleeStrike restarts the windup, so while holding we issue nothing.
                if (now >= _executeAt)
                {
                    intent.Action = "ExecuteMeleeWeaponStrike";
                    _strikeThrownAt = now;

                    // A stab we mean to cancel stays ours until the cancel goes out, so the sequencer waits on it
                    // instead of treating the attack as over. The cooldown is still charged here and refunded by
                    // the cancel, so a chain that breaks down does not leave the bot swinging for free.
                    bool feinting = FeintOwed();
                    if (feinting) BeginFeintedSwing(now, (targetPos - reachFrom).magnitude);
                    _attackPhase = feinting ? AttackPhase.Swing : AttackPhase.None;

                    _attackCooldownUntil = now + MissedStabDuration + Random.Range(0f, _attackReadBeat);

                    MeleeProbe.NoteStrike(_selfId, now, _lastAimDesired, _lastAimClamped, _targetId ?? -1, laneClear);

                    float held = now - _chamberStartedAt;

                    // The cap did not end it, so ReasonToHold did: they broke first, which is the hold paying off.
                    bool brokeEarly = now < _holdUntil;
                    NoteChamberEnded(brokeEarly);

                    if (_holdMax > 0f && MeleeProbe.IsProbing(_selfId))
                        MeleeProbe.LogHold(_selfId, _attackDir, held,
                            !_holdChosen ? "noHold"
                            : enemy.WindingUp ? "theyStabbed"
                            : now - enemy.FeintedAt < FeintPunishWindow ? "theyFeinted"
                            : !enemy.Guarding ? "guardDown" : "holdMax",
                            _holdPaysRate.Value, _baitRate.Value);

                    // Only the stab allowed to land says anything about the direction the feints set up, and
                    // only it ends the chain.
                    if (!feinting)
                    {
                        NoteStabThrown(now, _feintsDone > 0, _feintSwitched, _feintLong);
                        ResetFeints();
                    }

                    _priorityUntil = 0f;                              // riposte thrown, priority spent
                    // Commit to the swing, since blocking now would cancel it. If we threw first, commit longer so
                    // the bot backs its own stab as it lands instead of flinching into a guard and eating the trade.
                    _bladeLiveUntil = now + StrikeCommitWindow;
                    _strikeCommittedUntil = _bladeLiveUntil + (_threwFirst ? FirstStrikeCommitBonus : 0f);
                }
                return;
            }

            // A stab is in flight waiting for its cancel, or the cancel is still playing out. Chambering on top
            // of either would throw the chain away and skip the guard a player would have to go through.
            if (_attackPhase == AttackPhase.Swing || _attackPhase == AttackPhase.Feint) return;

            // Fresh out of the spawn: do not swing yet.
            if (now < _strikeReadyAt) return;

            // Mid-chain: the cancel already committed us to this attack, so finish it rather than asking press
            // and the cooldown again. Without this a chain stalls in the block whenever they close the distance.
            bool resuming = _feintsDone > 0 && now < _feintUntil + FeintResumeWindow;

            // Do not start a swing that would go through a squadmate. Only the start is gated.
            if (!laneClear) return;

            // Idle: begin a strike if allowed and close enough. A priority riposte ignores press and cooldown; a
            // non-priority strike (throwing first) needs press and respects the cooldown.
            if (!priority && !resuming)
            {
                if (!press) return;
                if (now < _attackCooldownUntil) return;
            }
            // A press attack only commits inside attackRange. A priority riposte always throws at the target
            // regardless of range, so a stationary RiposteDummy still counters an attacker who has backed off.
            if (!priority && !resuming && (targetPos - reachFrom).sqrMagnitude > _attackRange * _attackRange) return;

            // Direction is decided once, here, as the swing starts; per tick would flicker it mid-windup. A
            // re-chamber out of a feint already had its direction picked by the read when the cancel went out.
            string dir = _feintNextDir ?? PickStabDirection(assignedDir, matchDir);

            // Last gate before the swing, so only a bot actually about to stab files a claim.
            if (!SquadCoordinator.TryClaimStab(groupId, now, _stabSeparation, dir == "High")) return;

            _attackDir = dir;
            _chamberStartedAt = now;
            _executeAt = now + WindupSeconds;
            _guardSeenThisChamber = false;
            _holdChosen = WillHold();
            _holdUntil = now + (_holdChosen ? _holdMax : 0f);
            _holdMinUntil = now + _holdMin;
            _nextSustainAt = now + HoldSustainInterval;
            _threwFirst = false;                               // set true only if we out-time the enemy this windup
            _attackPhase = AttackPhase.Chamber;
            intent.Action = "MeleeStrike" + _attackDir;
        }

        // Whether a chambered stab is worth keeping back. Holding is only ever worth it against a raised guard:
        // drop the guard and it lands, start a stab and we release first, having been chambered the whole time.
        private bool ReasonToHold(CombatTracker.MeleeState enemy, float now)
        {
            if (enemy.WindingUp) return false;   // they have committed to a swing; ours is already up, so throw

            // They just threw a chamber away, which is the opening this whole mechanic exists to find. Holding
            // through it, even for holdMin, hands it straight back, so this outranks the minimum.
            if (now - enemy.FeintedAt < FeintPunishWindow) return false;

            // A hold stands for holdMin whatever the guard says. A person needs a moment to get one up after
            // seeing the chamber, and reading that moment as "not blocking" threw the stab before they could.
            if (now < _holdMinUntil) return true;

            if (enemy.Guarding) return true;     // guard up: nothing to throw at yet

            // Guard down, and whether that is an opening or a bait is the read. It was rolled once when the guard
            // fell, so the answer stands instead of being re-decided every tick until it happens to say throw.
            return !_takeThisDrop;
        }

        // Drop a windup without throwing it. The engine keeps cycling the attack until something ends it.
        private void AbandonChamber()
        {
            if (_attackPhase == AttackPhase.Chamber) _releasePending = true;
            _attackPhase = AttackPhase.None;
            ResetFeints();
        }

        // coordinate runs 0 to 1 with chance in the middle: the top half makes updowns, the bottom refuses them.
        private string PickStabDirection(string assignedDir, string matchDir)
        {
            if (assignedDir != null && matchDir != null)
            {
                float roll = Random.value;

                if (_coordinate > 0.5f && roll < (_coordinate - 0.5f) * 2f) return assignedDir;
                if (_coordinate < 0.5f && roll < (0.5f - _coordinate) * 2f) return matchDir;
            }

            return Random.value < 0.5f ? "High" : "Low";
        }

        // Picks how badly this bot holds its place. Called on joining a formation, then again on a timer.
        private void RollFormationError()
        {
            _slotBiasRerollAt = Time.realtimeSinceStartup + Random.Range(SlotBiasRerollMin, SlotBiasRerollMax);

            float drift = Random.Range(0f, _slotError);
            float angle = Random.Range(0f, Mathf.PI * 2f);

            // Held in the line's frame. The sideways half is capped so a bot cannot end up past its neighbour.
            float across = Mathf.Cos(angle) * drift;
            float along = Mathf.Sin(angle) * drift;
            float acrossLimit = _squadSpacing * 0.45f;

            _slotBiasTarget = new Vector2(Mathf.Clamp(across, -acrossLimit, acrossLimit), along);
        }

        // Slides the live bias toward the last rolled one, and rolls a new one once it has been held long enough.
        private void StepFormationError(float now, float deltaTime)
        {
            if (now >= _slotBiasRerollAt) RollFormationError();

            _slotBias = Vector2.MoveTowards(_slotBias, _slotBiasTarget, SlotBiasDriftRate * deltaTime);
        }

        // Where the bot believes its slot is: out of date by up to formationLag, and off by its bias for this
        // bout. Everything that moves it toward a slot goes through here, so imperfection lands in one place.
        private Vector2 SlotTarget(SquadSlot slot, float now)
        {
            // A loose slot is already where the bot stands, so adding the misplacement to it would walk the bot
            // off a bias-length at a time as each tick fed its new position back in as its place.
            if (slot.Loose) return slot.Position;

            if (!_slotSeenValid || now >= _resampleAt)
            {
                _slotSeen = slot.Position;
                _slotSeenValid = true;

                // Rolled from zero each time, so a laggy bot is sometimes barely late and sometimes badly so,
                // rather than reliably a fixed beat behind.
                _resampleAt = now + Random.Range(0f, _formationLag);
            }

            // The bias is in the line's frame, so it turns with the formation: a bot that hangs back stays behind
            // the line as the pair rotates, rather than its error swinging round to the flank.
            Vector2 forward = MovementSolver.DirectionFromHeading(slot.Facing);
            Vector2 across = new Vector2(-forward.y, forward.x);

            return _slotSeen + across * _slotBias.x + forward * _slotBias.y;
        }

        // Re-rolls the offensive and defensive spacings with a little jitter, so the bot varies its distance over
        // time instead of orbiting at a fixed radius.
        private void RollHoldRange()
        {
            _offensiveRange = _offensiveBase + Random.Range(0f, _offensiveVar);
            _defensiveRange = _defensiveBase + Random.Range(0f, _defensiveVar);
        }

        // Advancing waits a reaction beat; backing off applies immediately.
        private float MovementRange(bool wantOffensive, float now)
        {
            float target = wantOffensive ? _offensiveRange : _defensiveRange;
            if (target < _appliedRange - MoveHysteresis)      // wants to advance (move closer)
            {
                if (_advanceWantedSince < 0f) _advanceWantedSince = now;
                if (now - _advanceWantedSince >= MoveChangeDelay) _appliedRange = target;
            }
            else                                              // backing off or holding: no delay
            {
                _appliedRange = target;
                _advanceWantedSince = -1f;
            }
            return _appliedRange;
        }

        // World movement to sit at the given hold range, with a slop band where the bot just stands.
        private static Vector2 HoldRangeVelocity(BotPose pose, Vector2 targetPos, float range, bool pursue)
        {
            Vector2 toTarget = targetPos - pose.Position;
            float dist = toTarget.magnitude;
            if (dist < 1e-4f) return Vector2.zero;

            Vector2 dir = toTarget / dist;
            if (dist > range + RangeTolerance) return pursue ? dir : Vector2.zero;
            if (dist < range - RangeTolerance) return -dir * BackoffThrottle;
            return Vector2.zero;
        }

        // Movement for this tick. A slot overrides the hold range entirely.
        private static Vector2 EngageVelocity(BotPose pose, Vector2 targetPos, bool useSlot, Vector2 slotPos, float deadband, float holdRange, bool pursue)
        {
            if (useSlot)
            {
                return (slotPos - pose.Position).sqrMagnitude < deadband * deadband
                    ? Vector2.zero
                    : Steering.Seek(pose, slotPos);
            }

            return HoldRangeVelocity(pose, targetPos, holdRange, pursue);
        }

        // Adds the push away from nearby bots. It applies even when the bot would otherwise stand still, which is
        // the point: a clump that never spreads out spends the fight swinging through each other.
        private Vector2 WithSeparation(BotController self, BotPose pose, Vector2 worldMove, bool loose)
        {
            if (_separationRange <= 0f) return worldMove;

            float range = loose ? _separationRange * LooseSeparationScale : _separationRange;
            float weight = loose ? SeparationWeight * LooseSeparationBoost : SeparationWeight;

            Vector2 push = Separation(self, pose, range) * weight;
            if (loose && push.sqrMagnitude < LooseSeparationFloor * LooseSeparationFloor) return worldMove;

            return worldMove + push;
        }

        // Expresses a world movement in the bot's own frame for SetInputAxis.
        private static Vector2 ToAxis(BotPose pose, Vector2 worldMove)
        {
            float magnitude = worldMove.magnitude;
            if (magnitude < 1e-4f) return Vector2.zero;

            return MovementSolver.ToLocalAxis(pose, worldMove / magnitude, Mathf.Min(magnitude, 1f));
        }

        // Half-width of the forbidden cone around a mate at this distance. Floored for the clamp, never the gate.
        private float MateConeHalfWidth(float radius, float dist, bool floored)
        {
            float geometric = Mathf.Asin(Mathf.Clamp01(radius / dist)) * Mathf.Rad2Deg;
            if (!floored || _clampRadius <= 0f || _mateConeFloor <= 0f) return geometric;

            return Mathf.Max(geometric, _mateConeFloor * (radius / _clampRadius));
        }

        private float ClampAimAroundMates(BotController self, BotPose pose, Vector2 aimPos, float crowdDistance, out bool mateAcross)
        {
            mateAcross = false;

            float desired = MovementSolver.HeadingTo(pose.Position, aimPos);
            _lastAimDesired = desired;

            if (_clampRadius <= 0f) { _lastAimClamped = desired; return desired; }

            // Measured from where the blade points now, and off the blade rather than off the facing.
            float current = pose.Heading;
            float blade = current + BladeBearing;
            float sweep = Mathf.DeltaAngle(current, desired);
            float limit = sweep;


            FactionCountry? ours = self.Bot.Faction;
            float selfY = self.Position is Vector3 selfPos ? selfPos.y : 0f;
            var bots = BotManager.Bots;

            for (int i = 0; i < bots.Count; i++)
            {
                BotController mate = bots[i];
                if (mate.PlayerId == self.PlayerId) continue;

                // Enemies never constrain a swing. Unknown factions are treated as friendly, so a bot that has
                // not spawned yet cannot be run through by accident.
                FactionCountry? theirs = mate.Bot.Faction;
                if (ours != null && theirs != null && theirs != ours) continue;

                if (!(mate.Position is Vector3 p)) continue;

                if (Mathf.Abs(p.y - selfY) > MateVerticalReach) continue;   // out of the engine's vertical window

                Vector2 toMate = new Vector2(p.x, p.z) - pose.Position;
                float dist = toMate.magnitude;

                // Only the blade's length matters, plus the envelope radius, since a mate is a body and not a point.
                if (dist < 1e-4f || dist > BladeReach + _clampRadius) continue;

                float halfAngle = MateConeHalfWidth(_clampRadius, dist, floored: true);

                // Off the blade, not off the facing. limit stays a delta on the heading either way, since the
                // blade travels with the body and the offset between them is fixed.
                float mateDelta = Mathf.DeltaAngle(blade, MovementSolver.HeadingOf(toMate));

                // Close in, bearing stops predicting anything, so a crowded mate widens the band toward the forward arc.
                if (crowdDistance > 0f && dist < crowdDistance && Mathf.Abs(mateDelta) < 90f)
                {
                    float crowd = Mathf.InverseLerp(crowdDistance, crowdDistance * 0.6f, dist);
                    halfAngle = Mathf.Lerp(halfAngle, 90f, crowd);
                }

                // Already pointing through them: steer for the nearest way out rather than giving up.
                if (Mathf.Abs(mateDelta) < halfAngle) mateAcross = true;

                // One rule inside the band or outside it: stay on the side we are already on, no closer than the edge.
                float edge = halfAngle + _bladeMargin;

                if (mateDelta > 0f) limit = Mathf.Min(limit, mateDelta - edge);
                else                limit = Mathf.Max(limit, mateDelta + edge);
            }

            float heading = current + limit;
            _lastAimClamped = heading;
            return heading;
        }

        // Whether a squadmate stands in the blade's band right now. Gates the start of a swing.
        private bool MateInBladeBand(BotController self, BotPose pose)
        {
            if (_gateRadius <= 0f) return false;

            float blade = pose.Heading + BladeBearing;
            FactionCountry? ours = self.Bot.Faction;
            float selfY = self.Position is Vector3 selfPos ? selfPos.y : 0f;
            var bots = BotManager.Bots;

            for (int i = 0; i < bots.Count; i++)
            {
                BotController mate = bots[i];
                if (mate.PlayerId == self.PlayerId) continue;

                FactionCountry? theirs = mate.Bot.Faction;
                if (ours != null && theirs != null && theirs != ours) continue;

                if (!(mate.Position is Vector3 p)) continue;

                if (Mathf.Abs(p.y - selfY) > MateVerticalReach) continue;   // out of the engine's vertical window

                Vector2 toMate = new Vector2(p.x, p.z) - pose.Position;
                float dist = toMate.magnitude;
                if (dist < 1e-4f || dist > BladeReach + _gateRadius) continue;

                // Deliberately narrower than the clamp and with no crowding rule: this is the one that costs stabs.
                float halfAngle = MateConeHalfWidth(_gateRadius, dist, floored: false) + _bladeMargin;

                if (Mathf.Abs(Mathf.DeltaAngle(blade, MovementSolver.HeadingOf(toMate))) < halfAngle) return true;
            }

            return false;
        }

        // Whether aiming at the target would put the blade through a mate. Catches a target in line with one.
        private bool TargetBehindMate(BotController self, BotPose pose, Vector2 aimPos)
        {
            if (_gateRadius <= 0f) return false;

            // From the aim point, not the target: AimOffset already shifts the aim to centre the offset blade.
            float blade = MovementSolver.HeadingTo(pose.Position, aimPos) + BladeBearing;

            FactionCountry? ours = self.Bot.Faction;
            float selfY = self.Position is Vector3 selfPos ? selfPos.y : 0f;
            var bots = BotManager.Bots;

            for (int i = 0; i < bots.Count; i++)
            {
                BotController mate = bots[i];
                if (mate.PlayerId == self.PlayerId) continue;

                FactionCountry? theirs = mate.Bot.Faction;
                if (ours != null && theirs != null && theirs != ours) continue;

                if (!(mate.Position is Vector3 p)) continue;

                if (Mathf.Abs(p.y - selfY) > MateVerticalReach) continue;   // out of the engine's vertical window

                Vector2 toMate = new Vector2(p.x, p.z) - pose.Position;
                float dist = toMate.magnitude;
                if (dist < 1e-4f || dist > BladeReach + _gateRadius) continue;

                float halfAngle = MateConeHalfWidth(_gateRadius, dist, floored: false) + _bladeMargin;

                if (Mathf.Abs(Mathf.DeltaAngle(blade, MovementSolver.HeadingOf(toMate))) < halfAngle) return true;
            }

            return false;
        }

        // One line per tick while a swing is live, for the probe.
        private string DescribeMates(BotController self, BotPose pose)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            FactionCountry? ours = self.Bot.Faction;
            float selfY = self.Position is Vector3 selfPos ? selfPos.y : 0f;
            var bots = BotManager.Bots;

            for (int i = 0; i < bots.Count; i++)
            {
                BotController mate = bots[i];
                if (mate.PlayerId == self.PlayerId) continue;

                FactionCountry? theirs = mate.Bot.Faction;
                if (ours != null && theirs != null && theirs != ours) continue;
                if (!(mate.Position is Vector3 p)) continue;

                Vector2 toMate = new Vector2(p.x, p.z) - pose.Position;
                float dist = toMate.magnitude;
                if (dist < 1e-4f) continue;
                bool tooHigh = Mathf.Abs(p.y - selfY) > MateVerticalReach;

                sb.Append(" | mate=").Append(mate.PlayerId)
                  .Append(" d=").Append(dist.ToString("0.00"))
                  // Off the blade, matching what the clamp compares, so a held swing and its reason line up.
                  .Append(" off=").Append(Mathf.DeltaAngle(pose.Heading + BladeBearing, MovementSolver.HeadingOf(toMate)).ToString("0.#"))
                  .Append(" half=").Append(MateConeHalfWidth(_clampRadius, dist, floored: true).ToString("0.#"))
                  .Append(dist > BladeReach + _clampRadius ? " outOfReach" : "")
                  .Append(tooHigh ? " outOfHeight" : "");
            }

            return sb.ToString();
        }

        private Vector2 Separation(BotController self, BotPose pose, float range)
        {
            _separationNeighbours.Clear();

            var bots = BotManager.Bots;
            for (int i = 0; i < bots.Count; i++)
            {
                if (bots[i].PlayerId == self.PlayerId) continue;
                if (bots[i].Position is Vector3 p)
                    _separationNeighbours.Add(new Vector2(p.x, p.z));
            }

            return Steering.Separation(pose, _separationNeighbours, range);
        }

        // The friendly this bot escorts, or null when it isn't guarding anyone or that player is gone. A ward who
        // dies or leaves simply releases the bot back to ordinary melee behaviour rather than freezing it.
        private IPlayer ResolveWard(BotController self)
        {
            if (!_guard || !(_guardTargetId is int wardId)) return null;

            IPlayer ward = StateTracker.GetPlayerById(wardId);
            if (ward == null || ward.PlayerObject == null || !ward.IsAlive || wardId == self.PlayerId) return null;

            return ward;
        }

        // Whether the escort should break off and fight: an enemy has closed to within guardRange of the ward, or
        // the ward is swinging, which means they are already in a melee we should be part of.
        private bool ThreatensWard(IPlayer ward, IPlayer target, float now)
        {
            if (CombatTracker.TryGet(ward.PlayerId, out CombatTracker.MeleeState wardState) && wardState.IsThreat(now))
                return true;

            if (target?.PlayerObject == null) return false;

            Vector3 wardPos = ward.PlayerObject.transform.position;
            return (target.PlayerObject.transform.position - wardPos).sqrMagnitude <= _guardRange * _guardRange;
        }

        // Whether the bayonet is still out. Every early exit re-aims the bot, which would sweep a live blade.
        private bool BladeLive(float now) => _attackPhase == AttackPhase.Chamber || now < _bladeLiveUntil;

        // The game's baked strike geometry, keyed by the pitch chunks it uses. SetPitch speaks this scale.
        private static readonly float[] PitchKeys   = { -1.5f, -1f,   -0.7f, -0.5f, 0f,    0.5f,  1f,    1.25f, 1.5f,  2f    };
        private static readonly float[] HighBearing = { 16.5f, 15.7f, 15.3f, 15.3f, 16.6f, 20.5f, 26.6f, 30.2f, 34f,   41.8f };
        private static readonly float[] HighReach   = { 2.04f, 2.09f, 2.11f, 2.11f, 2.04f, 1.87f, 1.71f, 1.64f, 1.59f, 1.52f };
        private static readonly float[] LowBearing  = { 22.3f, 18.9f, 17.1f, 16.1f, 15.6f, 17.3f, 20.1f, 21.6f, 23f,   25.6f };
        private static readonly float[] LowReach    = { 1.86f, 1.96f, 2.03f, 2.06f, 2.09f, 2.01f, 1.92f, 1.88f, 1.84f, 1.78f };

        // Averaged across both stab directions, because the release gate must judge before one is picked.
        private void RefreshBladeGeometry()
        {
            BladeBearing = 0.5f * (Sample(HighBearing, _aimPitch) + Sample(LowBearing, _aimPitch));
            BladeReach   = 0.5f * (Sample(HighReach,   _aimPitch) + Sample(LowReach,   _aimPitch));
        }

        // Linear between the two nearest chunks, flat outside the table's ends. The ends are past anything a bot
        // would aim at, and the game clamps there too.
        private static float Sample(float[] values, float pitch)
        {
            if (pitch <= PitchKeys[0]) return values[0];

            for (int i = 1; i < PitchKeys.Length; i++)
            {
                if (pitch > PitchKeys[i]) continue;
                return Mathf.Lerp(values[i - 1], values[i], Mathf.InverseLerp(PitchKeys[i - 1], PitchKeys[i], pitch));
            }

            return values[values.Length - 1];
        }

        private BotIntent GuardIntent(BotController self, BotPose pose, IPlayer ward)
        {
            AbandonChamber(); // don't carry a chamber into the lull

            Transform wardTransform = ward.PlayerObject.transform;
            Vector2 wardPos = new Vector2(wardTransform.position.x, wardTransform.position.z);

            // Same rule as the idle path: a guard whose enemy dies mid-thrust must not spin to face its ward
            // with the blade still out, or the ward is who it runs through. See BladeLive.
            float now = Time.realtimeSinceStartup;
            float lookHeading = BladeLive(now) ? pose.Heading : wardTransform.eulerAngles.y;

            var intent = new BotIntent
            {
                LookHeading = lookHeading,
                LookPitch = _aimPitch,
                MoveAxis = ToAxis(pose, WithSeparation(self, pose, HoldRangeVelocity(pose, wardPos, _guardFollowRange, pursue: true), loose: false)),
            };

            if (_runPending)
            {
                intent.Running = true;
                _runPending = false;
            }

            return DropBlock(intent);
        }

        // Pin a preferred target (a higher-layer supervisor's seam for target control), or null to clear the pin
        // and fall back to auto-acquiring the nearest enemy.
        public void SetTarget(int? playerId) => _assignedTargetId = playerId;

        // Target resolution: external pin, then attacker-lock, then sticky, then nearest.
        private IPlayer ResolveTarget(BotController self, float now)
        {
            if (_assignedTargetId is int pinned)
            {
                IPlayer p = StateTracker.GetPlayerById(pinned);
                if (IsCandidate(self, p, ignoreRange: true))
                {
                    _targetId = pinned;   // report the pin too, or CurrentTargetId contradicts who we are fighting
                    return p;
                }
            }

            // engageOnAttack (Sparring) is a passive/engaged state machine that fully owns targeting.
            if (_engageOnAttack)
                return ResolveEngageOnAttack(self, now);

            // Attacker-lock: hold it while live, so the bot cannot be pulled off someone mid-strike.
            if (_lockTargetId is int locked)
            {
                IPlayer lp = StateTracker.GetPlayerById(locked);
                if (now < _lockUntil && IsCandidate(self, lp, ignoreRange: true))
                {
                    if (CombatTracker.TryGet(locked, out CombatTracker.MeleeState ls) && ls.IsThreat(now))
                        _lockUntil = now + LockTail;
                    _targetId = locked;
                    return lp;
                }
                _lockTargetId = null;
            }

            bool ownSwing = _attackPhase == AttackPhase.Chamber || now < _strikeCommittedUntil;
            if (!ownSwing)
            {
                IPlayer striker = FindNearestStriker(self, now);
                if (striker != null)
                {
                    _lockTargetId = striker.PlayerId;
                    _lockUntil = now + LockTail;
                    _targetId = striker.PlayerId;
                    return striker;
                }
            }

            if (_stickyTarget && _targetId is int cur)
            {
                IPlayer c = StateTracker.GetPlayerById(cur);
                if (IsCandidate(self, c, ignoreRange: false)) return c;
            }

            IPlayer nearest = FindNearestCandidate(self);
            _targetId = nearest?.PlayerId;
            return nearest;
        }

        // engageOnAttack: passive until a player's attack is blocked, then locked to that attacker.
        private IPlayer ResolveEngageOnAttack(BotController self, float now)
        {
            if (_engaged && _engagedTargetId is int et)
            {
                IPlayer t = StateTracker.GetPlayerById(et);
                if (IsCandidate(self, t, ignoreRange: true)) // still alive and valid: stay engaged, locked past range
                {
                    _targetId = et;
                    return t;
                }
                _engaged = false;          // target died or left the game, back to passive
                _engagedTargetId = null;
            }

            // Engage the player whose strike we just blocked (confirmed aimed at us), by id from the block event,
            // so a swing at someone else nearby never provokes us.
            float myBlock = CombatTracker.LastBlockTime(self.PlayerId);
            if (myBlock > _lastEngageBlock)
            {
                _lastEngageBlock = myBlock;
                if (CombatTracker.LastBlockAttacker(self.PlayerId) is int a)
                {
                    IPlayer atk = StateTracker.GetPlayerById(a);
                    if (IsCandidate(self, atk, ignoreRange: true))
                    {
                        _engaged = true;
                        _engagedTargetId = a;
                        _targetId = a;
                        return atk;
                    }
                }
            }

            // Nobody has attacked us yet: face and block the nearest player in range, the waiting posture.
            IPlayer near = FindNearestCandidate(self);
            _targetId = near?.PlayerId;
            return near;
        }

        // How long an attacker-lock outlives the strike: long enough to land our riposte (reaction plus window plus a beat).
        private float LockTail => _riposteReactionMax + _riposteWindow + 0.3f;

        // Closest candidate currently mid-strike.
        private IPlayer FindNearestStriker(BotController self, float now)
        {
            if (!(self.Position is Vector3 selfPos)) return null;

            float range = _targetRange > 0f ? _targetRange : StrikerLockRange;
            float rangeSqr = range * range;

            IPlayer nearest = null;
            float bestSqr = float.MaxValue;

            var players = StateTracker.AllPlayers;
            for (int i = 0; i < players.Count; i++)
            {
                IPlayer p = players[i];
                if (!IsCandidate(self, p, ignoreRange: true)) continue; // team and spawned; range applied below
                if (!CombatTracker.TryGet(p.PlayerId, out CombatTracker.MeleeState st) || !st.IsThreat(now)) continue;

                float sqr = (p.PlayerObject.transform.position - selfPos).sqrMagnitude;
                if (sqr > rangeSqr) continue;
                if (sqr < bestSqr) { bestSqr = sqr; nearest = p; }
            }

            return nearest;
        }

        private IPlayer FindNearestCandidate(BotController self)
        {
            if (!(self.Position is Vector3 selfPos)) return null;

            IPlayer nearest = null;
            float bestSqr = float.MaxValue;

            var players = StateTracker.AllPlayers;
            for (int i = 0; i < players.Count; i++)
            {
                IPlayer p = players[i];
                if (!IsCandidate(self, p, ignoreRange: false)) continue;

                float sqr = (p.PlayerObject.transform.position - selfPos).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; nearest = p; }
            }

            return nearest;
        }

        // A targetable player: spawned and alive (a corpse is skipped), not us, human unless we target bots too,
        // on a hostile faction unless ignoreTeam, and unless ignoreRange within targetRange (<= 0 = unlimited).
        private bool IsCandidate(BotController self, IPlayer p, bool ignoreRange)
        {
            if (p == null || p.PlayerObject == null || !p.IsAlive || p.PlayerId == self.PlayerId) return false;

            if (_ignoreBots && p.IsBot) return false;

            // Never raise a hand to the player we are guarding, whatever faction this body ended up on. Only
            // while actually guarding: otherwise a bot summoned for sparring would refuse to fight its summoner.
            if (_guard && _guardTargetId is int wardId && p.PlayerId == wardId) return false;

            if (!_ignoreTeam)
            {
                // Sides are judged from the ward while guarding, otherwise from our own body.
                FactionCountry? ourSide = _wardFaction ?? self.Bot.Faction;
                if (!p.Faction.HasValue || !ourSide.HasValue || p.Faction.Value == ourSide.Value)
                    return false;
            }

            if (!ignoreRange && _targetRange > 0f)
            {
                if (!(self.Position is Vector3 selfPos)) return false;
                if ((p.PlayerObject.transform.position - selfPos).sqrMagnitude > _targetRange * _targetRange)
                    return false;
            }

            return true;
        }

        private BotIntent DropBlock(BotIntent intent)
        {
            if (_blockToken != null)
            {
                intent.Action = "StopMeleeBlock";
                _blockToken = null;
            }
            return intent;
        }

        // Set by the summon commands so a bot summoned onto a player escorts that player. Equivalent to setting
        // the guardTarget lever, which is the manual route for a squad that already exists.
        public void SetGuardTarget(int playerId) => _guardTargetId = playerId > 0 ? playerId : (int?)null;

        // Whoever this bot settled on last tick. Groups are formed from the spawn batch, not from this, but a
        // group with no station of its own adopts it so the formation orients on what its members are fighting.
        public int? CurrentTargetId => _targetId;

        public bool WantsSquad => _squad;

        public SquadSettings SquadSettings => new SquadSettings
        {
            Spacing = _squadSpacing,
            SpacingVariance = _squadSpacingVar,
            LaneHalfWidth = _laneHalfWidth,
            Standoff = _squadStandoff,
            Post = _post,
            Breakoff = _breakoff,
            BreakoffRange = _breakoffRange,
            EngageDelay = _engageDelay,
            ResetRange = _resetRange,
            MinMembers = _minMembers,
            ReturnDelay = _returnDelay,
        };

        // Read by BotManager when this bot dies, to decide whether its replacement waits for the bout to finish.
        public bool HoldReplacement => _holdReplacement;

        // Only a real provocation counts, never merely having someone to look at.
        public int? ProvokedBy => _engaged ? _engagedTargetId : _provokedBy;

        // Woken by a squadmate being provoked. This is the same state a bot reaches by being stabbed itself, so
        // the rest of Decide cannot tell the difference and the group fights as one.
        public void Engage(int playerId)
        {
            _engaged = true;
            _engagedTargetId = playerId;
            _targetId = playerId;
        }

        // Back to waiting. The chamber goes with it, so a stab held when the fight ended is not still there to be
        // released at whoever walks up next.
        public void StandDown()
        {
            _engaged = false;
            _engagedTargetId = null;
            _provokedBy = null;   // spent; leaving it set would re-wake the station the instant it got home

            // Consume the block record too, or a hit landing moments earlier re-provokes on the very next tick.
            _lastProvokeBlock = CombatTracker.LastBlockTime(_selfId);
            _lastEngageBlock = _lastProvokeBlock;
            _lastConsumedBlock = _lastProvokeBlock;
            _lastConsumedClash = CombatTracker.LastClashTime(_selfId);

            AbandonChamber();
        }

        // Carry the per-bot lever overrides and any pinned target to a Replace replacement, so a bot tuned with
        // 'rc bot cfg' isn't reset to preset defaults on death.
        public void InheritFrom(IBotAi previous)
        {
            if (!(previous is MeleeAi p)) return;

            CopyLeversFrom(p);                       // every lever, including guardTarget (see MeleeAi.Levers.cs)
            _assignedTargetId = p._assignedTargetId;  // a standing order to fight someone outlives the bot

            // Deliberately not carried: engagement and provocation. A replacement starts passive.
            RollHoldRange();
        }
    }
}
