using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ItemGenerationSettingsPanel : UserControl
    {
        private readonly ObservableCollection<ItemGeneratedRowViewModel> rows = new();

        public ItemGenerationSettingsPanel()
        {
            InitializeComponent();
            InitializeOptions();
            WireEvents();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeOptions()
        {
            var itemTypeCombo = this.FindControl<ComboBox>("ItemTypeCombo");
            var rarityCombo = this.FindControl<ComboBox>("RarityCombo");
            var tierCombo = this.FindControl<ComboBox>("TierCombo");
            var weaponTypeCombo = this.FindControl<ComboBox>("WeaponTypeCombo");
            var armorSlotCombo = this.FindControl<ComboBox>("ArmorSlotCombo");
            var list = this.FindControl<ListBox>("ItemsListBox");

            if (itemTypeCombo != null)
            {
                itemTypeCombo.ItemsSource = new[] { "Both", "Weapons", "Armor" };
                itemTypeCombo.SelectedIndex = 0;
            }

            if (rarityCombo != null)
            {
                rarityCombo.ItemsSource = new[] { "Any", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
                rarityCombo.SelectedIndex = 0;
            }

            if (tierCombo != null)
            {
                tierCombo.ItemsSource = new[] { "Any", "1", "2", "3", "4", "5" };
                tierCombo.SelectedIndex = 0;
            }

            if (weaponTypeCombo != null)
            {
                weaponTypeCombo.ItemsSource = new[] { "Any", "Sword", "Dagger", "Mace", "Wand" };
                weaponTypeCombo.SelectedIndex = 0;
            }

            if (armorSlotCombo != null)
            {
                armorSlotCombo.ItemsSource = new[] { "Any", "Head", "Chest", "Feet" };
                armorSlotCombo.SelectedIndex = 0;
            }

            if (list != null)
            {
                list.ItemsSource = rows;
            }
        }

        private void WireEvents()
        {
            var generateButton = this.FindControl<Button>("GenerateButton");
            var clearButton = this.FindControl<Button>("ClearButton");
            var list = this.FindControl<ListBox>("ItemsListBox");

            if (generateButton != null)
                generateButton.Click += (_, __) => Generate();
            if (clearButton != null)
                clearButton.Click += (_, __) => Clear();
            if (list != null)
                list.SelectionChanged += (_, __) => UpdateSelectedDetails(list.SelectedItem as ItemGeneratedRowViewModel);
        }

        private void Generate()
        {
            var spec = new ItemGenerationSpec
            {
                ItemType = ReadItemType(),
                Rarity = ReadRarity(),
                Tier = ReadTier(),
                WeaponType = ReadWeaponType(),
                ArmorSlot = ReadArmorSlot(),
                PlayerLevel = 1,
                DungeonLevel = 1,
                // New seed each run; a fixed seed makes every batch identical (Random(spec.Seed) in the lab service).
                Seed = Random.Shared.Next()
            };

            int count = ReadCount();
            var batch = ItemGenerationLabService.GenerateBatch(spec, count);
            var sorted = ItemGenerationLabService.SortBestToWorst(batch);

            rows.Clear();
            int idx = 1;
            foreach (var row in sorted)
            {
                rows.Add(ItemGeneratedRowViewModel.FromRow(row, idx));
                idx++;
            }

            var list = this.FindControl<ListBox>("ItemsListBox");
            if (list != null && rows.Count > 0)
                list.SelectedIndex = 0;
        }

        private void Clear()
        {
            rows.Clear();
            UpdateSelectedDetails(null);
        }

        private void UpdateSelectedDetails(ItemGeneratedRowViewModel? row)
        {
            var name = this.FindControl<TextBlock>("SelectedNameText");
            var stats = this.FindControl<TextBlock>("SelectedStatsText");

            if (row == null)
            {
                if (name != null) name.Text = "(none)";
                if (stats != null) stats.Text = "";
                return;
            }

            if (name != null) name.Text = row.NameWithRarity;
            if (stats != null) stats.Text = row.DetailsText;
        }

        private int ReadCount()
        {
            var tb = this.FindControl<TextBox>("CountTextBox");
            if (tb == null) return 100;
            if (int.TryParse(tb.Text, out int n))
                return Math.Clamp(n, 1, 1000);
            return 100;
        }

        private ItemGenerationItemType ReadItemType()
        {
            var combo = this.FindControl<ComboBox>("ItemTypeCombo");
            string v = combo?.SelectedItem?.ToString() ?? "Both";
            return v switch
            {
                "Weapons" => ItemGenerationItemType.Weapons,
                "Armor" => ItemGenerationItemType.Armor,
                _ => ItemGenerationItemType.Both
            };
        }

        private string? ReadRarity()
        {
            var combo = this.FindControl<ComboBox>("RarityCombo");
            string v = combo?.SelectedItem?.ToString() ?? "Any";
            return v;
        }

        private int? ReadTier()
        {
            var combo = this.FindControl<ComboBox>("TierCombo");
            string v = combo?.SelectedItem?.ToString() ?? "Any";
            if (v == "Any") return null;
            return int.TryParse(v, out int t) ? t : null;
        }

        private WeaponType? ReadWeaponType()
        {
            var combo = this.FindControl<ComboBox>("WeaponTypeCombo");
            string v = combo?.SelectedItem?.ToString() ?? "Any";
            if (v == "Any") return null;
            return Enum.TryParse<WeaponType>(v, ignoreCase: true, out var wt) ? wt : null;
        }

        private ItemGenerationArmorSlot ReadArmorSlot()
        {
            var combo = this.FindControl<ComboBox>("ArmorSlotCombo");
            string v = combo?.SelectedItem?.ToString() ?? "Any";
            return v switch
            {
                "Head" => ItemGenerationArmorSlot.Head,
                "Chest" => ItemGenerationArmorSlot.Chest,
                "Feet" => ItemGenerationArmorSlot.Feet,
                _ => ItemGenerationArmorSlot.Any
            };
        }
    }

    public sealed class ItemGeneratedRowViewModel : INotifyPropertyChanged
    {
        private Item? _item;

        public int Index { get; set; }
        public string Rarity { get; set; } = "";
        public int Tier { get; set; }
        public string TypeLabel { get; set; } = "";
        public string Name { get; set; } = "";
        public string PrimaryStat { get; set; } = "";
        public string SpeedText { get; set; } = "";
        public string AffixesText { get; set; } = "";

        public string NameWithRarity => $"[{Rarity}] {Name}";

        public string DetailsText
        {
            get
            {
                if (_item == null) return "";

                var lines = new System.Collections.Generic.List<string>
                {
                    $"Tier: {Tier}",
                    $"Type: {TypeLabel}",
                };

                if (_item is WeaponItem w)
                {
                    lines.Add($"Damage: {w.GetTotalDamage()}");
                    lines.Add($"Attack speed: {w.GetTotalAttackSpeed():0.##}×");
                }
                else if (_item is HeadItem h)
                {
                    lines.Add($"Armor: {h.GetTotalArmor()}");
                }
                else if (_item is ChestItem c)
                {
                    lines.Add($"Armor: {c.GetTotalArmor()}");
                }
                else if (_item is FeetItem f)
                {
                    lines.Add($"Armor: {f.GetTotalArmor()}");
                }

                if (_item.Modifications != null && _item.Modifications.Count > 0)
                    lines.Add($"Prefixes: {string.Join(", ", _item.Modifications.Select(m => m.Name))}");
                if (_item.StatBonuses != null && _item.StatBonuses.Count > 0)
                    lines.Add($"Suffixes: {string.Join(", ", _item.StatBonuses.Select(s => s.Name))}");
                if (_item.ActionBonuses != null && _item.ActionBonuses.Count > 0)
                    lines.Add($"Action bonuses: {string.Join(", ", _item.ActionBonuses.Select(a => a.Name))}");

                return string.Join("\n", lines);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static ItemGeneratedRowViewModel FromRow(ItemGeneratedRow row, int displayIndex)
        {
            var vm = new ItemGeneratedRowViewModel
            {
                _item = row.Item,
                Index = displayIndex,
                Rarity = row.Rarity,
                Tier = row.Tier,
                TypeLabel = row.TypeLabel,
                Name = row.Item.Name ?? "(unnamed)",
                PrimaryStat = row.Item is WeaponItem ? row.Damage.ToString() : row.Armor.ToString(),
                SpeedText = row.Item is WeaponItem ? row.Speed.ToString("0.##") : "—",
                AffixesText = $"{row.PrefixCount}/{row.SuffixCount}/{row.ActionBonusCount}",
            };
            return vm;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

