using System;
using System.Collections.Generic;
using RPGGame.MCP.Models;

namespace RPGGame.MCP
{
    /// <summary>
    /// Tracks incremental changes to game state for efficient snapshot creation
    /// Only serializes changed properties instead of full deep copy
    /// </summary>
    public class IncrementalStateTracker
    {
        private GameStateSnapshot? _lastFullSnapshot = null;
        private readonly Dictionary<string, object?> _changedProperties = new Dictionary<string, object?>();
        private bool _isDirty = false;

        /// <summary>
        /// Marks a property as changed
        /// </summary>
        public void MarkDirty(string propertyPath, object? value)
        {
            _changedProperties[propertyPath] = value;
            _isDirty = true;
        }

        /// <summary>
        /// Marks player health as changed
        /// </summary>
        public void MarkPlayerHealthChanged(int currentHealth, int maxHealth, double healthPercentage)
        {
            MarkDirty("Player.CurrentHealth", currentHealth);
            MarkDirty("Player.MaxHealth", maxHealth);
            MarkDirty("Player.HealthPercentage", healthPercentage);
        }

        /// <summary>
        /// Marks player stats as changed
        /// </summary>
        public void MarkPlayerStatsChanged(int strength, int agility, int technique, int intelligence)
        {
            MarkDirty("Player.Strength", strength);
            MarkDirty("Player.Agility", agility);
            MarkDirty("Player.Technique", technique);
            MarkDirty("Player.Intelligence", intelligence);
        }

        /// <summary>
        /// Marks enemy health as changed
        /// </summary>
        public void MarkEnemyHealthChanged(int currentHealth, int maxHealth, double healthPercentage)
        {
            MarkDirty("Combat.CurrentEnemy.CurrentHealth", currentHealth);
            MarkDirty("Combat.CurrentEnemy.MaxHealth", maxHealth);
            MarkDirty("Combat.CurrentEnemy.HealthPercentage", healthPercentage);
        }

        /// <summary>
        /// Marks game state as changed
        /// </summary>
        public void MarkStateChanged(string newState)
        {
            MarkDirty("CurrentState", newState);
        }

        /// <summary>
        /// Marks combo step as changed
        /// </summary>
        public void MarkComboStepChanged(int comboStep)
        {
            MarkDirty("Player.ComboStep", comboStep);
        }

        /// <summary>
        /// Creates an incremental snapshot by applying deltas to last full snapshot
        /// </summary>
        public GameStateSnapshot CreateIncrementalSnapshot(GameStateSnapshot currentFullSnapshot)
        {
            if (!_isDirty || _lastFullSnapshot == null)
            {
                // First snapshot or no changes - return full snapshot
                _lastFullSnapshot = CloneSnapshot(currentFullSnapshot);
                _changedProperties.Clear();
                _isDirty = false;
                return _lastFullSnapshot;
            }

            // Apply deltas to last snapshot
            var incrementalSnapshot = CloneSnapshot(_lastFullSnapshot);

            foreach (var change in _changedProperties)
            {
                ApplyDelta(incrementalSnapshot, change.Key, change.Value);
            }

            // Update last snapshot
            _lastFullSnapshot = CloneSnapshot(incrementalSnapshot);
            _changedProperties.Clear();
            _isDirty = false;

            return incrementalSnapshot;
        }

        /// <summary>
        /// Creates a full snapshot (for first snapshot or when too many changes)
        /// </summary>
        public GameStateSnapshot CreateFullSnapshot(GameStateSnapshot snapshot)
        {
            _lastFullSnapshot = CloneSnapshot(snapshot);
            _changedProperties.Clear();
            _isDirty = false;
            return _lastFullSnapshot;
        }

        /// <summary>
        /// Applies a delta change to a snapshot
        /// </summary>
        private void ApplyDelta(GameStateSnapshot snapshot, string propertyPath, object? value)
        {
            var parts = propertyPath.Split('.');
            
            if (parts.Length == 1)
            {
                // Top-level property
                switch (parts[0])
                {
                    case "CurrentState":
                        snapshot.CurrentState = value?.ToString() ?? "";
                        break;
                }
            }
            else if (parts.Length == 2)
            {
                // Nested property
                switch (parts[0])
                {
                    case "Player":
                        ApplyPlayerDelta(snapshot, parts[1], value);
                        break;
                    case "Combat":
                        ApplyCombatDelta(snapshot, parts[1], value);
                        break;
                }
            }
            else if (parts.Length == 3)
            {
                // Deeply nested property
                if (parts[0] == "Combat" && parts[1] == "CurrentEnemy")
                {
                    ApplyEnemyDelta(snapshot, parts[2], value);
                }
            }
        }

        /// <summary>
        /// Applies a delta to player snapshot
        /// </summary>
        private void ApplyPlayerDelta(GameStateSnapshot snapshot, string property, object? value)
        {
            if (snapshot.Player == null)
                return;

            switch (property)
            {
                case "CurrentHealth":
                    snapshot.Player.CurrentHealth = value is int health ? health : 0;
                    break;
                case "MaxHealth":
                    snapshot.Player.MaxHealth = value is int maxHealth ? maxHealth : 0;
                    break;
                case "HealthPercentage":
                    snapshot.Player.HealthPercentage = value is double percentage ? percentage : 0.0;
                    break;
                case "Strength":
                    snapshot.Player.Strength = value is int str ? str : 0;
                    break;
                case "Agility":
                    snapshot.Player.Agility = value is int agi ? agi : 0;
                    break;
                case "Technique":
                    snapshot.Player.Technique = value is int tec ? tec : 0;
                    break;
                case "Intelligence":
                    snapshot.Player.Intelligence = value is int intel ? intel : 0;
                    break;
                case "ComboStep":
                    snapshot.Player.ComboStep = value is int step ? step : 0;
                    break;
            }
        }

