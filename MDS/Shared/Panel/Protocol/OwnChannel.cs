using System.Text;
using Holdfast.Modding;

// A server learns its own mod id from the echo of a store write, since no API names it.
// A write inside OnModLoaded raises no echo on the server (spike, 2026-11-17), so the owner calls Tick from its poll until the echo comes back.
namespace MDS.Shared.Panel.Protocol
{
    public sealed class OwnChannel
    {
        private const string HelloKey = "Panel.Self";

        private readonly IModNetworking _networking;
        private int _writes;

        public OwnChannel(IModNetworking networking)
        {
            _networking = networking;
        }

        public uint Id { get; private set; }

        public bool IsKnown => Id != 0;

        // Each write carries a new value, because a byte-identical value succeeds without replicating, so it would raise no echo either.
        public void Tick()
        {
            if (IsKnown) return;

            _writes++;
            _networking.SessionDataStore.SetValue(HelloKey, Encoding.UTF8.GetBytes(_writes.ToString()));
        }

        // Before any listener is registered only the mod's own channel is visible, so the first echo of the key names this mod.
        public bool TryLearn(uint modId, string key)
        {
            if (IsKnown || key != HelloKey) return false;

            Id = modId;
            return true;
        }
    }
}
