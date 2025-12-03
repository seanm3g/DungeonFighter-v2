using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Formats session statistics for display
    /// </summary>
    public static class StatisticsFormatter
    {
        /// <summary>
        /// Gets a formatted summary of session statistics
        /// </summary>
        public static string FormatDefeatSummary(SessionStatistics stats)
        {
            stats.EndSession();
            
            var summary = new List<string>();
            summary.Add("═══════════════════════════════════════");
            summary.Add("DEFEAT STATISTICS");
            summary.Add("═══════════════════════════════════════");
            summary.Add("");
            
            // Character progression
            summary.Add("📈 CHARACTER PROGRESSION:");
            summary.Add($"   Level: {stats.StartingLevel} → {stats.FinalLevel} (+{stats.LevelsGained})");
            summary.Add($"   Total XP Gained: {stats.TotalXP:N0}");
            summary.Add($"   Play Time: {stats.TotalPlayTimeMinutes} minutes");
            summary.Add("");
            
            // Combat performance
            summary.Add("⚔️  COMBAT PERFORMANCE:");
            summary.Add($"   Enemies Defeated: {stats.EnemiesDefeated}");
            summary.Add($"   Total Damage Dealt: {stats.TotalDamageDealt:N0}");
            summary.Add($"   Total Damage Received: {stats.TotalDamageReceived:N0}");
            summary.Add($"   Total Healing Received: {stats.TotalHealingReceived:N0}");
            summary.Add($"   Hit Rate: {stats.HitRate:F1}%");
            summary.Add($"   Critical Hit Rate: {stats.CriticalHitRate:F1}%");
            summary.Add("");
            
            // Damage records
            summary.Add("💥 DAMAGE RECORDS:");
            summary.Add($"   Highest Single Hit: {stats.HighestSingleHitDamage}");
            summary.Add($"   Highest Turn Damage: {stats.HighestTurnDamage}");
            summary.Add($"   Average Damage per Action: {stats.AverageDamagePerAction:F1}");
            summary.Add("");
            
            // Combo statistics
            summary.Add("🔥 COMBO MASTERY:");
            summary.Add($"   Total Combos Executed: {stats.TotalCombosExecuted}");
            summary.Add($"   Highest Combo Step: {stats.HighestComboStep}");
            summary.Add($"   Total Combo Damage: {stats.TotalComboDamage:N0}");
            summary.Add($"   Combo Success Rate: {stats.ComboSuccessRate:F1}%");
            summary.Add("");
            
            // Item collection
            summary.Add("🎒 ITEM COLLECTION:");
            summary.Add($"   Items Collected: {stats.ItemsCollected}");
            summary.Add($"   Items Equipped: {stats.ItemsEquipped}");
            summary.Add($"   Rare Items: {stats.RareItemsFound}");
            summary.Add($"   Epic Items: {stats.EpicItemsFound}");
            summary.Add($"   Legendary Items: {stats.LegendaryItemsFound}");
            summary.Add("");
            
            // Exploration
            summary.Add("🗺️  EXPLORATION:");
            summary.Add($"   Dungeons Completed: {stats.DungeonsCompleted}");
            summary.Add($"   Rooms Explored: {stats.RoomsExplored}");
            summary.Add($"   Encounters Survived: {stats.EncountersSurvived}");
            summary.Add("");
            
            // Achievements
            var achievements = new List<string>();
            if (stats.SurvivedNearDeath) achievements.Add("Near Death Survivor");
            if (stats.PerfectCombat) achievements.Add("Perfect Combat");
            if (stats.MassiveCombo) achievements.Add("Combo Master");
            if (stats.OneShotKill) achievements.Add("One-Shot Wonder");
            if (stats.HighestComboStep >= 3) achievements.Add("Combo Expert");
            if (stats.TotalCriticalHits >= 10) achievements.Add("Critical Striker");
            if (stats.ItemsCollected >= 20) achievements.Add("Treasure Hunter");
            if (stats.EnemiesDefeated >= 10) achievements.Add("Monster Slayer");
            
            if (achievements.Count > 0)
            {
                summary.Add("🏆 ACHIEVEMENTS UNLOCKED:");
                foreach (var achievement in achievements)
                {
                    summary.Add($"   ✓ {achievement}");
                }
                summary.Add("");
            }
            
            summary.Add("═══════════════════════════════════════");
            summary.Add("Better luck next time!");
            summary.Add("═══════════════════════════════════════");
            
            return string.Join("\n", summary);
        }
    }
}

