using System.Collections.Generic;
using UnityEngine;

namespace MDS.Systems
{
    // Where a squad member should stand and whether it may swing. Produced once per tick by SquadCoordinator and
    // read by the AI; a bot with no entry is fighting alone and keeps its ordinary duelling behaviour.
    public struct SquadSlot
    {
        public Vector2 Position;  // world XZ the bot should hold
        public bool LaneClear;    // no friendly between this bot and the target, so a stab is safe to throw
        public int Members;       // how many bots share this group, so the AI can tell a duel from a gang-up
        public float BlockTime;   // the most recent guard any member got in, shared so the whole squad can counter
        public bool StabHigh;     // the direction to swing, alternated along the line so neighbours never match
        public bool SharedHigh;   // the one direction the whole line would throw, for a pair deliberately matching
        public SquadPhase Phase;  // waiting, backing off, or fighting
        public float AttackAllowedAt; // realtime the group's engage delay expires, so members hold fire until then
        public float Spacing;     // the line's live gap, so mate-avoidance rules can be sized against it
        public float Facing;      // heading the line faces, used while there is no enemy to look at
        public bool Loose;        // a place to hold rather than march to: no misplacement, and a wide deadband
    }

    // Turns a spawn batch of bots into a formation: a line for the front rank, a loose band for the rest.
    public static class SquadCoordinator
    {
        private static readonly Dictionary<int, SquadSlot> _slots = new();
        private static readonly Dictionary<int, List<BotController>> _byGroup = new();
        private static readonly Dictionary<int, GroupState> _groups = new();
        private static readonly List<int> _finishedGroups = new();

        // Formations keyed by the batch whose fight state the group runs on; _memberOf maps follower to lead.
        private static readonly Dictionary<int, List<BotController>> _formations = new();
        private static readonly Dictionary<int, int> _memberOf = new();
        private static readonly List<int> _disbanding = new();

        // Kills waiting to be turned into a wake, groupId -> killer. Filled from the kill callback, which fires
        // outside the tick, and consumed in StepPhase so every phase change still happens in one place.
        private static readonly Dictionary<int, int> _pendingWakes = new();

        // Slack either side of the standoff. An enemy circling inside this band leaves the anchor alone, which is
        // what keeps a rotation from turning into a chase.
        private const float AnchorTolerance = 0.4f;

        // How fast the anchor may reposition. Slower than a bot walks, so the line is towed rather than dragged.
        private const float FollowSpeed = 3f;

        // Ground is given faster than it is taken. A line that cannot back off as fast as a player charges is
        // simply run through, while closing slowly is what keeps the formation deliberate.
        private const float RetreatSpeed = 5f;

        // Floor under the line's standoff, whatever its width works out to. Four members would otherwise stand
        // the point 0.79m out and crowd the enemy with the inner pair.
        private const float MinStandoff = 1f;

        // Slack on the range comparison that decides the group is losing ground, so float noise cannot read as
        // an enemy closing.
        private const float RangeEpsilon = 0.001f;

        // The band a reserve holds, in squadStandoff: a little further out than the line, and still inside a
        // blade at the far edge. A band rather than a ring so the pushback between reserves has somewhere to
        // move them without the pull back to range immediately undoing it.
        private const float ReserveInner = 1f;
        private const float ReserveOuter = 1.3f;

        // How close to its slot a member counts as being in place, used to decide the formation has re-formed.
        private const float SettleRadius = 0.7f;

        // Backing off gives up after this long. A player who simply walks into a bot can stop it reaching its
        // slot, and without a cap the group would stand there refusing to fight for as long as they kept it up.
        private const float BreakoffTimeout = 5f;

        // A withdrawal gives up after this long. Without it a player who simply follows a retreating group could
        // hold it in the withdrawing state indefinitely and keep the station from re-arming.
        private const float WithdrawTimeout = 10f;

        // How far from its post a formation will follow anything at all, whatever resetRange says. Not a tuning
        // value: it is the backstop that stops a group walking off the map behind a target it cannot reach.
        private const float MaxChaseFromPost = 120f;

        // Per-group state. Anchor and phase both outlive any individual member, which is what lets a station
        // survive its bots being killed and replaced.
        private class GroupState
        {
            public Vector2 Post;         // where the batch was stood up; its bots go home here
            public float PostHeading;    // the way it faced when stood up
            public float RestHeading;    // the way it is facing where it currently rests, if that isn't the post
            public int FoundedCount;     // members the batch was summoned with; also its floor for minMembers

            // Fight state. Held by whichever batch is leading the formation, which is the batch itself whenever
            // it is standing alone.
            public Vector2 Anchor;
            public SquadPhase Phase;
            public int? TargetId;
            public float PhaseSince;

            // Where the bout started and when. Stamped once on the provocation that wakes the group.
            public Vector2 BreakoffFrom;
            public float ProvokedAt;

            // Last tick's range to the enemy, so the anchor can tell whether giving ground actually gained any.
            public float LastRange;

