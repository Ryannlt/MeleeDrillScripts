using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Holdfast.Modding;
using MDS.Core;

// A debug mode that records what the melee actually did, off unless asked for with 'rc meleelog' or the
// EnableMeleeLog config variable. Everything goes to the server log.
//
//   hit ...     one line per melee damage event, written once the attacker is known
//   block/clash one line per event
//   probe[n]    one line per tick for each player under 'rc bot probe'
//   frames      the last second of every player, flushed on melee damage ('rc meleelog frames on')
//
// atk~, hold~ and block~ are inferred from the player's packets, because no API reports melee state. cmd is the last
// action MDS ordered for a bot.

namespace MDS.Systems
{
    public static class MeleeDebug
    {
        private const float SampleIntervalSeconds = 0.05f;
        private const int BufferedFrames = 20;

        // How long a hit waits for the game to name its attacker. The kill callback can trail the damage by ~0.5s.
        private const float MatchWindowSeconds = 1f;

        private struct BodySnapshot
        {
            public int Id;
            public Vector2 Position;
            public float Heading;
            public FactionCountry? Faction;
            public string Strike;
            public float? Pitch;
        }

        private sealed class PendingHit
        {
            public int VictimId;
            public float OldHp;
            public float NewHp;
            public EntityHealthChangedReason Reason;
            public int KillerId = -1;
            public float At;
            public List<BodySnapshot> Bodies;
        }

        private struct ScoreRecord
        {
            public int AttackerId;
            public int VictimId;
            public bool Friendly;
            public int Damage;
            public string Weapon;
            public float At;
        }

        private struct CommandRecord
        {
            public string Action;
            public float At;
        }

        private static bool _enabled;
        private static bool _framesEnabled;
        private static ModRoutine _sampler;

        // The scorable action and the hurt event are not promised in either order, so each waits for the other.
        private static readonly List<PendingHit> _pendingHits = new();
        private static readonly List<ScoreRecord> _recentScores = new();

        private static readonly Dictionary<int, float> _pitch = new();
        private static readonly Dictionary<int, CommandRecord> _commands = new();
        private static readonly Queue<string> _frames = new();

        public static bool Enabled => _enabled;
        public static bool FramesEnabled => _framesEnabled;

        public static void SetEnabled(bool enabled)
        {
            if (_enabled == enabled || !StateTracker.IsServer) return;
            _enabled = enabled;

            if (enabled)
            {
                Logger.Log("MeleeLog on. atk~, hold~ and block~ are inferred from packets; cmd is the last action MDS ordered for a bot.", LogLevel.INFO);
                _sampler = ModRoutines.Start(SampleLoop(), "melee log sampler");
                return;
            }

            ModRoutines.Stop(_sampler);
            _sampler = null;
            ClearBuffers();
            Logger.Log("MeleeLog off.", LogLevel.INFO);
        }

        public static void SetFramesEnabled(bool enabled)
        {
            _framesEnabled = enabled;
            if (!enabled) _frames.Clear();
        }

        // Player ids are reissued between rounds, so nothing here may carry over.
        public static void Reset()
        {
            ClearBuffers();
        }

        private static void ClearBuffers()
        {
            _pendingHits.Clear();
            _recentScores.Clear();
            _pitch.Clear();
            _commands.Clear();
            _frames.Clear();
        }

        public static void OnPacket(int playerId, float? pitch)
        {
            if (_enabled && pitch.HasValue) _pitch[playerId] = pitch.Value;
        }

        public static void NoteCommand(int playerId, string action)
        {
            if (_enabled) _commands[playerId] = new CommandRecord { Action = action, At = Time.realtimeSinceStartup };
        }

        public static void OnBlock(int attackerId, int defenderId)
        {
            if (_enabled) Logger.Log($"block atk={attackerId} def={defenderId} t={Time.realtimeSinceStartup:F2}", LogLevel.INFO);
        }

        public static void OnClash(int attackerId, int defenderId)
        {
            if (_enabled) Logger.Log($"clash atk={attackerId} def={defenderId} t={Time.realtimeSinceStartup:F2}", LogLevel.INFO);
        }

        public static void OnHurt(int victimId, float oldHp, float newHp, EntityHealthChangedReason reason)
        {
            if (!_enabled || !IsMelee(reason)) return;

            var hit = new PendingHit
            {
                VictimId = victimId,
                OldHp = oldHp,
                NewHp = newHp,
                Reason = reason,
                At = Time.realtimeSinceStartup,
                Bodies = SnapshotBodies(),
            };

            if (_framesEnabled) FlushFrames(hit);

            for (int i = 0; i < _recentScores.Count; i++)
            {
                if (_recentScores[i].VictimId != victimId) continue;

                WriteHit(hit, _recentScores[i]);
                _recentScores.RemoveAt(i);
                return;
            }

            _pendingHits.Add(hit);
        }

