using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;

namespace RPGGame.Simulation
{
    /// <summary>
    /// Simulates combat scenarios without UI output
    /// Designed for fast iteration on balance and tuning
    /// </summary>
    public class CombatSimulator
    {
        private const int MaxTurnsPerCombat = 1000; // Safety limit

        /// <summary>
        /// Result of a single combat simulation
        /// </summary>
        public class CombatSimulationResult
        {
            public bool PlayerWon { get; set; }
            public int TurnsToComplete { get; set; }
            public int PlayerFinalHealth { get; set; }
            public int PlayerMaxHealth { get; set; }
            public int EnemyFinalHealth { get; set; }
            public int EnemyMaxHealth { get; set; }
            
            // Phase detection
            public int Phase1Turns { get; set; } // 0-33% enemy health
            public int Phase2Turns { get; set; } // 33-66% enemy health
            public int Phase3Turns { get; set; } // 66-100% enemy health
            
            // Damage tracking
            public int TotalPlayerDamageDealt { get; set; }
            public int TotalEnemyDamageDealt { get; set; }
            public double AverageTurnsPerPhase { get; set; }
            
            // Player stats
            public string PlayerName { get; set; } = string.Empty;
            public int PlayerLevel { get; set; }
            public string EquippedWeapon { get; set; } = string.Empty;
            
            // Enemy stats
            public string EnemyName { get; set; } = string.Empty;
            public int EnemyLevel { get; set; }
            
            public double PlayerHealthPercentage => (double)PlayerFinalHealth / PlayerMaxHealth;
            public double EnemyHealthPercentage => (double)EnemyFinalHealth / EnemyMaxHealth;
        }

        /// <summary>
        /// Simulates a single combat between a player and enemy
        /// </summary>
        public static CombatSimulationResult SimulateCombat(Character player, Enemy enemy)
        {
            // Disable UI output during simulation
            var originalUIState = CombatManager.DisableCombatUIOutput;
            CombatManager.DisableCombatUIOutput = true;

            try
            {
                var manager = new CombatManager();
                manager.InitializeCombatEntities(player, enemy);
                
                int turnCount = 0;
                int playerDamageDealt = 0;
                int enemyDamageDealt = 0;
                
                // Track health at phase transitions for phase detection
                int initialEnemyHealth = enemy.GetEffectiveMaxHealth();
                var phaseTracker = new List<(int turn, double healthPercent)>();
                
                // Combat loop
                while (player.IsAlive && enemy.IsAlive && turnCount < MaxTurnsPerCombat)
                {
                    turnCount++;
                    
                    var actor = manager.GetNextEntityToAct();
                    if (actor == null) break;
                    
                    int healthBefore = actor == player ? player.CurrentHealth : enemy.CurrentHealth;
                    int targetHealthBefore = actor == player ? enemy.CurrentHealth : player.CurrentHealth;
                    
                    // Execute action
                    if (actor is Character character)
                    {
                        var action = SelectPlayerAction(character, enemy);
                        if (action != null)
                        {
                            CombatResults.ExecuteActionWithUIAndStatusEffectsColored(
                                character, enemy, action, null, null, null);
                            playerDamageDealt += Math.Max(0, targetHealthBefore - enemy.CurrentHealth);
                        }
                    }
                    else if (actor is Enemy enemyActor)
                    {
                        var action = SelectEnemyAction(enemyActor);
                        if (action != null)
                        {
                            CombatResults.ExecuteActionWithUIAndStatusEffectsColored(
                                enemyActor, player, action, null, null, null);
                            enemyDamageDealt += Math.Max(0, healthBefore - player.CurrentHealth);
                        }
                    }
                    
                    // Track phase transitions
                    double enemyHealthPercent = (double)enemy.CurrentHealth / initialEnemyHealth;
                    phaseTracker.Add((turnCount, enemyHealthPercent));
                }
                
                // Detect phases (phases go from 100% -> 66% -> 33% -> 0%)
                var (phase1, phase2, phase3) = DetectPhases(phaseTracker);
                
                return new CombatSimulationResult
                {
                    PlayerWon = !enemy.IsAlive,
                    TurnsToComplete = turnCount,
                    PlayerFinalHealth = player.CurrentHealth,
                    PlayerMaxHealth = player.GetEffectiveMaxHealth(),
                    EnemyFinalHealth = Math.Max(0, enemy.CurrentHealth),
                    EnemyMaxHealth = initialEnemyHealth,
                    
                    Phase1Turns = phase1,
                    Phase2Turns = phase2,
                    Phase3Turns = phase3,
                    
                    TotalPlayerDamageDealt = playerDamageDealt,
                    TotalEnemyDamageDealt = enemyDamageDealt,
                    AverageTurnsPerPhase = turnCount > 0 ? turnCount / 3.0 : 0,
                    
                    PlayerName = player.Name,
                    PlayerLevel = player.Level,
                    EquippedWeapon = (player.Weapon as WeaponItem)?.Name ?? "Unarmed",
                    
                    EnemyName = enemy.Name,
                    EnemyLevel = enemy.Level
                };
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = originalUIState;
            }
        }

