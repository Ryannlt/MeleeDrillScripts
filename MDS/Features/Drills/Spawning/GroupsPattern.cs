using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    // Small mixed fights: both teams dealt into groups of 2 to 4, each group close together and the groups spread over the area.
    // Spacing is the least distance between players of different groups.
    public sealed class GroupsPattern : ISpawnPattern
    {
        private const int SmallestGroup = 2;
        private const int LargestGroup = 4;

        // A group stands in a box 4 m across, room for four players 1.5 m apart.
        private const float GroupReach = 2f;
        private const float MemberSpacing = 1.5f;
        private const float Half = 0.5f;

        // A member can stand in a corner of the group's box, this far from its center.
        // Two centers that far from the corners plus the spacing apart keep any two groups the spacing apart.
        private const float CornerReach = GroupReach * 1.41421356f;
        private const float CenterClearance = CornerReach + CornerReach;

        private readonly ISpotGenerator _even;
        private readonly ISpotGenerator _organic;
        private readonly System.Random _random;

        public GroupsPattern(ISpotGenerator even, ISpotGenerator organic, System.Random random)
        {
            _even = even;
            _organic = organic;
            _random = random;
        }

        public SpawnPlan Plan(Lineup field, SpawnRequest request)
        {
            List<IPlayer> players = Interleaved(field);
            List<int> sizes = GroupSizes(players.Count);
            List<Vector2> centers = _even.Generate(SpotArea.Whole(Shrunk(request.Area, GroupReach)), request.Spacing + CenterClearance, sizes.Count);

            var plan = new SpawnPlan();
            int next = 0;
            for (int group = 0; group < sizes.Count; group++)
            {
                List<IPlayer> members = players.GetRange(next, sizes[group]);
                next += sizes[group];
                plan.Add(members, group < centers.Count ? MemberSpots(centers[group], members.Count, request.Area) : new List<Vector2>());
            }
            return plan;
        }

        // An area too small to shrink becomes its middle point, so it still holds one group.
        private static Rect Shrunk(Rect area, float by)
        {
            float x = area.width > by + by ? by : area.width * Half;
            float y = area.height > by + by ? by : area.height * Half;
            return Rect.MinMaxRect(area.xMin + x, area.yMin + y, area.xMax - x, area.yMax - y);
        }

        // Organic growth keeps a group in one tight pocket, inside its box cut to the area.
        // The even spread is the fallback when the pocket can't fit everyone.
        private List<Vector2> MemberSpots(Vector2 center, int count, Rect area)
        {
            var box = SpotArea.Whole(Rect.MinMaxRect(
                Mathf.Max(center.x - GroupReach, area.xMin), Mathf.Max(center.y - GroupReach, area.yMin),
                Mathf.Min(center.x + GroupReach, area.xMax), Mathf.Min(center.y + GroupReach, area.yMax)));
            List<Vector2> spots = _organic.Generate(box, MemberSpacing, count);
            return spots.Count >= count ? spots : _even.Generate(box, MemberSpacing, count);
        }

        // Attackers and defenders take turns down the list, so every group cut from it has both teams while both last.
        private List<IPlayer> Interleaved(Lineup field)
        {
            List<IPlayer> attackers = PickRules.Shuffled(field.Attackers, _random);
            List<IPlayer> defenders = PickRules.Shuffled(field.Defenders, _random);
            var players = new List<IPlayer>(attackers.Count + defenders.Count);
            for (int i = 0; i < Mathf.Max(attackers.Count, defenders.Count); i++)
            {
                if (i < attackers.Count) players.Add(attackers[i]);
                if (i < defenders.Count) players.Add(defenders[i]);
            }
            return players;
        }

        // The last group never ends up a lone player.
        private List<int> GroupSizes(int players)
        {
            var sizes = new List<int>();
            int left = players;
            while (left > LargestGroup)
            {
                int size = _random.Next(SmallestGroup, LargestGroup + 1);
                if (left - size < SmallestGroup) size = left - SmallestGroup;
                sizes.Add(size);
                left -= size;
            }
            if (left > 0) sizes.Add(left);
            return sizes;
        }
    }
}