        public static void OnScorableAction(ServerScorableActionDetails details)
        {
            if (!_enabled || details.IsAssist || details.PlayerId < 0 || details.TargetPlayerId < 0) return;

            // details is shared with every mod and must not be kept, so this copies what the ledger needs.
            var score = new ScoreRecord
            {
                AttackerId = details.PlayerId,
                VictimId = details.TargetPlayerId,
                Friendly = details.IsFriendly,
                Damage = details.DamageDealt,
                Weapon = details.Weapon.ToString(),
                At = Time.realtimeSinceStartup,
            };

            for (int i = 0; i < _pendingHits.Count; i++)
            {
                if (_pendingHits[i].VictimId != score.VictimId) continue;

                WriteHit(_pendingHits[i], score);
                _pendingHits.RemoveAt(i);
                return;
            }

            _recentScores.Add(score);
        }

        // Second best to a scorable action: it names the real killer, but says nothing of friendly fire or damage.
        // It can arrive first, so the hit keeps waiting and only falls back to this when no scorable action comes.
        public static void OnKill(int killerId, int victimId, EntityHealthChangedReason reason)
        {
            if (!_enabled || !IsMelee(reason)) return;

            for (int i = 0; i < _pendingHits.Count; i++)
            {
                if (_pendingHits[i].VictimId != victimId) continue;

                _pendingHits[i].KillerId = killerId;
                return;
            }
        }

        private static bool IsMelee(EntityHealthChangedReason reason) =>
            reason == EntityHealthChangedReason.HitByMeleeWeapon || reason == EntityHealthChangedReason.HitByMeleeSecondaryAttack;

        private static IEnumerator SampleLoop()
        {
            while (_enabled)
            {
                ExpireUnmatched();
                WriteProbes();

                if (_framesEnabled)
                {
                    _frames.Enqueue(DescribeFrame());
                    while (_frames.Count > BufferedFrames) _frames.Dequeue();
                }

                yield return ModRoutines.Wait(SampleIntervalSeconds);
            }
        }

        private static void ExpireUnmatched()
        {
            float cutoff = Time.realtimeSinceStartup - MatchWindowSeconds;

            for (int i = _pendingHits.Count - 1; i >= 0; i--)
            {
                if (_pendingHits[i].At > cutoff) continue;

                int killerId = _pendingHits[i].KillerId;
                WriteLine(_pendingHits[i], killerId, killerId >= 0 ? "kill" : "unknown", "?", "?", "?");
                _pendingHits.RemoveAt(i);
            }

            _recentScores.RemoveAll(s => s.At <= cutoff);
        }

        private static void WriteProbes()
        {
            IReadOnlyList<IPlayer> players = StateTracker.AllPlayers;
            for (int i = 0; i < players.Count; i++)
            {
                IPlayer player = players[i];
                if (!MeleeProbe.IsProbing(player.PlayerId) || !TrySnapshot(player, out BodySnapshot body)) continue;

                var bodies = SnapshotBodies();
                NearestTo(bodies, body, out float nearFriend, out float nearEnemy);
                Logger.Log($"probe[{player.PlayerId}] t={Time.realtimeSinceStartup:F2} {Describe(player, body)} " +
                           $"nearFriend={Distance(nearFriend)} nearEnemy={Distance(nearEnemy)}", LogLevel.INFO);
            }
        }

        private static string DescribeFrame()
        {
            string frame = $"t={Time.realtimeSinceStartup:F2}";

            IReadOnlyList<IPlayer> players = StateTracker.AllPlayers;
            for (int i = 0; i < players.Count; i++)
            {
                if (TrySnapshot(players[i], out BodySnapshot body))
                    frame += $" | {players[i].PlayerId} {Describe(players[i], body)}";
            }

            return frame;
        }

        private static string Describe(IPlayer player, BodySnapshot body)
        {
            string hp = StateTracker.TryGetPlayerStatus(player.PlayerId, out QueryServerPlayerStatus status)
                ? status.Vitals.Health.ToString("0.#")
                : "?";

            string attacking = "-";
            string held = "-";
            string block = "-";
            if (CombatTracker.TryGet(player.PlayerId, out CombatTracker.MeleeState melee))
            {
                attacking = melee.IsThreat(Time.realtimeSinceStartup) ? "1" : "0";
                if (melee.WindingUp) held = melee.WindupDir ?? "-";
                if (melee.Guarding) block = melee.GuardDir ?? "up";
            }

            string line = $"xz={body.Position.x:F2},{body.Position.y:F2} rot={body.Heading:F0} pitch={Pitch(body.Pitch)} hp={hp} " +
                          $"atk~={attacking} hold~={held} block~={block}";

            if (player.IsBot && _commands.TryGetValue(player.PlayerId, out CommandRecord command))
                line += $" cmd={command.Action}@{Time.realtimeSinceStartup - command.At:F2}s";

            return line;
        }

