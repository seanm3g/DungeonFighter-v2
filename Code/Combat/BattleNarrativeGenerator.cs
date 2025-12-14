using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Combat
{
    /// <summary>
    /// Pure narrative logic for battle events
    /// Handles event analysis and narrative generation without formatting concerns
    /// </summary>
    public static class BattleNarrativeGenerator
    {
        /// <summary>
        /// Generates narrative text for a battle event
        /// </summary>
        public static List<string> GenerateNarratives(RPGGame.BattleEvent evt, BattleNarrativeContext context)
        {
            var narratives = new List<string>();
            var settings = RPGGame.GameSettings.Instance;
            
            // Use the event analyzer from context
            var triggeredNarratives = context.EventAnalyzer.AnalyzeEvent(evt, settings);
            
            // Only return narratives if narrative events are enabled
            if (settings.EnableNarrativeEvents)
            {
                narratives.AddRange(triggeredNarratives);
            }
            
            return narratives;
        }
        
        /// <summary>
        /// Generates informational summary text for battle end
        /// </summary>
        public static string GenerateInformationalSummary(
            int totalPlayerDamage, 
            int totalEnemyDamage, 
            int playerComboCount, 
            int enemyComboCount, 
            bool playerWon,
            bool enemyWon,
            string playerName,
            string enemyName,
            bool showDamageWhenEnemyKilled = false)
        {
            if (playerWon)
            {
                // When enemy is killed during combat, only show combos executed (no total damage line)
                // But if showDamageWhenEnemyKilled is true (for final summary), show both
                if (showDamageWhenEnemyKilled)
                {
                    return $"Total damage dealt: {totalPlayerDamage} vs {totalEnemyDamage} received.\n" +
                           $"Combos executed: {playerComboCount} vs {enemyComboCount}.";
                }
                else
                {
                    return $"Combos executed: {playerComboCount} vs {enemyComboCount}.";
                }
            }
            else if (enemyWon)
            {
                return $"{enemyName} defeats {playerName}!\nTotal damage dealt: {totalEnemyDamage} vs {totalPlayerDamage} received.\n" +
                       $"Combos executed: {enemyComboCount} vs {playerComboCount}.";
            }
            else
            {
                return $"Battle ends in a stalemate. {playerName} dealt {totalPlayerDamage} damage, {enemyName} dealt {totalEnemyDamage} damage.\n" +
                       $"Combos: {playerComboCount} vs {enemyComboCount}.";
            }
        }
        
        /// <summary>
        /// Calculates battle statistics from events
        /// </summary>
        public static BattleStatistics CalculateStatistics(List<RPGGame.BattleEvent> events, string playerName, string enemyName)
        {
            var playerEvents = events.Where(e => e.Actor == playerName && e.IsSuccess).ToList();
            var enemyEvents = events.Where(e => e.Actor == enemyName && e.IsSuccess).ToList();
            
            return new BattleStatistics
            {
                TotalPlayerDamage = playerEvents.Sum(e => e.Damage),
                TotalEnemyDamage = enemyEvents.Sum(e => e.Damage),
                PlayerComboCount = playerEvents.Count(e => e.IsCombo),
                EnemyComboCount = enemyEvents.Count(e => e.IsCombo)
            };
        }
    }
    
    /// <summary>
    /// Context information for narrative generation
    /// </summary>
    public class BattleNarrativeContext
    {
        public RPGGame.BattleEventAnalyzer EventAnalyzer { get; set; } = null!;
        public string PlayerName { get; set; } = "";
        public string EnemyName { get; set; } = "";
        public string CurrentLocation { get; set; } = "";
    }
    
    /// <summary>
    /// Battle statistics for summary generation
    /// </summary>
    public class BattleStatistics
    {
        public int TotalPlayerDamage { get; set; }
        public int TotalEnemyDamage { get; set; }
        public int PlayerComboCount { get; set; }
        public int EnemyComboCount { get; set; }
    }
}

