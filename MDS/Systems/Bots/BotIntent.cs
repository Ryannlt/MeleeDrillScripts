using UnityEngine;

namespace MDS.Systems
{
    // A brain's desired output for a single tick. A null field means "issue no command on that
    // channel" so brains only emit console commands when something should change (keeps traffic low).
    public struct BotIntent
    {
        public Vector2? MoveAxis;    // (sideways, forwards), each in [-1, 1]
        public float? LookHeading;   // degrees from North
        public float? LookPitch;     // vertical aim, 0 being level; see BotController.PitchRefreshSeconds
        public bool? Running;        // toggle run

        // The action channel: one carbonPlayers playerAction token this tick, null for none. Edge-triggered.
        public string Action;

        public static BotIntent Idle => new BotIntent();
    }
}
