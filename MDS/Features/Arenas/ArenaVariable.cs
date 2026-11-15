using MDS.Shared.Config;

namespace MDS.Features.Arenas
{
    // MDS:Arena:(x,z),(x,z) sets the drill arena for this map.
    public sealed class ArenaVariable : IConfigVariable
    {
        private readonly ArenaStore _store;

        public ArenaVariable(ArenaStore store)
        {
            _store = store;
        }

        public string Key => "Arena";

        public bool Validate(string value) => ArenaParser.TryParseArena(value, out _);

        public void Apply(string value)
        {
            ArenaParser.TryParseArena(value, out Arena arena);
            _store.Set(arena);
        }
    }
}
