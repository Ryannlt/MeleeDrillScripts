using System;
using System.Collections.Generic;

// Keys are the ones XUI-PROTOCOL.md documents; an unknown key is ignored, so an older reader takes a newer page.
namespace MDS.Shared.Panel.Protocol
{
    public static class PanelCodec
    {
        public static string Write(PanelPage page)
        {
            SharedOptions shared = SharedOptions.Of(page, Definition);
            var json = new JsonWriter().BeginObject();
            json.Name("v").Number(PanelWire.Version);
            json.Name("module").String(page.Module).Name("tab").String(page.Tab);
            Put(json, "icon", page.Icon);
            json.Name("order").Number(page.Order);
            Put(json, "page", page.Page);
            json.Name("pageIndex").Number(page.PageIndex).Name("pageCount").Number(page.PageCount);
            Put(json, "denied", page.Denied);
            Put(json, "empty", page.Empty);
            Put(json, "staged", page.Staged);
            if (page.Status != null) WriteStatus(json.Name("status"), page.Status);
            PutList(json, "shared", shared.Definitions, (w, o) => WriteDefinition(w, o));
            PutList(json, "groups", page.Groups, (w, g) => WriteGroup(w, g, shared));
            PutList(json, "arenas", page.Arenas, WriteArena);
            PutList(json, "players", page.Players, WritePlayer);
            return json.EndObject().ToString();
        }

        public static bool TryRead(string text, out PanelPage page)
        {
            page = null;
            if (!JsonReader.TryParse(text, out object tree) || !(tree is Dictionary<string, object> json)) return false;

            List<PanelOption> shared = ReadList(json, "shared", o => ReadOption(o, null));
            page = new PanelPage
            {
                Module = Str(json, "module"), Tab = Str(json, "tab"), Icon = Str(json, "icon"), Order = Int(json, "order"),
                Page = Str(json, "page"), PageIndex = Int(json, "pageIndex"), PageCount = Math.Max(1, Int(json, "pageCount")),
                Denied = Flag(json, "denied"), Empty = Str(json, "empty"), Staged = Flag(json, "staged"),
                Status = Obj(json, "status") is { } status ? ReadStatus(status) : null,
                Groups = ReadList(json, "groups", g => ReadGroup(g, shared)), Arenas = ReadList(json, "arenas", ReadArena),
                Players = ReadList(json, "players", ReadPlayer),
            };
            return page.Module.Length > 0 && page.Tab.Length > 0;
        }

        private static void WriteStatus(JsonWriter json, PanelStatus status)
        {
            json.BeginObject();
            Put(json, "text", status.Text);
            if (status.Action != null) WriteAction(json.Name("action"), status.Action);
            json.EndObject();
        }

        private static void WriteAction(JsonWriter json, PanelAction action)
        {
            json.BeginObject().Name("label").String(action.Label).Name("text").String(action.Text);
            Put(json, "confirm", action.Confirm);
            json.EndObject();
        }

        private static void WriteGroup(JsonWriter json, PanelGroup group, SharedOptions shared)
        {
            json.BeginObject().Name("name").String(group.Name);
            PutList(json, "entries", group.Entries, (w, e) => WriteEntry(w, e, shared));
            json.EndObject();
        }

        private static void WriteEntry(JsonWriter json, PanelEntry entry, SharedOptions shared)
        {
            json.BeginObject().Name("label").String(entry.Label);
            Put(json, "meta", entry.Meta);
            Put(json, "help", entry.Help);
            Put(json, "command", entry.Command);
            Put(json, "template", entry.Template);
            Put(json, "run", entry.Run);
            Put(json, "confirm", entry.Confirm);
            if (entry.Secondary != null) WriteAction(json.Name("secondary"), entry.Secondary);
            PutList(json, "more", entry.More, WriteAction);
            PutList(json, "actions", entry.Actions, WriteAction);
            PutList(json, "lines", entry.Lines, (w, line) => w.String(line));
            PutList(json, "options", entry.Options, (w, o) => WriteOption(w, o, shared));
            PutList(json, "children", entry.Children, (w, c) => WriteEntry(w, c, shared));
            WriteTreePlace(json, entry);
            json.EndObject();
        }

        private static void WriteTreePlace(JsonWriter json, PanelEntry entry)
        {
            Put(json, "kind", entry.Kind);
            if (entry.Indent != 0) json.Name("indent").Number(entry.Indent);
            Put(json, "prefix", entry.Prefix);
            Put(json, "open", entry.Open);
        }

        private static void WriteOption(JsonWriter json, PanelOption o, SharedOptions shared)
        {
            json.BeginObject();
            int use = shared.IndexOf(o);
            if (use >= 0) json.Name("use").Number(use);
            else WriteAllButValue(json, o);
            Put(json, "value", o.Value);
            json.EndObject();
        }

