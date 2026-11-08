using UnityEngine;

// Owns one bot: its identity (IPlayer), spawn spec, AI, and death policy, and drives the actuator
// each tick. Phase 0 applies the AI's intent directly; a BotStateMachine arrives in a later phase.

namespace MDS.Systems
{
    public class BotController
    {
        public IPlayer Bot { get; }
        public int PlayerId => Bot.PlayerId;
        public BotAiEnum AiType => _ai.AiType;
        public IBotAi Ai => _ai;                          // current AI instance (debug/manual control hooks)
        public BotSpawnSpec Spec { get; }                 // null for randomly-spawned bots
        public BotDeathPolicy DeathPolicy { get; private set; }

        // The spawn batch this bot belongs to, 0 for none.
        public int GroupId { get; }

        // The heading this bot was asked to face when placed, which is what its post records.
        public float? SpawnHeading { get; private set; }
        public bool Initialized { get; private set; }

        // Whether the summon or replacement teleport has been issued. Initialized is set the moment the bot
        // spawns, which is before it has been moved, so it is not on its own enough to count a bot's position.
        public bool Placed { get; private set; }
        public bool IsAwaitingKick { get; private set; }   // a death-kick is scheduled; ignore further deaths

        // Forcing input rotation pins only the heading, so the vertical aim is re-asserted on this cadence.
        private const float LevelPitch = 0f;
        private const float PitchRefreshSeconds = 5f;

        private IBotAi _ai;
        private BotPlacement? _pendingPlacement;          // position + optional facing, applied on first spawn
        private float _nextPitchRefresh;
        private float _heldPitch = LevelPitch;   // what the refresh above re-asserts, last set by an intent
        private readonly float _trackedAt = Time.realtimeSinceStartup;

        public BotController(IPlayer bot, IBotAi ai, BotSpawnSpec spec, BotDeathPolicy deathPolicy, BotPlacement? placement, int groupId = 0)
        {
            Bot = bot;
            _ai = ai;
            Spec = spec;
            DeathPolicy = deathPolicy;
            _pendingPlacement = placement;
            GroupId = groupId;
        }

        public void SetAi(IBotAi ai)
        {
            _ai = ai;
            Logger.Log($"Bot {PlayerId} AI set to {ai.AiType}.", LogLevel.INFO);
        }

        public void SetDeathPolicy(BotDeathPolicy policy)
        {
            DeathPolicy = policy;
            Logger.Log($"Bot {PlayerId} death policy set to {policy}.", LogLevel.INFO);
        }

        public void MarkAwaitingKick() => IsAwaitingKick = true;

        // Called when the bot spawns (GameObject available). Enables input control on the first spawn,
        // and applies a pending placement (summon / replace): teleport + optional facing.
        public void OnSpawned()
        {
            if (!Initialized)
            {
                CarbonPlayerCommands.EnableInputControl(PlayerId);
                Initialized = true;
                Logger.Log($"Bot {PlayerId} initialized for input control.", LogLevel.DEBUG);
            }

            CarbonPlayerCommands.SetPitch(PlayerId, _heldPitch);
            _nextPitchRefresh = Time.realtimeSinceStartup + PitchRefreshSeconds;

            WarnIfSpawnedAsSomethingElse();

            if (_pendingPlacement.HasValue)
            {
                BotPlacement placement = _pendingPlacement.Value;
                CarbonPlayerCommands.Teleport(PlayerId, placement.Position);
                if (placement.Heading.HasValue)
                {
                    CarbonPlayerCommands.SetInputRotation(PlayerId, placement.Heading.Value);
                    SpawnHeading = placement.Heading;
                }

                Logger.Log($"Bot {PlayerId} placed at {placement.Position}{(placement.Heading.HasValue ? $" facing {placement.Heading.Value:F0} deg" : "")}.", LogLevel.DEBUG);
                _pendingPlacement = null;
            }

            // Nothing pending means it stands where it spawned on purpose, so either way it is where it belongs.
            Placed = true;
        }

        // The game does not always spawn the bot we asked for; warn rather than silently running the wrong one.
        private void WarnIfSpawnedAsSomethingElse()
        {
            if (Spec == null || !Bot.Faction.HasValue) return;
            if (Bot.Faction.Value == Spec.Faction && Bot.PlayerClass == Spec.Class) return;

            Logger.Log($"Bot {PlayerId} spawned as {FactionTokens.DisplayName(Bot.Faction.Value)}/{Bot.PlayerClass}, "
                       + $"but {FactionTokens.DisplayName(Spec.Faction)}/{Spec.Class} was requested. The game substituted it, "
                       + "usually because that team is full or the class is capped.", LogLevel.WARNING);
        }

        public void Tick(float deltaTime)
        {
            if (!Initialized) return;

            // Hold the aim level. Only while actually spawned, so we don't command a corpse.
            float now = Time.realtimeSinceStartup;
            if (now >= _nextPitchRefresh && Bot.PlayerObject != null)
            {
                CarbonPlayerCommands.SetPitch(PlayerId, _heldPitch);
                _nextPitchRefresh = now + PitchRefreshSeconds;
            }

            BotIntent intent = _ai.Decide(this, deltaTime);
            ApplyIntent(intent);
        }

        // Live world position via the spawn GameObject, or null if not currently spawned.
        public Vector3? Position => Bot.PlayerObject != null ? Bot.PlayerObject.transform.position : (Vector3?)null;

        // Live facing (degrees from North) via the spawn GameObject, or null if not currently spawned.
        public float? Heading => Bot.PlayerObject != null ? Bot.PlayerObject.transform.eulerAngles.y : (float?)null;

        // Live planar pose (world XZ + heading) for movement behaviors; false if not currently spawned.
        public bool TryGetPose(out BotPose pose)
        {
            pose = default;
            if (Position == null || Heading == null) return false;

            Vector3 p = Position.Value;
            pose = new BotPose(new Vector2(p.x, p.z), Heading.Value);
            return true;
        }

        // A bot the game accepted but never spawned. Dropped after a timeout so it cannot hold a slot forever.
        public bool IsUnspawnedGhost(float now, float timeoutSeconds) =>
            !Initialized && (now - _trackedAt) > timeoutSeconds;

        private void ApplyIntent(BotIntent intent)
        {
            if (intent.MoveAxis.HasValue)
                CarbonPlayerCommands.SetInputAxis(PlayerId, intent.MoveAxis.Value.x, intent.MoveAxis.Value.y);

            if (intent.LookHeading.HasValue)
                CarbonPlayerCommands.SetInputRotation(PlayerId, intent.LookHeading.Value);

            // Sent only when it changes. The refresh above is what keeps it from drifting, so re-sending an
            // unchanged pitch at 20 Hz would be pure command traffic for nothing.
            if (intent.LookPitch.HasValue && !Mathf.Approximately(intent.LookPitch.Value, _heldPitch))
            {
                _heldPitch = intent.LookPitch.Value;
                CarbonPlayerCommands.SetPitch(PlayerId, _heldPitch);
            }

            if (intent.Running.HasValue)
                CarbonPlayerCommands.SetRunning(PlayerId, intent.Running.Value);

            if (!string.IsNullOrEmpty(intent.Action))
                CarbonPlayerCommands.PerformAction(PlayerId, intent.Action);
        }
    }
}