            // The line's live gap and the width it is drifting toward. Per formation, so the whole line agrees.
            public float Spacing;
            public float SpacingTarget;
            public float SpacingRerollAt;
            public int Strength;         // biggest the formation has been this time out, for groups gathered from batches

            // The direction a deliberately-matching line throws this bout, rolled once when the bout starts.
            public bool SharedStabHigh;

            // When the formation last threw each direction. Two clocks, since the rule is about opposite pairs.
            public float LastHighAt = float.NegativeInfinity;
            public float LastLowAt = float.NegativeInfinity;
        }

        // The slot for a bot, or false when it isn't part of a squad this tick.
        public static bool TryGetSlot(int playerId, out SquadSlot slot) => _slots.TryGetValue(playerId, out slot);

        // Minimum gap between opposite stabs in one formation; two at once cannot be blocked at all.
        public static bool TryClaimStab(int groupId, float now, float minSeparation, bool wantHigh)
        {
            if (minSeparation <= 0f || groupId == 0) return true;

            // Filed against the formation, not the batch, so gathered bots are held to one another.
            if (!_groups.TryGetValue(LeadOf(groupId), out GroupState state)) return true;

            // Only opposite stabs are gated. Two the same way are stopped by a single block.
            float opposite = wantHigh ? state.LastLowAt : state.LastHighAt;
            if (now - opposite < minSeparation) return false;

            // Stamped even when it was not gated, so a same-direction stab still holds off the opposite one.
            if (wantHigh) state.LastHighAt = now;
            else state.LastLowAt = now;

            return true;
        }

        // Rebuilds every group's slots. Called once per bot tick, before the bots themselves tick, so each one
        // reads a slot computed from this tick's positions.
        public static void Refresh(IReadOnlyList<BotController> bots, float deltaTime)
        {
            _slots.Clear();
            Bucket(bots);

            // Every batch keeps its own post. That is where its bots came from and where they go home to,
            // whoever they end up fighting beside in between.
            foreach (var group in _byGroup)
                if (group.Value.Count > 0) StateFor(group.Key, group.Value);

            // Batches fighting the same enemy stand as one formation until it goes home.
            BuildFormations();

            foreach (var formation in _formations)
            {
                List<BotController> members = formation.Value;
                if (members.Count == 0) continue;

                SquadSettings settings = SettingsOf(members[0]);
                GroupState state = _groups[formation.Key];

                // Judged on how big the formation has actually been, not on how the leading batch was summoned.
                // Three bots gathered onto one player are a trio for minMembers, the same as a summoned three.
                state.Strength = Mathf.Max(state.Strength, members.Count);

                StepPhase(formation.Key, state, members, settings);

                // After StepPhase deliberately, so the width is gated on this tick's phase and not the last one's.
                StepSpacing(state, settings, deltaTime);

                IPlayer target = state.TargetId is int id ? StateTracker.GetPlayerById(id) : null;
                bool haveTarget = IsLiveTarget(target);

                if (haveTarget)
                {
                    Vector3 t = target.PlayerObject.transform.position;
                    Vector2 targetPos = new Vector2(t.x, t.z);

                    // Sized on the front rank, not the whole group: a fifth bot goes behind the line, so it must
                    // not drag the point in as though the line had got wider.
                    float spacing = state.Spacing > 0f ? state.Spacing : settings.Spacing;
                    int rankSize = RankSize(settings.Spacing, settings.Standoff);

                    // A withdrawing group re-forms where the bout ended: the anchor is frozen, not driven to any range.
                    Vector2 anchor;
                    if (state.Phase == SquadPhase.Withdrawing)
                    {
                        anchor = state.Anchor;
                    }
                    else if (state.Phase == SquadPhase.Breaking)
                    {
                        anchor = RetreatAnchor(state, targetPos, settings.BreakoffRange, deltaTime);
                    }
                    else
                    {
                        // Set from the ends of the line, so a wider formation stands its point that much closer
                        // and every member holds squadStandoff rather than only the one in the middle.
                        anchor = MoveAnchor(state, targetPos,
                            LineStandoff(Mathf.Min(members.Count, rankSize), spacing, settings.Standoff), deltaTime);
                    }

                    Vector2 toTarget = targetPos - anchor;
                    Vector2 forward = toTarget.sqrMagnitude > 1e-4f ? toTarget.normalized : MovementSolver.DirectionFromHeading(state.PostHeading);
                    AssignSlots(members, targetPos, anchor, forward, state, settings, rankSize);

                    // Settling is judged on the slots just written, so it has to come after them. The transition
                    // lands on the next tick, which nobody can see.
                    if (Settled(state, members, targetPos, settings))
                    {
                        if (state.Phase == SquadPhase.Breaking)
                            SetPhase(formation.Key, state, SquadPhase.Engaged, Time.realtimeSinceStartup);
                        else if (state.Phase == SquadPhase.Withdrawing)
                            StandDownToPost(formation.Key, state, members, settings, Time.realtimeSinceStartup);
                    }
                }
                else
                {
                    // No station and nothing to fight: issue no slots. Everything below is the station cycle.
                    if (!settings.Post) continue;

                    // Waiting, either where the last bout ended or back on the post once returnDelay has run out.
                    bool goHome = Time.realtimeSinceStartup - state.PhaseSince >= settings.ReturnDelay;
                    if (goHome)
                    {
                        state.Anchor = state.Post;

                        // Going home is where a gathered formation ends; each batch returns to its own post.
                        Disband(formation.Key, sendHome: true);
                    }

                    Vector2 home = goHome ? state.Post : state.Anchor;
                    Vector2 forward = MovementSolver.DirectionFromHeading(goHome ? state.PostHeading : state.RestHeading);

                    // The lane check needs somewhere to look, so it is given a point straight ahead of the line.
                    // One rank however many there are: ranks are for engaging, and a station waits as it was set up.
                    AssignSlots(members, home + forward, home, forward, state, settings, members.Count);
                }
            }
        }

