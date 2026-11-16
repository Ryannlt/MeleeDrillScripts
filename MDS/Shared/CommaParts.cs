using System.Collections.Generic;

namespace MDS.Shared
{
    // Config values are comma separated, and a comma inside brackets belongs to a list or a position, so (x,z) stays one part.
    public static class CommaParts
    {
        public static string[] Split(string text)
        {
            string line = text ?? string.Empty;
            var parts = new List<string>();
            int depth = 0;
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                depth += line[i] == '(' ? 1 : line[i] == ')' ? -1 : 0;
                if (line[i] != ',' || depth != 0) continue;

                Add(parts, line.Substring(start, i - start));
                start = i + 1;
            }
            Add(parts, line.Substring(start));
            return parts.ToArray();
        }

        private static void Add(List<string> parts, string part)
        {
            if (part.Trim().Length > 0) parts.Add(part.Trim());
        }
    }
}
