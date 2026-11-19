using System.Collections.Generic;
using MDS.Shared.Game;
using MDS.Shared.Panel.Protocol;
using MDS.Shared.Settings;

namespace MDS.Features.Panel
{
    // Values are read the way rc get reads them: the setting answers "<key> is <value>.".
    public sealed class SettingValues
    {
        private const string Is = " is ";

        private readonly SettingsRegistry _settings;
        private readonly Replies _replies;

        public SettingValues(SettingsRegistry settings, Replies replies)
        {
            _settings = settings;
            _replies = replies;
        }

        public void Fill(List<PanelPage> pages, int playerId)
        {
            var known = new Dictionary<string, string>();
            string ValueOfKey(string key) => ValueOf(key, known, playerId);
            foreach (PanelPage page in pages)
            {
                foreach (PanelGroup group in page.Groups)
                {
                    foreach (PanelEntry entry in group.Entries) FillEntry(entry, page.Staged, ValueOfKey);
                }
            }
        }

        private static void FillEntry(PanelEntry entry, bool isSettingsRow, System.Func<string, string> valueOf)
        {
            foreach (PanelOption option in entry.Options)
            {
                if (isSettingsRow) FillSettingRow(entry, option, valueOf(option.Name));
                else if (option.Setting.Length > 0) option.SettingValue = valueOf(option.Setting) ?? option.SettingValue;
            }
        }

        // The row's meta was written for the default ("15 m"), so it is rebuilt for the live value: a choice's label, or the value with the same unit.
        private static void FillSettingRow(PanelEntry entry, PanelOption option, string value)
        {
            if (value == null) return;

            PanelChoice choice = option.Values.Find(c => string.Equals(c.Value, value, System.StringComparison.OrdinalIgnoreCase));
            if (choice != null) entry.Meta = choice.Label;
            else if (option.Type == OptionTypes.Flag) entry.Meta = value.Equals("true", System.StringComparison.OrdinalIgnoreCase) ? "On" : "Off";
            else if (option.Value.Length > 0 && entry.Meta.StartsWith(option.Value)) entry.Meta = value + entry.Meta.Substring(option.Value.Length);
            option.Value = value;
        }

        private string ValueOf(string key, Dictionary<string, string> known, int playerId)
        {
            if (!known.TryGetValue(key, out string value)) known[key] = value = Read(key, playerId);
            return value;
        }

        private string Read(string key, int playerId)
        {
            if (!_settings.TryFind(new[] { key }, out ISetting setting, out string[] args)) return null;

            string said = _replies.Capture(playerId, () => setting.Get(playerId, args));
            int at = said.IndexOf(Is, System.StringComparison.Ordinal);
            return at < 0 ? null : AsChoice(said.Substring(at + Is.Length).TrimEnd('.'));
        }

        // A bool setting answers True or False, while its row's choices are true and false, as rc set takes them.
        // Choice values are lowercase words, so a setting that answers Scatter matches the choice scatter.
        private static string AsChoice(string value) => bool.TryParse(value, out bool flag) ? (flag ? "true" : "false") : value;
    }
}