        public static void Reset()
        {
            _slots.Clear();
            _byGroup.Clear();
            _groups.Clear();
            _pendingWakes.Clear();
            _formations.Clear();
            _memberOf.Clear();
            _disbanding.Clear();
        }

        // Decides who stands with whom this tick. Only Engaged batches on the same enemy merge.
        private static void BuildFormations()
        {
            foreach (var list in _formations.Values) list.Clear();

            // Drop batches that no longer exist, so a wiped one does not hold its formation open.
            _disbanding.Clear();
            foreach (var pair in _memberOf)
                if (!_byGroup.TryGetValue(pair.Key, out List<BotController> live) || live.Count == 0)
                    _disbanding.Add(pair.Key);
            for (int i = 0; i < _disbanding.Count; i++)
                _memberOf.Remove(_disbanding[i]);

            // New joins: a batch fighting someone another batch is already fighting throws in with them. Lowest
            // batch number leads, so the choice is stable no matter what order they arrive in.
            foreach (var group in _byGroup)
            {
                if (group.Value.Count == 0 || _memberOf.ContainsKey(group.Key)) continue;
                if (!(_groups[group.Key].TargetId is int targetId)) continue;

                // Only while actually fighting. A withdrawing batch still holds a target and must not be swept in.
                if (_groups[group.Key].Phase != SquadPhase.Engaged) continue;

                foreach (var other in _byGroup)
                {
                    if (other.Key == group.Key || other.Value.Count == 0) continue;

                    // Compare against the FORMATION's target: a follower's own TargetId freezes when it joins.
                    int otherLead = LeadOf(other.Key);
                    if (!(_groups[otherLead].TargetId is int otherTarget) || otherTarget != targetId) continue;
                    if (_groups[otherLead].Phase != SquadPhase.Engaged) continue;

                    // Join whatever they are already in rather than renumbering, which would split an existing formation.
                    int lead;
                    if (otherLead != other.Key)
                    {
                        lead = otherLead;
                    }
                    else
                    {
                        lead = Mathf.Min(group.Key, other.Key);
                        _memberOf[other.Key] = lead;
                    }

                    _memberOf[group.Key] = lead;

                    // Clear what the FOLLOWERS were fighting; the lead's target is the formation's and has to survive.
                    if (group.Key != lead) _groups[group.Key].TargetId = null;
                    if (other.Key != lead) _groups[other.Key].TargetId = null;

                    Logger.Log($"Squad: batches {group.Key} and {other.Key} now one formation, lead {lead}, target {targetId}.", LogLevel.INFO);
                    break;
                }
            }

            foreach (var group in _byGroup)
            {
                if (group.Value.Count == 0) continue;

                int key = LeadOf(group.Key);
                if (!_formations.TryGetValue(key, out List<BotController> members))
                {
                    members = new List<BotController>();
                    _formations[key] = members;
                }

                members.AddRange(group.Value);
            }
        }

        // How fast the line's gap may change, and how often it picks a new width to head for.
        private const float SpacingDriftRate = 0.25f;   // metres per second
        private const float SpacingRerollMin = 2.5f;    // seconds
        private const float SpacingRerollMax = 6f;

        private static int LeadOf(int groupId) => _memberOf.TryGetValue(groupId, out int lead) ? lead : groupId;

