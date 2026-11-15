using System.Collections.Generic;
using Holdfast.Modding;
using UnityEngine;

namespace MDS.Shared.Game
{
    // Without queries (on a client, in a unit test) it finds nothing.
    public sealed class PlayerQueries
    {
        private readonly IServerModQueries _queries;
        private readonly List<QuerySpawnSectionInfo> _sections = new();
        private readonly List<FactionCountry> _factions = new();

        public PlayerQueries(IServerModQueries queries)
        {
            _queries = queries;
        }

        public bool CanQuery => _queries != null;

        // False when the player has no body right now.
        public bool TryGetStatus(int playerId, out QueryServerPlayerStatus status)
        {
            status = default;
            return _queries != null && _queries.TryGetPlayerStatus(playerId, out status);
        }

        public bool TryGetPosition(int playerId, out Vector3 position)
        {
            bool found = TryGetStatus(playerId, out QueryServerPlayerStatus status);
            position = status.Position;
            return found;
        }

        // Ship sections sail away with their ship, so only land sections are listed.
        // Sections that name the faction win: one with no faction listed can be a spawn neither side holds this round.
        public void GetLandSpawnSectionsFor(FactionCountry faction, List<QuerySpawnSectionInfo> buffer)
        {
            buffer.Clear();
            if (_queries == null) return;

            _queries.GetAllSpawnSections(_sections);
            AddLandSections(faction, false, buffer);
            if (buffer.Count == 0) AddLandSections(faction, true, buffer);
        }

        // A ship section sails away with its ship, and one that lists factions must list this one.
        public bool TryGetLandSpawnSectionFor(int sectionId, FactionCountry faction, out QuerySpawnSectionInfo section)
        {
            section = default;
            if (_queries == null || !_queries.TryGetSpawnSection(sectionId, out section) || section.ParentShipId >= 0) return false;

            _queries.GetSpawnSectionAllowedFactions(sectionId, _factions);
            return _factions.Count == 0 || _factions.Contains(faction);
        }

        // Humans only: the game leaves bots out.
        public void GetConnectedHumans(List<int> buffer)
        {
            buffer.Clear();
            _queries?.GetConnectedPlayers(buffer);
        }

        // No listed faction means anyone may spawn there.
        private void AddLandSections(FactionCountry faction, bool unrestricted, List<QuerySpawnSectionInfo> buffer)
        {
            foreach (QuerySpawnSectionInfo section in _sections)
            {
                if (section.ParentShipId >= 0) continue;

                _queries.GetSpawnSectionAllowedFactions(section.SectionId, _factions);
                if (unrestricted ? _factions.Count == 0 : _factions.Contains(faction)) buffer.Add(section);
            }
        }
    }
}
