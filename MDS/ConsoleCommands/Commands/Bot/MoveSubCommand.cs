using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using MDS.Core;
using MDS.Events;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc bot move <bots> <behavior> [args] - drives the Manual test AI's steering behaviours.
    public class MoveSubCommand : IBotSubCommand
    {
        public BotCommandEnum SubCommandName => BotCommandEnum.Move;

        // Validate has no caller id; 'me' is structurally valid here and resolved to the real caller in Execute.
        public bool Validate(string[] args, out string errorMessage) =>
            TryParseOrder(args, callerId: 0, out _, out errorMessage);

        public void Execute(int playerId, string[] args)
        {
            if (!TryParseOrder(args, playerId, out MoveOrder order, out string error))
            {
                Feedback.Reply(playerId, error);
                return;
            }

            bool success = EventDispatcher.Trigger(EventEnum.SetBotMoveOrder,
                new object[] { args[0], order }, out string eventError);

            if (!success)
            {
                Logger.Log($"Move failed: {eventError}", LogLevel.WARNING);
                Feedback.Reply(playerId, eventError);
                return;
            }

            Feedback.Reply(playerId, $"Move order '{order.Kind}' sent to Manual-AI bots matching '{args[0]}'. (Bots on other AIs are unaffected.)");
        }

        private const string Usage =
            "Usage: rc bot move <bots> <behavior>. Behaviors: seek/arrive/flee/pursue/evade <dest> [facing <dest>] [flags], " +
            "wander [flags], facepoint <dest>, face <deg>, stop. dest = 'x z' | playerId | me. flags = separate | avoid | dodge.";

        private static bool TryParseOrder(string[] args, int callerId, out MoveOrder order, out string error)
        {
            order = default;
            error = string.Empty;

            if (args.Length < 2) { error = Usage; return false; }

            if (!BotTargetSelector.IsValidToken(args[0]))
            {
                error = $"Invalid target '{args[0]}'. Use a playerId, all, attacking, defending, or a faction name.";
                return false;
            }

            string behavior = args[1].ToLowerInvariant();
            switch (behavior)
            {
                case "stop":
                    if (args.Length != 2) { error = "Usage: rc bot move <bots> stop"; return false; }
                    order = MoveOrder.Stop();
                    return true;

                case "wander":
                {
                    string[] tail = args[2..];
                    bool wSeparate = StripFlag(ref tail, "separate");
                    bool wAvoid = StripFlag(ref tail, "avoid");
                    bool wDodge = StripFlag(ref tail, "dodge");
                    if (tail.Length != 0) { error = "Usage: rc bot move <bots> wander [separate] [avoid] [dodge]"; return false; }
                    order = MoveOrder.Wander();
                    order.Separate = wSeparate;
                    order.Avoid = wAvoid;
                    order.Dodge = wDodge;
                    return true;
                }

                case "face":
                    if (args.Length != 3 || !TryFloat(args[2], out float deg))
                    {
                        error = "Usage: rc bot move <bots> face <deg>";
                        return false;
                    }
                    order = MoveOrder.Face(deg);
                    return true;

                case "facepoint":
                    if (!TryParseTargetTokens(args[2..], callerId, out MoveTarget facePoint, out error))
                        return false;
                    order = MoveOrder.FacePoint(facePoint);
                    return true;

                case "seek":
                case "arrive":
                case "flee":
                case "pursue":
                case "evade":
                {
                    string[] tail = args[2..];
                    bool separate = StripFlag(ref tail, "separate");
                    bool avoid = StripFlag(ref tail, "avoid");
                    bool dodge = StripFlag(ref tail, "dodge");
                    string[] destTokens = tail;
                    MoveTarget? facing = null;

                    int fi = IndexOfFacing(tail);
                    if (fi >= 0)
                    {
                        destTokens = tail[..fi];
                        if (!TryParseTargetTokens(tail[(fi + 1)..], callerId, out MoveTarget f, out error))
                            return false;
                        facing = f;
                    }

                    if (!TryParseTargetTokens(destTokens, callerId, out MoveTarget dest, out error))
                        return false;

                    order = behavior switch
                    {
                        "seek" => MoveOrder.Seek(dest),
                        "arrive" => MoveOrder.Arrive(dest),
                        "flee" => MoveOrder.Flee(dest),
                        "pursue" => MoveOrder.Pursue(dest),
                        _ => MoveOrder.Evade(dest),
                    };
                    order.FaceTarget = facing;
                    order.Separate = separate;
                    order.Avoid = avoid;
                    order.Dodge = dodge;
                    return true;
                }

                default:
                    error = $"Unknown behavior '{args[1]}'. Valid: seek, arrive, flee, pursue, evade, face, facepoint, wander, stop.";
                    return false;
            }
        }

        // Parses a destination token slice: "x z" (two numbers) | "<playerId>" (one int) | "me" (caller).
        private static bool TryParseTargetTokens(string[] tokens, int callerId, out MoveTarget target, out string error)
        {
            target = default;
            error = string.Empty;

            if (tokens.Length == 1)
            {
                string tok = tokens[0];
                if (tok.Equals("me", System.StringComparison.OrdinalIgnoreCase))
                {
                    target = MoveTarget.Player(callerId);
                    return true;
                }
                if (int.TryParse(tok, out int id))
                {
                    target = MoveTarget.Player(id);
                    return true;
                }
                error = "Destination must be 'x z', a playerId, or 'me'.";
                return false;
            }

            if (tokens.Length == 2)
            {
                if (!TryFloat(tokens[0], out float x) || !TryFloat(tokens[1], out float z))
                {
                    error = "Destination 'x z' must be two numbers.";
                    return false;
                }
                target = MoveTarget.At(new Vector2(x, z));
                return true;
            }

            error = Usage;
            return false;
        }

        // Index of the "facing" keyword in the token slice, or -1 if absent.
        private static int IndexOfFacing(string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
                if (tokens[i].Equals("facing", System.StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        // Removes the first occurrence of a bare flag keyword (case-insensitive) from the token slice,
        // returning whether it was present. Lets 'separate' appear anywhere in the tail.
        private static bool StripFlag(ref string[] tokens, string flag)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].Equals(flag, System.StringComparison.OrdinalIgnoreCase))
                {
                    var remaining = new List<string>(tokens);
                    remaining.RemoveAt(i);
                    tokens = remaining.ToArray();
                    return true;
                }
            }
            return false;
        }

        private static bool TryFloat(string s, out float value) =>
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
