using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Handles dungeon execution logic
    /// Extracted from DungeonManager to follow Single Responsibility Principle
    /// </summary>
    public class DungeonRunner
    {
        /// <summary>
        /// Runs a complete dungeon with all its rooms
        /// </summary>
        /// <param name="selectedDungeon">The dungeon to run</param>
        /// <param name="player">The player character</param>
        /// <param name="combatManager">Combat manager for handling battles</param>
        /// <returns>True if player survived the dungeon, false if player died</returns>
        public bool RunDungeon(Dungeon selectedDungeon, Character player, CombatManager combatManager)
        {
            UIManager.WriteDungeonLine($"\nEntering {selectedDungeon.Name}...\n");

            // Room Sequence
            foreach (Environment room in selectedDungeon.Rooms)
            {
                if (!ProcessRoom(room, player, combatManager))
                {
                    return false; // Player died
                }
            }

            return true; // Player survived the dungeon
        }

        /// <summary>
        /// Processes a single room in the dungeon
        /// </summary>
        /// <param name="room">The room to process</param>
        /// <param name="player">The player character</param>
        /// <param name="combatManager">Combat manager for handling battles</param>
        /// <returns>True if player survived the room, false if player died</returns>
        private bool ProcessRoom(Environment room, Character player, CombatManager combatManager)
        {
            UIManager.WriteRoomLine($"Entering room: {room.Name}");
            UIManager.ApplyDelay(UIMessageType.Encounter); // Add configurable delay after encounter message

            UIManager.WriteRoomLine("");
            UIManager.WriteRoomLine(room.Description);
            UIManager.ApplyDelay(UIMessageType.Encounter);
            
            // Track room exploration statistics
            player.RecordRoomExplored();
            
            // Clear all temporary effects when entering a new room
            player.ClearAllTempEffects();

            while (room.HasLivingEnemies())
            {
                Enemy? currentEnemy = room.GetNextLivingEnemy();
                if (currentEnemy == null) break;

                if (!ProcessEnemyEncounter(currentEnemy, player, room, combatManager))
                {
                    return false; // Player died
                }
            }
            
            DisplayRoomCompletion(player);
            return true; // Player survived the room
        }

        /// <summary>
        /// Processes a single enemy encounter
        /// </summary>
        /// <param name="currentEnemy">The enemy to fight</param>
        /// <param name="player">The player character</param>
        /// <param name="room">The current room</param>
        /// <param name="combatManager">Combat manager for handling battles</param>
        /// <returns>True if player survived the encounter, false if player died</returns>
        private bool ProcessEnemyEncounter(Enemy currentEnemy, Character player, Environment room, CombatManager combatManager)
        {
            DisplayEnemyEncounter(currentEnemy, player, combatManager);
            
            // Clear all temporary effects before each fight
            player.ClearAllTempEffects();
            
            // Reset Divine reroll charges for new combat
            player.ResetRerollCharges();
            
            // Run combat using CombatManager
            bool playerSurvived = combatManager.RunCombat(player, currentEnemy, room);
            
            if (!playerSurvived)
            {
                HandlePlayerDefeat(player);
                return false; // Player died
            }
            else
            {
                HandleEnemyDefeat(currentEnemy, player);
            }
            
            return true; // Player survived
        }

        /// <summary>
        /// Displays enemy encounter information
        /// </summary>
        private void DisplayEnemyEncounter(Enemy currentEnemy, Character player, CombatManager combatManager)
        {
            // Display enemy encounter with weapon information
            string enemyWeaponInfo = "";
            if (currentEnemy.Weapon != null)
            {
                enemyWeaponInfo = $" with {currentEnemy.Weapon.Name}";
            }
            UIManager.WriteEnemyLine($"\nEncountered [{currentEnemy.Name}]{enemyWeaponInfo}!");
            UIManager.ApplyDelay(UIMessageType.Encounter); // Add configurable delay after encounter message
            
            // Display stats using stats blocks (no spacing between consecutive stats)
            BlockDisplayManager.DisplayStatsBlock($"Hero Stats - Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}, Armor: {player.GetTotalArmor()}, Attack: STR {player.GetEffectiveStrength()}, AGI {player.GetEffectiveAgility()}, TEC {player.GetEffectiveTechnique()}, INT {player.GetEffectiveIntelligence()}, Attack Time: {player.GetTotalAttackSpeed():F2}s");
            BlockDisplayManager.DisplayStatsBlock($"Enemy Stats - Health: {currentEnemy.CurrentHealth}/{currentEnemy.MaxHealth}, Armor: {currentEnemy.Armor}, Attack: STR {currentEnemy.Strength}, AGI {currentEnemy.Agility}, TEC {currentEnemy.Technique}, INT {currentEnemy.Intelligence}, Attack Time: {currentEnemy.GetTotalAttackSpeed():F2}s");
            
            UIManager.ApplyDelay(UIMessageType.Encounter); // Add configurable delay after encounter message

            // Show action speed info
            var speedSystem = combatManager.GetCurrentActionSpeedSystem();
            if (speedSystem != null)
            {
                UIManager.WriteMenuLine($"Turn Order: {speedSystem.GetTurnOrderInfo()}");
            }
            
            // Add manual spacing before combat starts (this is intentional UI design)
            UIManager.WriteBlankLine();
        }

        /// <summary>
        /// Handles player defeat
        /// </summary>
        private void HandlePlayerDefeat(Character player)
        {
            // Use TextDisplayIntegration for consistent entity tracking
            TextDisplayIntegration.DisplayCombatAction("\nYou have been defeated!", new List<string>(), new List<string>(), "System");
            
            // Display comprehensive defeat statistics
            string defeatSummary = player.GetDefeatSummary();
            Console.WriteLine(defeatSummary);
            
            // Delete save file when character dies
            Character.DeleteSaveFile();
        }

        /// <summary>
        /// Handles enemy defeat
        /// </summary>
        private void HandleEnemyDefeat(Enemy currentEnemy, Character player)
        {
            // Use TextDisplayIntegration for consistent entity tracking
            string defeatMessage = $"[{currentEnemy.Name}] has been defeated!";
            TextDisplayIntegration.DisplayCombatAction(defeatMessage, new List<string>(), new List<string>(), "System");
            player.AddXP(currentEnemy.XPReward);
            
            // Track statistics
            player.RecordEnemyDefeat();
            player.RecordXPGain(currentEnemy.XPReward);
            player.RecordEncounterSurvived();
            
            // Display narrative if balance is set to show poetic text
            var narrativeSettings = GameSettings.Instance;
            if (narrativeSettings.NarrativeBalance > 0.3)
            {
                // Battle narrative completed message removed
            }
        }

        /// <summary>
        /// Displays room completion information
        /// </summary>
        private void DisplayRoomCompletion(Character player)
        {
            UIManager.WriteRoomClearedLine($"\nRemaining Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}");
            UIManager.WriteRoomClearedLine("\nRoom cleared!");
            UIManager.WriteRoomClearedLine("====================================");
            
            // Reset combo at end of each room
            player.ResetCombo();
        }
    }
}