        /// <summary>
        /// Detects phase transitions in combat
        /// Returns (phase1Turns, phase2Turns, phase3Turns)
        /// Phase 1: 100% -> 66%
        /// Phase 2: 66% -> 33%
        /// Phase 3: 33% -> 0%
        /// </summary>
        private static (int, int, int) DetectPhases(List<(int turn, double healthPercent)> healthTracker)
        {
            if (healthTracker.Count == 0)
                return (0, 0, 0);

            int phase1Start = healthTracker[0].turn;
            int phase2Start = -1;
            int phase3Start = -1;
            int combatEnd = healthTracker[^1].turn;

            // Find when health crosses 66% and 33%
            bool crossedPhase2 = false;
            bool crossedPhase3 = false;

            foreach (var (turn, health) in healthTracker)
            {
                if (!crossedPhase2 && health <= 0.66)
                {
                    phase2Start = turn;
                    crossedPhase2 = true;
                }
                if (!crossedPhase3 && health <= 0.33)
                {
                    phase3Start = turn;
                    crossedPhase3 = true;
                }
            }

            // Calculate turns per phase
            int phase1Turns = (phase2Start > 0 ? phase2Start : combatEnd) - phase1Start;
            int phase2Turns = (phase3Start > 0 ? phase3Start : combatEnd) - (phase2Start > 0 ? phase2Start : phase1Start);
            int phase3Turns = combatEnd - (phase3Start > 0 ? phase3Start : combatEnd);

            return (phase1Turns, phase2Turns, phase3Turns);
        }

        /// <summary>
        /// Simple AI for player action selection
        /// Prioritizes high-damage actions with reasonable cooldowns
        /// </summary>
        private static Action? SelectPlayerAction(Character player, Enemy enemy)
        {
            var availableActions = player.Actions.GetAllActions(player);
            if (availableActions.Count == 0)
                return null;

            // Prefer actions that are off cooldown and do damage
            var bestAction = availableActions
                .Where(a => !a.IsOnCooldown)
                .OrderByDescending(a => a.DamageMultiplier)
                .FirstOrDefault();

            return bestAction ?? availableActions[0];
        }

        /// <summary>
        /// Simple AI for enemy action selection
        /// </summary>
        private static Action? SelectEnemyAction(Enemy enemy)
        {
            var availableActions = enemy.ActionPool.Select(x => x.action).ToList();
            if (availableActions.Count == 0)
                return null;

            // Prefer actions that are off cooldown
            var bestAction = availableActions
                .Where(a => !a.IsOnCooldown)
                .OrderByDescending(a => a.DamageMultiplier)
                .FirstOrDefault();

            return bestAction ?? availableActions[0];
        }
    }
}
