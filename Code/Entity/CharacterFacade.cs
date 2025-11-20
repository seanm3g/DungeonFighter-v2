using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Facade pattern for Character to provide a simplified interface
    /// Hides complex internal structure and provides clean, organized access to functionality
    /// </summary>
    public class CharacterFacade
    {
        private readonly Character _character;
        private readonly CharacterPropertyDelegator _properties;
        private readonly EquipmentManager _equipment;
        private readonly LevelUpManager _levelUp;

        public CharacterFacade(Character character)
        {
            _character = character;
            _properties = new CharacterPropertyDelegator(character);
            _equipment = new EquipmentManager(character);
            _levelUp = new LevelUpManager(character);
        }

        // === PROPERTY ACCESS ===
        public CharacterPropertyDelegator Properties => _properties;

        // === HEALTH MANAGEMENT ===
        public void TakeDamage(int amount) => _character.Health.TakeDamage(amount);
        public List<string> TakeDamageWithNotifications(int amount) => _character.Health.TakeDamageWithNotifications(amount);
        public (int finalDamage, int shieldReduction, bool shieldUsed) CalculateDamageWithShield(int amount) => _character.Health.CalculateDamageWithShield(amount);
        public void Heal(int amount) => _character.Health.Heal(amount);
        public bool IsAlive => _character.Health.IsAlive;
        public int GetEffectiveMaxHealth() => _character.Health.GetEffectiveMaxHealth();
        public double GetHealthPercentage() => _character.Health.GetHealthPercentage();
        public bool MeetsHealthThreshold(double threshold) => _character.Health.MeetsHealthThreshold(threshold);
        public void ApplyHealthMultiplier(double multiplier) => _character.Health.ApplyHealthMultiplier(multiplier);

        // === EQUIPMENT MANAGEMENT ===
        public Item? EquipItem(Item item, string slot) => _equipment.EquipItem(item, slot);
        public Item? UnequipItem(string slot) => _equipment.UnequipItem(slot);
        public void AddToInventory(Item item) => _equipment.AddToInventory(item);
        public bool RemoveFromInventory(Item item) => _equipment.RemoveFromInventory(item);

        // === LEVELING AND PROGRESSION ===
        public void AddXP(int amount)
        {
            int oldLevel = _character.Progression.Level;
            _character.Progression.AddXP(amount);
            
            // Apply character-level changes for each level gained
            int levelsGained = _character.Progression.Level - oldLevel;
            for (int i = 0; i < levelsGained; i++)
            {
                _levelUp.LevelUp();
            }
        }

        // === STAT ACCESS ===
        public int Strength 
        { 
            get => _character.Stats.GetEffectiveStrength(_character.Equipment.GetEquipmentStatBonus("STR"), _character.Equipment.GetModificationGodlikeBonus());
            set => _character.Stats.Strength = value;
        }
        public int Agility 
        { 
            get => _character.Stats.GetEffectiveAgility(_character.Equipment.GetEquipmentStatBonus("AGI"));
            set => _character.Stats.Agility = value;
        }
        public int Technique 
        { 
            get => _character.Stats.GetEffectiveTechnique(_character.Equipment.GetEquipmentStatBonus("TEC"));
            set => _character.Stats.Technique = value;
        }
        public int Intelligence 
        { 
            get => _character.Stats.GetEffectiveIntelligence(_character.Equipment.GetEquipmentStatBonus("INT"));
            set => _character.Stats.Intelligence = value;
        }

        // === EFFECTIVE STAT METHODS ===
        public int GetEffectiveStrength() => _character.Stats.GetEffectiveStrength(_character.Equipment.GetEquipmentStatBonus("STR"), _character.Equipment.GetModificationGodlikeBonus());
        public int GetEffectiveAgility() => _character.Stats.GetEffectiveAgility(_character.Equipment.GetEquipmentStatBonus("AGI"));
        public int GetEffectiveTechnique() => _character.Stats.GetEffectiveTechnique(_character.Equipment.GetEquipmentStatBonus("TEC"));
        public int GetEffectiveIntelligence() => _character.Stats.GetEffectiveIntelligence(_character.Equipment.GetEquipmentStatBonus("INT"));

        // === ACTION MANAGEMENT ===
        public List<Action> GetComboActions() => _character.Actions.GetComboActions();
        public List<Action> GetActionPool() => _character.Actions.GetActionPool(_character);
        public void AddToCombo(Action action) => _character.Actions.AddToCombo(action);
        public void RemoveFromCombo(Action action) => _character.Actions.RemoveFromCombo(action);
        public void InitializeDefaultCombo() => _character.Actions.InitializeDefaultCombo(_character, _character.Equipment.Weapon as WeaponItem);
        public double CalculateTurnsFromActionLength(double actionLength) => _character.Actions.CalculateTurnsFromActionLength(actionLength);
        public void RemoveItemActions() => _character.Actions.RemoveItemActions(_character);
        public void ApplyRollBonusesFromGear(Item gear) => _character.Actions.ApplyRollBonusesFromGear(_character, gear);

        // === EFFECT MANAGEMENT ===
        public void SetTempComboBonus(int bonus, int turns) => _character.Effects.SetTempComboBonus(bonus, turns);
        public int ConsumeTempComboBonus() => _character.Effects.ConsumeTempComboBonus();
        public void ActivateComboMode() => _character.Effects.ActivateComboMode();
        public void DeactivateComboMode() => _character.Effects.DeactivateComboMode();
        public void ResetCombo() => _character.Effects.ResetCombo();
        public void ApplyStatBonus(int bonus, string statType, int duration) => _character.Stats.ApplyStatBonus(bonus, statType, duration);
        public void UpdateTempEffects(double actionLength = 1.0)
        {
            // Update base class effects directly (avoid circular call)
            _character.Stats.UpdateTempEffects(actionLength);
            _character.Effects.UpdateTempEffects(actionLength);
            // Update base Actor effects (stun, weaken, roll penalty, poison, burn, damage reduction)
            double turnsPassed = actionLength / 1.0; // Using 1.0 as default action length
            
            // Update stun effects
            if (_character.StunTurnsRemaining > 0)
            {
                _character.StunTurnsRemaining = Math.Max(0, _character.StunTurnsRemaining - (int)Math.Ceiling(turnsPassed));
                if (_character.StunTurnsRemaining == 0)
                    _character.IsStunned = false;
            }
            
            // Update roll penalty effects
            if (_character.RollPenaltyTurns > 0)
            {
                _character.RollPenaltyTurns = Math.Max(0, _character.RollPenaltyTurns - (int)Math.Ceiling(turnsPassed));
                if (_character.RollPenaltyTurns == 0)
                    _character.RollPenalty = 0;
            }
            
            // Update weaken debuff
            if (_character.WeakenTurns > 0)
            {
                _character.WeakenTurns = Math.Max(0, _character.WeakenTurns - (int)Math.Ceiling(turnsPassed));
                if (_character.WeakenTurns == 0)
                {
                    _character.IsWeakened = false;
                }
            }
            
            // Update poison effects
            if (_character.PoisonStacks > 0)
            {
                _character.PoisonStacks = Math.Max(0, _character.PoisonStacks - (int)Math.Ceiling(turnsPassed));
                if (_character.PoisonStacks == 0)
                {
                    _character.PoisonDamage = 0;
                    _character.IsBleeding = false;
                }
            }
            
            // Update burn effects
            if (_character.BurnStacks > 0)
            {
                _character.BurnStacks = Math.Max(0, _character.BurnStacks - (int)Math.Ceiling(turnsPassed));
                if (_character.BurnStacks == 0)
                {
                    _character.BurnDamage = 0;
                }
            }
        }
        public void ApplySlow(double slowMultiplier, int duration) => _character.Effects.ApplySlow(slowMultiplier, duration);
        public void ApplyPoison(int damage, int stacks = 1, bool isBleeding = false) => _character.ApplyPoison(damage, stacks, isBleeding);
        public void ApplyBurn(int damage, int stacks = 1) => _character.ApplyBurn(damage, stacks);
        public void ApplyShield() => _character.Effects.ApplyShield();
        public bool ConsumeShield() => _character.Effects.ConsumeShield();
        public void ApplyWeaken(int turns) => _character.ApplyWeaken(turns);
        public int ProcessPoison(double currentTime) => _character.ProcessPoison(currentTime);
        public int ProcessBurn(double currentTime) => _character.ProcessBurn(currentTime);
        public string GetDamageTypeText() => _character.GetDamageTypeText();
        public void ClearAllTempEffects() 
        {
            // Clear base class effects directly (avoid circular call)
            _character.Effects.ClearAllTempEffects();
            // Clear temporary stat bonuses
            _character.Stats.TempStrengthBonus = 0;
            _character.Stats.TempAgilityBonus = 0;
            _character.Stats.TempTechniqueBonus = 0;
            _character.Stats.TempIntelligenceBonus = 0;
            _character.Stats.TempStatBonusTurns = 0;
            // Clear base Actor effects (poison, burn, stun, weaken, etc.)
            _character.PoisonDamage = 0;
            _character.PoisonStacks = 0;
            _character.IsBleeding = false;
            _character.LastPoisonTick = 0.0;
            _character.BurnDamage = 0;
            _character.BurnStacks = 0;
            _character.LastBurnTick = 0.0;
            _character.IsStunned = false;
            _character.StunTurnsRemaining = 0;
            _character.IsWeakened = false;
            _character.WeakenTurns = 0;
            _character.RollPenalty = 0;
            _character.RollPenaltyTurns = 0;
            _character.DamageReduction = 0.0;
        }
        public bool UseReroll() => _character.Effects.UseReroll();
        public void ResetRerollUsage() => _character.Effects.ResetRerollUsage();
        public void ResetRerollCharges() => _character.Effects.ResetRerollCharges();
        public int GetRemainingRerollCharges() => _character.Effects.GetRemainingRerollCharges(_character.Equipment.GetTotalRerollCharges());
        public bool UseRerollCharge() => _character.Effects.UseRerollCharge();

        // === EQUIPMENT BONUSES ===
        public int GetEquipmentDamageBonus() => _character.Equipment.GetEquipmentDamageBonus();
        public int GetEquipmentHealthBonus() => _character.Equipment.GetEquipmentHealthBonus();
        public int GetEquipmentRollBonus() => _character.Equipment.GetEquipmentRollBonus();
        public int GetMagicFind() => _character.Equipment.GetMagicFind();
        public double GetEquipmentAttackSpeedBonus() => _character.Equipment.GetEquipmentAttackSpeedBonus();
        public int GetEquipmentHealthRegenBonus() => _character.Equipment.GetEquipmentHealthRegenBonus();
        public int GetTotalArmor() => _character.Equipment.GetTotalArmor();
        public int GetTotalRerollCharges() => _character.Equipment.GetTotalRerollCharges();
        public int GetModificationMagicFind() => _character.Equipment.GetModificationMagicFind();
        public int GetModificationRollBonus() => _character.Equipment.GetModificationRollBonus();
        public int GetModificationDamageBonus() => _character.Equipment.GetModificationDamageBonus();
        public double GetModificationSpeedMultiplier() => _character.Equipment.GetModificationSpeedMultiplier();
        public double GetModificationDamageMultiplier() => _character.Equipment.GetModificationDamageMultiplier();
        public double GetModificationLifesteal() => _character.Equipment.GetModificationLifesteal();
        public int GetModificationGodlikeBonus() => _character.Equipment.GetModificationGodlikeBonus();
        public double GetModificationBleedChance() => _character.Equipment.GetModificationBleedChance();
        public double GetModificationUniqueActionChance() => _character.Equipment.GetModificationUniqueActionChance();
        public double GetArmorSpikeDamage() => _character.Equipment.GetArmorSpikeDamage();
        public List<ArmorStatus> GetEquippedArmorStatuses() => _character.Equipment.GetEquippedArmorStatuses();
        public bool HasAutoSuccess() => _character.Equipment.HasAutoSuccess();

        // === PROGRESSION ===
        public string GetCurrentClass() => _character.Progression.GetCurrentClass();
        public string GetFullNameWithQualifier() => _character.Progression.GetFullNameWithQualifier(_character.Name);
        public int GetNextClassThreshold(string className) => _character.Progression.GetNextClassThreshold(className);
        public string GetClassUpgradeInfo() => _character.Progression.GetClassUpgradeInfo();
        public void AwardClassPoint(WeaponType weaponType) => _character.Progression.AwardClassPoint(weaponType);

        // === COMBAT CALCULATIONS ===
        public double GetComboAmplifier() => _character.Combat.GetComboAmplifier();
        public double GetCurrentComboAmplification() => _character.Combat.GetCurrentComboAmplification();
        public double GetNextComboAmplification() => _character.Combat.GetNextComboAmplification();
        public string GetComboInfo() => _character.Combat.GetComboInfo();
        public int GetAttacksPerTurn() => _character.Combat.GetAttacksPerTurn();
        public double GetTotalAttackSpeed() => _character.Combat.GetTotalAttackSpeed();
        public int GetIntelligenceRollBonus() => _character.Combat.GetIntelligenceRollBonus();
        public void SetTechniqueForTesting(int value) => _character.Combat.SetTechniqueForTesting(value);
        public bool MeetsStatThreshold(string statType, double threshold) => _character.Combat.MeetsStatThreshold(statType, threshold);

        // === ENVIRONMENT ACTIONS ===
        public void AddEnvironmentActions(Environment environment) => _character.Actions.AddEnvironmentActions(_character, environment);
        public void ClearEnvironmentActions() => _character.Actions.ClearEnvironmentActions(_character);

        // === UNIQUE ACTIONS ===
        public List<Action> GetAvailableUniqueActions() => _character.Actions.GetAvailableUniqueActions(_character.Weapon as WeaponItem);

        // === DISPLAY METHODS ===
        public string GetDescription() => _character.Display.GetDescription();
        public void DisplayCharacterInfo() => _character.Display.DisplayCharacterInfo();

        // === SAVE/LOAD METHODS ===
        public void SaveCharacter(string? filename = null) => CharacterSaveManager.SaveCharacter(_character, filename);
        public static Character? LoadCharacter(string? filename = null) => CharacterSaveManager.LoadCharacter(filename);
        public static void DeleteSaveFile(string? filename = null) => CharacterSaveManager.DeleteSaveFile(filename);

        // === DIRECT ACCESS TO UNDERLYING CHARACTER ===
        public Character Character => _character;
    }
}


