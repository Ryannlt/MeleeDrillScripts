using UnityEngine;
using Holdfast.Modding;
using System;
using System.Collections.Generic;

namespace MDS.Systems
{
    public static class PlayerPacketAwaiter
    {
        private static readonly Dictionary<int, Action<PlayerPacketData>> _awaiters = new();

        public static void WaitForPacket(int playerId, Action<PlayerPacketData> callback)
        {
            _awaiters[playerId] = callback;
        }

        // Drops a pending wait, for a player who stops sending packets before one arrives.
        public static bool CancelWait(int playerId) => _awaiters.Remove(playerId);

        public static void HandlePlayerPacket(
            int playerId,
            Vector3? ownerPosition,
            double? packetTimestamp,
            Vector2? ownerInputAxis,
            float? ownerRotationY,
            float? ownerPitch,
            float? ownerYaw,
            PlayerActions[] actionCollection,
            Vector3? cameraPosition,
            Vector3? cameraForward,
            ushort? shipID,
            bool swimming)
        {
            if (_awaiters.TryGetValue(playerId, out var callback))
            {
                _awaiters.Remove(playerId);
                var data = new PlayerPacketData
                {
                    PlayerId = playerId,
                    OwnerPosition = ownerPosition,
                    PacketTimestamp = packetTimestamp,
                    OwnerInputAxis = ownerInputAxis,
                    OwnerRotationY = ownerRotationY,
                    OwnerPitch = ownerPitch,
                    OwnerYaw = ownerYaw,
                    ActionCollection = actionCollection,
                    CameraPosition = cameraPosition,
                    CameraForward = cameraForward,
                    ShipID = shipID,
                    Swimming = swimming
                };
                callback.Invoke(data);
            }
        }
    }
}
