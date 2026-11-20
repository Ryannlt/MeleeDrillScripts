using System.Collections.Generic;
using System.Text;

// Every message goes on its sender's own channel: XUI's client speaks on XUI's, a module answers on its own.
namespace MDS.Shared.Panel.Protocol
{
    public readonly struct PanelRun
    {
        public PanelRun(string module, int seq, string command)
        {
            Module = module;
            Seq = seq;
            Command = command;
        }

        public string Module { get; }
        public int Seq { get; }
        public string Command { get; }
    }

    public readonly struct PanelReply
    {
        public PanelReply(int seq, bool ok, string text)
        {
            Seq = seq;
            Ok = ok;
            Text = text;
        }

        public int Seq { get; }
        public bool Ok { get; }
        public string Text { get; }
    }

    public static class PanelWire
    {
        public const int Version = 1;

        public const string Hello = "Panel.Hello";
        public const string Bye = "Panel.Bye";
        public const string Run = "Panel.Run";
        public const string Page = "Panel.Page";
        public const string Reply = "Panel.Reply";

        // A page over one message's room, in order: "index\ncount\ntext".
        public const string PagePart = "Panel.PagePart";

        // The game refuses a mod message over 64 KB (Holdfast.Modding.xml, IModNetworking).
        public const int MessageBytes = 64 * 1024;

        // A part's text in UTF-8 bytes, leaving room under MessageBytes for the "index\ncount\n" header and the message name.
        public const int PageBytes = 60000;

        // Over 3 MB of page, far past anything a panel needs.
        public const int MaxPageParts = 200;

        public const string ModulesKey = "Panel.Modules";
        public const string HostKey = "Panel.Host";
        public const string NoHost = "none";

        public const string Ok = "ok";
        public const string Refused = "refused";

        // A typed rc is short, and a cap keeps a hostile client from handing every module a huge string.
        public const int MaxRunBytes = 512;

        // Both texts are three lines: the module or sequence number first, the free text last, so the text may hold anything but a control character.
        private const int Parts = 3;
        private const int TextPart = 2;

        public static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text ?? string.Empty);

        public static string Text(byte[] data) => data == null ? string.Empty : Encoding.UTF8.GetString(data);

        // One Page message when it fits, otherwise PagePart messages that PageParts puts back together.
        public static List<(string Name, byte[] Data)> PageMessages(string json)
        {
            var messages = new List<(string Name, byte[] Data)>();
            byte[] whole = Bytes(json);
            if (whole.Length <= PageBytes) messages.Add((Page, whole));
            else AddParts(json, messages);
            return messages;
        }

        private static void AddParts(string json, List<(string Name, byte[] Data)> messages)
        {
            var chunks = new List<string>();
            for (int start = 0; start < json.Length;)
            {
                int length = CharsThatFit(json, start);
                chunks.Add(json.Substring(start, length));
                start += length;
            }
            for (int i = 0; i < chunks.Count; i++) messages.Add((PagePart, Bytes($"{i}\n{chunks.Count}\n{chunks[i]}")));
        }

        // A part never ends between the two halves of a surrogate pair.
        private static int CharsThatFit(string json, int start)
        {
            int bytes = 0, at = start;
            while (at < json.Length)
            {
                bool pair = char.IsHighSurrogate(json[at]) && at + 1 < json.Length;
                int size = pair ? Utf8PairBytes : Utf8Bytes(json[at]);
                if (bytes + size > PageBytes) break;

                bytes += size;
                at += pair ? PairChars : 1;
            }
            return at - start;
        }

        // UTF-8 widths: below U+0080 one byte, below U+0800 two, any other char three, a surrogate pair four.
        private const int PairChars = 2;
        private const int Utf8PairBytes = 4;
        private const int OneByteBelow = 0x80;
        private const int TwoBytesBelow = 0x800;
        private const int TwoBytes = 2;
        private const int ThreeBytes = 3;

        private static int Utf8Bytes(char c) => c < OneByteBelow ? 1 : c < TwoBytesBelow ? TwoBytes : ThreeBytes;

        public static byte[] WriteRun(string module, int seq, string command) => Bytes($"{module}\n{seq}\n{command}");

        // Client data is hostile: anything malformed, oversized or carrying control characters is dropped before any command sees it.
        public static bool TryReadRun(byte[] data, out PanelRun run)
        {
            run = default;
            if (data == null || data.Length == 0 || data.Length > MaxRunBytes) return false;

            string[] parts = Text(data).Split(new[] { '\n' }, Parts);
            if (parts.Length != Parts || !int.TryParse(parts[1], out int seq) || HasControl(parts[TextPart])) return false;

            run = new PanelRun(parts[0], seq, parts[TextPart].Trim());
            return run.Module.Length > 0 && run.Command.Length > 0;
        }

        public static byte[] WriteReply(int seq, bool ok, string text) => Bytes($"{seq}\n{(ok ? Ok : Refused)}\n{text}");

        public static bool TryReadReply(byte[] data, out PanelReply reply)
        {
            reply = default;
            string[] parts = Text(data).Split(new[] { '\n' }, Parts);
            if (parts.Length != Parts || !int.TryParse(parts[0], out int seq)) return false;

            reply = new PanelReply(seq, parts[1] == Ok, parts[TextPart]);
            return true;
        }

        private static bool HasControl(string text)
        {
            foreach (char c in text)
            {
                if (char.IsControl(c)) return true;
            }
            return false;
        }
    }
}
