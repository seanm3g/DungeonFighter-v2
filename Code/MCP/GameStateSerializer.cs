using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.MCP.Models;

namespace RPGGame.MCP
{
    /// <summary>
    /// Serializes game state to JSON-serializable snapshots for MCP tools
    /// Supports incremental snapshots for performance optimization
    /// </summary>
    public static class GameStateSerializer
    {
        // Static tracker instance for incremental snapshots
        private static IncrementalStateTracker? _stateTracker = null;
        private const int MaxChangesBeforeFullSnapshot = 20; // Threshold for full snapshot

        /// <summary>
        /// Gets or creates the state tracker
        /// </summary>
        private static IncrementalStateTracker GetStateTracker()
        {
            if (_stateTracker == null)
            {
                _stateTracker = new IncrementalStateTracker();
            }
            return _stateTracker;
        }

        /// <summary>
        /// Serializes game state with incremental snapshot support
        /// </summary>
        public static GameStateSnapshot SerializeGameState(
            GameCoordinator game,
            OutputCapture? outputCapture = null,
            bool includeRecentOutput = false,
            bool useIncremental = true)
        {
            // Always create full snapshot first
            var fullSnapshot = new GameStateSnapshot
            {
                CurrentState = game.CurrentState.ToString(),
                Player = game.CurrentPlayer != null ? SerializePlayer(game.CurrentPlayer) : null,
                CurrentDungeon = game.CurrentDungeon != null ? SerializeDungeon(game.CurrentDungeon) : null,
                CurrentRoom = game.CurrentRoom != null ? SerializeRoom(game.CurrentRoom) : null,
                AvailableActions = GetAvailableActions(game),
                RecentOutput = includeRecentOutput ? (outputCapture?.GetRecentOutput(10) ?? new List<string>()) : new List<string>(),
            };

            // Add combat state if in combat
            if (game.CurrentState == GameState.Combat && game.CurrentPlayer != null)
            {
                fullSnapshot.Combat = SerializeCombat(game);
            }

            // Use incremental snapshot if enabled and tracker has changes
            if (useIncremental)
            {
                var tracker = GetStateTracker();
                
                // If too many changes, create full snapshot
                if (tracker.ChangeCount > MaxChangesBeforeFullSnapshot)
                {
                    return tracker.CreateFullSnapshot(fullSnapshot);
                }
                
                // Otherwise use incremental
                return tracker.CreateIncrementalSnapshot(fullSnapshot);
            }

            return fullSnapshot;
        }

        /// <summary>
        /// Marks player health as changed (call before SerializeGameState for incremental updates)
        /// </summary>
        public static void MarkPlayerHealthChanged(int currentHealth, int maxHealth, double healthPercentage)
        {
            GetStateTracker().MarkPlayerHealthChanged(currentHealth, maxHealth, healthPercentage);
        }

        /// <summary>
        /// Marks enemy health as changed (call before SerializeGameState for incremental updates)
        /// </summary>
        public static void MarkEnemyHealthChanged(int currentHealth, int maxHealth, double healthPercentage)
        {
            GetStateTracker().MarkEnemyHealthChanged(currentHealth, maxHealth, healthPercentage);
        }

        /// <summary>
        /// Marks combo step as changed (call before SerializeGameState for incremental updates)
        /// </summary>
        public static void MarkComboStepChanged(int comboStep)
        {
            GetStateTracker().MarkComboStepChanged(comboStep);
        }

        /// <summary>
        /// Resets the incremental tracker (call on major state changes like new game)
        /// </summary>
        public static void ResetIncrementalTracker()
        {
            _stateTracker?.Reset();
        }

        private static PlayerSnapshot SerializePlayer(Character player)
        {
            return new PlayerSnapshot
            {
                Name = player.Name,
                Level = player.Level,
                XP = player.XP,
                CurrentHealth = player.CurrentHealth,
                MaxHealth = player.MaxHealth,
                HealthPercentage = player.GetHealthPercentage(),
                Strength = player.Strength,
                Agility = player.Agility,
                Technique = player.Technique,
                Intelligence = player.Intelligence,
                Weapon = player.Weapon != null ? SerializeWeapon(player.Weapon as WeaponItem) : null,
                Armor = SerializeArmor(player),
                Inventory = player.Inventory.Select(SerializeItem).ToList(),
                ComboStep = player.ComboStep
            };
        }

