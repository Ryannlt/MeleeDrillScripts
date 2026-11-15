using System.Collections.Generic;
using MDS.Shared;

namespace MDS.Features.Drills
{
    // One wording for every drill that shrinks to fit its arena, so the replies read alike.
    public static class FitNote
    {
        public static string Of(int arena, IReadOnlyList<string> changes) => changes.Count > 0 ? $"Shrunk to fit arena {arena}: {string.Join(", ", changes)}." : string.Empty;

        public static string Change(string option, float asked, float used) => $"{option} {Numbers.Format(used)} instead of {Numbers.Format(asked)}";
    }
}
