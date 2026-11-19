using System;
using System.Collections;
using System.Collections.Generic;
using Holdfast.Modding;
using MDS.Shared.Game;
using MDS.Shared.Logging;
using MDS.Shared.Panel.Protocol;
using MDS.Shared.Players;

namespace MDS.Features.Panel
{
    // Without XUI it still writes Panel.Host as none, so MDS's clients know to show their own panel.
    public sealed class HostLink : IDisposable
    {
        private const float PollSeconds = 1f;
        private const float HostWaitSeconds = 30f;

        private readonly IModNetworking _networking;
        private readonly Replies _replies;
        private readonly PageSource _pages;
        private readonly PanelDoor _door;
        private readonly PlayerRegistry _players;
        private readonly IClock _clock;
        private readonly ModLog _log;
        private readonly OwnChannel _own;
        private readonly Rendezvous _rendezvous = new();
        private readonly HashSet<int> _viewers = new();
        private uint _hostId;
        private bool _announced;
        private bool _verdictWritten;

        public HostLink(PageSource pages, PanelDoor door, ModContext context)
        {
            _networking = context.Networking;
            _replies = context.Replies;
            _players = context.Players;
            _clock = context.Clock;
            _log = context.Log.For("Panel");
            _pages = pages;
            _door = door;
            _own = new OwnChannel(_networking);
            _players.IdFreed += OnIdFreed;
            context.Routines.Start(Poll(), "XUI link");
        }

        public void Dispose()
        {
            _players.IdFreed -= OnIdFreed;
            _rendezvous.Withdraw();
        }

        public void OnStoreChanged(uint modId, string key)
        {
            if (_own.TryLearn(modId, key)) _log.Info($"MDS's channel is {modId}.");
        }

        // Before XUI is found only MDS's own channel is heard.
        public void OnServerModMessage(int playerId, uint modId, string messageName, byte[] data)
        {
            if (modId != _hostId || _hostId == 0) return;

            if (messageName == PanelWire.Hello) SendPages(playerId);
            else if (messageName == PanelWire.Bye) _viewers.Remove(playerId);
            else if (messageName == PanelWire.Run) Run(playerId, data);
        }

        private IEnumerator Poll()
        {
            while (true)
            {
                _own.Tick();
                if (_own.IsKnown && !_announced) Announce();
                if (_announced && _hostId == 0) LookForHost();
                yield return Routines.Wait(PollSeconds);
            }
        }

        private void Announce()
        {
            _announced = true;
            _rendezvous.Announce(Rendezvous.ModuleEntry(PageSource.Module, _own.Id));
        }

        // The listener comes after MDS's own id is known, so the first store echo can't come from XUI's channel.
        private void LookForHost()
        {
            if (_rendezvous.TryFindHost(out uint hostId))
            {
                _hostId = hostId;
                _networking.RegisterModMessageListener(hostId);
                _log.Info($"Joined XUI's panel on channel {hostId}.");
                WriteHost(hostId.ToString());
            }
            else if (!_verdictWritten && _clock.GameSeconds >= HostWaitSeconds)
            {
                WriteHost(PanelWire.NoHost);
            }
        }

        private void WriteHost(string value)
        {
            _verdictWritten = true;
            _networking.SessionDataStore.SetValue(PanelWire.HostKey, PanelWire.Bytes(value));
        }

        private void SendPages(int playerId)
        {
            _viewers.Add(playerId);
            foreach (PanelPage page in _pages.For(playerId)) Send(playerId, page);
        }

        // A send the game refuses is logged, since nothing else would show it.
        private void Send(int playerId, PanelPage page)
        {
            foreach ((string name, byte[] data) in PanelWire.PageMessages(PanelCodec.Write(page)))
            {
                if (_replies.SendToClient(playerId, name, data)) continue;

                _log.Warning($"The {page.Page} page could not be sent to player {playerId} ({data.Length} bytes in one message).");
                return;
            }
        }

        private void Run(int playerId, byte[] data)
        {
            if (!PanelWire.TryReadRun(data, out PanelRun run) || run.Module != PageSource.Module) return;

            bool ok = _door.Run(playerId, run.Command, out string reply);
            _replies.SendToClient(playerId, PanelWire.Reply, PanelWire.WriteReply(run.Seq, ok, reply));
            if (_viewers.Contains(playerId)) SendPages(playerId);
        }

        private void OnIdFreed(int playerId) => _viewers.Remove(playerId);
    }
}
