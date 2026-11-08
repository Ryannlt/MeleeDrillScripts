using System.Collections.Generic;
using UnityEngine;
using Holdfast.Modding;

namespace MDS.Systems
{
    // Dev instrumentation with no gameplay effect. Logs a probed player's melee signals only when they change.
    public static class MeleeProbe
    {
        private static readonly HashSet<int> _probed = new();
        private static readonly Dictionary<int, string> _lastActions = new();
        private static readonly Dictionary<int, float> _lastChangeTime = new();

        public static bool IsProbing(int playerId) => _probed.Contains(playerId);

        // The last swing a bot released, kept for every bot: friendly kills are rare and are what is being hunted.
        private struct StrikeRecord
        {
            public float ReleasedAt;
            public float AimDesired;   // heading the swing wanted
            public float AimClamped;   // heading it was allowed after clearing squadmates
            public int TargetId;
            public bool LaneClear;     // what the start gate thought when the swing began
        }

        private static readonly Dictionary<int, StrikeRecord> _lastStrike = new();

        public static void NoteStrike(int playerId, float now, float aimDesired, float aimClamped, int targetId, bool laneClear)
        {
            _lastStrike[playerId] = new StrikeRecord
            {
                ReleasedAt = now,
                AimDesired = aimDesired,
                AimClamped = aimClamped,
                TargetId = targetId,
                LaneClear = laneClear,
            };
        }

        // One line per released hold: how long it ran and what ended it. The whole mechanic rests on whether
        // re-sending MeleeStrike sustains the chamber, so held is the number to read first.
        public static void LogHold(int playerId, string dir, float held, string reason, float holdPays, float baitRate)
        {
            Logger.Log($"MeleeHold[{playerId}] dir={dir} held={held:0.00}s released={reason} " +
                       $"holdPays={holdPays:0.00} baitRate={baitRate:0.00}", LogLevel.INFO);
        }

        // One line per cancel: where in the chain it was, how deep the chamber got, and which way the re-chamber
        // goes. depth and dwell are the two numbers the whole mechanic is being measured for.
        public static void LogFeint(int playerId, int index, int count, float depth, float wanted, float range,
                                    bool wasLong, float aimOff, string fromDir, string toDir,
                                    float longPays, float shortPays, float switchPays, float keepPays)
        {
            Logger.Log($"MeleeFeint[{playerId}] {index}/{count} depth={depth:0.00}s wanted={wanted:0.00}s " +
                       $"range={range:0.00}m {(wasLong ? "long" : "short")} aimOff={aimOff:0.0} " +
                       $"dir={fromDir}->{toDir} longPays={longPays:0.00} shortPays={shortPays:0.00} " +
                       $"switchPays={switchPays:0.00} keepPays={keepPays:0.00}", LogLevel.INFO);
        }

        // One line per tick while a swing is live. actual is where the blade points now, clamped is where we told it.
        public static void LogSwingTick(int playerId, float actual, float desired, float clamped, float turned,
                                        bool mateAcross, float gateR, float clampR, string mates)
        {
            Logger.Log(
                $"MeleeSwing[{playerId}] actual={actual:0.#} desired={desired:0.#} clamped={clamped:0.#} " +
                $"held={Mathf.DeltaAngle(desired, clamped):0.#} turned={turned:0.#} across={mateAcross} " +
                $"gateR={gateR:0.##} clampR={clampR:0.##}{mates}",
                LogLevel.INFO);
        }

