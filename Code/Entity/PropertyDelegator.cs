using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Utility class to reduce repetitive property delegation patterns in Character
    /// Provides a clean way to delegate properties to component managers
    /// </summary>
    public static class PropertyDelegator
    {
        /// <summary>
        /// Creates a property accessor that delegates to a component
        /// </summary>
        public static T GetProperty<T>(Func<T> getter) => getter();

        /// <summary>
        /// Creates a property setter that delegates to a component
        /// </summary>
        public static void SetProperty<T>(Action<T> setter, T value) => setter(value);

        /// <summary>
        /// Creates a read-only property that delegates to a component
        /// </summary>
        public static T GetReadOnlyProperty<T>(Func<T> getter) => getter();
    }

    /// <summary>
    /// Generic property delegation helper for Character components
    /// </summary>
    public class CharacterPropertyDelegator
    {
        private readonly Character _character;

        public CharacterPropertyDelegator(Character character)
        {
            _character = character;
        }

        // Health properties
        public int CurrentHealth
        {
            get => _character.Health.CurrentHealth;
            set => _character.Health.CurrentHealth = value;
        }

        public int MaxHealth
        {
            get => _character.Health.MaxHealth;
            set => _character.Health.MaxHealth = value;
        }

        // Equipment properties
        public List<Item> Inventory
        {
            get => _character.Equipment.Inventory;
            set => _character.Equipment.Inventory = value;
        }

        public Item? Head
        {
            get => _character.Equipment.Head;
            set => _character.Equipment.Head = value;
        }

        public Item? Body
        {
            get => _character.Equipment.Body;
            set => _character.Equipment.Body = value;
        }

        public Item? Weapon
        {
            get => _character.Equipment.Weapon;
            set => _character.Equipment.Weapon = value;
        }

        public Item? Feet
        {
            get => _character.Equipment.Feet;
            set => _character.Equipment.Feet = value;
        }

        // Progression properties
        public int Level
        {
            get => _character.Progression.Level;
            set => _character.Progression.Level = value;
        }

        public int XP
        {
            get => _character.Progression.XP;
            set => _character.Progression.XP = value;
        }

        // Class points (read-only)
        public int BarbarianPoints => _character.Progression.BarbarianPoints;
        public int WarriorPoints => _character.Progression.WarriorPoints;
        public int RoguePoints => _character.Progression.RoguePoints;
        public int WizardPoints => _character.Progression.WizardPoints;

        // Effects properties
        public int ComboStep
        {
            get => _character.Effects.ComboStep;
            set => _character.Effects.ComboStep = value;
        }

        public double ComboAmplifier => _character.Effects.ComboAmplifier;
        public int ComboBonus => _character.Effects.ComboBonus;
        public int TempComboBonus => _character.Effects.TempComboBonus;
        public int TempComboBonusTurns => _character.Effects.TempComboBonusTurns;
        public int EnemyRollPenalty
        {
            get => _character.Effects.EnemyRollPenalty;
            set => _character.Effects.EnemyRollPenalty = value;
        }
        public int EnemyRollPenaltyTurns
        {
            get => _character.Effects.EnemyRollPenaltyTurns;
            set => _character.Effects.EnemyRollPenaltyTurns = value;
        }
        public double SlowMultiplier => _character.Effects.SlowMultiplier;
        public int SlowTurns => _character.Effects.SlowTurns;
        public bool HasShield => _character.Effects.HasShield;
        public int LastShieldReduction => _character.Effects.LastShieldReduction;
        public bool ComboModeActive => _character.Effects.ComboModeActive;
        public Action? LastAction => _character.Effects.LastAction;
        public bool SkipNextTurn => _character.Effects.SkipNextTurn;
        public bool GuaranteeNextSuccess => _character.Effects.GuaranteeNextSuccess;
        public int ExtraAttacks => _character.Effects.ExtraAttacks;
        public int ExtraDamage
        {
            get => _character.Effects.ExtraDamage;
            set => _character.Effects.ExtraDamage = value;
        }
        public double LengthReduction
        {
            get => _character.Effects.LengthReduction;
            set => _character.Effects.LengthReduction = value;
        }
        public int LengthReductionTurns
        {
            get => _character.Effects.LengthReductionTurns;
            set => _character.Effects.LengthReductionTurns = value;
        }
        public double ComboAmplifierMultiplier => _character.Effects.ComboAmplifierMultiplier;
        public int ComboAmplifierTurns => _character.Effects.ComboAmplifierTurns;
        public int RerollCharges => _character.Effects.RerollCharges;
        public bool UsedRerollThisTurn => _character.Effects.UsedRerollThisTurn;

        // Stats properties
        public int TempStrengthBonus => _character.Stats.TempStrengthBonus;
        public int TempAgilityBonus => _character.Stats.TempAgilityBonus;
        public int TempTechniqueBonus => _character.Stats.TempTechniqueBonus;
        public int TempIntelligenceBonus => _character.Stats.TempIntelligenceBonus;
        public int TempStatBonusTurns => _character.Stats.TempStatBonusTurns;

        // Combo sequence
        public List<Action> ComboSequence => _character.Actions.ComboSequence;
    }
}