        private static void WriteHit(PendingHit hit, ScoreRecord score)
        {
            WriteLine(hit, score.AttackerId, "score", score.Friendly ? "1" : "0", score.Weapon, score.Damage.ToString());
        }

        // friendWasNearer is the point of the record. A strike spends its single hit on the nearest candidate in its
        // arc, so a friendly standing nearer than the enemy takes the blow.
        private static void WriteLine(PendingHit hit, int attackerId, string source, string friendly, string weapon, string damage)
        {
            string line = $"hit vic={hit.VictimId} atk={(attackerId < 0 ? "?" : attackerId.ToString())} src={source} ff={friendly} " +
                          $"weapon={weapon} dmg={damage} hp={hit.OldHp:0.#}->{hit.NewHp:0.#} reason={hit.Reason}";

            if (TryFind(hit.Bodies, attackerId, out BodySnapshot attacker) && TryFind(hit.Bodies, hit.VictimId, out BodySnapshot victim))
            {
                Vector2 toVictim = victim.Position - attacker.Position;
                float bearing = Mathf.DeltaAngle(attacker.Heading, MovementSolver.HeadingOf(toVictim));

                NearestTo(hit.Bodies, attacker, out float nearFriend, out float nearEnemy);
                string nearer = nearFriend < 0f || nearEnemy < 0f ? "?" : nearFriend < nearEnemy ? "1" : "0";

                line += $" strike~={attacker.Strike ?? "?"} vicDist={toVictim.magnitude:F2} vicBearing={bearing:F1} " +
                        $"atkPitch={Pitch(attacker.Pitch)} nearFriend={Distance(nearFriend)} nearEnemy={Distance(nearEnemy)} " +
                        $"friendWasNearer={nearer}";
            }

            Logger.Log(line, LogLevel.INFO);
        }

        private static void FlushFrames(PendingHit hit)
        {
            Logger.Log($"--- melee damage: player {hit.VictimId} {hit.OldHp:0.#}->{hit.NewHp:0.#} ({hit.Reason}), last {_frames.Count} frames ---", LogLevel.INFO);

            foreach (string frame in _frames)
                Logger.Log(frame, LogLevel.INFO);

            Logger.Log("--- end ---", LogLevel.INFO);
            _frames.Clear();
        }

        private static List<BodySnapshot> SnapshotBodies()
        {
            IReadOnlyList<IPlayer> players = StateTracker.AllPlayers;
            var bodies = new List<BodySnapshot>(players.Count);

            for (int i = 0; i < players.Count; i++)
                if (TrySnapshot(players[i], out BodySnapshot body)) bodies.Add(body);

            return bodies;
        }

        private static bool TrySnapshot(IPlayer player, out BodySnapshot body)
        {
            body = default;
            if (!player.IsAlive || player.PlayerObject == null) return false;

            Transform transform = player.PlayerObject.transform;
            Vector3 position = transform.position;

            CombatTracker.TryGet(player.PlayerId, out CombatTracker.MeleeState melee);

            body = new BodySnapshot
            {
                Id = player.PlayerId,
                Position = new Vector2(position.x, position.z),
                Heading = transform.eulerAngles.y,
                Faction = player.Faction,
                Strike = melee.WindupDir,
                Pitch = _pitch.TryGetValue(player.PlayerId, out float pitch) ? pitch : (float?)null,
            };
            return true;
        }

        private static bool TryFind(List<BodySnapshot> bodies, int playerId, out BodySnapshot found)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i].Id != playerId) continue;

                found = bodies[i];
                return true;
            }

            found = default;
            return false;
        }

        // Nearest friend and enemy of a body, in meters, or -1 where there is none.
        private static void NearestTo(List<BodySnapshot> bodies, BodySnapshot from, out float nearFriend, out float nearEnemy)
        {
            nearFriend = -1f;
            nearEnemy = -1f;

            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i].Id == from.Id) continue;

                float distance = Vector2.Distance(bodies[i].Position, from.Position);
                if (bodies[i].Faction == from.Faction)
                {
                    if (nearFriend < 0f || distance < nearFriend) nearFriend = distance;
                }
                else if (nearEnemy < 0f || distance < nearEnemy)
                {
                    nearEnemy = distance;
                }
            }
        }

        private static string Distance(float meters) => meters < 0f ? "-" : meters.ToString("F2");

        private static string Pitch(float? pitch) => pitch.HasValue ? pitch.Value.ToString("F2") : "-";
    }
}
