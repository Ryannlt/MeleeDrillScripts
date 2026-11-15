using System;
using System.Collections.Generic;
using System.Text;
using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Drills.Preview
{
    // "A:x,z;x,z|D:x,z;x,z|R:x1,z1,x2,z2", with invariant numbers since the client may use another language.
    public static class SpotViewMessage
    {
        public const string Name = "MDS.SpotView";

        // Sent instead of an empty payload when there is nothing to draw.
        private const string Nothing = "none";
        private const char SectionSeparator = '|';
        private const char SpotSeparator = ';';
        private const char KeySeparator = ':';
        private const string AttackersKey = "A";
        private const string DefendersKey = "D";
        private const string AreaKey = "R";
        private const int Pair = 2;
        private const int Corners = 4;
        private const int XMin = 0;
        private const int YMin = 1;
        private const int XMax = 2;
        private const int YMax = 3;

        public static byte[] Encode(DrillPreview preview)
        {
            var sections = new List<string> { $"{AttackersKey}{KeySeparator}{SpotsText(preview.Attackers)}", $"{DefendersKey}{KeySeparator}{SpotsText(preview.Defenders)}" };
            Rect area = preview.Area;
            if (area.width > 0f) sections.Add($"{AreaKey}{KeySeparator}{Numbers.Coordinate(area.xMin)},{Numbers.Coordinate(area.yMin)},{Numbers.Coordinate(area.xMax)},{Numbers.Coordinate(area.yMax)}");
            return Encoding.UTF8.GetBytes(string.Join(SectionSeparator.ToString(), sections));
        }

        public static byte[] EncodeNothing() => Encoding.UTF8.GetBytes(Nothing);

        // False for "nothing to draw"; a malformed spot is skipped rather than failing the rest.
        public static bool TryDecode(byte[] data, out DrillPreview preview)
        {
            preview = new DrillPreview();
            string text = Encoding.UTF8.GetString(data ?? Array.Empty<byte>());
            if (text == Nothing) return false;

            foreach (string section in text.Split(SectionSeparator)) ReadSection(section, preview);
            return true;
        }

        private static string SpotsText(List<Vector2> spots)
        {
            var parts = new List<string>(spots.Count);
            foreach (Vector2 spot in spots) parts.Add($"{Numbers.Coordinate(spot.x)},{Numbers.Coordinate(spot.y)}");
            return string.Join(SpotSeparator.ToString(), parts);
        }

        private static void ReadSection(string section, DrillPreview preview)
        {
            int split = section.IndexOf(KeySeparator);
            if (split < 0) return;

            string key = section.Substring(0, split);
            string value = section.Substring(split + 1);
            if (key == AttackersKey) ReadSpots(value, preview.Attackers);
            else if (key == DefendersKey) ReadSpots(value, preview.Defenders);
            else if (key == AreaKey && TryReadArea(value, out Rect area)) preview.Area = area;
        }

        private static void ReadSpots(string text, List<Vector2> spots)
        {
            foreach (string part in text.Split(SpotSeparator))
            {
                string[] numbers = part.Split(',');
                if (numbers.Length == Pair && Numbers.TryParse(numbers[0], out float x) && Numbers.TryParse(numbers[1], out float z)) spots.Add(new Vector2(x, z));
            }
        }

        private static bool TryReadArea(string text, out Rect area)
        {
            area = Rect.zero;
            string[] numbers = text.Split(',');
            if (numbers.Length != Corners) return false;

            var values = new float[Corners];
            for (int i = 0; i < Corners; i++)
            {
                if (!Numbers.TryParse(numbers[i], out values[i])) return false;
            }
            area = Rect.MinMaxRect(values[XMin], values[YMin], values[XMax], values[YMax]);
            return true;
        }
    }
}
