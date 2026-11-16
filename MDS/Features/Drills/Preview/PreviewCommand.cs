using System;
using System.Collections.Generic;
using MDS.Shared.Commands;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Preview
{
    public sealed class PreviewCommand : IConsoleCommand
    {
        public const string Usage = "Usage: rc preview <xvx|groupfight|openmelee|popcorn|customdrill> [its options], or rc preview off";

        private const string Off = "off";

        private readonly IReadOnlyList<IDrill> _drills;
        private readonly Replies _replies;

        public PreviewCommand(IReadOnlyList<IDrill> drills, Replies replies)
        {
            _drills = drills;
            _replies = replies;
        }

        public string Name => "preview";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (IsOff(parameters)) return true;
            if (!TryFind(parameters, out IDrill drill))
            {
                errorMessage = Usage;
                return false;
            }

            return drill.Syntax.TryParse(parameters[1..], out DrillOptions options, out errorMessage) && drill.Validate(options, out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            if (IsOff(parameters))
            {
                Clear(playerId, "Spawn preview cleared.");
                return;
            }

            TryFind(parameters, out IDrill drill);
            drill.Syntax.TryParse(parameters[1..], out DrillOptions options, out _);
            if (!drill.TryPreview(options, out DrillPreview preview, out string error))
            {
                Clear(playerId, error);
                return;
            }

            _replies.SendToClient(playerId, SpotViewMessage.Name, SpotViewMessage.Encode(preview));
            _replies.Send(playerId, Describe(drill, preview));
        }

        private static bool IsOff(string[] parameters) => parameters.Length == 1 && parameters[0].Equals(Off, StringComparison.OrdinalIgnoreCase);

        private static string Describe(IDrill drill, DrillPreview preview)
        {
            string sample = preview.IsSample ? " This is one random draw: the drill draws again when it runs." : string.Empty;
            string detail = preview.Detail.Length > 0 ? $" ({preview.Detail})" : string.Empty;
            string note = preview.Note.Length > 0 ? $" {preview.Note}" : string.Empty;
            return $"Previewing {drill.Name}{detail} for you only: {preview.Attackers.Count} attacker spot(s) in yellow, {preview.Defenders.Count} defender spot(s) in cyan.{note}{sample}";
        }

        private void Clear(int playerId, string reply)
        {
            _replies.SendToClient(playerId, SpotViewMessage.Name, SpotViewMessage.EncodeNothing());
            _replies.Send(playerId, reply);
        }

        private bool TryFind(string[] parameters, out IDrill drill)
        {
            drill = null;
            if (parameters.Length == 0) return false;

            foreach (IDrill candidate in _drills)
            {
                if (candidate.Name.Equals(parameters[0], StringComparison.OrdinalIgnoreCase)) drill = candidate;
            }
            return drill != null;
        }
    }
}
