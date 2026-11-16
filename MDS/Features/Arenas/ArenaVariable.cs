using MDS.Shared.Config;

namespace MDS.Features.Arenas
{
    public sealed class ArenaVariable : IConfigVariable
    {
        private readonly ArenaStore _store;

        public ArenaVariable(ArenaStore store)
        {
            _store = store;
        }

        public string Key => "Arena";

        public bool Validate(string value, out string reason)
        {
            reason = string.Empty;
            return ArenaParser.TryParseArena(value, out _);
        }

        public void Apply(string value)
        {
            ArenaParser.TryParseArena(value, out Arena arena);
            _store.Set(arena);
        }
    }
}
