using System.Collections.Generic;
using System.Text;

namespace MDS.Shared.Panel.Protocol
{
    // Puts a page sent in parts back together.
    // Each module's parts come in order, since the game delivers a connection's messages reliably and in order, so a part 0 always starts a new page.
    public sealed class PageParts
    {
        private const int Fields = 3;
        private const int TextField = 2;

        private readonly Dictionary<uint, StringBuilder> _open = new();
        private readonly Dictionary<uint, int> _next = new();

        public bool TryAdd(uint modId, byte[] data, out string json)
        {
            json = null;
            string[] fields = PanelWire.Text(data).Split(new[] { '\n' }, Fields);
            if (fields.Length != Fields || !int.TryParse(fields[0], out int index) || !int.TryParse(fields[1], out int count) || count > PanelWire.MaxPageParts) return false;

            if (index == 0) (_open[modId], _next[modId]) = (new StringBuilder(), 0);
            if (!_open.TryGetValue(modId, out StringBuilder text) || _next[modId] != index) return false;

            text.Append(fields[TextField]);
            _next[modId] = index + 1;
            if (index + 1 < count) return false;

            json = text.ToString();
            _open.Remove(modId);
            return true;
        }
    }
}
