using MDS.Shared.Config;
using UnityEngine;

namespace MDS.Features.Arenas
{
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

        public bool Validate(string value) => ArenaParser.TryParseNumberedCorner(value, out _, out _);

        public void Apply(string value)
        {
            ArenaParser.TryParseNumberedCorner(value, out int number, out Vector2 position);
            _store.SetCorner(number, _corner, position);
        }
    }
}
