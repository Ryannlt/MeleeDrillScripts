using System;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Custom;
using MDS.Features.Programs;
using MDS.Shared.Panel.Protocol;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Panel
{
    // The fixed parts come from PanelCatalog, generated from Source~/panel-pages.json; the lists come from MDS's live state.
    public sealed class PageSource
    {
        public const string Module = "mds";
        private const string Tab = "MDS";
        private const string Icon = "M";
        private const int Order = 10;

        private readonly ArenaStore _arenas;
        private readonly CustomDrillBook _drills;
        private readonly ProgramsFeature _programs;
        private readonly AdminRegistry _admins;
        private readonly SettingValues _settings;

        public PageSource(PanelLists lists, AdminRegistry admins, SettingValues settings)
        {
            _arenas = lists.Arenas;
            _drills = lists.Drills;
            _programs = lists.Programs;
            _admins = admins;
            _settings = settings;
        }

        public List<PanelPage> For(int playerId)
        {
            if (!_admins.IsAdmin(playerId)) return new List<PanelPage> { new() { Module = Module, Tab = Tab, Icon = Icon, Order = Order, Denied = true } };

            var pages = new List<PanelPage>();
            List<PanelArena> arenas = ArenaList();
            foreach (string json in PanelCatalog.Pages)
            {
                if (!PanelCodec.TryRead(json, out PanelPage page)) continue;
                page.Arenas = arenas;
                Fill(page);
                pages.Add(page);
            }
            _settings.Fill(pages, playerId);
            return pages;
        }

        private void Fill(PanelPage page)
        {
            switch (page.Page)
            {
                case "Drills": Group(page, "Custom").Entries.AddRange(PageEntries.CustomDrillsToRun(_drills.All)); break;
                case "Custom drills": Group(page, "Custom drills").Entries.AddRange(CustomDrillEntries.For(_drills.All)); break;
                case "Programs": FillPrograms(page); break;
                case "Arenas": FillArenas(page); break;
            }
        }

        private void FillPrograms(PanelPage page)
        {
            ProgramRun running = _programs.Runner.Current;
            page.Status = running == null ? null : new PanelStatus { Text = running.Status, Action = new PanelAction { Label = "Stop", Text = "program stop" } };
            Group(page, "Programs").Entries.AddRange(ProgramEntries.For(_programs.Book.All, _drills.All));
        }

        private void FillArenas(PanelPage page)
        {
            PanelGroup group = Group(page, "Arenas");
            var arenas = new List<Arena>(_arenas.All);
            arenas.Sort((a, b) => a.Number.CompareTo(b.Number));
            group.Entries.AddRange(PageEntries.Arenas(arenas));
            PageEntries.PointNewArena(group, arenas);
        }

        // Written by hand, since the HBox server may strip ColorUtility as it strips Color.HSVToRGB.
        private static string Hex(Color color) => $"#{Byte(color.r):X2}{Byte(color.g):X2}{Byte(color.b):X2}";

        private static int Byte(float channel) => (int)Math.Round(Math.Max(0f, Math.Min(1f, channel)) * byte.MaxValue);

        private List<PanelArena> ArenaList()
        {
            var list = new List<PanelArena>();
            foreach (Arena arena in _arenas.All) list.Add(new PanelArena { Number = arena.Number, Color = Hex(ArenaColors.ColorOf(arena.Number)) });
            list.Sort((a, b) => a.Number.CompareTo(b.Number));
            return list;
        }

        private static PanelGroup Group(PanelPage page, string name)
        {
            PanelGroup group = page.Groups.Find(g => g.Name == name);
            if (group != null) return group;

            group = new PanelGroup { Name = name };
            page.Groups.Add(group);
            return group;
        }
    }
}
