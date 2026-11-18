using System;
namespace RPGGame
{
    /// <summary>
    /// Loot system configuration
    /// </summary>
    public class LootSystemConfig
    {
        public double GoldDropMultiplier { get; set; }
        public double ItemValueMultiplier { get; set; }

        /// <summary>Starting combo sequence length before gear <see cref="Item.ExtraActionSlots"/>, named class-tier upgrades, and Wand intrinsic bonus.</summary>
        public int ComboSequenceBaseMax { get; set; } = 2;

        /// <summary>Hard cap on combo sequence length after gear ExtraActionSlots (catalog + affix + StatBonuses), class-tier upgrades, and Wand intrinsic bonus (<see cref="ComboSequenceMaxHelper"/>).</summary>
        public int ComboSequenceAbsoluteMax { get; set; } = 8;

        /// <summary>
        /// Rebuild 2.5 (D4.7): charms are cut for now. While false no charm drops, the charm slot is hidden and refuses
        /// gear, and a loaded save's equipped charm moves to the bag. True brings the old slot back.
        /// </summary>
        public bool CharmsEnabled { get; set; }

        /// <summary>True when the charm slot is in the game (see <see cref="CharmsEnabled"/>).</summary>
        public static bool CharmSlotOn => GameConfiguration.Instance?.LootSystem?.CharmsEnabled ?? false;

        /// <summary>INT threshold for combo sequence unlocks (see <see cref="GameConstants.ComboSequenceIntelligenceThreshold"/>).</summary>
        public int ComboSequenceIntelligenceThreshold { get; set; } = 10;

        /// <summary>
        /// Each material piece rolls one item trigger from its material's pool (<see cref="RPGGame.Data.MaterialTriggerCatalog"/>),
        /// so set pieces carry the procs their set feeds on (e.g. Silver Prism: connect → Focus, which mints LENS).
        /// false = legacy: material sets own all procs and pieces carry none.
        /// </summary>
        public bool MaterialPieceTriggers { get; set; }

        /// <summary>
        /// Level cap on combo strip slots as [level, slots] anchors (e.g. [[1,1],[10,3]]): the highest anchor at or below the
        /// hero's level caps base + class slots. Gear and Action Lab sandbox slots are added on top. Empty = no level cap (legacy). Action Lab sandbox slots bypass it.
        /// </summary>
        public System.Collections.Generic.List<int[]> ComboSlotCapByLevel { get; set; } = new();

        /// <summary>Slot cap for <paramref name="level"/> from <see cref="ComboSlotCapByLevel"/>; 0 = uncapped.</summary>
        public int GetComboSlotCapForLevel(int level)
        {
            int cap = 0, bestLevel = int.MinValue;
            if (ComboSlotCapByLevel == null)
                return 0;
            foreach (var a in ComboSlotCapByLevel)
            {
                if (a == null || a.Length < 2 || a[0] > level || a[0] < bestLevel)
                    continue;
                bestLevel = a[0];
                cap = a[1];
            }
            return Math.Max(0, cap);
        }

        /// <summary>
        /// Starter and lab helpers (<see cref="ItemMaterialRules.PickClassMaterial(WeaponType, Random)"/>): chance a weapon
        /// takes one of its weapon type's three family materials. Rolled loot uses <see cref="ItemGeneration"/> instead.
        /// </summary>
        public double WeaponFamilyMaterialShare { get; set; } = ItemMaterialRules.DefaultWeaponFamilyMaterialShare;

        /// <summary>Item generation knobs (slot / class / material shares, roll-ups, influence, level scaling).</summary>
        public ItemGenerationConfig ItemGeneration { get; set; } = new();

        public string Description { get; set; } = "";

        public void EnsureSensibleLootDefaults()
        {
            if (GoldDropMultiplier <= 0)
                GoldDropMultiplier = 1.0;
            if (ItemValueMultiplier <= 0)
                ItemValueMultiplier = 1.0;
            if (ComboSequenceBaseMax < 1)
                ComboSequenceBaseMax = 2;
            if (ComboSequenceAbsoluteMax < ComboSequenceBaseMax)
                ComboSequenceAbsoluteMax = ComboSequenceBaseMax;
            if (ComboSequenceIntelligenceThreshold <= 0)
                ComboSequenceIntelligenceThreshold = 10;
            ItemGeneration ??= new ItemGenerationConfig();
            ItemGeneration.EnsureSensibleDefaults();
        }
    }

    /// <summary>
    /// Modification rarity configuration
    /// </summary>
    public class ModificationRarityConfig
    {
        public double Common { get; set; } = 35.0;
        public double Uncommon { get; set; } = 25.0;
        public double Rare { get; set; } = 20.0;
        public double Epic { get; set; } = 12.0;
        public double Legendary { get; set; } = 6.0;
        public double Mythic { get; set; } = 1.8;
        public double TierBonusPerLevel { get; set; } = 1.5;
        public double BonusPointEffectiveness { get; set; } = 1.0;
        public string Description { get; set; } = "Modification rarity distribution percentages. Total should equal 100.0";
    }
}
