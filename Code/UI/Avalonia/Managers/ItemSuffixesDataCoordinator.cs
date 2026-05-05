using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace RPGGame.UI.Avalonia.Managers
{
    public class ItemSuffixesDataCoordinator
    {
        private readonly ItemSuffixesDataService dataService;
        private readonly Action<string, bool>? showStatusMessage;

        public ItemSuffixesDataCoordinator(ItemSuffixesDataService dataService, Action<string, bool>? showStatusMessage = null)
        {
            this.dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            this.showStatusMessage = showStatusMessage;
        }

        public (ObservableCollection<SuffixRarityGroupViewModel> groups, Dictionary<int, StatBonus> originals) LoadSuffixesData()
        {
            var availableRarities = dataService.GetAvailableRarities();
            var rows = dataService.LoadStatBonuses();
            int nextId = 0;
            var originals = new Dictionary<int, StatBonus>();

            var grouped = rows
                .Select(b =>
                {
                    int id = nextId++;
                    originals[id] = b;
                    return (id, b);
                })
                .GroupBy(x => string.IsNullOrWhiteSpace(x.b.Rarity) ? "Common" : x.b.Rarity.Trim())
                .OrderBy(g => GetRarityOrder(g.Key))
                .ToList();

            var groups = new ObservableCollection<SuffixRarityGroupViewModel>();
            foreach (var g in grouped)
            {
                var vmGroup = new SuffixRarityGroupViewModel { RarityName = g.Key };
                foreach (var (id, bonus) in g.OrderBy(x => x.b.Name, StringComparer.OrdinalIgnoreCase))
                {
                    var vm = new SuffixModifierViewModel
                    {
                        EntryId = id,
                        Name = bonus.Name ?? "",
                        Description = bonus.Description ?? "",
                        CurrentRarity = g.Key,
                        SelectedRarity = string.IsNullOrWhiteSpace(bonus.Rarity) ? g.Key : bonus.Rarity.Trim(),
                        ItemRank = bonus.ItemRank ?? "",
                        StatType = bonus.StatType ?? "",
                        Value = bonus.Value,
                        MechanicsText = FormatMechanicsForDisplay(bonus)
                    };
                    foreach (var r in availableRarities)
                        vm.AvailableRarities.Add(r);
                    vmGroup.Modifiers.Add(vm);
                }

                groups.Add(vmGroup);
            }

            return (groups, originals);
        }

        public void SaveSuffixes(
            ObservableCollection<SuffixRarityGroupViewModel> groups,
            Dictionary<int, StatBonus> originals)
        {
            if (groups == null)
            {
                showStatusMessage?.Invoke("No suffix data to save", false);
                return;
            }

            try
            {
                var output = new List<StatBonus>();
                foreach (var group in groups)
                {
                    foreach (var mod in group.Modifiers)
                    {
                        StatBonus row;
                        if (originals.TryGetValue(mod.EntryId, out var orig) && orig != null)
                        {
                            row = orig;
                            ApplyViewModelToStatBonus(mod, row);
                        }
                        else
                        {
                            row = new StatBonus();
                            ApplyViewModelToStatBonus(mod, row);
                        }

                        output.Add(row);
                    }
                }

                dataService.SaveStatBonuses(output);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving suffixes: {ex.Message}", false);
                throw;
            }
        }

        public List<string> GetAvailableRarities() => dataService.GetAvailableRarities();

        public int GetNextEntryId(ObservableCollection<SuffixRarityGroupViewModel>? groups, Dictionary<int, StatBonus> originals)
        {
            int max = -1;
            if (groups != null)
            {
                foreach (var g in groups)
                foreach (var m in g.Modifiers)
                    if (m.EntryId > max) max = m.EntryId;
            }

            foreach (var id in originals.Keys)
                if (id > max) max = id;

            return max + 1;
        }

        private static void ApplyViewModelToStatBonus(SuffixModifierViewModel vm, StatBonus row)
        {
            row.Name = vm.Name ?? "";
            row.Description = vm.Description ?? "";
            row.Rarity = vm.SelectedRarity ?? "Common";
            row.ItemRank = vm.ItemRank ?? "";

            var mechanics = ParseMechanicsText(vm.MechanicsText, vm.StatType ?? "", vm.Value);
            if (mechanics != null && mechanics.Count > 0)
            {
                row.Mechanics = mechanics;
                row.StatType = mechanics[0].StatType;
                row.Value = mechanics[0].Value;
            }
            else
            {
                row.Mechanics = null;
                row.StatType = vm.StatType ?? "";
                row.Value = vm.Value;
            }
        }

        internal static string FormatMechanicsForDisplay(StatBonus bonus)
        {
            if (bonus.Mechanics != null && bonus.Mechanics.Count > 0)
                return string.Join(global::System.Environment.NewLine, bonus.Mechanics.Where(m => m != null).Select(m => $"{m!.StatType}: {m.Value}"));

            if (!string.IsNullOrWhiteSpace(bonus.StatType))
                return $"{bonus.StatType}: {bonus.Value}";

            return "";
        }

        internal static List<StatBonusMechanic>? ParseMechanicsText(string? text, string fallbackStatType, double fallbackValue)
        {
            string t = text?.Trim() ?? "";
            if (t.Length > 0)
            {
                if (t.StartsWith('[') && t.EndsWith(']'))
                {
                    var arr = JsonArraySheetConverter.ParseStatBonusBracketMechanics(t);
                    if (arr != null && arr.Count > 0)
                        return JsonArrayToMechanics(arr);
                }

                var list = new List<StatBonusMechanic>();
                foreach (var line in t.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0) continue;
                    int col = trimmed.LastIndexOf(':');
                    if (col <= 0 || col >= trimmed.Length - 1) continue;
                    string st = trimmed[..col].Trim();
                    string vs = trimmed[(col + 1)..].Trim();
                    if (st.Length == 0) continue;
                    if (double.TryParse(vs, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                        list.Add(new StatBonusMechanic { StatType = st, Value = v });
                }

                if (list.Count > 0)
                    return list;
            }

            if (!string.IsNullOrWhiteSpace(fallbackStatType))
                return new List<StatBonusMechanic> { new StatBonusMechanic { StatType = fallbackStatType.Trim(), Value = fallbackValue } };

            return null;
        }

        private static List<StatBonusMechanic> JsonArrayToMechanics(JsonArray arr)
        {
            var list = new List<StatBonusMechanic>();
            foreach (var el in arr)
            {
                if (el is not JsonObject o) continue;
                if (!o.TryGetPropertyValue("StatType", out var stNode) || stNode is not JsonValue stv || !stv.TryGetValue<string>(out var statType) || statType == null)
                    continue;
                double val = 0;
                if (o.TryGetPropertyValue("Value", out var vn) && vn is JsonValue vv && vv.TryGetValue<double>(out var d))
                    val = d;
                list.Add(new StatBonusMechanic { StatType = statType, Value = val });
            }

            return list;
        }

        private static int GetRarityOrder(string rarity)
        {
            return rarity.ToLowerInvariant() switch
            {
                "common" => 0,
                "uncommon" => 1,
                "rare" => 2,
                "epic" => 3,
                "legendary" => 4,
                "mythic" => 5,
                "transcendent" => 6,
                _ => 99
            };
        }
    }
}
