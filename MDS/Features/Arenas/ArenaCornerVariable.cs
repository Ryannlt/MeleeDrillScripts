using MDS.Shared.Config;
using UnityEngine;

namespace MDS.Features.Arenas
{
    // MDS:ArenaCorner1:x,z and MDS:ArenaCorner2:x,z set one corner of the drill arena.
    public sealed class ArenaCornerVariable : IConfigVariable
    {
        private readonly ArenaCorner _corner;
        private readonly ArenaStore _store;

        public ArenaCornerVariable(ArenaCorner corner, ArenaStore store)
        {
            _corner = corner;
            _store = store;
        }

        public string Key => $"Arena{_corner}";

        public bool Validate(string value) => ArenaParser.TryParseCorner(value, out _);

        public void Apply(string value)
        {
            ArenaParser.TryParseCorner(value, out Vector2 position);
            _store.SetCorner(_corner, position);
        }
    }
}