        // The formation is finished: every batch in it goes back to being its own again, which is what sends each
        // one to its own post rather than to whichever post happened to be leading.
        private static void Disband(int formationKey, bool sendHome)
        {
            float now = Time.realtimeSinceStartup;

            _disbanding.Clear();
            foreach (var pair in _memberOf)
                if (pair.Value == formationKey) _disbanding.Add(pair.Key);

            if (_disbanding.Count > 0)
                Logger.Log($"Squad: formation {formationKey} disbanded, releasing {_disbanding.Count} batch(es) (sendHome={sendHome}).", LogLevel.INFO);

            for (int i = 0; i < _disbanding.Count; i++)
            {
                int batch = _disbanding[i];
                _memberOf.Remove(batch);
                if (!_groups.TryGetValue(batch, out GroupState member)) continue;

                // A batch's own fight state stopped being stepped when it joined, so it is still holding stale orders.
                member.TargetId = null;
                member.Phase = SquadPhase.Posted;
                member.Strength = 0;

                if (sendHome)
                {
                    // The formation already sat out its returnDelay, so its members go now rather than lingering
                    // a second time over a bout they have already finished.
                    member.PhaseSince = 0f;
                    member.Anchor = member.Post;
                }
                else
                {
                    // Broken up because the bout ended, not because it was time to leave. Each batch stands where
                    // it is and starts its own wait, so the next fight can begin from here.
                    member.PhaseSince = now;
                    if (_byGroup.TryGetValue(batch, out List<BotController> own) && own.Count > 0)
                    {
                        member.Anchor = MeanPosition(own);
                        member.RestHeading = MeanHeading(own);
                    }
                }
            }

            if (_groups.TryGetValue(formationKey, out GroupState lead)) lead.Strength = 0;
        }

        // A wiped group's phase is frozen wherever the fight left it, so clear it or replacements wake mid-bout.
        public static void OnGroupEmptied(int groupId)
        {
            _memberOf.Remove(groupId);

            // Other batches are still fighting under this one's state - it was leading a formation that outlives
            // it - so the bout is not over and its phase must not be cleared out from under them.
            foreach (var pair in _memberOf)
                if (pair.Value == groupId) return;

            if (!_groups.TryGetValue(groupId, out GroupState state)) return;

            state.TargetId = null;
            state.Phase = SquadPhase.Posted;
            state.PhaseSince = Time.realtimeSinceStartup;
            state.Strength = 0;
        }

        // Whether the bout is over, which is when a held replacement may appear. Phase only, deliberately.
        public static bool IsBoutOver(int groupId) =>
            !_groups.TryGetValue(LeadOf(groupId), out GroupState state)
            || state.Phase == SquadPhase.Posted
            || state.Phase == SquadPhase.Withdrawing;

        // Whether a kill counts as a casualty of this group's own bout. Call AFTER OnMemberKilled.
        public static bool IsBoutOpponent(int groupId, int playerId)
        {
            if (groupId == 0 || playerId <= 0) return false;

            int lead = LeadOf(groupId);
            if (!_groups.TryGetValue(lead, out GroupState state)) return false;

            // Mid-bout: only the player they are actually fighting counts.
            if (state.TargetId is int target) return target == playerId;

            // No target yet means this is the kill that wakes them, so the killer is about to become the target.
            return _pendingWakes.TryGetValue(lead, out int waker) && waker == playerId;
        }

        // A respawned target is a new fight. Clears the target and lets the normal stand-down path run.
        public static void OnTargetRespawned(int playerId)
        {
            bool ended = false;

            foreach (var pair in _groups)
                if (pair.Value.TargetId == playerId)
                {
                    pair.Value.TargetId = null;
                    ended = true;
                }

            // A wake filed against them moments before the respawn must go too, or the group simply re-acquires
            // them on the next tick and the respawn changes nothing.
            List<int> stale = null;
            foreach (var pair in _pendingWakes)
                if (pair.Value == playerId)
                {
                    if (stale == null) stale = new List<int>();
                    stale.Add(pair.Key);
                }

            if (stale != null)
                for (int i = 0; i < stale.Count; i++)
                {
                    _pendingWakes.Remove(stale[i]);
                    ended = true;
                }

            if (ended)
                Logger.Log($"Squad: target {playerId} respawned; ending the bout.", LogLevel.INFO);
        }

        // A member was killed. Waking only on a blocked hit misses a clean opening stab that kills outright.
        public static void OnMemberKilled(int groupId, int victimPlayerId, int killerPlayerId)
        {
            if (groupId == 0 || killerPlayerId == victimPlayerId) return;

            // Never turn a group onto one of its own. Formations exist to cut down friendly fire; answering it
            // with a revenge order would be the opposite.
            if (_byGroup.TryGetValue(groupId, out List<BotController> members))
                for (int i = 0; i < members.Count; i++)
                    if (members[i].PlayerId == killerPlayerId) return;

            // Filed against the formation, not the batch: it is the formation that wakes, and the victim's own
            // batch may not even be the one leading it.
            _pendingWakes[LeadOf(groupId)] = killerPlayerId;
        }

