using System.Collections.Generic;
using Holdfast.Modding;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills
{
    // Survivors of the last drill would otherwise join the next one.
    // Only the box around the new drill's spots is cleared, so a spawn inside a big arena keeps working.
    public sealed class LeftoverClearing
    {
        // A step or two past the outermost spots, for a survivor who moved off their spot while fighting.
        private const float ZoneMargin = 3f;

        private readonly DrillKit _kit;
        private readonly ExitSpots _spots;
        private readonly Dictionary<FactionCountry, List<QuerySpawnSectionInfo>> _sectionsByFaction = new();

        public LeftoverClearing(DrillKit kit)
        {
            _kit = kit;
            _spots = new ExitSpots(kit.Random);
        }

        private static Rect ZoneOf(DrillRun run)
        {
            Rect footprint = run.Footprint;
            return Rect.MinMaxRect(footprint.xMin - ZoneMargin, footprint.yMin - ZoneMargin, footprint.xMax + ZoneMargin, footprint.yMax + ZoneMargin);
        }

        public void Clear(DrillRun run)
        {
            if (!run.PlacedAnyone) return;

            _sectionsByFaction.Clear();
            Rect zone = ZoneOf(run);
            var exits = new Dictionary<int, SpawnExit>();
            List<Leftover> leftovers = Leftovers(run, zone);
            foreach (Leftover leftover in leftovers)
            {
                if (TryFindExit(leftover, zone, out QuerySpawnSectionInfo section)) ExitFor(exits, section).Players.Add(leftover.Player);
                else _kit.Log.Warning($"{run.Drill}: {leftover.Player} stays, since no spawn of theirs lies outside the drill's area.");
            }

            int moved = 0;
            List<Vector2> taken = Standing(leftovers);
            foreach (SpawnExit exit in exits.Values) moved += MoveAround(exit, new ExitArea(Flat(exit.Position), zone, taken));
            if (exits.Count > 0) _kit.Log.Info($"{run.Drill}: {moved} leftover player(s) sent to {exits.Count} spawn(s).");
        }

        private static Vector2 Flat(Vector3 position) => new Vector2(position.x, position.z);

        private static SpawnExit ExitFor(Dictionary<int, SpawnExit> exits, QuerySpawnSectionInfo section)
        {
            if (!exits.TryGetValue(section.SectionId, out SpawnExit exit)) exits[section.SectionId] = exit = new SpawnExit(section.WorldPosition);
            return exit;
        }

        private List<Leftover> Leftovers(DrillRun run, Rect zone)
        {
            var leftovers = new List<Leftover>();
            foreach (IPlayer player in Field())
            {
                if (!player.IsAlive || run.Lineup.Includes(player) || !_kit.Queries.TryGetPosition(player.PlayerId, out Vector3 at)) continue;
                if (zone.Contains(Flat(at))) leftovers.Add(new Leftover(player, Flat(at)));
            }
            return leftovers;
        }

        // Their own spawn first, if their side may still use it, else the nearest one of their side outside the drill's area.
        private bool TryFindExit(Leftover leftover, Rect zone, out QuerySpawnSectionInfo exit)
        {
            SpawnState spawn = leftover.Player.Spawn;
            if (_kit.Queries.TryGetLandSpawnSectionFor(spawn.SpawnSectionId, spawn.Faction, out exit) && !zone.Contains(Flat(exit.WorldPosition))) return true;

            return TryFindNearestOutside(leftover, zone, out exit);
        }

        private bool TryFindNearestOutside(Leftover leftover, Rect zone, out QuerySpawnSectionInfo exit)
        {
            exit = default;
            float nearest = float.MaxValue;
            foreach (QuerySpawnSectionInfo section in SectionsFor(leftover.Player.Spawn.Faction))
            {
                Vector2 at = Flat(section.WorldPosition);
                float distance = Vector2.Distance(leftover.At, at);
                if (distance >= nearest || zone.Contains(at)) continue;

                nearest = distance;
                exit = section;
            }
            return nearest < float.MaxValue;
        }

        private List<QuerySpawnSectionInfo> SectionsFor(FactionCountry faction)
        {
            if (_sectionsByFaction.TryGetValue(faction, out List<QuerySpawnSectionInfo> sections)) return sections;

            sections = new List<QuerySpawnSectionInfo>();
            _kit.Queries.GetLandSpawnSectionsFor(faction, sections);
            _sectionsByFaction[faction] = sections;
            return sections;
        }

        // Everyone alive who stays where they are, so nobody cleared lands inside them.
        private List<Vector2> Standing(List<Leftover> leftovers)
        {
            var standing = new List<Vector2>();
            foreach (IPlayer player in Field())
            {
                if (player.IsAlive && !leftovers.Exists(l => l.Player == player) && _kit.Queries.TryGetPosition(player.PlayerId, out Vector3 at)) standing.Add(Flat(at));
            }
            return standing;
        }

        private List<IPlayer> Field()
        {
            var field = new List<IPlayer>(_kit.Players.Attacking);
            field.AddRange(_kit.Players.Defending);
            return field;
        }

        private int MoveAround(SpawnExit exit, ExitArea area)
        {
            List<Vector2> spots = _spots.Around(area, exit.Players.Count);
            int moved = 0;
            for (int i = 0; i < spots.Count; i++)
            {
                if (_kit.Teleporter.MoveNear(exit.Players[i].PlayerId, spots[i], exit.Position.y)) moved++;
            }
            return moved;
        }

        private readonly struct Leftover
        {
            public Leftover(IPlayer player, Vector2 at)
            {
                Player = player;
                At = at;
            }

            public IPlayer Player { get; }

            public Vector2 At { get; }
        }

        private sealed class SpawnExit
        {
            public SpawnExit(Vector3 position)
            {
                Position = position;
            }

            public Vector3 Position { get; }

            public List<IPlayer> Players { get; } = new();
        }
    }
}
