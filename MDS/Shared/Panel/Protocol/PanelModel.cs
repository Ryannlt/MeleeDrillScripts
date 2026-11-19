using System.Collections.Generic;

// This file is copied into every module (shared code is copied), so a change goes into every copy and the protocol version.
namespace MDS.Shared.Panel.Protocol
{
    public sealed class PanelPage
    {
        public string Module { get; set; } = string.Empty;
        public string Tab { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Page { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public int PageCount { get; set; } = 1;
        public bool Denied { get; set; }
        public string Empty { get; set; } = string.Empty;
        public bool Staged { get; set; }
        public PanelStatus Status { get; set; }
        public List<PanelGroup> Groups { get; set; } = new();
        public List<PanelArena> Arenas { get; set; } = new();
        public List<PanelPlayer> Players { get; set; } = new();
    }

    public sealed class PanelStatus
    {
        public string Text { get; set; } = string.Empty;
        public PanelAction Action { get; set; }
    }

    public sealed class PanelAction
    {
        public string Label { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool Confirm { get; set; }
    }

    public sealed class PanelGroup
    {
        public string Name { get; set; } = string.Empty;
        public List<PanelEntry> Entries { get; set; } = new();
    }

    public sealed class PanelEntry
    {
        public string Label { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public string Help { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string Run { get; set; } = string.Empty;
        public bool Confirm { get; set; }
        public PanelAction Secondary { get; set; }
        public List<PanelAction> More { get; set; } = new();
        public List<PanelAction> Actions { get; set; } = new();
        public List<string> Lines { get; set; } = new();
        public List<PanelOption> Options { get; set; } = new();

        // Entries shown indented under this one while it is open, each with its own options.
        public List<PanelEntry> Children { get; set; } = new();
        public string Kind { get; set; } = string.Empty;
        public int Indent { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public bool Open { get; set; }
    }

    public static class EntryKinds
    {
        // A "+" row that makes a new item: its options are the empty template.
        public const string Add = "add";
    }

    public sealed class PanelOption
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = OptionTypes.Text;
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? HardMax { get; set; }
        public double? Step { get; set; }
        public string Value { get; set; } = string.Empty;
        public List<PanelChoice> Values { get; set; } = new();
        public string Setting { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public string DefaultLabel { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public bool Positional { get; set; }
        public bool Word { get; set; }
        public bool Optional { get; set; }
        public bool Free { get; set; }

        // A set drawn as - and + around its value instead of a dropdown, for an ordered list such as fight sizes.
        public bool Stepper { get; set; }

        // Shown only while each named option's value, or its default, is one of the listed values.
        public Dictionary<string, List<string>> When { get; set; } = new();

        // A point option with this set gets a "Where I stand" button that sends this value.
        public string HereValue { get; set; } = string.Empty;
    }

    public sealed class PanelChoice
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, List<string>> When { get; set; } = new();
    }

    public sealed class PanelArena
    {
        public int Number { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public sealed class PanelPlayer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Faction { get; set; } = string.Empty;
        public string PlayerClass { get; set; } = string.Empty;
    }

    public static class OptionTypes
    {
        public const string Number = "number";
        public const string Set = "set";
        public const string Arena = "arena";
        public const string Arenas = "arenas";
        public const string Point = "point";
        public const string Flag = "flag";
        public const string Name = "name";
        public const string Text = "text";
        public const string Player = "player";
    }
}