        /// <summary>
        /// Applies a delta to combat snapshot
        /// </summary>
        private void ApplyCombatDelta(GameStateSnapshot snapshot, string property, object? value)
        {
            if (snapshot.Combat == null)
                snapshot.Combat = new CombatSnapshot();

            // Handle combat-level properties if needed
        }

        /// <summary>
        /// Applies a delta to enemy snapshot
        /// </summary>
        private void ApplyEnemyDelta(GameStateSnapshot snapshot, string property, object? value)
        {
            if (snapshot.Combat?.CurrentEnemy == null)
                return;

            switch (property)
            {
                case "CurrentHealth":
                    snapshot.Combat.CurrentEnemy.CurrentHealth = value is int health ? health : 0;
                    break;
                case "MaxHealth":
                    snapshot.Combat.CurrentEnemy.MaxHealth = value is int maxHealth ? maxHealth : 0;
                    break;
                case "HealthPercentage":
                    snapshot.Combat.CurrentEnemy.HealthPercentage = value is double percentage ? percentage : 0.0;
                    break;
            }
        }

        /// <summary>
        /// Creates a deep clone of a snapshot
        /// </summary>
        private GameStateSnapshot CloneSnapshot(GameStateSnapshot source)
        {
            return new GameStateSnapshot
            {
                CurrentState = source.CurrentState,
                Player = source.Player != null ? new PlayerSnapshot
                {
                    Name = source.Player.Name,
                    Level = source.Player.Level,
                    XP = source.Player.XP,
                    CurrentHealth = source.Player.CurrentHealth,
                    MaxHealth = source.Player.MaxHealth,
                    HealthPercentage = source.Player.HealthPercentage,
                    Strength = source.Player.Strength,
                    Agility = source.Player.Agility,
                    Technique = source.Player.Technique,
                    Intelligence = source.Player.Intelligence,
                    Weapon = source.Player.Weapon != null ? new WeaponSnapshot
                    {
                        Name = source.Player.Weapon.Name,
                        Tier = source.Player.Weapon.Tier,
                        BaseDamage = source.Player.Weapon.BaseDamage,
                        AttackSpeed = source.Player.Weapon.AttackSpeed,
                        Type = source.Player.Weapon.Type,
                        Rarity = source.Player.Weapon.Rarity
                    } : null,
                    Armor = source.Player.Armor != null ? new ArmorSnapshot
                    {
                        Head = source.Player.Armor.Head,
                        Body = source.Player.Armor.Body,
                        Feet = source.Player.Armor.Feet
                    } : null,
                    Inventory = new List<ItemSnapshot>(source.Player.Inventory),
                    ComboStep = source.Player.ComboStep
                } : null,
                CurrentDungeon = source.CurrentDungeon != null ? new DungeonSnapshot
                {
                    Name = source.CurrentDungeon.Name,
                    Level = source.CurrentDungeon.Level,
                    Theme = source.CurrentDungeon.Theme,
                    TotalRooms = source.CurrentDungeon.TotalRooms,
                    CurrentRoomNumber = source.CurrentDungeon.CurrentRoomNumber
                } : null,
                CurrentRoom = source.CurrentRoom != null ? new RoomSnapshot
                {
                    Name = source.CurrentRoom.Name,
                    Description = source.CurrentRoom.Description,
                    IsHostile = source.CurrentRoom.IsHostile,
                    EnemyCount = source.CurrentRoom.EnemyCount
                } : null,
                AvailableActions = new List<string>(source.AvailableActions),
                RecentOutput = new List<string>(source.RecentOutput),
                Combat = source.Combat != null ? new CombatSnapshot
                {
                    CurrentEnemy = source.Combat.CurrentEnemy != null ? new EnemySnapshot
                    {
                        Name = source.Combat.CurrentEnemy.Name,
                        Level = source.Combat.CurrentEnemy.Level,
                        CurrentHealth = source.Combat.CurrentEnemy.CurrentHealth,
                        MaxHealth = source.Combat.CurrentEnemy.MaxHealth,
                        HealthPercentage = source.Combat.CurrentEnemy.HealthPercentage,
                        PrimaryAttribute = source.Combat.CurrentEnemy.PrimaryAttribute,
                        IsAlive = source.Combat.CurrentEnemy.IsAlive
                    } : null,
                    AvailableCombatActions = new List<string>(source.Combat.AvailableCombatActions),
                    IsPlayerTurn = source.Combat.IsPlayerTurn
                } : null
            };
        }

        /// <summary>
        /// Resets the tracker (for new game or major state changes)
        /// </summary>
        public void Reset()
        {
            _lastFullSnapshot = null;
            _changedProperties.Clear();
            _isDirty = false;
        }

        /// <summary>
        /// Gets the number of tracked changes
        /// </summary>
        public int ChangeCount => _changedProperties.Count;

        /// <summary>
        /// Checks if tracker has changes
        /// </summary>
        public bool HasChanges => _isDirty;
    }
}
