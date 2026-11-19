using System;
using System.Collections.Generic;
using System.Text;
using Holdfast.Modding;
using MDS.Shared.Logging;

namespace MDS.Shared.Game
{
    // No HBox API writes to the F1 console yet, so a reply goes out as a private message on screen and as a mod message for the F2 client log.
    public sealed class Replies
    {
        private const string ReplyMessage = "MDS.Reply";

        private readonly GameConsole _console;
        private readonly IModNetworking _networking;
        private readonly ModLog _log;
        private readonly List<string> _captured = new();
        private bool _capturing;
        private int _capturedPlayer;

        public Replies(GameConsole console, IModNetworking networking, ModLog log)
        {
            _console = console;
            _networking = networking;
            _log = log;
        }

        public void Send(int playerId, string text)
        {
            if (_capturing && playerId == _capturedPlayer)
            {
                _captured.Add(text);
                return;
            }

            _console.Execute($"serverAdmin privateMessage {playerId} {text}");
            LogOnClient(playerId, text);
        }

        // While a panel request runs, what the server would tell that player comes back as the request's answer instead of a private message.
        // Replies to anyone else, and anything said later (countdowns, program steps), go out as usual.
        public string Capture(int playerId, Action run)
        {
            _capturing = true;
            _capturedPlayer = playerId;
            _captured.Clear();
            try
            {
                run();
            }
            finally
            {
                _capturing = false;
            }
            return string.Join(" ", _captured);
        }

        // For things not worth interrupting the player on screen.
        public void LogOnClient(int playerId, string text)
        {
            _networking?.SendModMessageToPlayer(playerId, ReplyMessage, Encoding.UTF8.GetBytes(text));
        }

        // Data for this mod's client half, which takes it by name through the ClientMessageRouter.
        public bool SendToClient(int playerId, string messageName, byte[] data) => _networking?.SendModMessageToPlayer(playerId, messageName, data) ?? false;

        public void OnClientMessage(string messageName, byte[] data)
        {
            if (messageName == ReplyMessage && data != null) _log.Info(Encoding.UTF8.GetString(data));
        }
    }
}
