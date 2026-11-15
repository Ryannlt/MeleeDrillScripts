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

        // For things not worth interrupting the player on screen.
        public void LogOnClient(int playerId, string text)
        {
            _networking?.SendModMessageToPlayer(playerId, ReplyMessage, Encoding.UTF8.GetBytes(text));
        }

        // Data for this mod's client half, which takes it by name through the ClientMessageRouter.
        public void SendToClient(int playerId, string messageName, byte[] data) => _networking?.SendModMessageToPlayer(playerId, messageName, data);

        public void OnClientMessage(string messageName, byte[] data)
        {
            if (messageName == ReplyMessage && data != null) _log.Info(Encoding.UTF8.GetString(data));
        }
    }
}