        private static WeaponSnapshot? SerializeWeapon(WeaponItem? weapon)
        {
            if (weapon == null) return null;

            return new WeaponSnapshot
            {
                Name = weapon.Name,
                Tier = weapon.Tier,
                BaseDamage = weapon.BaseDamage,
                AttackSpeed = weapon.GetTotalAttackSpeed(),
                Type = weapon.Type.ToString(),
                Rarity = weapon.Rarity ?? "Common"
            };
        }

        private static ArmorSnapshot SerializeArmor(Character player)
        {
            return new ArmorSnapshot
            {
                Head = player.Head?.Name,
                Body = player.Body?.Name,
                Feet = player.Feet?.Name
            };
        }

        private static ItemSnapshot SerializeItem(Item item)
        {
            return new ItemSnapshot
            {
                Name = item.Name,
                Type = item.GetType().Name,
                Tier = item.Tier,
                Rarity = item.Rarity ?? "Common"
            };
        }

        private static DungeonSnapshot SerializeDungeon(Dungeon dungeon)
        {
            return new DungeonSnapshot
            {
                Name = dungeon.Name,
                Level = dungeon.MinLevel, // Use MinLevel as the dungeon level
                Theme = dungeon.Theme,
                TotalRooms = dungeon.Rooms?.Count ?? 0,
                CurrentRoomNumber = 0 // TODO: Track current room number
            };
        }

        private static RoomSnapshot SerializeRoom(Environment room)
        {
            return new RoomSnapshot
            {
                Name = room.Name,
                Description = room.Description,
                IsHostile = room.IsHostile,
                EnemyCount = room.GetEnemies()?.Count(e => e.IsAlive) ?? 0
            };
        }

        private static CombatSnapshot SerializeCombat(GameCoordinator game)
        {
            var combat = new CombatSnapshot
            {
                AvailableCombatActions = GetCombatActions(game),
                IsPlayerTurn = true // TODO: Determine from combat state
            };

            // Get current enemy from combat state
            if (game.CurrentRoom != null)
            {
                var livingEnemy = game.CurrentRoom.GetEnemies()?.FirstOrDefault(e => e.IsAlive);
                if (livingEnemy != null)
                {
                    combat.CurrentEnemy = SerializeEnemy(livingEnemy);
                }
            }

            return combat;
        }

        private static EnemySnapshot SerializeEnemy(Enemy enemy)
        {
            return new EnemySnapshot
            {
                Name = enemy.Name,
                Level = enemy.Level,
                CurrentHealth = enemy.CurrentHealth,
                MaxHealth = enemy.MaxHealth,
                HealthPercentage = enemy.GetHealthPercentage(),
                PrimaryAttribute = enemy.PrimaryAttribute.ToString(),
                IsAlive = enemy.IsAlive
            };
        }

        private static List<string> GetAvailableActions(GameCoordinator game)
        {
            var actions = new List<string>();

            switch (game.CurrentState)
            {
                case GameState.MainMenu:
                    actions.AddRange(new[] { "1", "2", "3", "0" }); // Start, Load, Settings, Exit
                    break;

                case GameState.GameLoop:
                    actions.AddRange(new[] { "1", "2", "0" }); // Dungeon, Inventory, Exit
                    break;

                case GameState.DungeonSelection:
                    // Add dungeon selection options
                    if (game.AvailableDungeons != null && game.AvailableDungeons.Count > 0)
                    {
                        for (int i = 1; i <= game.AvailableDungeons.Count; i++)
                        {
                            actions.Add(i.ToString());
                        }
                    }
                    actions.Add("0"); // Back
                    break;

                case GameState.Combat:
                    actions.AddRange(GetCombatActions(game));
                    break;

                case GameState.Inventory:
                    actions.AddRange(new[] { "1", "2", "3", "4", "5", "6", "0" });
                    break;
            }

            return actions;
        }

        private static List<string> GetCombatActions(GameCoordinator game)
        {
            var actions = new List<string>();

            if (game.CurrentPlayer != null)
            {
                // Get available actions from character
                var comboActions = game.CurrentPlayer.GetComboActions();
                if (comboActions != null && comboActions.Count > 0)
                {
                    for (int i = 0; i < comboActions.Count; i++)
                    {
                        actions.Add((i + 1).ToString());
                    }
                }
            }

            return actions;
        }
    }
}

