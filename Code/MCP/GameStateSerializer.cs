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
            var inputContext = AgentChoiceBuilder.ResolveInputContext(game);
            var choices = AgentChoiceBuilder.BuildChoices(game, inputContext);

            var fullSnapshot = new GameStateSnapshot
            {
                CurrentState = game.CurrentState.ToString(),
                Player = game.CurrentPlayer != null ? SerializePlayer(game.CurrentPlayer) : null,
                CurrentDungeon = game.CurrentDungeon != null ? SerializeDungeon(game.CurrentDungeon, game.CurrentRoom) : null,
                CurrentRoom = game.CurrentRoom != null ? SerializeRoom(game.CurrentRoom) : null,
                AvailableActions = AgentChoiceBuilder.GetInputKeys(choices),
                RecentOutput = includeRecentOutput ? (outputCapture?.GetRecentOutput(10) ?? new List<string>()) : new List<string>(),
                PendingInputMode = inputContext.PendingInputMode,
                CustomLevelBuffer = inputContext.CustomLevelBuffer,
                Hints = AgentChoiceBuilder.BuildHints(inputContext, game.CurrentState),
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

        private static DungeonSnapshot SerializeDungeon(Dungeon dungeon, Environment? currentRoom)
        {
            int roomNumber = 0;
            if (dungeon.Rooms != null && currentRoom != null)
            {
                int idx = dungeon.Rooms.IndexOf(currentRoom);
                if (idx >= 0)
                    roomNumber = idx + 1;
            }

            return new DungeonSnapshot
            {
                Name = dungeon.Name,
                Level = dungeon.MinLevel, // Use MinLevel as the dungeon level
                Theme = dungeon.Theme,
                TotalRooms = dungeon.Rooms?.Count ?? 0,
                CurrentRoomNumber = roomNumber
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

        private static bool ResolveIsPlayerTurn(GameCoordinator game)
        {
            var next = game.Combat?.GetNextEntityToAct();
            if (game.CurrentPlayer == null || next == null)
                return true;
            return ReferenceEquals(next, game.CurrentPlayer);
        }

        private static CombatSnapshot SerializeCombat(GameCoordinator game)
        {
            var inputContext = AgentChoiceBuilder.ResolveInputContext(game);
            var combat = new CombatSnapshot
            {
                AvailableCombatActions = AgentChoiceBuilder.GetInputKeys(AgentChoiceBuilder.BuildCombatChoices(game)),
                IsPlayerTurn = ResolveIsPlayerTurn(game)
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
            var inputContext = AgentChoiceBuilder.ResolveInputContext(game);
            return AgentChoiceBuilder.GetInputKeys(AgentChoiceBuilder.BuildChoices(game, inputContext));
        }
    }
}