        // Drives the station cycle: wait, break off, fight, return. A group without Post is always fighting, which
        // is the behaviour every squad had before stations existed.
        private static void StepPhase(int groupId, GroupState state, List<BotController> members, SquadSettings settings)
        {
            float now = Time.realtimeSinceStartup;

            // Always taken, even when it goes unused, so a kill during a fight cannot sit around and re-trigger
            // once the group is back at its post.
            int? killer = null;
            if (_pendingWakes.TryGetValue(groupId, out int k))
            {
                _pendingWakes.Remove(groupId);
                killer = k;
            }

            if (!settings.Post)
            {
                // No station: the group forms around whoever actually attacked a member, never who it is looking at.
                bool wasIdle = state.TargetId == null;
                SetPhase(groupId, state, SquadPhase.Engaged, now);
                state.TargetId = FirstProvoker(members);

                // Starting a fresh fight: form up where the members are, not where they spawned.
                if (wasIdle && state.TargetId != null)
                {
                    state.Anchor = MeanPosition(members);
                    state.BreakoffFrom = state.Anchor;
                    state.ProvokedAt = now;
                    state.SharedStabHigh = Random.value < 0.5f;
                    state.LastHighAt = state.LastLowAt = float.NegativeInfinity;
                }
                return;
            }

            // Too few left to be worth fighting. Capped by the group's own size so a lone bot still duels.
            int minMembers = Mathf.Min(settings.MinMembers, Mathf.Max(state.Strength, state.FoundedCount));
            bool shortHanded = members.Count < minMembers;
            bool targetLive = TargetStillValid(state, settings);

            // Time to break off: either the bout is over or the target is gone.
            if ((state.Phase == SquadPhase.Breaking || state.Phase == SquadPhase.Engaged) && (shortHanded || !targetLive))
            {
                // Withdraw rather than switch off while the enemy is still standing: a bot walking home is free kills.
                if (targetLive)
                {
                    SetPhase(groupId, state, SquadPhase.Withdrawing, now);
                }
                else
                {
                    StandDownToPost(groupId, state, members, settings, now);
                    return;
                }
            }

            // Withdrawing ends when the enemy is gone or it has taken too long.
            if (state.Phase == SquadPhase.Withdrawing && (!targetLive || now - state.PhaseSince > WithdrawTimeout))
            {
                StandDownToPost(groupId, state, members, settings, now);
                return;
            }

            switch (state.Phase)
            {
                case SquadPhase.Posted:
                    // Provoking any one member wakes the whole group onto whoever did it.
                    if (!shortHanded && (FirstProvoker(members) ?? killer) is int provoker)
                    {
                        state.TargetId = provoker;

                        // The fight starts where the group is standing, NOT at the post, which it is often not on.
                        state.Anchor = MeanPosition(members);
                        state.BreakoffFrom = state.Anchor;
                        state.ProvokedAt = now;
                        state.SharedStabHigh = Random.value < 0.5f;
                        state.LastHighAt = state.LastLowAt = float.NegativeInfinity;
                        SetPhase(groupId, state, settings.Breakoff ? SquadPhase.Breaking : SquadPhase.Engaged, now);
                    }
                    break;

                case SquadPhase.Breaking:
                    // Give up backing off if a player body blocking a member has stalled it this long. The
                    // formation-is-set case is checked in Refresh, once this tick's slots exist to measure against.
                    if (now - state.PhaseSince > BreakoffTimeout)
                        SetPhase(groupId, state, SquadPhase.Engaged, now);
                    break;
            }

            // Re-asserted every tick, because a replacement starts passive by design and would never fight.
            if (state.Phase != SquadPhase.Posted && state.TargetId is int targetId)
                WakeAll(members, targetId);
        }

        // Fully off: guard down, target forgotten, waiting to be provoked again.
        private static void StandDownToPost(int formationKey, GroupState state, List<BotController> members, SquadSettings settings, float now)
        {
            for (int i = 0; i < members.Count; i++)
                AsMember(members[i])?.StandDown();

            state.TargetId = null;
            state.Anchor = MeanPosition(members);

            // Keep whatever way they finished facing; the post's bearing is not a direction they hold everywhere.
            state.RestHeading = MeanHeading(members);

            SetPhase(formationKey, state, SquadPhase.Posted, now);

            // A formation that was never a unit does not outlive the fight that created it. Judged on minMembers.
            if (settings.MinMembers <= 0) Disband(formationKey, sendHome: false);
        }

        // Every phase change goes through here so the transitions are traceable.
        private static void SetPhase(int formationKey, GroupState state, SquadPhase phase, float now)
        {
            if (state.Phase == phase) return;

            Logger.Log($"Squad: formation {formationKey} {state.Phase} -> {phase} target={(state.TargetId is int t ? t.ToString() : "none")}.", LogLevel.INFO);

            state.Phase = phase;
            state.PhaseSince = now;
        }

        private static void WakeAll(List<BotController> members, int targetId)
        {
            for (int i = 0; i < members.Count; i++)
                AsMember(members[i])?.Engage(targetId);
        }

