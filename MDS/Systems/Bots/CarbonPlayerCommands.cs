using System.Globalization;
using UnityEngine;
using MDS.Core;

// The one place that knows the Holdfast 'carbonPlayers' command vocabulary.

namespace MDS.Systems
{
    public static class CarbonPlayerCommands
    {
        private const string Prefix = "carbonPlayers";

        // Placeholder for an empty name/regtag slot, so a following positional arg (e.g. uniformId) can
        // still be passed. The value only needs to be some non-empty string; it just occupies the slot.
        private const string EmptyArgPlaceholder = "none";

        // Spawn <count> bots with random class/faction across spawn points.
        public static void Spawn(int count)
        {
            CommandExecutor.ExecuteCommand($"{Prefix} spawn {count}");
        }

        // Spawn one bot with an explicit spec.
        public static void SpawnSpecific(BotSpawnSpec spec)
        {
            string cmd = $"{Prefix} spawnSpecific {spec.Faction} {spec.Class}";

            // Positional optional args: name, regtag, uniformId.
            if (!string.IsNullOrEmpty(spec.Name) || !string.IsNullOrEmpty(spec.RegTag) || spec.UniformId.HasValue)
                cmd += $" {Arg(spec.Name)}";

            if (!string.IsNullOrEmpty(spec.RegTag) || spec.UniformId.HasValue)
                cmd += $" {Arg(spec.RegTag)}";

            if (spec.UniformId.HasValue)
                cmd += $" {spec.UniformId.Value}";

            CommandExecutor.ExecuteCommand(cmd);
        }

        // Enable direct input control. Until this is set, the bot ignores inputAxis/inputRotation.
        public static void EnableInputControl(int playerId)
        {
            CommandExecutor.ExecuteCommand($"{Prefix} forceInputAxis true {playerId}");
            CommandExecutor.ExecuteCommand($"{Prefix} forceInputRotation true {playerId}");
        }

        public static void DisableInputControl(int playerId)
        {
            CommandExecutor.ExecuteCommand($"{Prefix} forceInputAxis false {playerId}");
            CommandExecutor.ExecuteCommand($"{Prefix} forceInputRotation false {playerId}");
        }

        // sideways/forwards each in [-1, 1]. Issued every tick per bot, so its result is not logged
        // (logResult: false) - otherwise it floods the debug log. Failures still surface.
        public static void SetInputAxis(int playerId, float sideways, float forwards)
        {
            CommandExecutor.ExecuteCommand($"{Prefix} inputAxis {Fmt(sideways)} {Fmt(forwards)} {playerId}", logResult: false);
        }

        // heading in degrees from North. Per-tick like inputAxis, so likewise not result-logged.
        public static void SetInputRotation(int playerId, float degrees)
        {
            CommandExecutor.ExecuteCommand($"{Prefix} inputRotation {Fmt(degrees)} {playerId}", logResult: false);
        }

        // NOT degrees: this is the engine's own pitch scale, 0 level, the same one the strike table is keyed in.
        public static void SetPitch(int playerId, float pitch)
        {
            CommandExecutor.ExecuteCommand($"{Prefix} pitch {Fmt(pitch)} {playerId}", logResult: false);
        }

        public static void SetRunning(int playerId, bool enable)
        {
            CommandExecutor.ExecuteCommand($"{Prefix} setRunning {Bool(enable)} {playerId}");
        }

        // Performs a Player Action. A held action is started and stopped by separate tokens, not re-sent.
        public static void PerformAction(int playerId, string action)
        {
            CommandExecutor.ExecuteCommand($"{Prefix} playerAction {action} {playerId}");
            MeleeDebug.NoteCommand(playerId, action);
        }

        // Actions that take a second argument, e.g. StartGestureAnimation PlayerGestureDancingFunny.
        public static void PerformAction(int playerId, string action, string argument)
        {
            CommandExecutor.ExecuteCommand($"{Prefix} playerAction {action} {argument} {playerId}");
            MeleeDebug.NoteCommand(playerId, action);
        }

        // Removes a bot from the world.
        public static void Despawn(int playerId)
        {
            CommandExecutor.ExecuteCommand($"serverAdmin kick {playerId}");
        }

        // Teleports a player or bot to a world position. 'teleport' is a general console command, not
        // carbonPlayers-prefixed. Used for summon and return-to-death placement.
        public static void Teleport(int playerId, Vector3 position)
        {
            CommandExecutor.ExecuteCommand($"teleport {playerId} {Fmt(position.x)},{Fmt(position.y)},{Fmt(position.z)}");
        }

        // A positional name or regtag arg, or the placeholder when it is absent.
        private static string Arg(string value) =>
            string.IsNullOrEmpty(value) ? EmptyArgPlaceholder : value.Replace(' ', NameSpaceChar);

        private const char NameSpaceChar = '\u2002'; // EN SPACE: renders as a space in-game, not an arg delimiter

        // Invariant culture so floats never serialize with a locale comma decimal separator.
        private static string Fmt(float value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(bool value) => value ? "true" : "false";
    }
}
