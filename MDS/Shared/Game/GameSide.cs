using Holdfast.Modding;

namespace MDS.Shared.Game
{
    public enum GameSide
    {
        Unknown,
        Server,
        Client
    }

    public static class GameSides
    {
        // The side is asked for rather than announced: a server is handed server queries, a client client ones.
        public static GameSide Detect(ModHandles handles)
        {
            QueryLocalIdentity identity = default;
            bool known = handles.ServerQueries != null
                ? handles.ServerQueries.TryGetLocalIdentity(out identity)
                : handles.ClientQueries != null && handles.ClientQueries.TryGetLocalIdentity(out identity);
            if (!known) return GameSide.Unknown;

            return identity.IsServer ? GameSide.Server : GameSide.Client;
        }
    }
}