        // The group has re-formed: it has its distance and everyone is on their slot.
        private static bool Settled(GroupState state, List<BotController> members, Vector2 targetPos, SquadSettings settings)
        {
            // Only the retreat has a distance to reach, and it is measured from where the bout started.
            if (state.Phase == SquadPhase.Breaking)
            {
                float given = Vector2.Distance(state.Anchor, state.BreakoffFrom);
                if (given < settings.BreakoffRange - AnchorTolerance) return false;
            }

            for (int i = 0; i < members.Count; i++)
            {
                if (!_slots.TryGetValue(members[i].PlayerId, out SquadSlot slot)) return false;
                if (Vector2.Distance(Planar(members[i]), slot.Position) > SettleRadius) return false;
            }

            return true;
        }

        // A target is worth keeping while alive, spawned, and near enough to the post to count as using it.
        private static bool TargetStillValid(GroupState state, SquadSettings settings)
        {
            IPlayer target = state.TargetId is int id ? StateTracker.GetPlayerById(id) : null;
            if (!IsLiveTarget(target)) return false;

            Vector3 t = target.PlayerObject.transform.position;
            float fromPost = Vector2.Distance(new Vector2(t.x, t.z), state.Post);

            // Safety rail rather than a drill setting: resetRange 0 lifts the limit, this stops a march off the map.
            if (fromPost > MaxChaseFromPost) return false;

            return settings.ResetRange <= 0f || fromPost <= settings.ResetRange;
        }

        // Spawned AND alive. The aliveness check is the important half: a corpse keeps its PlayerObject.
        private static bool IsLiveTarget(IPlayer target) => target?.PlayerObject != null && target.IsAlive;

        // The bearing a fresh group posts on, taken from what each bot was ASKED to face, not where it looks.
        private static float PostHeadingOf(List<BotController> members)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < members.Count; i++)
                sum += MovementSolver.DirectionFromHeading(members[i].SpawnHeading ?? members[i].Heading ?? 0f);

