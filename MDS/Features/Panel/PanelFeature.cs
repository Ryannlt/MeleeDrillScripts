using System;

namespace MDS.Features.Panel
{
    public sealed class PanelFeature : IDisposable
    {
        private readonly HostLink _link;

        public PanelFeature(ModContext context, PanelLists lists)
        {
            if (!context.IsServer) return;

            Pages = new PageSource(lists, context.Admins, new SettingValues(context.Settings, context.Replies));
            Door = new PanelDoor(context.Commands, context.Replies);
            _link = new HostLink(Pages, Door, context);
        }

        // Null on a client.
        public PageSource Pages { get; }

        public PanelDoor Door { get; }

        public void Dispose() => _link?.Dispose();

        public void OnServerModMessage(int playerId, uint modId, string messageName, byte[] data) => _link?.OnServerModMessage(playerId, modId, messageName, data);

        public void OnServerStoreChanged(uint modId, string key) => _link?.OnStoreChanged(modId, key);
    }
}
