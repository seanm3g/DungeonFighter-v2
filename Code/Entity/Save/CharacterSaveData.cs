using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Data class for character save/load operations
    /// </summary>
    public class CharacterSaveData
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int XP { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public int BarbarianPoints { get; set; }
        public int WarriorPoints { get; set; }
        public int RoguePoints { get; set; }
        public int WizardPoints { get; set; }

        /// <summary>Learned skill ranks by node id. Preferred over <see cref="LearnedSkillNodeIds"/>.</summary>
        public Dictionary<string, int> LearnedSkillRanks { get; set; } = new();

        /// <summary>Legacy learned skill-tree node ids (each becomes rank 1 on load). Omitted on new saves.</summary>
        public List<string> LearnedSkillNodeIds { get; set; } = new();
        public int ComboStep { get; set; }
        public int ComboBonus { get; set; }
        public int TempComboBonus { get; set; }
        public int TempComboBonusTurns { get; set; }
        public double DamageReduction { get; set; }
        public List<Item> Inventory { get; set; } = new List<Item>();
        public Item? Head { get; set; }
        public Item? Body { get; set; }
        public Item? Legs { get; set; }
        public Item? Weapon { get; set; }
        public Item? Feet { get; set; }

        /// <summary>
        /// Ordered combo-strip action names. Omitted/empty on legacy saves falls back to default combo after load.
        /// </summary>
        public List<string> ComboStripActionNames { get; set; } = new List<string>();

        /// <summary>When true, this file is a tombstone only; the game will not load this adventurer.</summary>
        public bool IsDead { get; set; }

        /// <summary>When true, resume pre-weapon Training Ground flow after load (omit or false for legacy saves).</summary>
        public bool PendingPreWeaponTrainingGround { get; set; }

        /// <summary>Current region id for the character; omitted legacy saves default to Ancient Forest.</summary>
        public string CurrentRegionId { get; set; } = GameConstants.DefaultRegionId;
    }
}

