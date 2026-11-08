using System.Collections.Generic;
using UnityEngine;
using Holdfast.Modding;

namespace MDS.Systems
{
    // Turns the edge-triggered melee PlayerActions in each packet into a per-player melee state.
    public static class CombatTracker
    {
        // How long after a commit to keep the block up, covering the stab's lethal window.
        public const float LethalWindowSeconds = 1.0f;

        public struct MeleeState
        {
            public string WindupDir;   // "High"/"Low"/"Left"/"Right" of the latest windup (null if never seen)
            public float WindupTime;    // realtime of the latest windup start
            public float CommitTime;    // realtime of the latest ExecuteMeleeWeaponStrike
            public bool WindingUp;      // a windup is chambered: seen, not yet committed or canceled

            // The live guard. A held stab is thrown the moment this drops, so it is a read, not just bookkeeping.
            public bool Guarding;       // a block is up: raised and not yet stopped
            public string GuardDir;     // "High"/"Low"/"Left"/"Right" of that block
            public float GuardSince;    // realtime it went up
            public float GuardDroppedAt; // realtime it last came down

            public float SpentAt;       // realtime the committed swing was blocked, so it can no longer land
            public float FeintedAt;     // realtime a chamber was thrown away rather than committed

            // True while this player is a melee threat: winding up, or a committed swing still in flight. A swing
            // somebody has already blocked is spent, whatever is left of its lethal window.
            public bool IsThreat(float now) =>
                WindingUp || ((now - CommitTime) < LethalWindowSeconds && CommitTime > SpentAt);
        }

        private static readonly Dictionary<int, MeleeState> _states = new();

        // Realtime of each player's most recent successful block.
        private static readonly Dictionary<int, float> _lastBlock = new();

        // Who each defender most recently blocked, the attacker of their last absorbed hit. Lets a bot engage the
        // player who actually attacked it, rather than anyone merely swinging nearby.
        private static readonly Dictionary<int, int> _lastBlockAttacker = new();

        // Realtime of each player's most recent clash, and who it was with.
        private static readonly Dictionary<int, float> _lastClash = new();
        private static readonly Dictionary<int, int> _lastClashPartner = new();

        public static void OnPacket(int playerId, PlayerActions[] actions)
        {
            if (actions == null || actions.Length == 0) return;

            _states.TryGetValue(playerId, out MeleeState s);
            float now = Time.realtimeSinceStartup;
            bool changed = false;

            for (int i = 0; i < actions.Length; i++)
            {
                string name = actions[i].ToString();

                if (name.Length > 11 && name.StartsWith("MeleeStrike"))       // MeleeStrike{High|Low|...}
                {
                    s.WindupDir = name.Substring(11);
                    s.WindupTime = now;
                    s.WindingUp = true;
                    changed = true;
                }
                else if (name == "ExecuteMeleeWeaponStrike")                  // committed the swing
                {
                    s.CommitTime = now;
                    s.WindingUp = false;
                    changed = true;
                }
                else if (name.StartsWith("MeleeBlock") || name == "StartMeleeBlock" || name == "ChangeMeleeBlock")
                {
                    // a block mid-windup is a feint or cancel; the chamber is gone, and they are briefly open.
                    if (s.WindingUp) { s.WindingUp = false; s.FeintedAt = now; changed = true; }

                    // GuardSince is only stamped on the way up, so switching direction does not restart the clock.
                    if (!s.Guarding) { s.Guarding = true; s.GuardSince = now; }
                    if (name.Length > 10 && name.StartsWith("MeleeBlock")) s.GuardDir = name.Substring(10);
                    changed = true;
                }
                else if (name == "StopMeleeBlock")
                {
                    if (s.Guarding) { s.Guarding = false; s.GuardDroppedAt = now; changed = true; }
                }
            }

            if (changed) _states[playerId] = s;
        }

        public static bool TryGet(int playerId, out MeleeState state) => _states.TryGetValue(playerId, out state);

        // A block landed: defenderId successfully blocked attackerId's strike. This does not shorten the attacker's
        // own recovery; a blocked stab still costs the full ~1.5s, the same as a miss.
        public static void OnBlock(int attackerId, int defenderId)
        {
            float now = Time.realtimeSinceStartup;

            _lastBlock[defenderId] = now;
            _lastBlockAttacker[defenderId] = attackerId;

            // That swing is spent. Without this it stays a "threat" for the rest of its lethal window, so every
            // bot keeps guarding against a stab it has already stopped instead of answering it.
            MarkSwingSpent(attackerId, now);
        }

        // Two strikes met, so both swings are over and neither can land for the rest of its lethal window.
        public static void OnClash(int attackerId, int defenderId)
        {
            float now = Time.realtimeSinceStartup;

            _lastClash[attackerId] = now;
            _lastClash[defenderId] = now;
            _lastClashPartner[attackerId] = defenderId;
            _lastClashPartner[defenderId] = attackerId;

            MarkSwingSpent(attackerId, now);
            MarkSwingSpent(defenderId, now);
        }

        private static void MarkSwingSpent(int playerId, float now)
        {
            if (_states.TryGetValue(playerId, out MeleeState s))
            {
                s.SpentAt = now;
                _states[playerId] = s;
            }
        }

        // Realtime of playerId's last clash on either side, or 0 if none seen.
        public static float LastClashTime(int playerId) => _lastClash.TryGetValue(playerId, out float t) ? t : 0f;

        // Who playerId last clashed with, or null if none seen.
        public static int? LastClashPartner(int playerId) => _lastClashPartner.TryGetValue(playerId, out int p) ? p : (int?)null;

        // Realtime of playerId's last successful block as defender, or 0 if none seen.
        public static float LastBlockTime(int playerId) => _lastBlock.TryGetValue(playerId, out float t) ? t : 0f;

        // The attacker of playerId's last absorbed hit (as defender), or null if none seen.
        public static int? LastBlockAttacker(int playerId) => _lastBlockAttacker.TryGetValue(playerId, out int a) ? a : (int?)null;

        // Drops one player's melee state, called when they leave.
        public static void Clear(int playerId)
        {
            _states.Remove(playerId);
            _lastBlock.Remove(playerId);
            _lastBlockAttacker.Remove(playerId);
            _lastClash.Remove(playerId);
            _lastClashPartner.Remove(playerId);
        }

        public static void Reset()
        {
            _states.Clear();
            _lastBlock.Clear();
            _lastBlockAttacker.Clear();
            _lastClash.Clear();
            _lastClashPartner.Clear();
        }
    }
}
