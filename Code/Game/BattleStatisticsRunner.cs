using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Combat;
using RPGGame.Entity;
using RPGGame.World;
using RPGGame.Utils;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Runs parallel battles for statistical analysis on different stat combinations
    /// </summary>
    public class BattleStatisticsRunner
    {
        public class BattleResult
        {
            public bool PlayerWon { get; set; }
            public int Turns { get; set; }
            public int PlayerDamageDealt { get; set; }
            public int EnemyDamageDealt { get; set; }
            public int PlayerFinalHealth { get; set; }
            public int EnemyFinalHealth { get; set; }
            public string? ErrorMessage { get; set; }
            public List<CombatTurnLog>? TurnLogs { get; set; } // Optional turn-by-turn logs
            public Dictionary<string, int>? ActionUsageCount { get; set; } // Optional action usage stats
            public FunMomentTracker.FunMomentSummary? FunMomentSummary { get; set; } // Fun moment tracking data
        }

        public class BattleConfiguration
        {
            public int PlayerDamage { get; set; }
            public double PlayerAttackSpeed { get; set; }
            public int PlayerArmor { get; set; }
            public int PlayerHealth { get; set; }
            
            public int EnemyDamage { get; set; }
            public double EnemyAttackSpeed { get; set; }
            public int EnemyArmor { get; set; }
            public int EnemyHealth { get; set; }
        }

        public class StatisticsResult
        {
            public BattleConfiguration Config { get; set; } = new();
            public int TotalBattles { get; set; }
            public int PlayerWins { get; set; }
            public int EnemyWins { get; set; }
            public double WinRate { get; set; }
            public double AverageTurns { get; set; }
            public double AveragePlayerDamageDealt { get; set; }
            public double AverageEnemyDamageDealt { get; set; }
            public int MinTurns { get; set; }
            public int MaxTurns { get; set; }
            public List<BattleResult> BattleResults { get; set; } = new();
        }

        /// <summary>
        /// Runs parallel battles for a given configuration
        /// </summary>
        public static async Task<StatisticsResult> RunParallelBattles(
            BattleConfiguration config, 
            int numberOfBattles = 100,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var result = new StatisticsResult
            {
                Config = config,
                TotalBattles = numberOfBattles
            };

            // Disable UI output for faster execution
            var originalDisableFlag = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;

            try
            {
                var battleTasks = new List<Task<BattleResult>>();
                var semaphore = new System.Threading.SemaphoreSlim(System.Environment.ProcessorCount * 2, System.Environment.ProcessorCount * 2);

                for (int i = 0; i < numberOfBattles; i++)
                {
                    int battleIndex = i;
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var battleResult = await RunSingleBattle(config, battleIndex);
                            progress?.Report((battleIndex + 1, numberOfBattles, $"Battle {battleIndex + 1}/{numberOfBattles}"));
                            return battleResult;
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue
                            var errorResult = new BattleResult
                            {
                                ErrorMessage = $"Task exception: {ex.GetType().Name}: {ex.Message}",
                                PlayerWon = false,
                                Turns = 0
                            };
                            progress?.Report((battleIndex + 1, numberOfBattles, $"Battle {battleIndex + 1}/{numberOfBattles} (Error)"));
                            return errorResult;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                    battleTasks.Add(task);
                }

                // Wait for all tasks with a timeout
                var allTasks = Task.WhenAll(battleTasks);
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10)); // 10 minute overall timeout
                var completedTask = await Task.WhenAny(allTasks, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    // Timeout occurred - collect what we have
                    var completedResults = new List<BattleResult>();
                    for (int i = 0; i < battleTasks.Count; i++)
                    {
                        if (battleTasks[i].IsCompleted)
                        {
                            try
                            {
                                completedResults.Add(await battleTasks[i]);
                            }
                            catch
                            {
                                completedResults.Add(new BattleResult { ErrorMessage = "Task failed", PlayerWon = false });
                            }
                        }
                        else
                        {
                            completedResults.Add(new BattleResult { ErrorMessage = "Task timed out", PlayerWon = false });
                        }
                    }
                    result.BattleResults = completedResults;
                }
                else
                {
                    var results = await allTasks;
                    result.BattleResults = results.ToList();
                }

                // Calculate statistics
                CalculateStatistics(result);
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = originalDisableFlag;
            }

            return result;
        }

        /// <summary>
        /// Runs a single battle with the given configuration
        /// Uses real game systems (weapons, enemies, actions) and adjusts stats to match configuration
        /// </summary>
        private static async Task<BattleResult> RunSingleBattle(BattleConfiguration config, int battleIndex)
        {
            var result = new BattleResult();
            
            try
            {
                // Create player with real weapon and game systems, then adjust stats
                var player = CreateTestCharacter(
                    name: $"TestPlayer_{battleIndex}",
                    damage: config.PlayerDamage,
                    attackSpeed: config.PlayerAttackSpeed,
                    armor: config.PlayerArmor,
                    health: config.PlayerHealth
                );

                // Use a real enemy from game data for proper actions/weapons, but create with direct stats
                // to match the configuration. We'll use a real enemy's name and archetype if available.
                Enemy enemy;
                var allEnemyTypes = EnemyLoader.GetAllEnemyTypes();
                
                if (allEnemyTypes.Count > 0)
                {
                    // Get a real enemy to use its name and archetype
                    var enemyType = allEnemyTypes[0];
                    var enemyData = EnemyLoader.GetEnemyData(enemyType);
                    
                    if (enemyData != null)
                    {
                        // Parse archetype
                        EnemyArchetype archetype = EnemyArchetype.Berserker;
                        if (Enum.TryParse<EnemyArchetype>(enemyData.Archetype, true, out var parsedArchetype))
                        {
                            archetype = parsedArchetype;
                        }
                        
                        // Create enemy with direct stats but use real enemy's name and archetype
                        enemy = new Enemy(
                            name: enemyData.Name,
                            level: 1,
                            maxHealth: config.EnemyHealth,
                            damage: config.EnemyDamage,
                            armor: config.EnemyArmor,
                            attackSpeed: config.EnemyAttackSpeed,
                            primaryAttribute: PrimaryAttribute.Strength,
                            isLiving: enemyData.IsLiving,
                            archetype: archetype,
                            useDirectStats: true
                        );
                        
                        // Add real enemy's weapon and actions if available
                        var realEnemy = EnemyLoader.CreateEnemy(enemyType, level: 1);
                        if (realEnemy != null && realEnemy.Weapon != null)
                        {
                            enemy.Weapon = realEnemy.Weapon;
                        }
                        // Note: Actions are set in constructor, but enemy will have default actions
                    }
                    else
                    {
                        // Fallback: create enemy with direct stats
                        enemy = new Enemy(
                            name: $"TestEnemy_{battleIndex}",
                            level: 1,
                            maxHealth: config.EnemyHealth,
                            damage: config.EnemyDamage,
                            armor: config.EnemyArmor,
                            attackSpeed: config.EnemyAttackSpeed,
                            primaryAttribute: PrimaryAttribute.Strength,
                            isLiving: true,
                            archetype: EnemyArchetype.Berserker,
                            useDirectStats: true
                        );
                    }
                }
                else
                {
                    // Fallback: create enemy with direct stats if no enemies loaded
                    enemy = new Enemy(
                        name: $"TestEnemy_{battleIndex}",
                        level: 1,
                        maxHealth: config.EnemyHealth,
                        damage: config.EnemyDamage,
                        armor: config.EnemyArmor,
                        attackSpeed: config.EnemyAttackSpeed,
                        primaryAttribute: PrimaryAttribute.Strength,
                        isLiving: true,
                        archetype: EnemyArchetype.Berserker,
                        useDirectStats: true
                    );
                }

                // Create a simple non-hostile environment for testing
                var environment = new Environment(
                    name: "Test Room",
                    description: "Testing environment",
                    isHostile: false,
                    theme: "neutral"
                );

                // Track initial health for damage calculation
                int initialPlayerHealth = player.CurrentHealth;
                int initialEnemyHealth = enemy.CurrentHealth;
                int turnCount = 0;

                // Run combat with timeout (30 seconds max per battle)
                var combatManager = new CombatManager();
                var combatTask = combatManager.RunCombat(player, enemy, environment);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                
                var completedTask = await Task.WhenAny(combatTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    result.ErrorMessage = "Battle timed out after 30 seconds";
                    result.PlayerWon = false;
                    result.Turns = 0;
                    return result;
                }

                bool playerWon = await combatTask;

                // Calculate results using accurate data from battle narrative
                result.PlayerWon = playerWon;
                result.PlayerFinalHealth = player.CurrentHealth;
                result.EnemyFinalHealth = enemy.CurrentHealth;
                
                // Get accurate statistics from battle narrative and turn manager
                var narrative = combatManager.GetCurrentBattleNarrative();
                int currentTurn = combatManager.GetCurrentTurn();
                int totalActionCount = combatManager.GetTotalActionCount();
                var funTracker = combatManager.GetFunMomentTracker();
                
                if (narrative != null)
                {
                    var events = narrative.GetAllEvents();
                    
                    // Calculate damage from actual events (more accurate than health difference)
                    // Sum all damage dealt by player to enemy
                    result.PlayerDamageDealt = events
                        .Where(e => e.Actor == player.Name && e.Target == enemy.Name && e.Damage > 0)
                        .Sum(e => e.Damage);
                    
                    // Sum all damage dealt by enemy to player
                    result.EnemyDamageDealt = events
                        .Where(e => e.Actor == enemy.Name && e.Target == player.Name && e.Damage > 0)
                        .Sum(e => e.Damage);
                    
                    // Use current turn number from TurnManager (turns increment every 10 actions)
                    // If no actions were recorded, calculate from action count: (actionCount + 9) / 10
                    if (currentTurn > 0)
                    {
                        turnCount = currentTurn;
                    }
                    else if (totalActionCount > 0)
                    {
                        // Calculate turns from action count: turns increment every 10 actions
                        // Round up: (actionCount + 9) / 10
                        turnCount = (totalActionCount + 9) / 10;
                    }
                    else
                    {
                        // Fallback: count all events where player or enemy acted (including misses)
                        var playerActions = events.Count(e => e.Actor == player.Name && e.Target == enemy.Name);
                        var enemyActions = events.Count(e => e.Actor == enemy.Name && e.Target == player.Name);
                        int totalActions = playerActions + enemyActions;
                        turnCount = Math.Max(1, (totalActions + 9) / 10);
                    }
                    
                    // Collect turn-by-turn logs if requested (for enhanced analysis)
                    // This can be enabled via a flag in BattleConfiguration
                    result.TurnLogs = BuildTurnLogs(events, player.Name, enemy.Name);
                    result.ActionUsageCount = BuildActionUsageStats(events, player.Name);
                    
                    // Collect fun moment summary
                    if (funTracker != null)
                    {
                        result.FunMomentSummary = funTracker.GetSummary();
                    }
                }
                else
                {
                    // Fallback: use health difference if narrative not available
                    result.PlayerDamageDealt = Math.Max(0, initialEnemyHealth - enemy.CurrentHealth);
                    result.EnemyDamageDealt = Math.Max(0, initialPlayerHealth - player.CurrentHealth);
                    
                    // Use current turn number if available, otherwise calculate from action count
                    if (currentTurn > 0)
                    {
                        turnCount = currentTurn;
                    }
                    else if (totalActionCount > 0)
                    {
                        // Calculate turns from action count: turns increment every 10 actions
                        // Round up: (actionCount + 9) / 10
                        turnCount = (totalActionCount + 9) / 10;
                    }
                    else
                    {
                        // Fallback: estimate turns from damage
                        int totalDamageDealt = result.PlayerDamageDealt + result.EnemyDamageDealt;
                        int averageDamagePerAction = Math.Max(1, (config.PlayerDamage + config.EnemyDamage) / 2);
                        int estimatedActions = Math.Max(1, (int)Math.Ceiling((double)totalDamageDealt / averageDamagePerAction));
                        turnCount = Math.Max(1, (estimatedActions + 9) / 10);
                    }
                }
                result.Turns = turnCount;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Exception: {ex.GetType().Name}: {ex.Message}";
                result.PlayerWon = false;
                result.Turns = 0;
            }

            return result;
        }

        /// <summary>
        /// Builds turn-by-turn logs from battle events
        /// </summary>
        private static List<CombatTurnLog> BuildTurnLogs(List<BattleEvent> events, string playerName, string enemyName)
        {
            var turnLogs = new List<CombatTurnLog>();
            int turnNumber = 1;
            
            // Sort events by timestamp to maintain order
            var sortedEvents = events.OrderBy(e => e.Timestamp).ToList();
            
            foreach (var evt in sortedEvents)
            {
                // Only log combat actions (damage/healing events)
                if (evt.Damage > 0 || evt.HealAmount > 0 || !evt.IsSuccess)
                {
                    var isPlayer = evt.Actor == playerName;
                    var turnLog = new CombatTurnLog
                    {
                        TurnNumber = turnNumber,
                        Actor = isPlayer ? "player" : "enemy",
                        Action = evt.Action,
                        DamageDealt = evt.Damage,
                        DamageReceived = 0, // Will be set by matching events
                        PlayerHealthAfter = isPlayer ? evt.ActorHealthAfter : evt.TargetHealthAfter,
                        EnemyHealthAfter = isPlayer ? evt.TargetHealthAfter : evt.ActorHealthAfter,
                        WasCritical = evt.IsCritical,
                        WasMiss = !evt.IsSuccess && evt.Damage == 0,
                        StatusEffectsApplied = new List<string>(),
                        RollValue = evt.Roll > 0 ? evt.Roll : null,
                        ActionType = evt.IsCombo ? "combo" : "basic"
                    };
                    
                    // Add status effects if present
                    if (evt.CausesBleed) turnLog.StatusEffectsApplied.Add("Bleed");
                    if (evt.CausesWeaken) turnLog.StatusEffectsApplied.Add("Weaken");
                    
                    turnLogs.Add(turnLog);
                    
                    // Increment turn number for each action
                    turnNumber++;
                }
            }
            
            // Update damage received by matching events
            for (int i = 0; i < turnLogs.Count; i++)
            {
                var log = turnLogs[i];
                // Find corresponding damage event
                if (log.Actor == "player")
                {
                    // Player dealt damage, enemy received it
                    var matchingEvent = sortedEvents.FirstOrDefault(e => 
                        e.Actor == playerName && 
                        e.Target == enemyName && 
                        e.Damage > 0 &&
                        Math.Abs((e.Timestamp - sortedEvents.First(ev => ev.Actor == playerName && ev.Action == log.Action).Timestamp).TotalSeconds) < 0.1);
                    // Damage received is already captured in the next enemy action
                }
            }
            
            return turnLogs;
        }
        
        /// <summary>
        /// Builds action usage statistics
        /// </summary>
        private static Dictionary<string, int> BuildActionUsageStats(List<BattleEvent> events, string playerName)
        {
            var actionUsage = new Dictionary<string, int>();
            
            foreach (var evt in events.Where(e => e.Actor == playerName))
            {
                var actionName = evt.Action;
                if (string.IsNullOrEmpty(actionName))
                    actionName = "Unknown";
                    
                if (!actionUsage.ContainsKey(actionName))
                    actionUsage[actionName] = 0;
                    
                actionUsage[actionName]++;
            }
            
            return actionUsage;
        }

        /// <summary>
        /// Creates a test character with custom stats using real game systems
        /// Loads real weapons from Weapons.json and uses proper character creation
        /// Then adjusts stats to match the desired configuration
        /// </summary>
        private static Character CreateTestCharacter(string name, int damage, double attackSpeed, int armor, int health)
        {
            // Create character using real game systems
            var character = new Character(name, level: 1);
            
            // Set health
            character.MaxHealth = health;
            character.CurrentHealth = health;
            
            var tuning = GameConfiguration.Instance;
            
            // Load a real weapon from Weapons.json
            WeaponItem? weapon = LoadRealWeapon(WeaponType.Sword);
            if (weapon == null)
            {
                // Fallback: create a basic weapon if loading fails
                weapon = new WeaponItem("Test Sword", 1, 5, 1.0, WeaponType.Sword);
            }
            
            // Adjust weapon damage to help reach target damage
            // Character damage = Strength + Weapon Damage + Equipment bonuses
            int baseStrength = Math.Max(1, damage / 2);
            character.Strength = baseStrength;
            
            // Adjust weapon damage to reach target (accounting for strength)
            int currentWeaponDamage = weapon.GetTotalDamage();
            int targetWeaponDamage = Math.Max(1, damage - baseStrength);
            weapon.BaseDamage = Math.Max(1, targetWeaponDamage - weapon.BonusDamage);
            weapon.BaseAttackSpeed = attackSpeed;
            
            // Equip the weapon (this sets up all actions, combos, etc.)
            character.EquipItem(weapon, "weapon");
            character.InitializeDefaultCombo();
            
            // Adjust agility to match attack speed
            // Attack time = baseAttackTime - (Agility * agilitySpeedReduction)
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            int targetAgility = (int)Math.Max(1, (baseAttackTime - attackSpeed) / tuning.Combat.AgilitySpeedReduction);
            character.Agility = targetAgility;
            
            // Equip armor pieces to match desired armor value
            if (armor > 0)
            {
                int headArmor = Math.Max(1, armor / 3);
                int chestArmor = Math.Max(1, armor / 2);
                int feetArmor = Math.Max(1, armor - headArmor - chestArmor);
                
                var headItem = new HeadItem("Test Helmet", 1, headArmor);
                var chestItem = new ChestItem("Test Chest", 1, chestArmor);
                var feetItem = new FeetItem("Test Boots", 1, feetArmor);
                
                character.EquipItem(headItem, "head");
                character.EquipItem(chestItem, "body");
                character.EquipItem(feetItem, "feet");
            }
            
            return character;
        }
        
        /// <summary>
        /// Loads a real weapon from Weapons.json
        /// </summary>
        private static WeaponItem? LoadRealWeapon(WeaponType preferredType)
        {
            try
            {
                // Use LoadJsonList which takes a fileName and finds the file automatically
                var weaponDataList = JsonLoader.LoadJsonList<WeaponData>("Weapons.json");
                if (weaponDataList == null || weaponDataList.Count == 0)
                    return null;
                
                // Try to find a weapon of the preferred type, fallback to any weapon
                var preferredWeapon = weaponDataList.FirstOrDefault(w => 
                    Enum.TryParse<WeaponType>(w.Type, true, out var wt) && wt == preferredType);
                
                var weaponData = preferredWeapon ?? weaponDataList[0];
                
                if (Enum.TryParse<WeaponType>(weaponData.Type, true, out var weaponType))
                {
                    return new WeaponItem(
                        weaponData.Name,
                        weaponData.Tier,
                        weaponData.BaseDamage,
                        weaponData.AttackSpeed,
                        weaponType
                    );
                }
            }
            catch (Exception ex)
            {
                Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Error loading weapon: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Calculates statistics from battle results
        /// </summary>
        private static void CalculateStatistics(StatisticsResult result)
        {
            var validResults = result.BattleResults.Where(r => r.ErrorMessage == null).ToList();
            
            if (validResults.Count == 0)
            {
                return;
            }

            result.PlayerWins = validResults.Count(r => r.PlayerWon);
            result.EnemyWins = validResults.Count(r => !r.PlayerWon);
            result.WinRate = (double)result.PlayerWins / validResults.Count * 100.0;
            
            result.AverageTurns = validResults.Average(r => r.Turns);
            result.AveragePlayerDamageDealt = validResults.Average(r => r.PlayerDamageDealt);
            result.AverageEnemyDamageDealt = validResults.Average(r => r.EnemyDamageDealt);
            
            result.MinTurns = validResults.Min(r => r.Turns);
            result.MaxTurns = validResults.Max(r => r.Turns);
        }

        /// <summary>
        /// Runs multiple configurations in parallel for comparison
        /// </summary>
        public static async Task<List<StatisticsResult>> RunMultipleConfigurations(
            List<BattleConfiguration> configurations,
            int battlesPerConfig = 100,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            var results = new List<StatisticsResult>();
            
            for (int i = 0; i < configurations.Count; i++)
            {
                var config = configurations[i];
                progress?.Report((i, configurations.Count, $"Configuration {i + 1}/{configurations.Count}"));
                
                var result = await RunParallelBattles(config, battlesPerConfig, 
                    new Progress<(int, int, string)>(p => 
                    {
                        progress?.Report((i * battlesPerConfig + p.Item1, configurations.Count * battlesPerConfig, 
                            $"Config {i + 1}/{configurations.Count}: {p.Item3}"));
                    }));
                
                results.Add(result);
            }
            
            return results;
        }

        /// <summary>
        /// Runs battle tests for each weapon type against random enemies
        /// Tests each weapon with its associated actions
        /// </summary>
        public static async Task<List<WeaponTestResult>> RunWeaponTypeTests(
            int battlesPerWeapon = 50,
            int playerLevel = 1,
            int enemyLevel = 1,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            // Disable UI output and delays for faster testing
            var originalDisableFlag = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;
            
            try
            {
                var results = new List<WeaponTestResult>();
                // Only use the four primary weapons: Mace, Sword, Dagger, Wand
                var allWeaponTypes = new List<WeaponType> { WeaponType.Mace, WeaponType.Sword, WeaponType.Dagger, WeaponType.Wand };
                var allEnemyTypes = EnemyLoader.GetAllEnemyTypes();
                
                if (allEnemyTypes.Count == 0)
                {
                    // Fallback to a default enemy if none loaded
                    allEnemyTypes = new List<string> { "Goblin" };
                }
                
                int totalBattles = allWeaponTypes.Count * battlesPerWeapon;
                int completedBattles = 0;
                
                foreach (var weaponType in allWeaponTypes)
                {
                    var weaponResult = new WeaponTestResult
                    {
                        WeaponType = weaponType,
                        TotalBattles = battlesPerWeapon
                    };
                    
                    var battleTasks = new List<Task<BattleResult>>();
                    var semaphore = new System.Threading.SemaphoreSlim(System.Environment.ProcessorCount * 2, System.Environment.ProcessorCount * 2);
                    
                    for (int i = 0; i < battlesPerWeapon; i++)
                    {
                        int battleIndex = i;
                        var task = Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                // Select random enemy for this battle
                                var randomEnemyType = allEnemyTypes[RandomUtility.Next(allEnemyTypes.Count)];
                                var battleResult = await RunSingleBattleWithWeapon(weaponType, randomEnemyType, playerLevel, enemyLevel, battleIndex);
                                
                                completedBattles++;
                                progress?.Report((completedBattles, totalBattles, 
                                    $"{weaponType} vs {randomEnemyType} ({completedBattles}/{totalBattles})"));
                                
                                return battleResult;
                            }
                            catch (Exception ex)
                            {
                                var errorResult = new BattleResult
                                {
                                    ErrorMessage = $"Exception: {ex.GetType().Name}: {ex.Message}",
                                    PlayerWon = false,
                                    Turns = 0
                                };
                                completedBattles++;
                                progress?.Report((completedBattles, totalBattles, 
                                    $"{weaponType} - Error ({completedBattles}/{totalBattles})"));
                                return errorResult;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });
                        battleTasks.Add(task);
                    }
                    
                    // Wait for all battles for this weapon
                    var allResults = await Task.WhenAll(battleTasks);
                    weaponResult.BattleResults = allResults.ToList();
                    
                    // Calculate statistics for this weapon
                    CalculateWeaponStatistics(weaponResult);
                    results.Add(weaponResult);
                }
                
                return results;
            }
            finally
            {
                // Restore original UI output setting
                CombatManager.DisableCombatUIOutput = originalDisableFlag;
            }
        }

        /// <summary>
        /// Runs a single battle with a specific weapon type against a random enemy
        /// </summary>
        private static async Task<BattleResult> RunSingleBattleWithWeapon(
            WeaponType weaponType, 
            string enemyType, 
            int playerLevel, 
            int enemyLevel, 
            int battleIndex)
        {
            var result = new BattleResult();
            var battleStartTime = DateTime.Now;
            
            try
            {
                // Create character with the specified weapon
                var charCreateStart = DateTime.Now;
                var character = CreateTestCharacterWithWeapon($"TestPlayer_{weaponType}_{battleIndex}", weaponType, playerLevel);
                var charCreateTime = (DateTime.Now - charCreateStart).TotalMilliseconds;
                
                // Create enemy from enemy data
                var enemyCreateStart = DateTime.Now;
                var enemy = EnemyLoader.CreateEnemy(enemyType, enemyLevel);
                if (enemy == null)
                {
                    // Fallback to basic enemy if loading fails
                    enemy = new Enemy($"TestEnemy_{battleIndex}", enemyLevel, 80, 8, 6, 4, 3);
                }
                var enemyCreateTime = (DateTime.Now - enemyCreateStart).TotalMilliseconds;
                
                // Create a simple non-hostile environment for testing
                var environment = new Environment(
                    name: "Test Room",
                    description: "Testing environment",
                    isHostile: false,
                    theme: "neutral"
                );
                
                // Track initial health for damage calculation
                int initialPlayerHealth = character.CurrentHealth;
                int initialEnemyHealth = enemy.CurrentHealth;
                
                // Run combat with timeout (30 seconds max per battle)
                var combatStartTime = DateTime.Now;
                var combatManager = new CombatManager();
                var combatTask = combatManager.RunCombat(character, enemy, environment);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                
                var completedTask = await Task.WhenAny(combatTask, timeoutTask);
                var combatWaitTime = (DateTime.Now - combatStartTime).TotalSeconds;
                
                if (completedTask == timeoutTask)
                {
                    Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Battle TIMEOUT after {combatWaitTime:F2}s: {weaponType} vs {enemyType} (battle {battleIndex})");
                    result.ErrorMessage = $"Battle timed out after 30 seconds (Weapon: {weaponType}, Enemy: {enemyType})";
                    result.PlayerWon = false;
                    result.Turns = 0;
                    return result;
                }
                
                bool playerWon = await combatTask;
                var totalBattleTime = (DateTime.Now - battleStartTime).TotalSeconds;
                
                if (totalBattleTime > 5.0)
                {
                    Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Slow battle detected: {weaponType} vs {enemyType} took {totalBattleTime:F2}s (char: {charCreateTime:F0}ms, enemy: {enemyCreateTime:F0}ms, combat: {combatWaitTime:F2}s)");
                }
                
                // Calculate results using accurate data from battle narrative
                result.PlayerWon = playerWon;
                result.PlayerFinalHealth = character.CurrentHealth;
                result.EnemyFinalHealth = enemy.CurrentHealth;
                
                // Get accurate statistics from battle narrative and turn manager
                var narrative = combatManager.GetCurrentBattleNarrative();
                int currentTurn = combatManager.GetCurrentTurn();
                int totalActionCount = combatManager.GetTotalActionCount();
                var funTracker = combatManager.GetFunMomentTracker();
                
                if (narrative != null)
                {
                    var events = narrative.GetAllEvents();
                    
                    // Calculate damage from actual events
                    result.PlayerDamageDealt = events
                        .Where(e => e.Actor == character.Name && e.Target == enemy.Name && e.Damage > 0)
                        .Sum(e => e.Damage);
                    
                    result.EnemyDamageDealt = events
                        .Where(e => e.Actor == enemy.Name && e.Target == character.Name && e.Damage > 0)
                        .Sum(e => e.Damage);
                    
                    // Use current turn number from TurnManager (turns increment every 10 actions)
                    // If no actions were recorded, calculate from action count: (actionCount + 9) / 10
                    if (currentTurn > 0)
                    {
                        result.Turns = currentTurn;
                    }
                    else if (totalActionCount > 0)
                    {
                        // Calculate turns from action count: turns increment every 10 actions
                        // Round up: (actionCount + 9) / 10
                        result.Turns = (totalActionCount + 9) / 10;
                    }
                    else
                    {
                        var playerActions = events.Count(e => e.Actor == character.Name && e.Target == enemy.Name);
                        var enemyActions = events.Count(e => e.Actor == enemy.Name && e.Target == character.Name);
                        int totalActions = playerActions + enemyActions;
                        result.Turns = Math.Max(1, (totalActions + 9) / 10);
                    }
                    
                    // Collect fun moment summary
                    if (funTracker != null)
                    {
                        result.FunMomentSummary = funTracker.GetSummary();
                    }
                }
                else
                {
                    // Fallback: use health difference if narrative not available
                    result.PlayerDamageDealt = Math.Max(0, initialEnemyHealth - enemy.CurrentHealth);
                    result.EnemyDamageDealt = Math.Max(0, initialPlayerHealth - character.CurrentHealth);
                    
                    // Use current turn number if available, otherwise calculate from action count
                    if (currentTurn > 0)
                    {
                        result.Turns = currentTurn;
                    }
                    else if (totalActionCount > 0)
                    {
                        // Calculate turns from action count: turns increment every 10 actions
                        // Round up: (actionCount + 9) / 10
                        result.Turns = (totalActionCount + 9) / 10;
                    }
                    else
                    {
                        result.Turns = 1; // Default fallback
                    }
                }
            }
            catch (Exception ex)
            {
                var totalTime = (DateTime.Now - battleStartTime).TotalSeconds;
                Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Battle EXCEPTION after {totalTime:F2}s: {weaponType} vs {enemyType} (battle {battleIndex}) - {ex.GetType().Name}: {ex.Message}");
                Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Stack trace: {ex.StackTrace}");
                result.ErrorMessage = $"Exception: {ex.GetType().Name}: {ex.Message}";
                result.PlayerWon = false;
                result.Turns = 0;
                result.PlayerDamageDealt = 0;
                result.EnemyDamageDealt = 0;
            }
            
            return result;
        }

        /// <summary>
        /// Creates a test character with a specific weapon type and proper actions
        /// Uses Character.EquipItem to ensure all actions are properly added
        /// </summary>
        private static Character CreateTestCharacterWithWeapon(string name, WeaponType weaponType, int level)
        {
            var character = new Character(name, level);
            
            // Create a weapon of the specified type
            var weapon = new WeaponItem(
                name: $"{weaponType} Test Weapon",
                tier: 1,
                baseDamage: 2,
                baseAttackSpeed: 1.0,
                weaponType: weaponType
            );
            
            // Use Character.EquipItem to properly equip the weapon
            // This ensures all actions, class actions, and combo sequences are set up correctly
            character.EquipItem(weapon, "weapon");
            
            // Ensure class actions are added based on weapon type
            character.Actions.AddClassActions(character, character.Progression, weaponType);
            
            return character;
        }

        /// <summary>
        /// Calculates statistics for a weapon test result
        /// </summary>
        private static void CalculateWeaponStatistics(WeaponTestResult result)
        {
            var validResults = result.BattleResults.Where(r => r.ErrorMessage == null).ToList();
            
            if (validResults.Count == 0)
            {
                return;
            }
            
            result.PlayerWins = validResults.Count(r => r.PlayerWon);
            result.EnemyWins = validResults.Count(r => !r.PlayerWon);
            result.WinRate = (double)result.PlayerWins / validResults.Count * 100.0;
            
            result.AverageTurns = validResults.Average(r => r.Turns);
            result.AveragePlayerDamageDealt = validResults.Average(r => r.PlayerDamageDealt);
            result.AverageEnemyDamageDealt = validResults.Average(r => r.EnemyDamageDealt);
            
            result.MinTurns = validResults.Min(r => r.Turns);
            result.MaxTurns = validResults.Max(r => r.Turns);
        }

        /// <summary>
        /// Runs comprehensive tests: every weapon against every enemy
        /// Tests each weapon-enemy combination multiple times
        /// </summary>
        public static async Task<ComprehensiveWeaponEnemyTestResult> RunComprehensiveWeaponEnemyTests(
            int battlesPerCombination = 10,
            int playerLevel = 1,
            int enemyLevel = 1,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            // Disable UI output and delays for faster testing
            var originalDisableFlag = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;
            
            try
            {
                var result = new ComprehensiveWeaponEnemyTestResult();
                // Only use the four primary weapons: Mace, Sword, Dagger, Wand
                var allWeaponTypes = new List<WeaponType> { WeaponType.Mace, WeaponType.Sword, WeaponType.Dagger, WeaponType.Wand };
                var allEnemyTypes = EnemyLoader.GetAllEnemyTypes();
                
                Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Starting comprehensive test - {allWeaponTypes.Count} primary weapons, {allEnemyTypes.Count} enemies");
            
            if (allEnemyTypes.Count == 0)
            {
                // Fallback to a default enemy if none loaded
                allEnemyTypes = new List<string> { "Goblin" };
                Utils.ScrollDebugLogger.Log("BattleStatisticsRunner: No enemies loaded, using fallback Goblin");
            }
            
            result.WeaponTypes = allWeaponTypes;
            result.EnemyTypes = allEnemyTypes;
            
            // Calculate total battles: weapon_count * enemy_count * battles_per_combination
            int totalBattles = allWeaponTypes.Count * allEnemyTypes.Count * battlesPerCombination;
            int completedBattles = 0;
            int totalCombinations = allWeaponTypes.Count * allEnemyTypes.Count;
            int completedCombinations = 0;
            
            Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Total combinations: {totalCombinations}, Total battles: {totalBattles}");
            
            // Test each weapon against each enemy
            foreach (var weaponType in allWeaponTypes)
            {
                foreach (var enemyType in allEnemyTypes)
                {
                    completedCombinations++;
                    Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Starting combination {completedCombinations}/{totalCombinations}: {weaponType} vs {enemyType}");
                    
                    var combinationResult = new WeaponEnemyCombinationResult
                    {
                        WeaponType = weaponType,
                        EnemyType = enemyType,
                        TotalBattles = battlesPerCombination
                    };
                    
                    var battleTasks = new List<Task<BattleResult>>();
                    var semaphore = new System.Threading.SemaphoreSlim(System.Environment.ProcessorCount * 2, System.Environment.ProcessorCount * 2);
                    int battlesStarted = 0;
                    int battlesCompleted = 0;
                    
                    for (int i = 0; i < battlesPerCombination; i++)
                    {
                        int battleIndex = i;
                        var task = Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            battlesStarted++;
                            var battleStartTime = DateTime.Now;
                            
                            try
                            {
                                Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Battle {battleIndex + 1}/{battlesPerCombination} starting: {weaponType} vs {enemyType}");
                                
                                var battleResult = await RunSingleBattleWithWeapon(weaponType, enemyType, playerLevel, enemyLevel, battleIndex);
                                
                                var battleDuration = (DateTime.Now - battleStartTime).TotalSeconds;
                                battlesCompleted++;
                                completedBattles++;
                                
                                Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Battle {battleIndex + 1}/{battlesPerCombination} completed in {battleDuration:F2}s: {weaponType} vs {enemyType} - {(battleResult.ErrorMessage != null ? "ERROR" : battleResult.PlayerWon ? "WIN" : "LOSS")}");
                                
                                progress?.Report((completedBattles, totalBattles, 
                                    $"{weaponType} vs {enemyType} ({completedBattles}/{totalBattles})"));
                                
                                return battleResult;
                            }
                            catch (Exception ex)
                            {
                                var battleDuration = (DateTime.Now - battleStartTime).TotalSeconds;
                                Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Battle {battleIndex + 1}/{battlesPerCombination} EXCEPTION after {battleDuration:F2}s: {weaponType} vs {enemyType} - {ex.GetType().Name}: {ex.Message}");
                                
                                var errorResult = new BattleResult
                                {
                                    ErrorMessage = $"Exception: {ex.GetType().Name}: {ex.Message}",
                                    PlayerWon = false,
                                    Turns = 0
                                };
                                battlesCompleted++;
                                completedBattles++;
                                progress?.Report((completedBattles, totalBattles, 
                                    $"{weaponType} vs {enemyType} - Error ({completedBattles}/{totalBattles})"));
                                return errorResult;
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });
                        battleTasks.Add(task);
                    }
                    
                    Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Waiting for {battlesPerCombination} battles to complete: {weaponType} vs {enemyType}");
                    var waitStartTime = DateTime.Now;
                    
                    // Wait for all battles for this combination with timeout check
                    try
                    {
                        var allResults = await Task.WhenAll(battleTasks);
                        var waitDuration = (DateTime.Now - waitStartTime).TotalSeconds;
                        
                        Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: All {battlesPerCombination} battles completed in {waitDuration:F2}s: {weaponType} vs {enemyType} (Started: {battlesStarted}, Completed: {battlesCompleted})");
                        
                        combinationResult.BattleResults = allResults.ToList();
                    }
                    catch (Exception ex)
                    {
                        Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: ERROR waiting for battles: {weaponType} vs {enemyType} - {ex.GetType().Name}: {ex.Message}");
                        Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Stack trace: {ex.StackTrace}");
                        
                        // Get any completed results
                        var completedResults = battleTasks
                            .Where(t => t.IsCompletedSuccessfully)
                            .Select(t => t.Result)
                            .ToList();
                        combinationResult.BattleResults = completedResults;
                    }
                    
                    // Calculate statistics for this combination
                    CalculateWeaponEnemyStatistics(combinationResult);
                    result.CombinationResults.Add(combinationResult);
                    
                    Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Combination {completedCombinations}/{totalCombinations} finished: {weaponType} vs {enemyType} - Win Rate: {combinationResult.WinRate:F1}%");
                }
            }
            
            Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: All combinations complete, calculating overall statistics");
            
                // Calculate overall statistics
                CalculateComprehensiveStatistics(result);
                
                Utils.ScrollDebugLogger.Log($"BattleStatisticsRunner: Comprehensive test complete - Overall Win Rate: {result.OverallWinRate:F1}%");
                
                return result;
            }
            finally
            {
                // Restore original UI output setting
                CombatManager.DisableCombatUIOutput = originalDisableFlag;
            }
        }

        /// <summary>
        /// Calculates statistics for a weapon-enemy combination
        /// </summary>
        private static void CalculateWeaponEnemyStatistics(WeaponEnemyCombinationResult result)
        {
            var validResults = result.BattleResults.Where(r => r.ErrorMessage == null).ToList();
            
            if (validResults.Count == 0)
            {
                return;
            }
            
            result.PlayerWins = validResults.Count(r => r.PlayerWon);
            result.EnemyWins = validResults.Count(r => !r.PlayerWon);
            result.WinRate = (double)result.PlayerWins / validResults.Count * 100.0;
            
            result.AverageTurns = validResults.Average(r => r.Turns);
            result.AveragePlayerDamageDealt = validResults.Average(r => r.PlayerDamageDealt);
            result.AverageEnemyDamageDealt = validResults.Average(r => r.EnemyDamageDealt);
            
            result.MinTurns = validResults.Min(r => r.Turns);
            result.MaxTurns = validResults.Max(r => r.Turns);
        }

        /// <summary>
        /// Calculates overall statistics for comprehensive test
        /// </summary>
        private static void CalculateComprehensiveStatistics(ComprehensiveWeaponEnemyTestResult result)
        {
            var allValidResults = result.CombinationResults
                .SelectMany(c => c.BattleResults)
                .Where(r => r.ErrorMessage == null)
                .ToList();
            
            if (allValidResults.Count == 0)
            {
                return;
            }
            
            result.TotalBattles = allValidResults.Count;
            result.TotalPlayerWins = allValidResults.Count(r => r.PlayerWon);
            result.TotalEnemyWins = allValidResults.Count(r => !r.PlayerWon);
            result.OverallWinRate = (double)result.TotalPlayerWins / allValidResults.Count * 100.0;
            result.OverallAverageTurns = allValidResults.Average(r => r.Turns);
            result.OverallAveragePlayerDamage = allValidResults.Average(r => r.PlayerDamageDealt);
            result.OverallAverageEnemyDamage = allValidResults.Average(r => r.EnemyDamageDealt);
            
            // Calculate fun moment statistics
            var funResults = allValidResults.Where(r => r.FunMomentSummary != null).ToList();
            if (funResults.Count > 0)
            {
                result.OverallAverageFunScore = funResults.Average(r => r.FunMomentSummary!.FunScore);
                result.OverallAverageActionVariety = funResults.Average(r => r.FunMomentSummary!.ActionVarietyScore);
                result.OverallAverageTurnVariance = funResults.Average(r => r.FunMomentSummary!.TurnVariance);
                result.TotalHealthLeadChanges = funResults.Sum(r => r.FunMomentSummary!.HealthLeadChanges);
                result.TotalComebacks = funResults.Sum(r => r.FunMomentSummary!.Comebacks);
                result.TotalCloseCalls = funResults.Sum(r => r.FunMomentSummary!.CloseCalls);
            }
            
            // Calculate per-weapon statistics
            foreach (var weaponType in result.WeaponTypes)
            {
                var weaponResults = result.CombinationResults
                    .Where(c => c.WeaponType == weaponType)
                    .SelectMany(c => c.BattleResults)
                    .Where(r => r.ErrorMessage == null)
                    .ToList();
                
                if (weaponResults.Count > 0)
                {
                    var funWeaponResults = weaponResults.Where(r => r.FunMomentSummary != null).ToList();
                    result.WeaponStatistics[weaponType] = new WeaponOverallStats
                    {
                        TotalBattles = weaponResults.Count,
                        Wins = weaponResults.Count(r => r.PlayerWon),
                        WinRate = (double)weaponResults.Count(r => r.PlayerWon) / weaponResults.Count * 100.0,
                        AverageTurns = weaponResults.Average(r => r.Turns),
                        AverageDamage = weaponResults.Average(r => r.PlayerDamageDealt),
                        AverageFunScore = funWeaponResults.Count > 0 ? funWeaponResults.Average(r => r.FunMomentSummary!.FunScore) : 0.0,
                        AverageActionVariety = funWeaponResults.Count > 0 ? funWeaponResults.Average(r => r.FunMomentSummary!.ActionVarietyScore) : 0.0,
                        AverageHealthLeadChanges = funWeaponResults.Count > 0 ? funWeaponResults.Average(r => r.FunMomentSummary!.HealthLeadChanges) : 0.0
                    };
                }
            }
            
            // Calculate per-enemy statistics
            foreach (var enemyType in result.EnemyTypes)
            {
                var enemyResults = result.CombinationResults
                    .Where(c => c.EnemyType == enemyType)
                    .SelectMany(c => c.BattleResults)
                    .Where(r => r.ErrorMessage == null)
                    .ToList();
                
                if (enemyResults.Count > 0)
                {
                    var funEnemyResults = enemyResults.Where(r => r.FunMomentSummary != null).ToList();
                    result.EnemyStatistics[enemyType] = new EnemyOverallStats
                    {
                        TotalBattles = enemyResults.Count,
                        Wins = enemyResults.Count(r => r.PlayerWon),
                        WinRate = (double)enemyResults.Count(r => r.PlayerWon) / enemyResults.Count * 100.0,
                        AverageTurns = enemyResults.Average(r => r.Turns),
                        AverageDamageReceived = enemyResults.Average(r => r.PlayerDamageDealt),
                        AverageFunScore = funEnemyResults.Count > 0 ? funEnemyResults.Average(r => r.FunMomentSummary!.FunScore) : 0.0,
                        AverageActionVariety = funEnemyResults.Count > 0 ? funEnemyResults.Average(r => r.FunMomentSummary!.ActionVarietyScore) : 0.0,
                        AverageHealthLeadChanges = funEnemyResults.Count > 0 ? funEnemyResults.Average(r => r.FunMomentSummary!.HealthLeadChanges) : 0.0
                    };
                }
            }
        }

        /// <summary>
        /// Result for weapon-specific battle testing
        /// </summary>
        public class WeaponTestResult
        {
            public WeaponType WeaponType { get; set; }
            public int TotalBattles { get; set; }
            public int PlayerWins { get; set; }
            public int EnemyWins { get; set; }
            public double WinRate { get; set; }
            public double AverageTurns { get; set; }
            public double AveragePlayerDamageDealt { get; set; }
            public double AverageEnemyDamageDealt { get; set; }
            public int MinTurns { get; set; }
            public int MaxTurns { get; set; }
            public List<BattleResult> BattleResults { get; set; } = new();
        }

        /// <summary>
        /// Result for a single weapon-enemy combination
        /// </summary>
        public class WeaponEnemyCombinationResult
        {
            public WeaponType WeaponType { get; set; }
            public string EnemyType { get; set; } = "";
            public int TotalBattles { get; set; }
            public int PlayerWins { get; set; }
            public int EnemyWins { get; set; }
            public double WinRate { get; set; }
            public double AverageTurns { get; set; }
            public double AveragePlayerDamageDealt { get; set; }
            public double AverageEnemyDamageDealt { get; set; }
            public int MinTurns { get; set; }
            public int MaxTurns { get; set; }
            public List<BattleResult> BattleResults { get; set; } = new();
        }

        /// <summary>
        /// Overall statistics for a weapon across all enemies
        /// </summary>
        public class WeaponOverallStats
        {
            public int TotalBattles { get; set; }
            public int Wins { get; set; }
            public double WinRate { get; set; }
            public double AverageTurns { get; set; }
            public double AverageDamage { get; set; }
            public double AverageFunScore { get; set; }
            public double AverageActionVariety { get; set; }
            public double AverageHealthLeadChanges { get; set; }
        }

        /// <summary>
        /// Overall statistics for an enemy across all weapons
        /// </summary>
        public class EnemyOverallStats
        {
            public int TotalBattles { get; set; }
            public int Wins { get; set; }
            public double WinRate { get; set; }
            public double AverageTurns { get; set; }
            public double AverageDamageReceived { get; set; }
            public double AverageFunScore { get; set; }
            public double AverageActionVariety { get; set; }
            public double AverageHealthLeadChanges { get; set; }
        }

        /// <summary>
        /// Comprehensive result for all weapon-enemy combinations
        /// </summary>
        public class ComprehensiveWeaponEnemyTestResult
        {
            public List<WeaponType> WeaponTypes { get; set; } = new();
            public List<string> EnemyTypes { get; set; } = new();
            public List<WeaponEnemyCombinationResult> CombinationResults { get; set; } = new();
            
            // Overall statistics
            public int TotalBattles { get; set; }
            public int TotalPlayerWins { get; set; }
            public int TotalEnemyWins { get; set; }
            public double OverallWinRate { get; set; }
            public double OverallAverageTurns { get; set; }
            public double OverallAveragePlayerDamage { get; set; }
            public double OverallAverageEnemyDamage { get; set; }
            
            // Fun moment statistics
            public double OverallAverageFunScore { get; set; }
            public double OverallAverageActionVariety { get; set; }
            public double OverallAverageTurnVariance { get; set; }
            public int TotalHealthLeadChanges { get; set; }
            public int TotalComebacks { get; set; }
            public int TotalCloseCalls { get; set; }
            
            // Per-weapon statistics
            public Dictionary<WeaponType, WeaponOverallStats> WeaponStatistics { get; set; } = new();
            
            // Per-enemy statistics
            public Dictionary<string, EnemyOverallStats> EnemyStatistics { get; set; } = new();
        }
    }
}

