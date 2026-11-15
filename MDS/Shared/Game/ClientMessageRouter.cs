using System;
using System.Collections.Generic;

namespace MDS.Shared.Game
{
    // The host can't know the features, so each client feature registers the messages it takes by name.
    public sealed class ClientMessageRouter
    {
        private readonly Dictionary<string, Action<byte[]>> _handlers = new();

        public void Register(string messageName, Action<byte[]> handler) => _handlers[messageName] = handler;

        public void Handle(string messageName, byte[] data)
        {
            if (data != null && _handlers.TryGetValue(messageName, out Action<byte[]> handler)) handler(data);
        }
    }
}