            return sum.sqrMagnitude > 1e-4f ? MovementSolver.HeadingOf(sum) : 0f;
        }

        // Average bearing of the members right now. Summed as direction vectors rather than as degrees: averaging
        // the numbers would take 350 and 10 to 180 and point the formation backwards.
        private static float MeanHeading(List<BotController> members)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < members.Count; i++)
                sum += MovementSolver.DirectionFromHeading(members[i].Heading ?? 0f);

            return sum.sqrMagnitude > 1e-4f ? MovementSolver.HeadingOf(sum) : 0f;
        }

        // Midpoint of the members as they stand right now.
        private static Vector2 MeanPosition(List<BotController> members)
        {
            if (members.Count == 0) return Vector2.zero;

            Vector2 mean = Vector2.zero;
            for (int i = 0; i < members.Count; i++)
                mean += Planar(members[i]);

            return mean / members.Count;
        }

        private static int? FirstProvoker(List<BotController> members)
        {
            for (int i = 0; i < members.Count; i++)
                if (AsMember(members[i])?.ProvokedBy is int id) return id;

            return null;
        }

        // The group's state, with the post measured from its members the first time it is seen.
        private static GroupState StateFor(int groupId, List<BotController> members)
        {
            if (!_groups.TryGetValue(groupId, out GroupState state))
            {
                state = new GroupState { PhaseSince = Time.realtimeSinceStartup };
                _groups[groupId] = state;
            }

            // Only an idle group may found its post. A fighting one is somewhere it walked to, and replacement
            // churn briefly puts more bots in the batch than it was summoned with, which would move the post there.
            if (state.Phase != SquadPhase.Posted || state.TargetId != null) return state;

            if (members.Count <= state.FoundedCount) return state;

            state.Post = MeanPosition(members);
            state.PostHeading = PostHeadingOf(members);
            state.RestHeading = state.PostHeading;   // never fought yet, so it rests the way it was set up
            state.Anchor = state.Post;
            state.FoundedCount = members.Count;

            return state;
        }

        // Opens and closes the line over a fight. A fixed gap is the easiest thing about a formation to read.
        private static void StepSpacing(GroupState state, SquadSettings settings, float deltaTime)
        {
            float now = Time.realtimeSinceStartup;
            float min = settings.Spacing;
            float max = settings.Spacing + settings.SpacingVariance;

            // First tick for this group, or the levers moved under it.
            if (state.Spacing < min || state.Spacing > max)
            {
                state.Spacing = state.Spacing <= 0f ? min : Mathf.Clamp(state.Spacing, min, max);
                state.SpacingRerollAt = 0f;
            }

            // Only a group that is fighting breathes; otherwise it settles back to the minimum and holds still.
            if (state.Phase != SquadPhase.Engaged)
            {
                state.SpacingTarget = min;
                state.SpacingRerollAt = 0f;   // re-roll on the first tick of the next engagement
            }
            else if (now >= state.SpacingRerollAt)
            {
                state.SpacingTarget = Random.Range(min, max);
                state.SpacingRerollAt = now + Random.Range(SpacingRerollMin, SpacingRerollMax);
            }

            state.Spacing = Mathf.MoveTowards(state.Spacing, state.SpacingTarget, SpacingDriftRate * deltaTime);
        }

        // Gives ground away from the enemy, capped on distance travelled from where the bout started.
        private static Vector2 RetreatAnchor(GroupState state, Vector2 targetPos, float maxGiven, float deltaTime)
        {
            Vector2 anchor = state.Anchor;

            float remaining = maxGiven - Vector2.Distance(anchor, state.BreakoffFrom);
            if (remaining <= AnchorTolerance) return anchor;

            Vector2 away = anchor - targetPos;
            if (away.sqrMagnitude < 1e-4f) return anchor;   // standing on top of us: no direction to give ground in

            anchor += away.normalized * Mathf.Min(FollowSpeed * deltaTime, remaining);

            state.Anchor = anchor;
            return anchor;
        }

        // How many a line may hold before LineStandoff would push it inside MinStandoff. The same constraint that
        // already sets the standoff, so the front rank follows squadStandoff and squadSpacing rather than being a
        // number of its own. Fed the lever and not the live gap, so a breathing line cannot reshuffle the ranks.
        private static int RankSize(float spacing, float standoff)
        {
            if (spacing <= 0f) return 1;

            float inner = (standoff * standoff) - (MinStandoff * MinStandoff);
            if (inner <= 0f) return 1;

            return Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.Sqrt(inner) / spacing) + 1);
        }

        // The range squadStandoff describes, measured from the ends of the line rather than its middle. The ends
        // sit sqrt(standoff^2 + offset^2) out, so setting the point from them puts the whole line at standoff.
        private static float LineStandoff(int members, float spacing, float standoff)
        {
            float halfSpan = (members - 1) * spacing * 0.5f;
            float inner = (standoff * standoff) - (halfSpan * halfSpan);

            return Mathf.Max(inner <= 0f ? 0f : Mathf.Sqrt(inner), MinStandoff);
        }

        private static Vector2 MoveAnchor(GroupState state, Vector2 targetPos, float standoff, float deltaTime)
        {
            Vector2 anchor = state.Anchor;
            Vector2 toTarget = targetPos - anchor;
            float distance = toTarget.magnitude;

            if (distance > 1e-4f)
            {
                Vector2 dir = toTarget / distance;

                if (distance > standoff + AnchorTolerance)
                {
                    anchor += dir * Mathf.Min(FollowSpeed * deltaTime, distance - standoff);
                }
                else if (distance < standoff - AnchorTolerance)
                {
                    // Already giving ground as fast as it can, and the enemy closed anyway. Backing up cannot win
                    // from here, so the group stands instead: the enemy runs past, the direction to them reverses,
                    // and the line ends up behind them facing back without anything having to decide to turn.
                    float deficit = standoff - distance;
                    bool flatOut = deficit > RetreatSpeed * deltaTime;
                    bool losing = distance < state.LastRange - RangeEpsilon;

                    if (!(flatOut && losing))
                        anchor -= dir * Mathf.Min(RetreatSpeed * deltaTime, deficit);
                }
            }

            state.LastRange = distance;
            state.Anchor = anchor;
            return anchor;
        }

        // Groups the squad-enabled bots by the batch they spawned in.
        private static void Bucket(IReadOnlyList<BotController> bots)
        {
            foreach (var list in _byGroup.Values) list.Clear();

            for (int i = 0; i < bots.Count; i++)
            {
                BotController bot = bots[i];
                if (bot.GroupId == 0) continue;                                     // no batch, so nothing to hold it to
                if (!(bot.Ai is ISquadMember member)) continue;

                // Either half is reason enough to be here: a formation needs slots to fight from, and a station
                // needs one to stand on even when it is alone and never forms up with anybody.
                if (!member.WantsSquad && !member.SquadSettings.Post) continue;

                // Initialized, not merely spawned: a bot is teleported to where it was summoned as it spawns, and
                // counting it before that would found the group's post at the game's spawn point instead.
                if (!bot.Initialized || !bot.Placed || bot.Position == null) continue;

                if (!_byGroup.TryGetValue(bot.GroupId, out List<BotController> group))
                {
                    group = new List<BotController>();
                    _byGroup[bot.GroupId] = group;
                }

                group.Add(bot);
            }

            // Drop buckets nobody is using any more so the dictionary doesn't grow across a long session. The
            // group state itself is kept: a station whose members are all mid-replacement must not lose its post.
            _finishedGroups.Clear();
            foreach (var pair in _byGroup)
                if (pair.Value.Count == 0) _finishedGroups.Add(pair.Key);
            for (int i = 0; i < _finishedGroups.Count; i++)
                _byGroup.Remove(_finishedGroups[i]);
        }

        // Lays out one group on its anchor and records each member's slot.
        private static void AssignSlots(List<BotController> group, Vector2 facePos, Vector2 anchor, Vector2 forward, GroupState state, SquadSettings settings, int rankSize)
        {
            // The line is held square to the enemy, so the gap between members always faces them.
            Vector2 right = new Vector2(-forward.y, forward.x);

            // Nearest the formation's own point holds the line, not nearest the enemy. Ranking on the enemy
            // promotes a bot standing behind them and then hands it a slot on the far side, so it walks into the
            // enemy trying to reach it. Ranked on the point, a bot round the back is simply not a line bot.
            if (group.Count > rankSize)
                group.Sort((a, b) => (Planar(a) - anchor).sqrMagnitude.CompareTo((Planar(b) - anchor).sqrMagnitude));

            // Only a fight has reserves. Backing off to re-form and waiting at the post are done as one line, or
            // the extras would be circling in toward the enemy while the rest of the group walks away from it.
            int front = state.Phase == SquadPhase.Engaged
                ? Mathf.Min(rankSize, group.Count)
                : group.Count;

            // Ordering along that line keeps every bot on the side it is already on, so the slots turn with the
            // enemy instead of being swapped between bots and sending them through each other.
            group.Sort(0, front, new AlongLine(anchor, right));

            // The squad shares its most recent successful guard: a blocked stab is spent for the whole line.
            float blockTime = 0f;
            for (int i = 0; i < group.Count; i++)
                blockTime = Mathf.Max(blockTime, CombatTracker.LastBlockTime(group[i].PlayerId));

            float spacing = state.Spacing > 0f ? state.Spacing : settings.Spacing;
            float span = (front - 1) * spacing;

            for (int i = 0; i < group.Count; i++)
            {
                bool loose = i >= front;

                Vector2 position;
                if (loose)
                {
                    // Bearing kept, range clamped into the band: a reserve closes on the enemy from whichever side
                    // it is already on, so it can reach and stab like anyone else. No bearing is assigned to it.
                    // The pushback between reserves is what spreads them into a ring rather than a queue.
                    Vector2 fromTarget = Planar(group[i]) - facePos;
                    float range = fromTarget.magnitude;
                    Vector2 bearing = range > 1e-4f ? fromTarget / range : -forward;

                    position = facePos + bearing * Mathf.Clamp(range,
                        settings.Standoff * ReserveInner, settings.Standoff * ReserveOuter);
                }
                else
                {
                    position = anchor + right * ((i * spacing) - (span * 0.5f));
                }

                _slots[group[i].PlayerId] = new SquadSlot
                {
                    Position = position,
                    Loose = loose,
                    LaneClear = IsLaneClear(group, i, facePos, settings),
                    Members = group.Count,
                    BlockTime = blockTime,

                    // Alternate the swing along the line so neighbours never throw the same way.
                    StabHigh = (i % 2) == 0,
                    SharedHigh = state.SharedStabHigh,

                    Phase = state.Phase,
                    AttackAllowedAt = state.ProvokedAt + settings.EngageDelay,
                    Spacing = spacing,
                    Facing = MovementSolver.HeadingOf(forward),
                };
            }
        }

        // Whether this member can swing at the target without a squadmate in the way. Only squadmates are checked:
        // they are the ones lined up on the same enemy, and they are the friendly fire actually being caused here.
        private static bool IsLaneClear(List<BotController> group, int index, Vector2 targetPos, SquadSettings settings)
        {
            Vector2 from = Planar(group[index]);
            Vector2 toTarget = targetPos - from;
            float reach = toTarget.magnitude;
            if (reach < 1e-4f) return true;

            Vector2 dir = toTarget / reach;

            for (int i = 0; i < group.Count; i++)
            {
                if (i == index) continue;

                Vector2 toMate = Planar(group[i]) - from;
                float along = Vector2.Dot(toMate, dir);
                if (along <= 0f || along > reach) continue; // behind us, or further off than the target

                float lateral = Mathf.Abs(toMate.x * dir.y - toMate.y * dir.x); // distance from the swing line
                if (lateral < settings.LaneHalfWidth) return false;
            }

            return true;
        }

        private static ISquadMember AsMember(BotController bot) => bot.Ai as ISquadMember;

        private static SquadSettings SettingsOf(BotController bot) =>
            bot.Ai is ISquadMember member
                ? member.SquadSettings
                : new SquadSettings { Spacing = 0.85f, LaneHalfWidth = 0.5f, Standoff = 1.5f, BreakoffRange = 2f, ResetRange = 15f };

        // Comparer<T>.Create is stripped from the IL2CPP game, so this comparison is its own class.
        private sealed class AlongLine : IComparer<BotController>
        {
            private readonly Vector2 _anchor;
            private readonly Vector2 _right;

            public AlongLine(Vector2 anchor, Vector2 right)
            {
                _anchor = anchor;
                _right = right;
            }

            public int Compare(BotController a, BotController b)
            {
                return Vector2.Dot(Planar(a) - _anchor, _right).CompareTo(Vector2.Dot(Planar(b) - _anchor, _right));
            }
        }

        private static Vector2 Planar(BotController bot)
        {
            Vector3 p = bot.Position ?? Vector3.zero;
            return new Vector2(p.x, p.z);
        }
    }
}
