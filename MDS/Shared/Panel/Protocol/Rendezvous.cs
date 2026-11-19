using System;
using UnityEngine;
using Object = UnityEngine.Object;

// No API lists the loaded mods or their ids, so the server halves meet under one named object in the server's scene.
// Entries read "host <id> v<version>" or "module <key> <id> v<version>"; the id is always the next to last word.
namespace MDS.Shared.Panel.Protocol
{
    public sealed class Rendezvous
    {
        public const string RootName = "AdminPanel.Registry";
        public const string HostRole = "host ";
        public const string ModuleRole = "module ";
        private const int MinEntryWords = 3;
        private const int IdWordFromEnd = 2;

        private GameObject _entry;

        public static string HostEntry(uint id) => $"{HostRole}{id} v{PanelWire.Version}";

        public static string ModuleEntry(string key, uint id) => $"{ModuleRole}{key} {id} v{PanelWire.Version}";

        public void Announce(string entryName)
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null) root = new GameObject(RootName);

            Withdraw();
            _entry = new GameObject(entryName);
            _entry.transform.SetParent(root.transform, false);
        }

        public bool TryFindHost(out uint id) => TryFindId(HostRole, out id);

        public void ForEachModule(Action<string, uint> found)
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null) return;

            foreach (Transform child in root.transform)
            {
                if (!child.name.StartsWith(ModuleRole, StringComparison.Ordinal)) continue;

                string[] words = child.name.Split(' ');
                if (words.Length >= MinEntryWords + 1 && uint.TryParse(words[words.Length - IdWordFromEnd], out uint id)) found(words[1], id);
            }
        }

        public void Withdraw()
        {
            if (_entry != null) Object.Destroy(_entry);
            _entry = null;
        }

        private static bool TryFindId(string rolePrefix, out uint id)
        {
            id = 0;
            GameObject root = GameObject.Find(RootName);
            if (root == null) return false;

            foreach (Transform child in root.transform)
            {
                if (!child.name.StartsWith(rolePrefix, StringComparison.Ordinal)) continue;

                string[] words = child.name.Split(' ');
                if (words.Length >= MinEntryWords && uint.TryParse(words[words.Length - IdWordFromEnd], out id)) return true;
            }
            return false;
        }
    }
}
