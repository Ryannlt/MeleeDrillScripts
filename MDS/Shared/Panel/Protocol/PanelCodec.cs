using System;
using System.Collections.Generic;

// Keys are the ones XUI-PROTOCOL.md documents; an unknown key is ignored, so an older reader takes a newer page.
namespace MDS.Shared.Panel.Protocol
{
    public static class PanelCodec
    {
        public static string Write(PanelPage page)
        {
            var json = new JsonWriter().BeginObject();
            json.Name("v").Number(PanelWire.Version);
            json.Name("module").String(page.Module).Name("tab").String(page.Tab).Name("icon").String(page.Icon);
            json.Name("order").Number(page.Order).Name("page").String(page.Page);
            json.Name("pageIndex").Number(page.PageIndex).Name("pageCount").Number(page.PageCount);
            json.Name("denied").Bool(page.Denied).Name("empty").String(page.Empty).Name("staged").Bool(page.Staged);
            if (page.Status != null) WriteStatus(json.Name("status"), page.Status);
            WriteList(json.Name("groups"), page.Groups, WriteGroup);
            WriteList(json.Name("arenas"), page.Arenas, WriteArena);
            WriteList(json.Name("players"), page.Players, WritePlayer);
            return json.EndObject().ToString();
        }

        public static bool TryRead(string text, out PanelPage page)
        {
            page = null;
            if (!JsonReader.TryParse(text, out object tree) || !(tree is Dictionary<string, object> json)) return false;

            page = new PanelPage
            {
                Module = Str(json, "module"), Tab = Str(json, "tab"), Icon = Str(json, "icon"), Order = Int(json, "order"),
                Page = Str(json, "page"), PageIndex = Int(json, "pageIndex"), PageCount = Math.Max(1, Int(json, "pageCount")),
                Denied = Flag(json, "denied"), Empty = Str(json, "empty"), Staged = Flag(json, "staged"),
                Status = Obj(json, "status") is { } status ? ReadStatus(status) : null,
                Groups = ReadList(json, "groups", ReadGroup), Arenas = ReadList(json, "arenas", ReadArena),
                Players = ReadList(json, "players", ReadPlayer),
            };
            return page.Module.Length > 0 && page.Tab.Length > 0;
        }

        private static void WriteStatus(JsonWriter json, PanelStatus status)
        {
            json.BeginObject().Name("text").String(status.Text);
            if (status.Action != null) WriteAction(json.Name("action"), status.Action);
            json.EndObject();
        }

        private static void WriteAction(JsonWriter json, PanelAction action) =>
            json.BeginObject().Name("label").String(action.Label).Name("text").String(action.Text).Name("confirm").Bool(action.Confirm).EndObject();

        private static void WriteGroup(JsonWriter json, PanelGroup group)
        {
            json.BeginObject().Name("name").String(group.Name);
            WriteList(json.Name("entries"), group.Entries, WriteEntry);
            json.EndObject();
        }

        private static void WriteEntry(JsonWriter json, PanelEntry entry)
        {
            json.BeginObject().Name("label").String(entry.Label).Name("meta").String(entry.Meta).Name("help").String(entry.Help);
            json.Name("command").String(entry.Command).Name("template").String(entry.Template).Name("run").String(entry.Run);
            json.Name("confirm").Bool(entry.Confirm);
            if (entry.Secondary != null) WriteAction(json.Name("secondary"), entry.Secondary);
            WriteList(json.Name("more"), entry.More, WriteAction);
            WriteList(json.Name("actions"), entry.Actions, WriteAction);
            WriteList(json.Name("lines"), entry.Lines, (w, line) => w.String(line));
            WriteList(json.Name("options"), entry.Options, WriteOption);
            WriteList(json.Name("children"), entry.Children, WriteEntry);
            json.Name("kind").String(entry.Kind).Name("indent").Number(entry.Indent).Name("prefix").String(entry.Prefix).Name("open").Bool(entry.Open);
            json.EndObject();
        }

        private static void WriteOption(JsonWriter json, PanelOption o)
        {
            json.BeginObject().Name("name").String(o.Name).Name("label").String(o.Label).Name("description").String(o.Description);
            json.Name("type").String(o.Type).Name("value").String(o.Value);
            WriteNumber(json, "min", o.Min);
            WriteNumber(json, "max", o.Max);
            WriteNumber(json, "hardMax", o.HardMax);
            WriteNumber(json, "step", o.Step);
            WriteList(json.Name("values"), o.Values, WriteChoice);
            json.Name("setting").String(o.Setting).Name("settingValue").String(o.SettingValue);
            json.Name("defaultLabel").String(o.DefaultLabel).Name("placeholder").String(o.Placeholder);
            json.Name("positional").Bool(o.Positional).Name("word").Bool(o.Word).Name("optional").Bool(o.Optional).Name("free").Bool(o.Free);
            json.Name("hereValue").String(o.HereValue).Name("stepper").Bool(o.Stepper);
            WriteWhen(json, o.When);
            json.EndObject();
        }

        private static void WriteWhen(JsonWriter json, Dictionary<string, List<string>> when)
        {
            if (when.Count == 0) return;

            json.Name("when").BeginObject();
            foreach (KeyValuePair<string, List<string>> pair in when) WriteList(json.Name(pair.Key), pair.Value, (w, value) => w.String(value));
            json.EndObject();
        }

        private static void WriteNumber(JsonWriter json, string name, double? value)
        {
            if (value.HasValue) json.Name(name).Number(value.Value);
        }

