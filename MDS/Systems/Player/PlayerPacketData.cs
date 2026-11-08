using UnityEngine;
using Holdfast.Modding;

namespace MDS.Systems
{
    public class PlayerPacketData
    {
        public int PlayerId;
        public Vector3? OwnerPosition;
        public double? PacketTimestamp;
        public Vector2? OwnerInputAxis;
        public float? OwnerRotationY;
        public float? OwnerPitch;
        public float? OwnerYaw;
        public PlayerActions[] ActionCollection;
        public Vector3? CameraPosition;
        public Vector3? CameraForward;
        public ushort? ShipID;
        public bool Swimming;
    }
}
