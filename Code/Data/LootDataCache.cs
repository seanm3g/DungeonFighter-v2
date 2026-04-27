using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// Centralized data cache for all loot generation data
    /// Provides single source of truth for loading and caching loot-related data
    /// </summary>
    public class LootDataCache
    {
        public List<TierDistribution> TierDistributions { get; private set; }
        public List<ArmorData> ArmorData { get; private set; }
        public List<WeaponData> WeaponData { get; private set; }
        public List<StatBonus> StatBonuses { get; private set; }
        public List<ActionBonus> ActionBonuses { get; private set; }
        public List<Modification> Modifications { get; private set; }
        public List<RarityData> RarityData { get; private set; }

        private LootDataCache()
        {
            TierDistributions = new List<TierDistribution>();
            ArmorData = new List<ArmorData>();
            WeaponData = new List<WeaponData>();
            StatBonuses = new List<StatBonus>();
            ActionBonuses = new List<ActionBonus>();
            Modifications = new List<Modification>();
            RarityData = new List<RarityData>();
        }

        /// <summary>
        /// Creates an empty LootDataCache instance for testing purposes
        /// </summary>
        internal static LootDataCache CreateEmpty()
        {
            return new LootDataCache();
        }

        /// <summary>
        /// Loads all loot data from JSON files
        /// </summary>
        public static LootDataCache Load()
        {
            var cache = new LootDataCache();
            cache.LoadAll();
            return cache;
        }

        /// <summary>
        /// Reloads all data from files
        /// </summary>
        public void Reload()
        {
            LoadAll();
        }

        /// <summary>
        /// Clears all cached data
        /// </summary>
        public void Clear()
        {
            TierDistributions.Clear();
            ArmorData.Clear();
            WeaponData.Clear();
            StatBonuses.Clear();
            ActionBonuses.Clear();
            Modifications.Clear();
            RarityData.Clear();
        }

        private void LoadAll()
        {
            LoadTierDistributions();
            LoadArmorData();
            LoadWeaponData();
            LoadStatBonuses();
            LoadActionBonuses();
            LoadModifications();
            LoadRarityData();
        }

        private void LoadTierDistributions()
        {
            string? filePath = JsonLoader.FindGameDataFile("TierDistribution.json");
            if (filePath != null)
            {
                TierDistributions = JsonLoader.LoadJsonList<TierDistribution>(filePath) ?? new List<TierDistribution>();
            }
            else
            {
                UIManager.WriteLine("Error loading tier distributions: TierDistribution.json not found", UIMessageType.System);
                TierDistributions = new List<TierDistribution>();
            }
        }

        private void LoadArmorData()
        {
            string? filePath = JsonLoader.FindGameDataFile("Armor.json");
            if (filePath != null)
            {
                ArmorData = JsonLoader.LoadJsonList<ArmorData>(filePath) ?? new List<ArmorData>();
            }
            else
            {
                UIManager.WriteLine("Error loading armor data: Armor.json not found", UIMessageType.System);
                ArmorData = new List<ArmorData>();
            }
        }

        private void LoadWeaponData()
        {
            string? filePath = JsonLoader.FindGameDataFile("Weapons.json");
            if (filePath != null)
            {
                WeaponData = JsonLoader.LoadJsonList<WeaponData>(filePath) ?? new List<WeaponData>();
            }
            else
            {
                UIManager.WriteLine("Error loading weapon data: Weapons.json not found", UIMessageType.System);
                WeaponData = new List<WeaponData>();
            }
        }

        private void LoadStatBonuses()
        {
            string? filePath = JsonLoader.FindGameDataFile("StatBonuses.json");
            if (filePath != null)
            {
                StatBonuses = JsonLoader.LoadJsonList<StatBonus>(filePath) ?? new List<StatBonus>();
                foreach (var sb in StatBonuses)
                {
                    if (string.IsNullOrWhiteSpace(sb.Rarity))
                        sb.Rarity = "Common";
                }
            }
            else
            {
                UIManager.WriteLine("Error loading stat bonuses: StatBonuses.json not found", UIMessageType.System);
                StatBonuses = new List<StatBonus>();
            }
        }

        private void LoadActionBonuses()
        {
            // Use ActionLoader so format is handled in one place (legacy ActionData and spreadsheet format).
            ActionLoader.LoadActions();
            var actionDataList = ActionLoader.GetAllActionData();
            if (actionDataList != null && actionDataList.Count > 0)
            {
                ActionBonuses = actionDataList.Select(a => new ActionBonus
                {
                    Name = a.Name,
                    Description = a.Description,
                    Weight = RarityToWeight(a.Rarity)
                }).ToList();
            }
            else
            {
                UIManager.WriteLine("Error loading action bonuses: No actions loaded from Actions.json", UIMessageType.System);
                ActionBonuses = new List<ActionBonus>();
            }
        }

        /// <summary>
        /// Maps action Rarity to selection weight (higher = more likely when choosing actions for items).
        /// </summary>
        private static int RarityToWeight(string? rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity)) return 1;
            return (rarity.Trim().ToLowerInvariant()) switch
            {
                "common" => 1,
                "uncommon" => 2,
                "rare" => 3,
                "epic" => 4,
                "legendary" => 5,
                "transcendent" => 6,
                _ => 1
            };
        }

        private void LoadModifications()
        {
            Modifications = new List<Modification>();
            string? filePath = JsonLoader.FindGameDataFile("Modifications.json");
            if (filePath != null)
            {
                var core = JsonLoader.LoadJsonList<Modification>(filePath) ?? new List<Modification>();
                Modifications.AddRange(core);
            }
            else
            {
                UIManager.WriteLine("Error loading modifications: Modifications.json not found", UIMessageType.System);
            }

            string? prefixExtra = JsonLoader.FindGameDataFile("PrefixMaterialQuality.json");
            if (prefixExtra != null)
            {
                var extra = JsonLoader.LoadJsonList<Modification>(prefixExtra) ?? new List<Modification>();
                Modifications.AddRange(extra);
            }
        }

        private void LoadRarityData()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("RarityTable.json");
                if (filePath != null)
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    RarityData = JsonSerializer.Deserialize<List<RarityData>>(json, options) ?? new List<RarityData>();
                    // Normalize rarity names to remove any whitespace
                    foreach (var rarity in RarityData)
                    {
                        rarity.Name = rarity.Name?.Trim() ?? "";
                    }
                }
                else
                {
                    Console.WriteLine("Error loading rarity data: RarityTable.json not found");
                    RarityData = new List<RarityData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading rarity data: {ex.Message}");
                RarityData = new List<RarityData>();
            }
        }
    }
}

