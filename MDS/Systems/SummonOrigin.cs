using System;
using System.Collections;
using UnityEngine;
using MDS.Core;

namespace MDS.Systems
{
    // Resolves where a summon should land for a caller: spawned body, spectator, or free roam.
    public static class SummonOrigin
    {
        // Below this planar length the camera points almost straight up/down and has no usable yaw.
        private const float MinPlanarForwardSqr = 1e-4f;

        // How long to wait for a packet before giving up on a free-roam summon.
        private const float ViewpointTimeoutSeconds = 2f;

        // The origin for a summon centred on the caller. To place at another player instead, callers use
        // ResolveAtPlayer directly (see the summonAt commands).
        public static void Resolve(int playerId, Action<BotPlacement> onResolved, Action<string> onFailed)
        {
            if (StateTracker.IsSpectator(playerId))
            {
                ResolveFromViewpoint(playerId, onResolved, onFailed);
                return;
            }

            IPlayer caller = StateTracker.GetPlayerById(playerId);
            if (caller?.PlayerObject == null)
            {
                onFailed("Cannot summon - your position is unavailable (are you spawned?).");
                return;
            }

            Transform t = caller.PlayerObject.transform;
            onResolved(new BotPlacement(t.position, t.eulerAngles.y));
        }

        // Origin at another player's live position.
        public static void ResolveAtPlayer(int targetPlayerId, Action<BotPlacement> onResolved, Action<string> onFailed)
        {
            IPlayer target = StateTracker.GetPlayerById(targetPlayerId);
            if (target == null)
            {
                onFailed($"Cannot summon - no player with id {targetPlayerId}.");
                return;
            }

            if (target.PlayerObject == null || StateTracker.IsSpectator(targetPlayerId))
            {
                onFailed($"Cannot summon - player {targetPlayerId} ({target.PlayerName}) is not currently spawned.");
                return;
            }

            Transform t = target.PlayerObject.transform;
            onResolved(new BotPlacement(t.position, t.eulerAngles.y));
        }

        // Free roam: place directly below the viewpoint, since it has no body on the ground.
        private static void ResolveFromViewpoint(int playerId, Action<BotPlacement> onResolved, Action<string> onFailed)
        {
            Logger.Log($"Awaiting viewpoint of player {playerId} to resolve summon origin...", LogLevel.DEBUG);

            bool answered = false;

            PlayerPacketAwaiter.WaitForPacket(playerId, packet =>
            {
                answered = true;

                Logger.Log($"Summon origin packet for {playerId}: camPos={packet.CameraPosition.HasValue}, " +
                           $"ownerPos={packet.OwnerPosition.HasValue}, camFwd={packet.CameraForward.HasValue}, " +
                           $"yaw={packet.OwnerYaw.HasValue}, rotY={packet.OwnerRotationY.HasValue}.", LogLevel.DEBUG);

                Vector3? viewpoint = packet.CameraPosition ?? packet.OwnerPosition;
                if (!viewpoint.HasValue)
                {
                    onFailed("Cannot summon - your position is unavailable while spectating.");
                    return;
                }

                Vector2 planar = new Vector2(viewpoint.Value.x, viewpoint.Value.z);
                Vector3 position = new Vector3(planar.x, TerrainSampler.GetYAt(planar), planar.y);

                onResolved(new BotPlacement(position, ViewHeading(packet)));
            });

            ModRoutines.Start(FailIfNoPacket(playerId, () => answered, onFailed), "summon viewpoint timeout");
        }

        // A free-flying player can send NO packets at all. Without this the wait would sit queued and fire
        // much later - when they eventually respawn - summoning a bot at that moment instead of failing.
        private static IEnumerator FailIfNoPacket(int playerId, Func<bool> answered, Action<string> onFailed)
        {
            yield return ModRoutines.Wait(ViewpointTimeoutSeconds);
            if (answered()) yield break;

            PlayerPacketAwaiter.CancelWait(playerId);
            Logger.Log($"Summon origin: no packet from player {playerId} within {ViewpointTimeoutSeconds}s (the server appears to receive none while free-flying).", LogLevel.WARNING);
            onFailed("Cannot summon - no position received while in free roam. Spawn in first, or place bots with 'rc spawnLine <x> <z> <rotation>'.");
        }

        // The heading the viewpoint looks along, in degrees from North.
        private static float ViewHeading(PlayerPacketData packet)
        {
            if (packet.CameraForward.HasValue)
            {
                Vector2 planar = new Vector2(packet.CameraForward.Value.x, packet.CameraForward.Value.z);
                if (planar.sqrMagnitude >= MinPlanarForwardSqr)
                    return MovementSolver.HeadingOf(planar);
            }

            if (packet.OwnerYaw.HasValue) return packet.OwnerYaw.Value;
            if (packet.OwnerRotationY.HasValue) return packet.OwnerRotationY.Value;
            return 0f;
        }
    }
}
