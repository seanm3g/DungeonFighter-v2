using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Tracks comprehensive session statistics for the player's current game session
    /// </summary>
    public class SessionStatistics
    {
        // Basic session info
        public DateTime SessionStartTime { get; set; }
        public DateTime? SessionEndTime { get; set; }
        public int TotalPlayTimeMinutes { get; set; }
        
        // Character progression
        public int StartingLevel { get; set; }
        public int FinalLevel { get; set; }
        public int TotalXP { get; set; }
        public int LevelsGained { get; set; }
        
        // Combat statistics
        public int EnemiesDefeated { get; set; }
        public int TotalDamageDealt { get; set; }
        public int TotalDamageReceived { get; set; }
        public int TotalHealingReceived { get; set; }
        public int TotalActionsPerformed { get; set; }
        public int TotalMisses { get; set; }
        public int TotalCriticalHits { get; set; }
        public int TotalCriticalMisses { get; set; }
        
        // Combo statistics
        public int TotalCombosExecuted { get; set; }
        public int HighestComboStep { get; set; }
        public int TotalComboDamage { get; set; }
        
        // Damage records
        public int HighestSingleHitDamage { get; set; }
        public int HighestTurnDamage { get; set; }
        public int CurrentTurnDamage { get; set; }
        
        // Item statistics
        public int ItemsCollected { get; set; }
        public int ItemsEquipped { get; set; }
        public int RareItemsFound { get; set; }
        public int EpicItemsFound { get; set; }
        public int LegendaryItemsFound { get; set; }
        
        // Dungeon statistics
        public int DungeonsCompleted { get; set; }
        public int RoomsExplored { get; set; }
        public int EncountersSurvived { get; set; }
        
        // Efficiency metrics
        public double AverageDamagePerTurn { get; set; }
        public double AverageDamagePerAction { get; set; }
        public double HitRate { get; set; }
        public double CriticalHitRate { get; set; }
        public double ComboSuccessRate { get; set; }
        
        // Special achievements
        public bool SurvivedNearDeath { get; set; } // Health below 10%
        public bool PerfectCombat { get; set; } // No damage taken in a combat
        public bool MassiveCombo { get; set; } // Combo of 5+ steps
        public bool OneShotKill { get; set; } // Defeated enemy in single hit
        
        public SessionStatistics()
        {
            SessionStartTime = DateTime.Now;
            StartingLevel = 1;
            FinalLevel = 1;
            HighestComboStep = 0;
            HighestSingleHitDamage = 0;
            HighestTurnDamage = 0;
            CurrentTurnDamage = 0;
        }
        
        /// <summary>
        /// Records an enemy defeat
        /// </summary>
        public void RecordEnemyDefeat()
        {
            EnemiesDefeated++;
        }
        
        /// <summary>
        /// Records damage dealt by the player
        /// </summary>
        public void RecordDamageDealt(int damage, bool isCritical = false)
        {
            TotalDamageDealt += damage;
            CurrentTurnDamage += damage;
            
            if (damage > HighestSingleHitDamage)
            {
                HighestSingleHitDamage = damage;
            }
            
            if (isCritical)
            {
                TotalCriticalHits++;
            }
        }
        
        /// <summary>
        /// Records damage received by the player
        /// </summary>
        public void RecordDamageReceived(int damage)
        {
            TotalDamageReceived += damage;
        }
        
        /// <summary>
        /// Records healing received by the player
        /// </summary>
        public void RecordHealingReceived(int healing)
        {
            TotalHealingReceived += healing;
        }
        
        /// <summary>
        /// Records an action performed
        /// </summary>
        public void RecordAction(bool hit, bool isCritical = false, bool isCriticalMiss = false)
        {
            TotalActionsPerformed++;
            
            if (!hit)
            {
                TotalMisses++;
            }
            
            if (isCriticalMiss)
            {
                TotalCriticalMisses++;
            }
        }
        
        /// <summary>
        /// Records a combo execution
        /// </summary>
        public void RecordCombo(int comboStep, int comboDamage)
        {
            TotalCombosExecuted++;
            TotalComboDamage += comboDamage;
            
            if (comboStep > HighestComboStep)
            {
                HighestComboStep = comboStep;
            }
            
            if (comboStep >= 5)
            {
                MassiveCombo = true;
            }
        }
        
        /// <summary>
        /// Records an item collection
        /// </summary>
        public void RecordItemCollected(Item item)
        {
            ItemsCollected++;
            
            switch (item.Rarity)
            {
                case "Rare":
                    RareItemsFound++;
                    break;
                case "Epic":
                    EpicItemsFound++;
                    break;
                case "Legendary":
                    LegendaryItemsFound++;
                    break;
            }
        }
        
        /// <summary>
        /// Records an item being equipped
        /// </summary>
        public void RecordItemEquipped()
        {
            ItemsEquipped++;
        }
        
        /// <summary>
        /// Records dungeon completion
        /// </summary>
        public void RecordDungeonCompleted()
        {
            DungeonsCompleted++;
        }
        
        /// <summary>
        /// Records room exploration
        /// </summary>
        public void RecordRoomExplored()
        {
            RoomsExplored++;
        }
        
        /// <summary>
        /// Records encounter survival
        /// </summary>
        public void RecordEncounterSurvived()
        {
            EncountersSurvived++;
        }
        
        /// <summary>
        /// Records level up
        /// </summary>
        public void RecordLevelUp(int newLevel)
        {
            FinalLevel = newLevel;
            LevelsGained = FinalLevel - StartingLevel;
        }
        
        /// <summary>
        /// Records XP gain
        /// </summary>
        public void RecordXPGain(int xp)
        {
            TotalXP += xp;
        }
        
        /// <summary>
        /// Records health status for achievements
        /// </summary>
        public void RecordHealthStatus(double healthPercentage)
        {
            if (healthPercentage <= 0.1)
            {
                SurvivedNearDeath = true;
            }
        }
        
        /// <summary>
        /// Records perfect combat (no damage taken)
        /// </summary>
        public void RecordPerfectCombat()
        {
            PerfectCombat = true;
        }
        
        /// <summary>
        /// Records one-shot kill
        /// </summary>
        public void RecordOneShotKill()
        {
            OneShotKill = true;
        }
        
        /// <summary>
        /// Ends the current turn and updates turn damage record
        /// </summary>
        public void EndTurn()
        {
            if (CurrentTurnDamage > HighestTurnDamage)
            {
                HighestTurnDamage = CurrentTurnDamage;
            }
            CurrentTurnDamage = 0;
        }
        
        /// <summary>
        /// Ends the session and calculates final statistics
        /// </summary>
        public void EndSession()
        {
            SessionEndTime = DateTime.Now;
            TotalPlayTimeMinutes = (int)(SessionEndTime.Value - SessionStartTime).TotalMinutes;
            
            // Calculate efficiency metrics
            if (TotalActionsPerformed > 0)
            {
                HitRate = ((double)(TotalActionsPerformed - TotalMisses) / TotalActionsPerformed) * 100;
                CriticalHitRate = ((double)TotalCriticalHits / TotalActionsPerformed) * 100;
            }
            
            if (TotalActionsPerformed > 0)
            {
                AverageDamagePerAction = (double)TotalDamageDealt / TotalActionsPerformed;
            }
            
            if (EncountersSurvived > 0)
            {
                AverageDamagePerTurn = (double)TotalDamageDealt / EncountersSurvived;
            }
            
            if (TotalCombosExecuted > 0)
            {
                ComboSuccessRate = ((double)TotalCombosExecuted / EncountersSurvived) * 100;
            }
        }
        
        /// <summary>
        /// Gets a formatted summary of session statistics
        /// </summary>
        public string GetDefeatSummary()
        {
            return StatisticsFormatter.FormatDefeatSummary(this);
        }
        
        /// <summary>
        /// Saves session statistics to a JSON file
        /// </summary>
        /// <param name="filename">The filename to save to</param>
        public void SaveSessionStatistics(string? filename = null)
        {
            StatisticsSerializer.SaveSessionStatistics(this, filename);
        }
        
        /// <summary>
        /// Loads session statistics from a JSON file
        /// </summary>
        /// <param name="filename">The filename to load from</param>
        /// <returns>Loaded session statistics or new instance if file doesn't exist</returns>
        public static SessionStatistics LoadSessionStatistics(string? filename = null)
        {
            return StatisticsSerializer.LoadSessionStatistics(filename);
        }
    }
}
