using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Responsible for generating and spawning enemies in an environment.
    /// Handles enemy loading, creation, and level scaling.
    /// </summary>
    public class EnemyGenerationManager
    {
        private readonly string theme;
        private readonly bool isHostile;
        private readonly Random random;
        private List<Enemy> enemies;

        public EnemyGenerationManager(string theme, bool isHostile)
        {
            this.theme = theme;
            this.isHostile = isHostile;
            this.random = new Random();
            this.enemies = new List<Enemy>();
        }

        /// <summary>
        /// Gets all enemies in this environment.
        /// </summary>
        public List<Enemy> Enemies => enemies;

        /// <summary>
        /// Checks if the environment has any living enemies.
        /// </summary>
        public bool HasLivingEnemies => enemies.Any(e => e.IsAlive);

        /// <summary>
        /// Gets the next living enemy, or null if none remain.
        /// </summary>
        public Enemy? GetNextLivingEnemy => enemies.FirstOrDefault(e => e.IsAlive);

        /// <summary>
        /// Removes dead enemies from the enemies list.
        /// This ensures dead enemies don't persist and cause issues during room transitions.
        /// </summary>
        public void RemoveDeadEnemies()
        {
            enemies.RemoveAll(e => !e.IsAlive);
        }

        /// <summary>
        /// Generates enemies for this environment based on room level.
        /// </summary>
        public void GenerateEnemies(int roomLevel, List<string>? possibleEnemies = null, int? minLevel = null, int? maxLevel = null)
        {
            if (!isHostile)
                return;

            enemies.Clear();
            int enemyCount = Math.Max(1, (int)Math.Ceiling(roomLevel / 2.0));

            // Try to load enemy data from JSON first
            var jsonEnemies = LoadEnemyDataFromJson();
            if (jsonEnemies != null && jsonEnemies.Count > 0)
            {
                GenerateEnemiesFromJson(roomLevel, enemyCount, jsonEnemies, possibleEnemies, minLevel, maxLevel);
                return;
            }

            // Fallback: Create basic enemies if JSON loading fails
            BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse("Warning: Could not load enemy data from JSON, creating basic enemies"));
            var tuning = GameConfiguration.Instance;
            var basicEnemies = new[] {
                new { Name = "Basic Enemy", BaseHealth = 80, BaseStrength = 8, BaseAgility = 6, BaseTechnique = 4, BaseIntelligence = 3, Primary = PrimaryAttribute.Strength }
            };

            for (int i = 0; i < enemyCount; i++)
            {
                int enemyLevel = CalculateEnemyLevel(roomLevel, tuning.EnemySystem.LevelVariance, minLevel, maxLevel);
                var enemyType = basicEnemies[random.Next(basicEnemies.Length)];

                var calculatedStats = new
                {
                    Health = enemyType.BaseHealth + (enemyLevel - 1) * tuning.Character.EnemyHealthPerLevel,
                    Strength = enemyType.BaseStrength + (enemyLevel - 1) * tuning.Attributes.EnemyAttributesPerLevel,
                    Agility = enemyType.BaseAgility + (enemyLevel - 1) * tuning.Attributes.EnemyAttributesPerLevel,
                    Technique = enemyType.BaseTechnique + (enemyLevel - 1) * tuning.Attributes.EnemyAttributesPerLevel,
                    Intelligence = enemyType.BaseIntelligence + (enemyLevel - 1) * tuning.Attributes.EnemyAttributesPerLevel,
                    Armor = (int)(0 + (enemyLevel - 1) * tuning.EnemyScaling.EnemyArmorPerLevel)
                };

                var enemy = new Enemy(
                    enemyType.Name,
                    enemyLevel,
                    calculatedStats.Health,
                    calculatedStats.Strength,
                    calculatedStats.Agility,
                    calculatedStats.Technique,
                    calculatedStats.Intelligence,
                    calculatedStats.Armor,
                    enemyType.Primary,
                    true, // isLiving
                    EnemyArchetype.Berserker // Default archetype
                );
                enemies.Add(enemy);
            }
        }

        private List<EnemyData>? LoadEnemyDataFromJson()
        {
            try
            {
                string[] possiblePaths = {
                    Path.Combine("GameData", "Enemies.json"),
                    Path.Combine("..", "GameData", "Enemies.json"),
                    Path.Combine("..", "..", "GameData", "Enemies.json"),
                    Path.Combine("DF4 - CONSOLE", "GameData", "Enemies.json"),
                    Path.Combine("..", "DF4 - CONSOLE", "GameData", "Enemies.json")
                };

                string? foundPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    var loadedEnemies = System.Text.Json.JsonSerializer.Deserialize<List<EnemyData>>(jsonContent);
                    return loadedEnemies;
                }
                else
                {
                    BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse($"Warning: Enemies.json not found. Tried paths: {string.Join(", ", possiblePaths)}"));
                }
            }
            catch (Exception ex)
            {
                BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse($"Error loading enemy data from JSON: {ex.Message}"));
            }
            return null;
        }

        private void GenerateEnemiesFromJson(int roomLevel, int enemyCount, List<EnemyData> enemyData, List<string>? possibleEnemies = null, int? minLevel = null, int? maxLevel = null)
        {
            var tuning = GameConfiguration.Instance;

            // Filter enemies by possible enemies list first, then by theme
            List<EnemyData> availableEnemies;
            if (possibleEnemies != null && possibleEnemies.Count > 0)
            {
                availableEnemies = enemyData.Where(e => possibleEnemies.Contains(e.Name)).ToList();
                if (availableEnemies.Count == 0)
                {
                    availableEnemies = GetThemeAppropriateEnemies(enemyData);
                }
            }
            else
            {
                var themeEnemies = GetThemeAppropriateEnemies(enemyData);
                availableEnemies = themeEnemies.Count > 0 ? themeEnemies : enemyData;
            }

            if (availableEnemies.Count == 0)
            {
                BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse($"Warning: No theme-appropriate enemies found for theme '{theme}', using all available enemies"));
                availableEnemies = enemyData;
            }

            if (availableEnemies.Count == 0)
            {
                BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse("Error: No enemy data available, cannot generate enemies"));
                return;
            }

            for (int i = 0; i < enemyCount; i++)
            {
                int enemyLevel = CalculateEnemyLevel(roomLevel, tuning.EnemySystem.LevelVariance, minLevel, maxLevel);
                var enemyTemplate = availableEnemies[random.Next(availableEnemies.Count)];

                var enemy = EnemyLoader.CreateEnemy(enemyTemplate.Name, enemyLevel);
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
                else
                {
                    BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse($"Warning: Could not create enemy {enemyTemplate.Name} from EnemyLoader, creating basic enemy"));
                    var basicEnemy = new Enemy(
                        enemyTemplate.Name,
                        enemyLevel,
                        80 + (enemyLevel * tuning.Character.EnemyHealthPerLevel),
                        3,
                        3,
                        3,
                        3,
                        2,
                        PrimaryAttribute.Strength,
                        true,
                        EnemyArchetype.Berserker
                    );
                    enemies.Add(basicEnemy);
                }
            }
        }

        /// <summary>
        /// Calculates enemy level with variance, clamped to dungeon level bounds if provided.
        /// </summary>
        private int CalculateEnemyLevel(int roomLevel, int levelVariance, int? minLevel, int? maxLevel)
        {
            // Calculate base level with variance
            int enemyLevel = Math.Max(1, roomLevel + random.Next(-levelVariance, levelVariance + 1));
            
            // Clamp to dungeon bounds if provided
            if (minLevel.HasValue)
            {
                enemyLevel = Math.Max(enemyLevel, minLevel.Value);
            }
            if (maxLevel.HasValue)
            {
                enemyLevel = Math.Min(enemyLevel, maxLevel.Value);
            }
            
            return enemyLevel;
        }

        private List<EnemyData> GetThemeAppropriateEnemies(List<EnemyData> allEnemies)
        {
            var themeEnemyMap = new Dictionary<string, string[]>
            {
                ["Forest"] = new[] { "Goblin", "Spider", "Wolf", "Bear", "Treant" },
                ["Lava"] = new[] { "Wraith", "Slime", "Bat", "Fire Elemental", "Lava Golem", "Salamander" },
                ["Crypt"] = new[] { "Skeleton", "Zombie", "Wraith", "Lich", "Ghoul", "Wight" },
                ["Crystal"] = new[] { "Crystal Golem", "Prism Spider", "Shard Beast", "Crystal Sprite", "Geode Beast", "Crystal Wyrm" },
                ["Temple"] = new[] { "Stone Guardian", "Temple Warden", "Ancient Sentinel", "Temple Guard", "Priest", "Paladin" },
                ["Generic"] = new[] { "Bandit", "Orc", "Troll", "Kobold", "Goblin" },
                ["Ice"] = new[] { "Ice Elemental", "Frost Wolf", "Yeti", "Ice Golem", "Frozen Wraith", "Blizzard Beast" },
                ["Shadow"] = new[] { "Shadow Stalker", "Dark Mage", "Void Walker", "Shadow Beast", "Nightmare", "Umbra" },
                ["Steampunk"] = new[] { "Steam Golem", "Clockwork Soldier", "Mechanical Spider", "Gear Beast", "Steam Knight", "Automaton" },
                ["Swamp"] = new[] { "Swamp Troll", "Poison Frog", "Bog Witch", "Marsh Serpent", "Toxic Slime", "Venomous Spider" },
                ["Astral"] = new[] { "Star Guardian", "Cosmic Wisp", "Astral Mage", "Nebula Beast", "Comet Spirit", "Galaxy Walker" },
                ["Underground"] = new[] { "Deep Dwarf", "Cave Troll", "Underground Rat", "Mole Beast", "Tunnel Worm", "Subterranean Guard" },
                ["Storm"] = new[] { "Storm Elemental", "Lightning Bird", "Thunder Giant", "Wind Spirit", "Tempest Beast", "Hurricane Wraith" },
                ["Nature"] = new[] { "Nature Spirit", "Flower Guardian", "Vine Beast", "Garden Sprite", "Thorn Wolf", "Bloom Elemental" },
                ["Arcane"] = new[] { "Arcane Scholar", "Book Golem", "Knowledge Spirit", "Scroll Guardian", "Tome Beast", "Librarian Wraith" },
                ["Desert"] = new[] { "Sand Elemental", "Desert Nomad", "Cactus Beast", "Oasis Spirit", "Sand Worm", "Mirage Phantom" },
                ["Volcano"] = new[] { "Magma Beast", "Volcano Spirit", "Ash Elemental", "Lava Serpent", "Ember Golem", "Pyroclast" },
                ["Ruins"] = new[] { "Skeleton", "Zombie", "Wraith", "Lich", "Stone Guardian", "Temple Warden" },
                ["Ocean"] = new[] { "Sea Serpent", "Kraken Spawn", "Deep Sea Beast", "Ocean Spirit", "Coral Guardian", "Abyssal Walker" },
                ["Mountain"] = new[] { "Mountain Giant", "Eagle Spirit", "Rock Elemental", "Summit Guardian", "Peak Beast", "Altitude Wraith" },
                ["Temporal"] = new[] { "Time Wraith", "Chronos Beast", "Echo Spirit", "Paradox Guardian", "Temporal Mage", "Time Elemental" },
                ["Dream"] = new[] { "Dream Walker", "Sleep Spirit", "Lucid Beast", "Nightmare Lord", "Dream Guardian", "Sleep Paralysis Demon" },
                ["Void"] = new[] { "Void Beast", "Null Actor", "Void Walker", "Empty Spirit", "Void Guardian", "Null Wraith" },
                ["Dimensional"] = new[] { "Dimension Walker", "Portal Guardian", "Reality Beast", "Multiverse Spirit", "Plane Walker", "Dimension Mage" },
                ["Divine"] = new[] { "Divine Guardian", "Celestial Being", "Angel Warrior", "Heavenly Spirit", "Divine Beast", "Sacred Guardian" }
            };

            if (themeEnemyMap.TryGetValue(theme, out var themeEnemyNames))
            {
                return allEnemies.Where(e => themeEnemyNames.Contains(e.Name)).ToList();
            }

            return allEnemies;
        }
    }
}

