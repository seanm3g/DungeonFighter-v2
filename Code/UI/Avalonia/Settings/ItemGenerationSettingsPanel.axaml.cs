using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
            RefreshHeroLevelLootPreviews();
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
                rarityCombo.ItemsSource = new[] { "Any", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
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
            var heroLevel = this.FindControl<TextBox>("HeroLevelTextBox");
            var dungeonDelta = this.FindControl<TextBox>("DungeonDeltaTextBox");
            var rarityCombo = this.FindControl<ComboBox>("RarityCombo");
            var baseRatio = this.FindControl<TextBox>("BaseRatioTextBox");
            var fixedC = this.FindControl<TextBox>("FixedChanceCommon");
            var fixedU = this.FindControl<TextBox>("FixedChanceUncommon");
            var fixedR = this.FindControl<TextBox>("FixedChanceRare");
            var fixedE = this.FindControl<TextBox>("FixedChanceEpic");
            var fixedL = this.FindControl<TextBox>("FixedChanceLegendary");
            var fixedM = this.FindControl<TextBox>("FixedChanceMythic");

            if (generateButton != null)
                generateButton.Click += (_, __) => Generate();
            if (clearButton != null)
                clearButton.Click += (_, __) => Clear();
            if (list != null)
                list.SelectionChanged += (_, __) => UpdateSelectedDetails(list.SelectedItem as ItemGeneratedRowViewModel);
            if (heroLevel != null)
                heroLevel.TextChanged += (_, __) => RefreshHeroLevelLootPreviews();
            if (dungeonDelta != null)
                dungeonDelta.TextChanged += (_, __) => RefreshHeroLevelLootPreviews();
            if (baseRatio != null)
                baseRatio.TextChanged += (_, __) =>
                {
                    TryApplyBaseRatioToFixedChanceBoxes();
                    RefreshHeroLevelLootPreviews();
                };
            if (fixedC != null) fixedC.TextChanged += (_, __) => RefreshHeroLevelLootPreviews();
            if (fixedU != null) fixedU.TextChanged += (_, __) => RefreshHeroLevelLootPreviews();
            if (fixedR != null) fixedR.TextChanged += (_, __) => RefreshHeroLevelLootPreviews();
            if (fixedE != null) fixedE.TextChanged += (_, __) => RefreshHeroLevelLootPreviews();
            if (fixedL != null) fixedL.TextChanged += (_, __) => RefreshHeroLevelLootPreviews();
            if (fixedM != null) fixedM.TextChanged += (_, __) => RefreshHeroLevelLootPreviews();
            if (rarityCombo != null)
                rarityCombo.SelectionChanged += (_, __) => RefreshHeroLevelLootPreviews();
            var tierCombo = this.FindControl<ComboBox>("TierCombo");
            if (tierCombo != null)
                tierCombo.SelectionChanged += (_, __) => RefreshHeroLevelLootPreviews();
            Loaded += (_, __) => RefreshHeroLevelLootPreviews();
        }

        /// <summary>
        /// Rarity line: same base weights as <see cref="LootRarityProcessor.RollRarity"/> when Rarity is Any.
        /// Tier lines: <see cref="LootTierCalculator.GetTierRollPreview"/> at loot level (hero vs dungeon 1), matching <see cref="LootGenerator"/>; lab caveat when Tier is Any.
        /// </summary>
        private void RefreshHeroLevelLootPreviews()
        {
            TryApplyBaseRatioToFixedChanceBoxes();

            var rarityPreview = this.FindControl<TextBlock>("HeroLevelRarityPreviewText");
            var fixedSummary = this.FindControl<TextBlock>("FixedChanceSummaryText");
            if (rarityPreview == null)
                return;

            var rarityCombo = this.FindControl<ComboBox>("RarityCombo");
            string rv = rarityCombo?.SelectedItem?.ToString() ?? "Any";
            if (!string.Equals(rv, "Any", StringComparison.OrdinalIgnoreCase))
            {
                rarityPreview.Text = "Rarity is not Any — rolls use the chosen rarity; hero level is not used for rarity.";
            }
            else
            {
                int level = ReadHeroLevel();
                try
                {
                    var cache = LootDataCache.Load();
                    var rows = LootRarityProcessor.GetBaseRollDistribution(cache, level);
                    if (rows.Count == 0)
                    {
                        rarityPreview.Text = "No rarity table loaded.";
                    }
                    else
                    {
                        string pct(double p) => p.ToString("0.##", CultureInfo.InvariantCulture) + "%";
                        string line = "When Rarity is Any, each item’s first rarity roll: " +
                                       string.Join(" · ", rows.Select(t => $"{t.Name} {pct(t.ProbabilityPercent)}"));
                        bool upgrades = GameConfiguration.Instance?.LootSystem?.RarityUpgrade?.Enabled ?? false;
                        string tail = upgrades
                            ? " Rarity upgrade (TuningConfig): On — final rarity can step up from this base roll."
                            : " Rarity upgrade (TuningConfig): Off.";
                        rarityPreview.Text = line + tail;
                    }
                }
                catch
                {
                    rarityPreview.Text = "(Could not load rarity preview.)";
                }
            }

            // Fixed rarity summary (used by the lab when Rarity is Any)
            if (fixedSummary != null)
            {
                var fixedMap = ReadFixedRarityChancesPercent();
                double sum = fixedMap.Values.Sum();
                string pct(double p) => p.ToString("0.##", CultureInfo.InvariantCulture) + "%";
                fixedSummary.Text = "Sum " + pct(sum) + " (auto-normalized). " +
                                    $"C {pct(fixedMap["Common"])} · U {pct(fixedMap["Uncommon"])} · R {pct(fixedMap["Rare"])} · E {pct(fixedMap["Epic"])} · L {pct(fixedMap["Legendary"])} · M {pct(fixedMap["Mythic"])}";
            }

            int heroLevel = ReadHeroLevel();
            int dungeonDelta = ReadDungeonDelta();
            int dungeonLevel = DungeonLevelMath.ResolveEffectiveDungeonLevel(heroLevel, dungeonDelta);
            var tierCombo = this.FindControl<ComboBox>("TierCombo");
            string tierChoice = tierCombo?.SelectedItem?.ToString() ?? "Any";
            bool tierIsAny = string.Equals(tierChoice, "Any", StringComparison.OrdinalIgnoreCase);

            var title = this.FindControl<TextBlock>("TierTableTitleText");
            var subtitle = this.FindControl<TextBlock>("TierTableSubtitleText");
            var p1 = this.FindControl<TextBlock>("TierPct1Text");
            var p2 = this.FindControl<TextBlock>("TierPct2Text");
            var p3 = this.FindControl<TextBlock>("TierPct3Text");
            var p4 = this.FindControl<TextBlock>("TierPct4Text");
            var p5 = this.FindControl<TextBlock>("TierPct5Text");
            var note = this.FindControl<TextBlock>("TierTableNoteText");

            if (title == null || subtitle == null || p1 == null || p2 == null || p3 == null || p4 == null || p5 == null || note == null)
                return;

            try
            {
                var cache = LootDataCache.Load();
                var (lootLevel, tierRows) = LootTierCalculator.GetTierRollPreview(cache, heroLevel, dungeonLevel);
                title.Text = "Live loot tier chances";
                subtitle.Text = $"Loot level {lootLevel} (hero {heroLevel} vs dungeon {dungeonLevel} from Δ {dungeonDelta:+#;-#;0})";

                if (tierRows == null || tierRows.Count == 0)
                {
                    p1.Text = p2.Text = p3.Text = p4.Text = p5.Text = "—";
                    note.Text = "No tier distribution row found in TierDistribution.json.";
                    return;
                }

                string pct(double v) => v.ToString("0.##", CultureInfo.InvariantCulture) + "%";
                double v1 = tierRows.FirstOrDefault(t => t.Tier == 1).ProbabilityPercent;
                double v2 = tierRows.FirstOrDefault(t => t.Tier == 2).ProbabilityPercent;
                double v3 = tierRows.FirstOrDefault(t => t.Tier == 3).ProbabilityPercent;
                double v4 = tierRows.FirstOrDefault(t => t.Tier == 4).ProbabilityPercent;
                double v5 = tierRows.FirstOrDefault(t => t.Tier == 5).ProbabilityPercent;

                p1.Text = pct(v1);
                p2.Text = pct(v2);
                p3.Text = pct(v3);
                p4.Text = pct(v4);
                p5.Text = pct(v5);

                note.Text = tierIsAny
                    ? "When Tier is Any, each item uses these tier chances (same as LootGenerator)."
                    : "Your Tier filter fixes tier for this batch.";
            }
            catch
            {
                p1.Text = p2.Text = p3.Text = p4.Text = p5.Text = "—";
                subtitle.Text = "";
                note.Text = "(Could not load tier preview.)";
            }
        }

        private void Generate()
        {
            int heroLevel = ReadHeroLevel();
            int dungeonDelta = ReadDungeonDelta();
            int dungeonLevel = DungeonLevelMath.ResolveEffectiveDungeonLevel(heroLevel, dungeonDelta);

            var spec = new ItemGenerationSpec
            {
                ItemType = ReadItemType(),
                Rarity = ReadRarity(),
                Tier = ReadTier(),
                WeaponType = ReadWeaponType(),
                ArmorSlot = ReadArmorSlot(),
                PlayerLevel = heroLevel,
                DungeonLevel = dungeonLevel,
                FixedRarityChancesPercent = ReadFixedRarityChancesPercent(),
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

            UpdateBatchStats(batch);
        }

        private void Clear()
        {
            rows.Clear();
            UpdateSelectedDetails(null);
            UpdateBatchStats(null);
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

        private void UpdateBatchStats(IReadOnlyList<ItemGeneratedRow>? batch)
        {
            var tb = this.FindControl<TextBlock>("BatchStatsText");
            if (tb == null) return;

            var stats = ItemGenerationBatchStatistics.Compute(batch ?? Array.Empty<ItemGeneratedRow>());
            tb.Text = stats.ToDisplayText();
        }

        private int ReadCount()
        {
            var tb = this.FindControl<TextBox>("CountTextBox");
            if (tb == null) return 100;
            string raw = (tb.Text ?? "").Trim();
            if (raw.Length == 0)
                return 100;

            // Ergonomics: allow "1,000,000" style input.
            raw = raw.Replace(",", "");

            if (int.TryParse(raw, out int n))
                return Math.Clamp(n, 1, 1_000_000);
            return 100;
        }

        private int ReadHeroLevel()
        {
            var tb = this.FindControl<TextBox>("HeroLevelTextBox");
            if (tb == null) return 1;
            if (int.TryParse((tb.Text ?? "").Trim(), out int n))
                return Math.Clamp(n, 1, 99);
            return 1;
        }

        private int ReadDungeonLevel()
        {
            return DungeonLevelMath.ResolveEffectiveDungeonLevel(ReadHeroLevel(), ReadDungeonDelta());
        }

        private int ReadDungeonDelta()
        {
            var tb = this.FindControl<TextBox>("DungeonDeltaTextBox");
            if (tb == null) return 0;

            string text = (tb.Text ?? "").Trim();
            if (text.Length == 0)
                return 0;

            // Allow leading '+' for ergonomics: "+5"
            if (text.StartsWith("+", StringComparison.Ordinal))
                text = text.Substring(1).Trim();

            if (int.TryParse(text, out int n))
                return DungeonLevelMath.ClampDungeonDelta(ReadHeroLevel(), n);
            return 0;
        }

        private Dictionary<string, double> ReadFixedRarityChancesPercent()
        {
            var byRatio = TryReadBaseRatio();
            if (byRatio != null)
                return byRatio;

            double read(string name, double fallback)
            {
                var tb = this.FindControl<TextBox>(name);
                if (tb == null) return fallback;
                if (double.TryParse((tb.Text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                    return Math.Clamp(v, 0.0, 100.0);
                if (double.TryParse((tb.Text ?? "").Trim(), out v))
                    return Math.Clamp(v, 0.0, 100.0);
                return fallback;
            }

            // Defaults match the current preview’s level-1 base distribution (good starting point),
            // but these are fixed for the lab and ignore level gating.
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["Common"] = read("FixedChanceCommon", 87.87),
                ["Uncommon"] = read("FixedChanceUncommon", 8.79),
                ["Rare"] = read("FixedChanceRare", 2.64),
                ["Epic"] = read("FixedChanceEpic", 0.53),
                ["Legendary"] = read("FixedChanceLegendary", 0.18),
                ["Mythic"] = read("FixedChanceMythic", 0.02),
            };
        }

        private Dictionary<string, double>? TryReadBaseRatio()
        {
            var tb = this.FindControl<TextBox>("BaseRatioTextBox");
            if (tb == null)
                return null;

            string text = (tb.Text ?? "").Trim();
            if (text.Length == 0)
                return null; // blank means manual % mode

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio) ||
                double.TryParse(text, out ratio))
            {
                if (ratio <= 1.0)
                    return null;
                return RarityChanceMath.BuildGeometricRarityChancesPercent(ratio);
            }

            return null;
        }

        private void TryApplyBaseRatioToFixedChanceBoxes()
        {
            var map = TryReadBaseRatio();
            if (map == null)
                return;

            void set(string name, string key)
            {
                var tb = this.FindControl<TextBox>(name);
                if (tb == null) return;
                if (!map.TryGetValue(key, out double v)) return;
                tb.Text = v.ToString("0.##", CultureInfo.InvariantCulture);
            }

            set("FixedChanceCommon", "Common");
            set("FixedChanceUncommon", "Uncommon");
            set("FixedChanceRare", "Rare");
            set("FixedChanceEpic", "Epic");
            set("FixedChanceLegendary", "Legendary");
            set("FixedChanceMythic", "Mythic");
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
                    lines.Add($"Prefix: {string.Join(", ", _item.Modifications.Select(m => m.Name))}");
                if (_item.StatBonuses != null && _item.StatBonuses.Count > 0)
                    lines.Add($"Suffix: {string.Join(", ", _item.StatBonuses.Select(s => s.Name))}");
                if (_item.ActionBonuses != null && _item.ActionBonuses.Count > 0)
                    lines.Add($"Actions: {string.Join(", ", _item.ActionBonuses.Select(a => a.Name))}");

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