        private static string Definition(PanelOption o) => WriteDefinition(new JsonWriter(), o).ToString();

        private static JsonWriter WriteDefinition(JsonWriter json, PanelOption o)
        {
            json.BeginObject();
            WriteAllButValue(json, o);
            return json.EndObject();
        }

        private static void WriteAllButValue(JsonWriter json, PanelOption o)
        {
            json.Name("name").String(o.Name);
            Put(json, "label", o.Label);
            Put(json, "description", o.Description);
            json.Name("type").String(o.Type);
            WriteNumber(json, "min", o.Min);
            WriteNumber(json, "max", o.Max);
            WriteNumber(json, "hardMax", o.HardMax);
            WriteNumber(json, "step", o.Step);
            PutList(json, "values", o.Values, WriteChoice);
            WriteTexts(json, o);
            WriteSwitches(json, o);
            WriteWhen(json, o.When);
        }

        private static void WriteTexts(JsonWriter json, PanelOption o)
        {
            Put(json, "setting", o.Setting);
            Put(json, "settingValue", o.SettingValue);
            Put(json, "defaultLabel", o.DefaultLabel);
            Put(json, "placeholder", o.Placeholder);
            Put(json, "hereValue", o.HereValue);
        }

        private static void WriteSwitches(JsonWriter json, PanelOption o)
        {
            Put(json, "positional", o.Positional);
            Put(json, "word", o.Word);
            Put(json, "optional", o.Optional);
            Put(json, "free", o.Free);
            Put(json, "stepper", o.Stepper);
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

        // A label that only repeats the value is left out, and the reader takes the value as the label.
        private static void WriteChoice(JsonWriter json, PanelChoice c)
        {
            json.BeginObject().Name("value").String(c.Value);
            if (c.Label != c.Value) json.Name("label").String(c.Label);
            Put(json, "description", c.Description);
            WriteWhen(json, c.When);
            json.EndObject();
        }

        private static void WriteArena(JsonWriter json, PanelArena a) =>
            json.BeginObject().Name("n").Number(a.Number).Name("color").String(a.Color).EndObject();

        private static void WritePlayer(JsonWriter json, PanelPlayer p) =>
            json.BeginObject().Name("id").Number(p.Id).Name("name").String(p.Name).Name("faction").String(p.Faction)
                .Name("playerClass").String(p.PlayerClass).EndObject();

        // Empty texts, false switches and empty lists are left out, since the reader's defaults are the same values.
        private static void Put(JsonWriter json, string name, string value)
        {
            if (!string.IsNullOrEmpty(value)) json.Name(name).String(value);
        }

        private static void Put(JsonWriter json, string name, bool value)
        {
            if (value) json.Name(name).Bool(true);
        }

        private static void PutList<T>(JsonWriter json, string name, List<T> items, Action<JsonWriter, T> write)
        {
            if (items.Count > 0) WriteList(json.Name(name), items, write);
        }

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

        private static PanelGroup ReadGroup(Dictionary<string, object> json, List<PanelOption> shared) =>
            new() { Name = Str(json, "name"), Entries = ReadList(json, "entries", e => ReadEntry(e, shared)) };

        private static PanelEntry ReadEntry(Dictionary<string, object> json, List<PanelOption> shared) => new()
        {
            Label = Str(json, "label"), Meta = Str(json, "meta"), Help = Str(json, "help"), Command = Str(json, "command"),
            Template = Str(json, "template"), Run = Str(json, "run"), Confirm = Flag(json, "confirm"),
            Secondary = Obj(json, "secondary") is { } secondary ? ReadAction(secondary) : null,
            More = ReadList(json, "more", ReadAction), Actions = ReadList(json, "actions", ReadAction),
            Lines = ReadStrings(json, "lines"), Options = ReadList(json, "options", o => ReadOption(o, shared)),
            Children = ReadList(json, "children", c => ReadEntry(c, shared)), Kind = Str(json, "kind"), Indent = Int(json, "indent"),
            Prefix = Str(json, "prefix"), Open = Flag(json, "open"),
        };

        // shared is null while the shared definitions themselves are read.
        private static PanelOption ReadOption(Dictionary<string, object> json, List<PanelOption> shared)
        {
            int use = (int)(Num(json, "use") ?? -1);
            return shared != null && use >= 0 && use < shared.Count ? SharedOptions.Expand(shared[use], Str(json, "value")) : ReadFields(json);
        }

        private static PanelOption ReadFields(Dictionary<string, object> json) => new()
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

        private static PanelChoice ReadChoice(Dictionary<string, object> json)
        {
            string value = Str(json, "value");
            string label = json.ContainsKey("label") ? Str(json, "label") : value;
            return new PanelChoice { Value = value, Label = label, Description = Str(json, "description"), When = ReadWhen(json) };
        }

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