        // One line per friendly-fire kill, in the killer's own aim frame.
        public static void LogFriendlyFire(int killerId, int victimId, BotAiEnum killerAi, Vector2 killerPos, float killerHeading, Vector2 victimPos)
        {
            Vector2 toVictim = victimPos - killerPos;
            float dist = toVictim.magnitude;

            Vector2 forward = MovementSolver.DirectionFromHeading(killerHeading);
            Vector2 right = new Vector2(forward.y, -forward.x);

            float along = Vector2.Dot(toVictim, forward);
            float lateral = Vector2.Dot(toVictim, right);
            float bearing = Mathf.DeltaAngle(killerHeading, MovementSolver.HeadingOf(toVictim));

            string strike = "no strike on record";
            if (_lastStrike.TryGetValue(killerId, out StrikeRecord r))
            {
                // bearingAtRelease measures the victim against where the bot was pointing when it committed.
                float bearingAtRelease = Mathf.DeltaAngle(r.AimClamped, MovementSolver.HeadingOf(toVictim));

                strike = string.Format("strikeAge={0:0.00} bearingAtRelease={1:0.#} desired={2:0.#} clamped={3:0.#} moved={4:0.#} target={5} laneClear={6}",
                    Time.realtimeSinceStartup - r.ReleasedAt,
                    bearingAtRelease,
                    r.AimDesired,
                    r.AimClamped,
                    Mathf.DeltaAngle(r.AimDesired, r.AimClamped),
                    r.TargetId,
                    r.LaneClear);
            }

            // A dummy line stabbing itself is what a dummy line does, and at thousands a day it buries the
            // handful of kills a fighting AI causes. Same line either way, so one grep still finds them all.
            bool dummy = killerAi == BotAiEnum.StabbingDummy || killerAi == BotAiEnum.RiposteDummy;

            Logger.Log(
                string.Format("FriendlyFire: {0} ({1}) killed {2} | dist={3:0.00} bearing={4:0.#} along={5:0.00} lateral={6:0.00} behind={7} | {8}",
                    killerId, killerAi, victimId, dist, bearing, along, lateral, along < 0f, strike),
                dummy ? LogLevel.INFO : LogLevel.WARNING);
        }

        // One line per tick the aim is frozen because the blade is out with nothing left to fight.
        public static void LogBladeHold(int playerId, float actual, float wanted, string reason)
        {
            Logger.Log(
                $"MeleeHold[{playerId}] actual={actual:0.#} wanted={wanted:0.#} " +
                $"held={Mathf.DeltaAngle(actual, wanted):0.#} reason={reason}",
                LogLevel.INFO);
        }

        // Returns the new on/off state.
        public static bool Toggle(int playerId)
        {
            bool on = !_probed.Contains(playerId);
            Set(playerId, on);
            return on;
        }

        public static void Set(int playerId, bool on)
        {
            if (on)
            {
                if (_probed.Add(playerId))
                {
                    // The weapon's real strike properties need Assembly-CSharp; run MeleeLogger alongside for those.
                    Logger.Log($"MeleeProbe: probing player {playerId} - perform attacks/blocks/feints and read this log.", LogLevel.INFO);
                }
            }
            else if (_probed.Remove(playerId))
            {
                _lastActions.Remove(playerId);
                _lastChangeTime.Remove(playerId);
                Logger.Log($"MeleeProbe: stopped probing player {playerId}.", LogLevel.INFO);
            }
        }

        // Called for every player's packet; logs only probed players, and only on an action-set change.
        public static void OnPacket(int playerId, PlayerActions[] actions, float? rotationY, float? pitch, float? yaw)
        {
            if (_probed.Count == 0 || !_probed.Contains(playerId)) return;

            string current = (actions == null || actions.Length == 0) ? "(none)" : string.Join(",", actions);
            if (_lastActions.TryGetValue(playerId, out string prev) && prev == current) return; // unchanged

            float now = Time.realtimeSinceStartup;
            float delta = _lastChangeTime.TryGetValue(playerId, out float last) ? now - last : 0f;
            _lastActions[playerId] = current;
            _lastChangeTime[playerId] = now;

            Logger.Log($"MeleeProbe[{playerId}] t={now:F2} (+{delta:F2}s) actions=[{current}] rotY={Fmt(rotationY)} pitch={FmtFine(pitch)} yaw={Fmt(yaw)}", LogLevel.INFO);
        }

        // Called for every hurt event; logs when a probed player takes damage, to measure hit timing and blocks.
        public static void OnHurt(int victimId, float oldHp, float newHp)
        {
            if (_probed.Count == 0 || !_probed.Contains(victimId)) return;

            Logger.Log($"MeleeProbe[{victimId}] t={Time.realtimeSinceStartup:F2} HURT {oldHp}->{newHp}{(newHp <= 0f ? " (DEAD)" : "")}", LogLevel.INFO);
        }

        // Cleared on round change (player ids are round-scoped).
        public static void Reset()
        {
            _probed.Clear();
            _lastActions.Clear();
            _lastChangeTime.Clear();
        }

        private static string Fmt(float? v) => v.HasValue ? v.Value.ToString("F0") : "-";

        // Pitch needs decimals where the headings do not: the whole useful range of the game's chunk key is
        // about -1.5 to 2, so rounding to whole numbers would throw the measurement away.
        private static string FmtFine(float? v) => v.HasValue ? v.Value.ToString("F3") : "-";
    }
}
