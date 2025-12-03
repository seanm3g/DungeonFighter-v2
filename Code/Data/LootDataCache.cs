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
            }
            else
            {
                UIManager.WriteLine("Error loading stat bonuses: StatBonuses.json not found", UIMessageType.System);
                StatBonuses = new List<StatBonus>();
            }
        }

        private void LoadActionBonuses()
        {
            string? filePath = JsonLoader.FindGameDataFile("Actions.json");
            if (filePath != null)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };
                var actions = JsonLoader.LoadJsonList<ActionData>(filePath, true);
                ActionBonuses = actions?.Select(a => new ActionBonus { Name = a.Name, Description = a.Description, Weight = 1 }).ToList() ?? new List<ActionBonus>();
            }
            else
            {
                UIManager.WriteLine("Error loading action bonuses: Actions.json not found", UIMessageType.System);
                ActionBonuses = new List<ActionBonus>();
            }
        }

        private void LoadModifications()
        {
            string? filePath = JsonLoader.FindGameDataFile("Modifications.json");
            if (filePath != null)
            {
                Modifications = JsonLoader.LoadJsonList<Modification>(filePath) ?? new List<Modification>();
            }
            else
            {
                UIManager.WriteLine("Error loading modifications: Modifications.json not found", UIMessageType.System);
                Modifications = new List<Modification>();
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

