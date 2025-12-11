using System;
using System.Collections.Generic;

namespace RPGGame.MCP.Models
{
    /// <summary>
    /// Serializable snapshot of game state for MCP tools
    /// </summary>
    public class GameStateSnapshot
    {
        public string CurrentState { get; set; } = "";
        public PlayerSnapshot? Player { get; set; }
        public DungeonSnapshot? CurrentDungeon { get; set; }
        public RoomSnapshot? CurrentRoom { get; set; }
        public List<string> AvailableActions { get; set; } = new();
        public List<string> RecentOutput { get; set; } = new();
        public CombatSnapshot? Combat { get; set; }
    }

    public class PlayerSnapshot
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int XP { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public double HealthPercentage { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public WeaponSnapshot? Weapon { get; set; }
        public ArmorSnapshot? Armor { get; set; }
        public List<ItemSnapshot> Inventory { get; set; } = new();
        public int ComboStep { get; set; }
    }

    public class WeaponSnapshot
    {
        public string Name { get; set; } = "";
        public int Tier { get; set; }
        public int BaseDamage { get; set; }
        public double AttackSpeed { get; set; }
        public string Type { get; set; } = "";
        public string Rarity { get; set; } = "";
    }

    public class ArmorSnapshot
    {
        public string? Head { get; set; }
        public string? Body { get; set; }
        public string? Feet { get; set; }
    }

    public class ItemSnapshot
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int Tier { get; set; }
        public string Rarity { get; set; } = "";
    }

    public class DungeonSnapshot
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public string Theme { get; set; } = "";
        public int TotalRooms { get; set; }
        public int CurrentRoomNumber { get; set; }
    }

    public class RoomSnapshot
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsHostile { get; set; }
        public int EnemyCount { get; set; }
    }

    public class CombatSnapshot
    {
        public EnemySnapshot? CurrentEnemy { get; set; }
        public List<string> AvailableCombatActions { get; set; } = new();
        public bool IsPlayerTurn { get; set; }
    }

    public class EnemySnapshot
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public double HealthPercentage { get; set; }
        public string PrimaryAttribute { get; set; } = "";
        public bool IsAlive { get; set; }
    }
}