        private static void WriteChoice(JsonWriter json, PanelChoice c)
        {
            json.BeginObject().Name("value").String(c.Value).Name("label").String(c.Label).Name("description").String(c.Description);
            WriteWhen(json, c.When);
            json.EndObject();
        }

        private static void WriteArena(JsonWriter json, PanelArena a) =>
            json.BeginObject().Name("n").Number(a.Number).Name("color").String(a.Color).EndObject();

        private static void WritePlayer(JsonWriter json, PanelPlayer p) =>
            json.BeginObject().Name("id").Number(p.Id).Name("name").String(p.Name).Name("faction").String(p.Faction)
                .Name("playerClass").String(p.PlayerClass).EndObject();

        private static void WriteList<T>(JsonWriter json, List<T> items, Action<JsonWriter, T> write)
        {
            json.BeginArray();
            foreach (T item in items) write(json, item);
            json.EndArray();
        }

        private static PanelStatus ReadStatus(Dictionary<string, object> json) =>
            new() { Text = Str(json, "text"), Action = Obj(json, "action") is { } action ? ReadAction(action) : null };

        private static PanelAction ReadAction(Dictionary<string, object> json) =>
            new() { Label = Str(json, "label"), Text = Str(json, "text"), Confirm = Flag(json, "confirm") };

        private static PanelGroup ReadGroup(Dictionary<string, object> json) =>
            new() { Name = Str(json, "name"), Entries = ReadList(json, "entries", ReadEntry) };

        private static PanelEntry ReadEntry(Dictionary<string, object> json) => new()
        {
            Label = Str(json, "label"), Meta = Str(json, "meta"), Help = Str(json, "help"), Command = Str(json, "command"),
            Template = Str(json, "template"), Run = Str(json, "run"), Confirm = Flag(json, "confirm"),
            Secondary = Obj(json, "secondary") is { } secondary ? ReadAction(secondary) : null,
            More = ReadList(json, "more", ReadAction), Actions = ReadList(json, "actions", ReadAction),
            Lines = ReadStrings(json, "lines"), Options = ReadList(json, "options", ReadOption),
            Children = ReadList(json, "children", ReadEntry), Kind = Str(json, "kind"), Indent = Int(json, "indent"),
            Prefix = Str(json, "prefix"), Open = Flag(json, "open"),
        };

        private static PanelOption ReadOption(Dictionary<string, object> json) => new()
        {
            Name = Str(json, "name"), Label = Str(json, "label"), Description = Str(json, "description"),
            Type = Str(json, "type"), Value = Str(json, "value"), Min = Num(json, "min"), Max = Num(json, "max"),
            HardMax = Num(json, "hardMax"), Step = Num(json, "step"), Values = ReadList(json, "values", ReadChoice),
            Setting = Str(json, "setting"), SettingValue = Str(json, "settingValue"), DefaultLabel = Str(json, "defaultLabel"),
            Placeholder = Str(json, "placeholder"), Positional = Flag(json, "positional"), Word = Flag(json, "word"),
            Optional = Flag(json, "optional"), Free = Flag(json, "free"), HereValue = Str(json, "hereValue"), Stepper = Flag(json, "stepper"), When = ReadWhen(json),
        };

        private static Dictionary<string, List<string>> ReadWhen(Dictionary<string, object> json)
        {
            var when = new Dictionary<string, List<string>>();
            if (!(Obj(json, "when") is { } names)) return when;

            foreach (string name in names.Keys) when[name] = ReadStrings(names, name);
            return when;
        }

        private static PanelChoice ReadChoice(Dictionary<string, object> json) =>
            new() { Value = Str(json, "value"), Label = Str(json, "label"), Description = Str(json, "description"), When = ReadWhen(json) };

        private static PanelArena ReadArena(Dictionary<string, object> json) => new() { Number = Int(json, "n"), Color = Str(json, "color") };

        private static PanelPlayer ReadPlayer(Dictionary<string, object> json) => new()
        {
            Id = Int(json, "id"), Name = Str(json, "name"), Faction = Str(json, "faction"), PlayerClass = Str(json, "playerClass"),
        };

        private static List<T> ReadList<T>(Dictionary<string, object> json, string name, Func<Dictionary<string, object>, T> read)
        {
            var result = new List<T>();
            if (!json.TryGetValue(name, out object value) || !(value is List<object> items)) return result;

            foreach (object item in items)
            {
                if (item is Dictionary<string, object> child) result.Add(read(child));
            }
            return result;
        }

        private static List<string> ReadStrings(Dictionary<string, object> json, string name)
        {
            var result = new List<string>();
            if (!json.TryGetValue(name, out object value) || !(value is List<object> items)) return result;

            foreach (object item in items)
            {
                if (item is string text) result.Add(text);
            }
            return result;
        }

        private static Dictionary<string, object> Obj(Dictionary<string, object> json, string name) =>
            json.TryGetValue(name, out object value) ? value as Dictionary<string, object> : null;

        private static string Str(Dictionary<string, object> json, string name) =>
            json.TryGetValue(name, out object value) && value is string text ? text : string.Empty;

        private static double? Num(Dictionary<string, object> json, string name) =>
            json.TryGetValue(name, out object value) && value is double number ? number : (double?)null;

        private static int Int(Dictionary<string, object> json, string name) => (int)(Num(json, name) ?? 0);

        private static bool Flag(Dictionary<string, object> json, string name) => json.TryGetValue(name, out object value) && value is true;
    }
}
