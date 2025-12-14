using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.Combat;
using RPGGame.Entity;
using RPGGame.World;

namespace RPGGame.BattleStatistics
{
    /// <summary>
    /// Executes single battles and extracts results from combat
    /// </summary>
    public static class BattleExecutor
    {
        /// <summary>
        /// Runs a single battle with the given configuration
        /// </summary>
        public static async Task<BattleResult> RunSingleBattle(BattleConfiguration config, int battleIndex)
        {
            var result = new BattleResult();
            
            try
            {
                var player = TestCharacterFactory.CreateTestCharacter(
                    name: $"TestPlayer_{battleIndex}",
                    damage: config.PlayerDamage,
                    attackSpeed: config.PlayerAttackSpeed,
                    armor: config.PlayerArmor,
                    health: config.PlayerHealth
                );

                var enemy = TestCharacterFactory.CreateTestEnemy(config, battleIndex);
                var environment = TestCharacterFactory.CreateTestEnvironment();

                int initialPlayerHealth = player.CurrentHealth;
                int initialEnemyHealth = enemy.CurrentHealth;
                int turnCount = 0;

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

                result.PlayerWon = playerWon;
                result.PlayerFinalHealth = player.CurrentHealth;
                result.EnemyFinalHealth = enemy.CurrentHealth;
                
                var narrative = combatManager.GetCurrentBattleNarrative();
                int currentTurn = combatManager.GetCurrentTurn();
                int totalActionCount = combatManager.GetTotalActionCount();
                var funTracker = combatManager.GetFunMomentTracker();
                
                if (narrative != null)
                {
                    var events = narrative.GetAllEvents();

                    result.PlayerDamageDealt = events
                        .Where(e => e.Actor == player.Name && e.Target == enemy.Name && e.Damage > 0)
                        .Sum(e => e.Damage);

                    result.EnemyDamageDealt = events
                        .Where(e => e.Actor == enemy.Name && e.Target == player.Name && e.Damage > 0)
                        .Sum(e => e.Damage);

                    if (currentTurn > 0)
                    {
                        turnCount = currentTurn;
                    }
                    else if (totalActionCount > 0)
                    {
                        turnCount = totalActionCount;
                    }
                    else
                    {
                        var playerActions = events.Count(e => e.Actor == player.Name && e.Target == enemy.Name);
                        var enemyActions = events.Count(e => e.Actor == enemy.Name && e.Target == player.Name);
                        turnCount = Math.Max(1, playerActions + enemyActions);
                    }
                    
                    result.TurnLogs = BuildTurnLogs(events, player.Name, enemy.Name);
                    result.ActionUsageCount = BuildActionUsageStats(events, player.Name);
                    
                    if (funTracker != null)
                    {
                        result.FunMomentSummary = funTracker.GetSummary();
                    }
                }
                else
                {
                    result.PlayerDamageDealt = Math.Max(0, initialEnemyHealth - enemy.CurrentHealth);
                    result.EnemyDamageDealt = Math.Max(0, initialPlayerHealth - player.CurrentHealth);

                    if (currentTurn > 0)
                    {
                        turnCount = currentTurn;
                    }
                    else if (totalActionCount > 0)
                    {
                        turnCount = totalActionCount;
                    }
                    else
                    {
                        int totalDamageDealt = result.PlayerDamageDealt + result.EnemyDamageDealt;
                        int averageDamagePerAction = Math.Max(1, (config.PlayerDamage + config.EnemyDamage) / 2);
                        int estimatedActions = Math.Max(1, (int)Math.Ceiling((double)totalDamageDealt / averageDamagePerAction));
                        turnCount = Math.Max(1, estimatedActions);
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
        /// Runs a single battle with a specific weapon type against a random enemy
        /// </summary>
        public static async Task<BattleResult> RunSingleBattleWithWeapon(
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
                var character = TestCharacterFactory.CreateTestCharacterWithWeapon(
                    $"TestPlayer_{weaponType}_{battleIndex}", weaponType, playerLevel);
                
                var enemy = EnemyLoader.CreateEnemy(enemyType, enemyLevel);
                if (enemy == null)
                {
                    enemy = new Enemy($"TestEnemy_{battleIndex}", enemyLevel, 80, 8, 6, 4, 3);
                }
                
                var environment = TestCharacterFactory.CreateTestEnvironment();
                
                int initialPlayerHealth = character.CurrentHealth;
                int initialEnemyHealth = enemy.CurrentHealth;
                
                var combatStartTime = DateTime.Now;
                var combatManager = new CombatManager();
                var combatTask = combatManager.RunCombat(character, enemy, environment);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                
                var completedTask = await Task.WhenAny(combatTask, timeoutTask);
                var combatWaitTime = (DateTime.Now - combatStartTime).TotalSeconds;
                
                if (completedTask == timeoutTask)
                {
                    Utils.ScrollDebugLogger.Log($"BattleExecutor: Battle TIMEOUT after {combatWaitTime:F2}s: {weaponType} vs {enemyType} (battle {battleIndex})");
                    result.ErrorMessage = $"Battle timed out after 30 seconds (Weapon: {weaponType}, Enemy: {enemyType})";
                    result.PlayerWon = false;
                    result.Turns = 0;
                    return result;
                }
                
                bool playerWon = await combatTask;
                var totalBattleTime = (DateTime.Now - battleStartTime).TotalSeconds;
                
                if (totalBattleTime > 5.0)
                {
                    Utils.ScrollDebugLogger.Log($"BattleExecutor: Slow battle detected: {weaponType} vs {enemyType} took {totalBattleTime:F2}s");
                }
                
                result.PlayerWon = playerWon;
                result.PlayerFinalHealth = character.CurrentHealth;
                result.EnemyFinalHealth = enemy.CurrentHealth;
                
                var narrative = combatManager.GetCurrentBattleNarrative();
                int currentTurn = combatManager.GetCurrentTurn();
                int totalActionCount = combatManager.GetTotalActionCount();
                var funTracker = combatManager.GetFunMomentTracker();
                
                if (narrative != null)
                {
                    var events = narrative.GetAllEvents();

                    result.PlayerDamageDealt = events
                        .Where(e => e.Actor == character.Name && e.Target == enemy.Name && e.Damage > 0)
                        .Sum(e => e.Damage);

                    result.EnemyDamageDealt = events
                        .Where(e => e.Actor == enemy.Name && e.Target == character.Name && e.Damage > 0)
                        .Sum(e => e.Damage);

                    if (currentTurn > 0)
                    {
                        result.Turns = currentTurn;
                    }
                    else if (totalActionCount > 0)
                    {
                        result.Turns = totalActionCount;
                    }
                    else
                    {
                        var playerActions = events.Count(e => e.Actor == character.Name && e.Target == enemy.Name);
                        var enemyActions = events.Count(e => e.Actor == enemy.Name && e.Target == character.Name);
                        int totalActions = playerActions + enemyActions;
                        result.Turns = Math.Max(1, totalActions);
                    }

                    if (funTracker != null)
                    {
                        result.FunMomentSummary = funTracker.GetSummary();
                    }
                }
                else
                {
                    result.PlayerDamageDealt = Math.Max(0, initialEnemyHealth - enemy.CurrentHealth);
                    result.EnemyDamageDealt = Math.Max(0, initialPlayerHealth - character.CurrentHealth);

                    if (currentTurn > 0)
                    {
                        result.Turns = currentTurn;
                    }
                    else if (totalActionCount > 0)
                    {
                        result.Turns = totalActionCount;
                    }
                    else
                    {
                        result.Turns = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                var totalTime = (DateTime.Now - battleStartTime).TotalSeconds;
                Utils.ScrollDebugLogger.Log($"BattleExecutor: Battle EXCEPTION after {totalTime:F2}s: {weaponType} vs {enemyType} (battle {battleIndex}) - {ex.GetType().Name}: {ex.Message}");
                Utils.ScrollDebugLogger.Log($"BattleExecutor: Stack trace: {ex.StackTrace}");
                result.ErrorMessage = $"Exception: {ex.GetType().Name}: {ex.Message}";
                result.PlayerWon = false;
                result.Turns = 0;
                result.PlayerDamageDealt = 0;
                result.EnemyDamageDealt = 0;
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
            
            var sortedEvents = events.OrderBy(e => e.Timestamp).ToList();
            
            foreach (var evt in sortedEvents)
            {
                if (evt.Damage > 0 || evt.HealAmount > 0 || !evt.IsSuccess)
                {
                    var isPlayer = evt.Actor == playerName;
                    var turnLog = new CombatTurnLog
                    {
                        TurnNumber = turnNumber,
                        Actor = isPlayer ? "player" : "enemy",
                        Action = evt.Action,
                        DamageDealt = evt.Damage,
                        DamageReceived = 0,
                        PlayerHealthAfter = isPlayer ? evt.ActorHealthAfter : evt.TargetHealthAfter,
                        EnemyHealthAfter = isPlayer ? evt.TargetHealthAfter : evt.ActorHealthAfter,
                        WasCritical = evt.IsCritical,
                        WasMiss = !evt.IsSuccess && evt.Damage == 0,
                        StatusEffectsApplied = new List<string>(),
                        RollValue = evt.Roll > 0 ? evt.Roll : null,
                        ActionType = evt.IsCombo ? "combo" : "basic"
                    };
                    
                    if (evt.CausesBleed) turnLog.StatusEffectsApplied.Add("Bleed");
                    if (evt.CausesWeaken) turnLog.StatusEffectsApplied.Add("Weaken");
                    
                    turnLogs.Add(turnLog);
                    turnNumber++;
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
    }
}

