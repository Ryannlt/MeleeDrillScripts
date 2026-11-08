using System.Text;
using Holdfast.Modding;

// Replies to the player who ran a command or changed a config value. A reply goes out twice: as a private message
// they see on screen, and as a mod message their client logs, which lands in the F2 client log. No HBox API can
// write to the F1 console yet, so dropping the private message later is a one-line change here.

namespace MDS.Core
{
    public static class Feedback
    {
        private const string ReplyMessage = "MDS.Reply";

        private static IModNetworking _networking;

        public static void Initialize(IModNetworking networking)
        {
            _networking = networking;
        }

        public static void Reply(int playerId, string text)
        {
            CommandExecutor.ExecuteCommand($"serverAdmin privateMessage {playerId} {text}");
            LogOnClient(playerId, text);
        }

        // A line for the player's client log only, for things not worth interrupting them on screen.
        public static void LogOnClient(int playerId, string text)
        {
            if (_networking == null) return;

            _networking.SendModMessageToPlayer(playerId, ReplyMessage, Encoding.UTF8.GetBytes(text));
        }

        public static void OnClientModMessage(string messageName, byte[] data)
        {
            if (messageName != ReplyMessage || data == null) return;

            Logger.Log(Encoding.UTF8.GetString(data), LogLevel.INFO);
        }
    }
}
