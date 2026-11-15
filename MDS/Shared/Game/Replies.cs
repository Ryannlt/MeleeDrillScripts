using System.Text;
using Holdfast.Modding;
using MDS.Shared.Logging;

namespace MDS.Shared.Game
{
    // Answers the player who ran a command: a private message on screen, plus a mod message that lands in their F2
    // client log. No HBox API writes to the F1 console yet.
    public sealed class Replies
    {
        private const string ReplyMessage = "MDS.Reply";

        private readonly GameConsole _console;
        private readonly IModNetworking _networking;
        private readonly ModLog _log;

        public Replies(GameConsole console, IModNetworking networking, ModLog log)
        {
            _console = console;
            _networking = networking;
            _log = log;
        }

        public void Send(int playerId, string text)
        {
            _console.Execute($"serverAdmin privateMessage {playerId} {text}");
            LogOnClient(playerId, text);
        }

        // A line for the player's client log only, for things not worth interrupting them on screen.
        public void LogOnClient(int playerId, string text)
        {
            _networking?.SendModMessageToPlayer(playerId, ReplyMessage, Encoding.UTF8.GetBytes(text));
        }

        public void OnClientMessage(string messageName, byte[] data)
        {
            if (messageName == ReplyMessage && data != null) _log.Info(Encoding.UTF8.GetString(data));
        }
    }
}
