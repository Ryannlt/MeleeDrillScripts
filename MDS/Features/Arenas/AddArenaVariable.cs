using MDS.Shared.Config;

namespace MDS.Features.Arenas
{
    // MDS:AddArena:(x,z),(x,z) lists one more arena after the first.
    public sealed class AddArenaVariable : IConfigVariable
    {
        private readonly ArenaStore _store;

        public AddArenaVariable(ArenaStore store)
        {
            _store = store;
        }

        public string Key => "AddArena";

        public bool Validate(string value) => ArenaParser.TryParseArena(value, out _);

        public void Apply(string value)
        {
            ArenaParser.TryParseArena(value, out Arena arena);
            _store.Add(arena);
        }
    }
}
