using System.Text.Json;

namespace RPGGame
{
    public interface IComboMemory
    {
        int LastComboActionIdx { get; set; }
    }

    public class Character : Entity, IComboMemory
    {
        // Attributes
        public int Strength { get; protected set; }
        public int Agility { get; protected set; }
        public int Technique { get; protected set; }
        public int Intelligence { get; protected set; }

        // Health
        public int CurrentHealth { get; protected set; }
        public int MaxHealth { get; protected set; }

        // Level and XP
        public int Level { get; protected set; }
        public int XP { get; protected set; }

        // Inventory
        public List<Item> Inventory { get; private set; }

        // Item slots
        public Item? Head { get; private set; }
        public Item? Body { get; private set; }
        public Item? Weapon { get; private set; }
        public Item? Feet { get; private set; }

        // Combo system state
        public int ComboStep { get; set; } = 0;
        public double ComboAmplifier { get; set; } = 1.0;
        public int ComboBonus { get; set; } = 0; // For loot integration
        // Temporary bonus for Taunt
        public int TempComboBonus { get; set; } = 0;
        public int TempComboBonusTurns { get; set; } = 0;
        public int LastComboActionIdx { get; set; } = -1;
        // New combo mode tracking
        public bool ComboModeActive { get; set; } = false;
        
        // Combo sequence - the actions currently selected for the combo
        public List<Action> ComboSequence { get; private set; } = new List<Action>();

        // New mechanics for advanced actions
        public int TempStrengthBonus { get; set; } = 0;
        public int TempAgilityBonus { get; set; } = 0;
        public int TempTechniqueBonus { get; set; } = 0;
        public int TempIntelligenceBonus { get; set; } = 0;
        public int TempStatBonusTurns { get; set; } = 0;
        public Action? LastAction { get; set; } = null; // For Deja Vu
        public bool SkipNextTurn { get; set; } = false; // For True Strike
        public bool GuaranteeNextSuccess { get; set; } = false; // For True Strike
        public int ExtraAttacks { get; set; } = 0; // For Flurry/Precision Strike
        public int ExtraDamage { get; set; } = 0; // For Opening Volley
        public double DamageReduction { get; set; } = 0.0; // For Sharp Edge
        public bool IsStunned { get; set; } = false; // For Stun effects
        public int StunTurnsRemaining { get; set; } = 0; // Stun duration
        public double LengthReduction { get; set; } = 0.0; // For Taunt
        public int LengthReductionTurns { get; set; } = 0; // Length reduction duration
        public int EnemyRollPenalty { get; set; } = 0; // For Quick Reflexes
        public int EnemyRollPenaltyTurns { get; set; } = 0; // Penalty duration
        public double ComboAmplifierMultiplier { get; set; } = 1.0; // For Pretty Boy Swag
        public int ComboAmplifierTurns { get; set; } = 0; // Amplifier duration

        // Class points system
        public int BarbarianPoints { get; set; } = 0; // Mace weapon
        public int WarriorPoints { get; set; } = 0;   // Sword weapon
        public int RoguePoints { get; set; } = 0;     // Dagger weapon
        public int WizardPoints { get; set; } = 0;    // Wand weapon

        public Character(string? name = null, int level = 1)
            : base(name ?? FlavorText.GenerateCharacterName())
        {
            Level = level;
            // Initialize attributes - balanced for all classes
            Strength = 20;
            Agility = 20;
            Technique = 20;
            Intelligence = 20;

            // Initialize health
            MaxHealth = 100 + level * 3;
            CurrentHealth = MaxHealth;

            // Initialize level and XP
            XP = 0;

            // Initialize inventory
            Inventory = new List<Item>();

            // Initialize item slots
            Head = null;
            Body = null;
            Weapon = null;
            Feet = null;

            // Add default actions
            AddDefaultActions();
            AddComboActions();
            
            // Initialize combo sequence with core class actions
            InitializeDefaultCombo();
        }

        private void AddDefaultActions()
        {
            // Add basic attack that's always available
            var basicAttack = new Action(
                name: "BASIC ATTACK",
                type: ActionType.Attack,
                targetType: TargetType.SingleTarget,
                baseValue: 0,
                range: 1,
                cooldown: 0,
                description: "A simple attack",
                comboOrder: -1, // Basic attack doesn't participate in combos
                damageMultiplier: 1.0,
                length: 1.0,
                causesBleed: false,
                causesWeaken: false,
                isComboAction: false
            );
            AddAction(basicAttack, 1.0); // High probability for basic attack
        }

        private void AddComboActions()
        {
            // Load class actions based on current weapon
            AddClassActions();
        }

        private void AddClassActions()
        {
            // Remove existing class actions first
            RemoveClassActions();
            
            // No core class actions - all actions come from equipped gear
        }

        private void LoadClassActionsFromJson(WeaponType weaponType)
        {
            try
            {
                string jsonPath = Path.Combine("..", "GameData", "Actions.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var allActions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (allActions != null)
                    {
                        // Define which actions belong to which class
                        var classActions = GetClassActions(weaponType);
                        
                        foreach (var actionData in allActions)
                        {
                            if (classActions.Contains(actionData.name))
                            {
                                var action = CreateActionFromData(actionData);
                                if (action.IsComboAction)
                                {
                                    AddAction(action, 0.0);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading class actions from JSON: {ex.Message}");
            }
        }

        private List<string> GetClassActions(WeaponType weaponType)
        {
            // No core class actions - all actions come from equipped gear
            return new List<string>();
        }

        private void LoadActionsFromJson()
        {
            try
            {
                string jsonPath = Path.Combine("..", "GameData", "Actions.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var actions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (actions != null)
                    {
                        // Clear existing actions first
                        ActionPool.Clear();
                        
                        foreach (var actionData in actions)
                        {
                            var action = CreateActionFromData(actionData);
                            if (action.IsComboAction)
                            {
                                AddAction(action, 0.0);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Actions.json file not found at {jsonPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading actions from JSON: {ex.Message}");
            }
        }


        private Action CreateActionFromData(ActionData data)
        {
            var action = new Action(
                name: data.name,
                type: ParseActionType(data.type),
                targetType: TargetType.SingleTarget,
                baseValue: 0,
                range: 1,
                cooldown: 0,
                description: data.description ?? "",
                comboOrder: data.comboOrder ?? 0,
                damageMultiplier: data.damageMultiplier,
                length: data.length,
                causesBleed: data.causesBleed ?? false,
                causesWeaken: data.causesWeaken ?? false,
                isComboAction: data.isComboAction ?? false,
                comboBonusAmount: data.comboBonusAmount ?? 0,
                comboBonusDuration: data.comboBonusDuration ?? 0
            );
            
            // Set additional properties if they exist in the JSON
            if (data.statBonus.HasValue)
                action.StatBonus = data.statBonus.Value;
            if (!string.IsNullOrEmpty(data.statBonusType))
                action.StatBonusType = data.statBonusType;
            if (data.statBonusDuration.HasValue)
                action.StatBonusDuration = data.statBonusDuration.Value;
            if (data.multiHitCount.HasValue)
                action.MultiHitCount = data.multiHitCount.Value;
            if (data.multiHitDamagePercent.HasValue)
                action.MultiHitDamagePercent = data.multiHitDamagePercent.Value;
            if (data.selfDamagePercent.HasValue)
                action.SelfDamagePercent = data.selfDamagePercent.Value;
            if (data.rollBonus.HasValue)
                action.RollBonus = data.rollBonus.Value;
            if (data.skipNextTurn.HasValue)
                action.SkipNextTurn = data.skipNextTurn.Value;
            if (data.repeatLastAction.HasValue)
                action.RepeatLastAction = data.repeatLastAction.Value;
                
            return action;
        }

        private ActionType ParseActionType(string type)
        {
            return type.ToLower() switch
            {
                "attack" => ActionType.Attack,
                "heal" => ActionType.Heal,
                "buff" => ActionType.Buff,
                "debuff" => ActionType.Debuff,
                "spell" => ActionType.Spell,
                _ => ActionType.Attack
            };
        }

        // Methods to add/remove items from inventory
        public void AddToInventory(Item item) => Inventory.Add(item);
        public bool RemoveFromInventory(Item item) => Inventory.Remove(item);

        // Methods to equip/unequip items
        public Item? EquipItem(Item item, string slot)
        {
            Item? previousItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    previousItem = Head;
                    Head = item;
                    // Add armor actions if applicable
                    AddArmorActions(item);
                    break;
                case "body": 
                    previousItem = Body;
                    Body = item;
                    // Add armor actions if applicable
                    AddArmorActions(item);
                    break;
                case "weapon": 
                    previousItem = Weapon;
                    Weapon = item;
                    // Add weapon-specific actions
                    if (item is WeaponItem weapon)
                    {
                        AddWeaponActions(weapon);
                    }
                    break;
                case "feet": 
                    previousItem = Feet;
                    Feet = item;
                    // Add armor actions if applicable
                    AddArmorActions(item);
                    break;
            }
            
            // Reinitialize combo sequence after equipment change
            InitializeDefaultCombo();
            
            return previousItem;
        }

        public Item? UnequipItem(string slot)
        {
            Item? unequippedItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    unequippedItem = Head;
                    Head = null;
                    // Remove armor actions if applicable
                    RemoveArmorActions(unequippedItem);
                    break;
                case "body": 
                    unequippedItem = Body;
                    Body = null;
                    // Remove armor actions if applicable
                    RemoveArmorActions(unequippedItem);
                    break;
                case "weapon": 
                    unequippedItem = Weapon;
                    Weapon = null;
                    // Remove weapon actions
                    RemoveWeaponActions();
                    break;
                case "feet": 
                    unequippedItem = Feet;
                    Feet = null;
                    // Remove armor actions if applicable
                    RemoveArmorActions(unequippedItem);
                    break;
            }
            return unequippedItem;
        }

        // Methods to add XP and handle leveling up
        public void AddXP(int amount)
        {
            XP += amount;
            while (XP >= XPToNextLevel())
            {
                XP -= XPToNextLevel();
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            MaxHealth += 10;
            CurrentHealth = MaxHealth;
            Strength += 2;
            Agility += 2;
            Technique += 2;
            Intelligence += 2;
            
            // Award class point based on equipped weapon
            if (Weapon is WeaponItem weapon)
            {
                string className = weapon.WeaponType switch
                {
                    WeaponType.Mace => "Barbarian",
                    WeaponType.Sword => "Warrior",
                    WeaponType.Dagger => "Rogue",
                    WeaponType.Wand => "Wizard",
                    _ => "Unknown"
                };
                
                AwardClassPoint(weapon.WeaponType);
                Console.WriteLine($"\n*** LEVEL UP! ***");
                Console.WriteLine($"You reached level {Level}!");
                Console.WriteLine($"Gained +1 {className} class point!");
                Console.WriteLine($"Current class: {GetCurrentClass()}");
                Console.WriteLine($"Class Points: Barbarian({BarbarianPoints}) Warrior({WarriorPoints}) Rogue({RoguePoints}) Wizard({WizardPoints})");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"\n*** LEVEL UP! ***");
                Console.WriteLine($"You reached level {Level}!");
                Console.WriteLine("No weapon equipped - no class point gained.");
                Console.WriteLine();
            }
        }

        private int XPToNextLevel()
        {
            return 100 * Level;
        }

        public void TakeDamage(int amount)
        {
            // Apply damage reduction if active
            if (DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - DamageReduction));
            }
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
        }

        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        public bool IsAlive => CurrentHealth > 0;

        public override string GetDescription()
        {
            return $"Level {Level} (Health: {CurrentHealth}/{MaxHealth}) (STR: {Strength}, AGI: {Agility}, TEC: {Technique}, INT: {Intelligence})";
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public void UpdateComboBonus()
        {
            int bonus = 0;
            // Equipped items
            if (Head != null) bonus += Head.ComboBonus;
            if (Body != null) bonus += Body.ComboBonus;
            if (Weapon != null) bonus += Weapon.ComboBonus;
            if (Feet != null) bonus += Feet.ComboBonus;
            // Inventory items (optional: only if you want inventory to count)
            // foreach (var item in Inventory) bonus += item.ComboBonus;
            ComboBonus = bonus;
        }

        public List<Action> GetComboActions()
        {
            // Return the current combo sequence, sorted by combo order
            var sortedCombo = ComboSequence.ToList();
            sortedCombo.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            return sortedCombo;
        }
        
        public List<Action> GetActionPool()
        {
            // Return all available actions from the action pool (no ordering)
            var allActions = new List<Action>();
            foreach (var entry in ActionPool)
            {
                if (entry.action.IsComboAction)
                    allActions.Add(entry.action);
            }
            // Don't sort by ComboOrder - Action Pool is just a list of available actions
            return allActions;
        }
        
        public void AddToCombo(Action action)
        {
            if (!ComboSequence.Contains(action) && action.IsComboAction)
            {
                ComboSequence.Add(action);
                // Reassign combo orders to be sequential starting from 1
                ReorderComboSequence();
            }
        }
        
        public void RemoveFromCombo(Action action)
        {
            if (ComboSequence.Contains(action))
            {
                ComboSequence.Remove(action);
                // Reset the action's combo order since it's no longer in the combo
                action.ComboOrder = 0;
                // Reassign combo orders to be sequential starting from 1
                ReorderComboSequence();
            }
        }
        
        private void ReorderComboSequence()
        {
            // Sort by current combo order, then reassign sequential orders starting from 1
            // Only assign combo orders to actions that are in the combo sequence
            ComboSequence.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            for (int i = 0; i < ComboSequence.Count; i++)
            {
                ComboSequence[i].ComboOrder = i + 1;
            }
        }
        
        private void InitializeDefaultCombo()
        {
            // No default combo actions - players must select from available actions
            // Combo sequence starts empty and players add actions from their Action Pool
        }

        public void SetTempComboBonus(int bonus, int turns)
        {
            TempComboBonus = bonus;
            TempComboBonusTurns = turns;
        }

        public int ConsumeTempComboBonus()
        {
            int bonus = TempComboBonus;
            if (TempComboBonusTurns > 0)
            {
                TempComboBonusTurns--;
                if (TempComboBonusTurns == 0)
                    TempComboBonus = 0;
            }
            return bonus;
        }

        public void ApplyHealthMultiplier(double multiplier)
        {
            MaxHealth = (int)(MaxHealth * multiplier);
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// Activates combo mode when a combo attack is triggered
        /// </summary>
        public void ActivateComboMode()
        {
            ComboModeActive = true;
        }

        /// <summary>
        /// Deactivates combo mode when combo fails or completes
        /// </summary>
        public void DeactivateComboMode()
        {
            ComboModeActive = false;
        }

        /// <summary>
        /// Resets combo state including combo mode
        /// </summary>
        public void ResetCombo()
        {
            ComboStep = 0;
            ComboAmplifier = 1.0;
            LastComboActionIdx = -1;
            ComboModeActive = false;
        }

        // New methods for advanced mechanics
        public void ApplyStatBonus(int bonus, string statType, int duration)
        {
            switch (statType.ToUpper())
            {
                case "STR":
                    TempStrengthBonus = bonus;
                    break;
                case "AGI":
                    TempAgilityBonus = bonus;
                    break;
                case "TEC":
                    TempTechniqueBonus = bonus;
                    break;
                case "INT":
                    TempIntelligenceBonus = bonus;
                    break;
            }
            TempStatBonusTurns = duration;
        }

        public void UpdateTempEffects()
        {
            // Update temporary stat bonuses
            if (TempStatBonusTurns > 0)
            {
                TempStatBonusTurns--;
                if (TempStatBonusTurns == 0)
                {
                    TempStrengthBonus = 0;
                    TempAgilityBonus = 0;
                    TempTechniqueBonus = 0;
                    TempIntelligenceBonus = 0;
                }
            }

            // Update stun
            if (StunTurnsRemaining > 0)
            {
                StunTurnsRemaining--;
                if (StunTurnsRemaining == 0)
                    IsStunned = false;
            }

            // Update length reduction
            if (LengthReductionTurns > 0)
            {
                LengthReductionTurns--;
                if (LengthReductionTurns == 0)
                    LengthReduction = 0.0;
            }

            // Update enemy roll penalty
            if (EnemyRollPenaltyTurns > 0)
            {
                EnemyRollPenaltyTurns--;
                if (EnemyRollPenaltyTurns == 0)
                    EnemyRollPenalty = 0;
            }

            // Update combo amplifier multiplier
            if (ComboAmplifierTurns > 0)
            {
                ComboAmplifierTurns--;
                if (ComboAmplifierTurns == 0)
                    ComboAmplifierMultiplier = 1.0;
            }

            // Update extra damage decay
            if (ExtraDamage > 0)
            {
                ExtraDamage = Math.Max(0, ExtraDamage - 1); // Default decay of 1 per turn
            }

            // Update damage reduction decay
            if (DamageReduction > 0)
            {
                DamageReduction = Math.Max(0.0, DamageReduction - 0.1); // Default decay of 10% per turn
            }
        }

        public int GetEffectiveStrength()
        {
            return Strength + TempStrengthBonus;
        }

        public int GetEffectiveAgility()
        {
            return Agility + TempAgilityBonus;
        }

        public int GetEffectiveTechnique()
        {
            return Technique + TempTechniqueBonus;
        }

        public int GetEffectiveIntelligence()
        {
            return Intelligence + TempIntelligenceBonus;
        }

        public double GetHealthPercentage()
        {
            return (double)CurrentHealth / MaxHealth;
        }

        public bool MeetsHealthThreshold(double threshold)
        {
            return GetHealthPercentage() <= threshold;
        }

        public bool MeetsStatThreshold(string statType, double threshold)
        {
            return statType.ToUpper() switch
            {
                "STR" => GetEffectiveStrength() >= threshold,
                "AGI" => GetEffectiveAgility() >= threshold,
                "TEC" => GetEffectiveTechnique() >= threshold,
                "INT" => GetEffectiveIntelligence() >= threshold,
                _ => false
            };
        }

        public void DisplayCharacterInfo()
        {
            Console.WriteLine($"=== CHARACTER INFORMATION ===");
            Console.WriteLine($"Name: {Name}");
            Console.WriteLine($"Class: {GetCurrentClass()}");
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"Health: {CurrentHealth}/{MaxHealth}");
            Console.WriteLine($"XP: {XP}");
            Console.WriteLine();
            Console.WriteLine("=== STATS ===");
            Console.WriteLine($"Strength: {GetEffectiveStrength()}");
            Console.WriteLine($"Agility: {GetEffectiveAgility()}");
            Console.WriteLine($"Technique: {GetEffectiveTechnique()}");
            Console.WriteLine($"Intelligence: {GetEffectiveIntelligence()}");
            Console.WriteLine();
            Console.WriteLine("=== CLASS POINTS ===");
            Console.WriteLine($"Barbarian (Mace): {BarbarianPoints}");
            Console.WriteLine($"Warrior (Sword): {WarriorPoints}");
            Console.WriteLine($"Rogue (Dagger): {RoguePoints}");
            Console.WriteLine($"Wizard (Wand): {WizardPoints}");
            Console.WriteLine();
            Console.WriteLine("=== EQUIPMENT ===");
            Console.WriteLine($"Weapon: {(Weapon?.Name ?? "None")}");
            Console.WriteLine($"Head: {(Head?.Name ?? "None")}");
            Console.WriteLine($"Body: {(Body?.Name ?? "None")}");
            Console.WriteLine($"Feet: {(Feet?.Name ?? "None")}");
            Console.WriteLine();
        }

        // Class-specific action methods
        private void AddBarbarianActions()
        {
            // Basic Barbarian actions (always available)
            AddAction(new Action("RAGE", ActionType.Buff, TargetType.Self, 0, 1, 0, "Enter a berserker rage, +3 STR for 3 turns", 2, 0.0, 1.0, false, false, true), 0.0);
            AddAction(new Action("SMASH", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Devastating overhead strike, 200% damage", 3, 2.0, 1.5, false, false, true), 0.0);
            
            if (BarbarianPoints >= 5)
            {
                // Additional Barbarian actions
            }
            if (BarbarianPoints >= 20)
            {
                // Advanced Barbarian actions
                AddAction(new Action("BERSERKER CHARGE", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Charge forward with unstoppable force, 250% damage", -1, 2.5, 2.0, false, false, true), 0.0);
                AddAction(new Action("BATTLE FURY", ActionType.Buff, TargetType.Self, 0, 1, 0, "Unleash primal fury, +5 STR for 5 turns", -1, 0.0, 1.0, false, false, true), 0.0);
            }
            if (BarbarianPoints >= 40)
            {
                // Master Barbarian actions
                AddAction(new Action("WORLD BREAKER", ActionType.Attack, TargetType.AreaOfEffect, 0, 1, 0, "Shatter the very earth, 400% damage to all enemies", -1, 4.0, 3.0, false, false, true), 0.0);
                AddAction(new Action("PRIMAL AWAKENING", ActionType.Buff, TargetType.Self, 0, 1, 0, "Channel ancient power, +10 STR for 10 turns", -1, 0.0, 1.0, false, false, true), 0.0);
            }
        }

        private void AddWarriorActions()
        {
            // Basic Warrior actions (always available)
            AddAction(new Action("SHIELD BASH", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Strike with shield, stuns enemy for 2 turns", 2, 1.0, 1.0, false, false, true), 0.0);
            AddAction(new Action("DEFENSIVE STANCE", ActionType.Buff, TargetType.Self, 0, 1, 0, "Adopt defensive posture, +50% damage reduction", 3, 0.0, 1.0, false, false, true), 0.0);
            
            if (WarriorPoints >= 5)
            {
                // Additional Warrior actions
            }
            if (WarriorPoints >= 20)
            {
                // Advanced Warrior actions
                AddAction(new Action("KNIGHT'S CHARGE", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Honorable charge attack, 200% damage", -1, 2.0, 1.5, false, false, true), 0.0);
                AddAction(new Action("BATTLE CRY", ActionType.Buff, TargetType.Self, 0, 1, 0, "Inspire allies, +3 to all stats for 3 turns", -1, 0.0, 1.0, false, false, true), 0.0);
            }
            if (WarriorPoints >= 40)
            {
                // Master Warrior actions
                AddAction(new Action("CHAMPION'S STRIKE", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Perfect technique strike, 300% damage", -1, 3.0, 2.0, false, false, true), 0.0);
                AddAction(new Action("LEGENDARY VALOR", ActionType.Buff, TargetType.Self, 0, 1, 0, "Unbreakable spirit, +5 to all stats for 5 turns", -1, 0.0, 1.0, false, false, true), 0.0);
            }
        }

        private void AddRogueActions()
        {
            // Basic Rogue actions (always available)
            AddAction(new Action("BACKSTAB", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Sneak attack from behind, 250% damage", 2, 2.5, 0.5, false, false, true), 0.0);
            AddAction(new Action("STEALTH", ActionType.Buff, TargetType.Self, 0, 1, 0, "Become invisible, next attack guaranteed hit", 3, 0.0, 1.0, false, false, true), 0.0);
            
            if (RoguePoints >= 5)
            {
                // Additional Rogue actions
            }
            if (RoguePoints >= 20)
            {
                // Advanced Rogue actions
                AddAction(new Action("SHADOW STRIKE", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Attack from the shadows, 300% damage", -1, 3.0, 0.5, false, false, true), 0.0);
                AddAction(new Action("POISON BLADE", ActionType.Debuff, TargetType.SingleTarget, 0, 1, 0, "Apply deadly poison, 50 damage over 5 turns", -1, 0.0, 1.0, false, false, true), 0.0);
            }
            if (RoguePoints >= 40)
            {
                // Master Rogue actions
                AddAction(new Action("ASSASSIN'S MARK", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Mark target for death, 500% damage", -1, 5.0, 1.0, false, false, true), 0.0);
                AddAction(new Action("SHADOW MASTER", ActionType.Buff, TargetType.Self, 0, 1, 0, "Become one with shadows, +10 AGI for 10 turns", -1, 0.0, 1.0, false, false, true), 0.0);
            }
        }

        private void AddWizardActions()
        {
            // Basic Wizard actions (always available)
            AddAction(new Action("FIREBALL", ActionType.Spell, TargetType.SingleTarget, 0, 1, 0, "Launch a ball of fire, 200% damage", 2, 2.0, 1.5, false, false, true), 0.0);
            AddAction(new Action("MAGIC MISSILE", ActionType.Spell, TargetType.SingleTarget, 0, 1, 0, "Guaranteed hit spell, 150% damage", 3, 1.5, 1.0, false, false, true), 0.0);
            
            if (WizardPoints >= 5)
            {
                // Additional Wizard actions
            }
            if (WizardPoints >= 20)
            {
                // Advanced Wizard actions
                AddAction(new Action("LIGHTNING BOLT", ActionType.Spell, TargetType.SingleTarget, 0, 1, 0, "Strike with lightning, 250% damage", -1, 2.5, 1.5, false, false, true), 0.0);
                AddAction(new Action("ARCANE SHIELD", ActionType.Buff, TargetType.Self, 0, 1, 0, "Protective barrier, +75% damage reduction", -1, 0.0, 1.0, false, false, true), 0.0);
            }
            if (WizardPoints >= 40)
            {
                // Master Wizard actions
                AddAction(new Action("METEOR", ActionType.Spell, TargetType.AreaOfEffect, 0, 1, 0, "Summon a meteor from the sky, 400% damage", -1, 4.0, 3.0, false, false, true), 0.0);
                AddAction(new Action("ARCHMAGE'S POWER", ActionType.Buff, TargetType.Self, 0, 1, 0, "Channel ultimate magic, +10 TEC for 10 turns", -1, 0.0, 1.0, false, false, true), 0.0);
            }
        }

        // Class progression methods
        public void AwardClassPoint(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Mace:
                    BarbarianPoints++;
                    break;
                case WeaponType.Sword:
                    WarriorPoints++;
                    break;
                case WeaponType.Dagger:
                    RoguePoints++;
                    break;
                case WeaponType.Wand:
                    WizardPoints++;
                    break;
            }
            
            // Re-add class actions when points change
            AddClassActions();
        }

        public string GetCurrentClass()
        {
            var classes = new List<(string name, int points)>
            {
                ("Barbarian", BarbarianPoints),
                ("Warrior", WarriorPoints),
                ("Rogue", RoguePoints),
                ("Wizard", WizardPoints)
            };

            // Sort by points descending
            classes.Sort((a, b) => b.points.CompareTo(a.points));

            if (classes[0].points == 0)
                return "Fighter";

            string primaryClass = GetClassTier(classes[0].name, classes[0].points);
            
            // Check for hybrid classes
            if (classes[1].points >= 5)
            {
                string secondaryClass = GetClassTier(classes[1].name, classes[1].points);
                return $"{primaryClass}-{secondaryClass}";
            }

            return primaryClass;
        }

        private string GetClassTier(string baseClass, int points)
        {
            if (points >= 40)
                return $"Grand {baseClass}";
            if (points >= 20)
                return $"Master {baseClass}";
            if (points >= 5)
                return baseClass;
            return "Novice";
        }

        private void AddWeaponActions(WeaponItem weapon)
        {
            // Add gear-specific actions based on weapon type
            AddGearActions(weapon);
        }

        private void AddGearActions(Item gear)
        {
            // Define gear-specific actions that are lost when unequipped
            var gearActions = GetGearActions(gear);
            
            // Determine if this gear should have actions
            bool shouldHaveActions = false;
            
            if (gear.Type == ItemType.Weapon)
            {
                // All weapons (starter and loot) always get at least 1 action
                shouldHaveActions = true;
            }
            else if (!gear.IsStarterItem)
            {
                // For non-starter armor, use probability-based action assignment
                // Higher tier items have better chance of having actions
                double actionChance = Math.Min(0.3 + (gear.Tier * 0.1), 0.8); // 30% base + 10% per tier, max 80%
                shouldHaveActions = new Random().NextDouble() < actionChance;
            }
            // Starter armor (IsStarterItem = true but Type != Weapon) gets no actions
            
            if (shouldHaveActions)
            {
                foreach (var actionName in gearActions)
                {
                    // Load gear actions from JSON to ensure they're properly configured
                    LoadGearActionFromJson(actionName);
                }
            }
        }

        private List<string> GetGearActions(Item gear)
        {
            if (gear is WeaponItem weapon)
            {
                return weapon.WeaponType switch
                {
                    WeaponType.Sword => new List<string> { "PARRY", "SWORD SLASH", "SWORDMASTER STRIKE" },
                    WeaponType.Mace => new List<string> { "CRUSHING BLOW", "SHIELD BREAK", "CRUSHING MOMENTUM" },
                    WeaponType.Dagger => new List<string> { "QUICK STAB", "POISON BLADE", "VENOMOUS ASSASSIN" },
                    WeaponType.Wand => new List<string> { "MAGIC MISSILE", "ARCANE SHIELD", "ARCANE FURY" },
                    _ => new List<string>()
                };
            }
            else if (gear is HeadItem)
            {
                return new List<string> { "HEADBUTT" };
            }
            else if (gear is ChestItem)
            {
                return new List<string> { "CHEST BASH" };
            }
            else if (gear is FeetItem)
            {
                return new List<string> { "KICK" };
            }
            
            return new List<string>();
        }

        private void LoadGearActionFromJson(string actionName)
        {
            try
            {
                string jsonPath = Path.Combine("..", "GameData", "Actions.json");
                
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var allActions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (allActions != null)
                    {
                        var actionData = allActions.FirstOrDefault(a => a.name == actionName);
                        if (actionData != null)
                        {
                            var action = CreateActionFromData(actionData);
                            if (action.IsComboAction)
                            {
                                // Find the highest combo order among combo actions only and add 1
                                var comboActions = ActionPool.Where(a => a.action.IsComboAction).ToList();
                                int maxOrder = comboActions.Count > 0 ? comboActions.Max(a => a.action.ComboOrder) : 0;
                                action.ComboOrder = maxOrder + 1;
                                
                                AddAction(action, 0.0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading gear action {actionName} from JSON: {ex.Message}");
            }
        }

        private void RemoveWeaponActions()
        {
            // Remove weapon-specific gear actions
            var weaponActions = new[] { "SWORD SLASH", "PARRY", "QUICK STAB", "POISON BLADE", "CRUSHING BLOW", "SHIELD BREAK", "MAGIC MISSILE", "ARCANE SHIELD" };
            foreach (var actionName in weaponActions)
            {
                var actionToRemove = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                if (actionToRemove.action != null)
                {
                    RemoveAction(actionToRemove.action);
                }
            }
        }

        private void RemoveClassActions()
        {
            // Remove class-specific actions (core combo actions)
            var classActions = new[] { 
                "TAUNT", "JAB", "STUN", "CRIT", "SHIELD BASH", "DEFENSIVE STANCE", 
                "BERSERK", "BLOOD FRENZY", "PRECISION STRIKE", "QUICK REFLEXES",
                "FOCUS", "READ BOOK"
            };
            foreach (var actionName in classActions)
            {
                var actionToRemove = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                if (actionToRemove.action != null)
                {
                    RemoveAction(actionToRemove.action);
                }
            }
        }

        private void AddArmorActions(Item armor)
        {
            // Add gear-specific actions for armor
            AddGearActions(armor);
        }

        private void RemoveArmorActions(Item? armor)
        {
            if (armor == null) return;
            
            // Determine which action to remove based on armor type
            string actionName = "";
            
            if (armor is HeadItem)
            {
                actionName = "HEADBUTT";
            }
            else if (armor is ChestItem)
            {
                actionName = "CHEST BASH";
            }
            else if (armor is FeetItem)
            {
                actionName = "KICK";
            }
            
            if (!string.IsNullOrEmpty(actionName))
            {
                var actionToRemove = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                if (actionToRemove.action != null)
                {
                    RemoveAction(actionToRemove.action);
                }
            }
        }

        // Save/Load methods
        public void SaveCharacter(string filename = "character_save.json")
        {
            try
            {
                var saveData = new CharacterSaveData
                {
                    Name = Name,
                    Level = Level,
                    XP = XP,
                    CurrentHealth = CurrentHealth,
                    MaxHealth = MaxHealth,
                    Strength = Strength,
                    Agility = Agility,
                    Technique = Technique,
                    Intelligence = Intelligence,
                    BarbarianPoints = BarbarianPoints,
                    WarriorPoints = WarriorPoints,
                    RoguePoints = RoguePoints,
                    WizardPoints = WizardPoints,
                    ComboStep = ComboStep,
                    ComboBonus = ComboBonus,
                    TempComboBonus = TempComboBonus,
                    TempComboBonusTurns = TempComboBonusTurns,
                    DamageReduction = DamageReduction,
                    Inventory = Inventory,
                    Head = Head,
                    Body = Body,
                    Weapon = Weapon,
                    Feet = Feet
                };

                string json = System.Text.Json.JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filename, json);
                Console.WriteLine($"Character saved to {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving character: {ex.Message}");
            }
        }

        public static Character? LoadCharacter(string filename = "character_save.json")
        {
            try
            {
                if (!File.Exists(filename))
                {
                    Console.WriteLine($"Save file {filename} not found.");
                    return null;
                }

                string json = File.ReadAllText(filename);
                var saveData = System.Text.Json.JsonSerializer.Deserialize<CharacterSaveData>(json);
                
                if (saveData == null)
                {
                    Console.WriteLine("Error: Could not deserialize save data.");
                    return null;
                }

                // Create character with saved data
                var character = new Character(saveData.Name, saveData.Level);
                character.XP = saveData.XP;
                character.CurrentHealth = saveData.CurrentHealth;
                character.MaxHealth = saveData.MaxHealth;
                character.Strength = saveData.Strength;
                character.Agility = saveData.Agility;
                character.Technique = saveData.Technique;
                character.Intelligence = saveData.Intelligence;
                character.BarbarianPoints = saveData.BarbarianPoints;
                character.WarriorPoints = saveData.WarriorPoints;
                character.RoguePoints = saveData.RoguePoints;
                character.WizardPoints = saveData.WizardPoints;
                character.ComboStep = saveData.ComboStep;
                character.ComboBonus = saveData.ComboBonus;
                character.TempComboBonus = saveData.TempComboBonus;
                character.TempComboBonusTurns = saveData.TempComboBonusTurns;
                character.DamageReduction = saveData.DamageReduction;
                character.Inventory = saveData.Inventory;
                character.Head = saveData.Head;
                character.Body = saveData.Body;
                character.Weapon = saveData.Weapon;
                character.Feet = saveData.Feet;

                // Rebuild action pool with proper structure
                character.ActionPool.Clear();
                character.AddDefaultActions(); // Add basic attack
                character.AddClassActions(); // Add class actions based on weapon
                
                // Re-add gear actions for equipped items (with probability for non-starter items)
                if (character.Head != null)
                    character.AddArmorActions(character.Head);
                if (character.Body != null)
                    character.AddArmorActions(character.Body);
                if (character.Weapon is WeaponItem weapon)
                    character.AddWeaponActions(weapon);
                if (character.Feet != null)
                    character.AddArmorActions(character.Feet);

                // Initialize combo sequence after all actions are loaded
                character.InitializeDefaultCombo();

                Console.WriteLine($"Character loaded from {filename}");
                return character;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading character: {ex.Message}");
                return null;
            }
        }

        public static void DeleteSaveFile(string filename = "character_save.json")
        {
            try
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    Console.WriteLine($"Save file {filename} has been deleted.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting save file: {ex.Message}");
            }
        }
    }

    // Data class for JSON deserialization
    public class ActionData
    {
        public string name { get; set; } = "";
        public string type { get; set; } = "Attack";
        public double damageMultiplier { get; set; } = 1.0;
        public double length { get; set; } = 1.0;
        public string? description { get; set; }
        public bool? causesBleed { get; set; }
        public bool? causesWeaken { get; set; }
        public int? comboBonusAmount { get; set; }
        public int? comboBonusDuration { get; set; }
        public int? comboOrder { get; set; }
        public bool? isComboAction { get; set; }
        public List<string>? tags { get; set; }
        
        // Stat bonus properties
        public int? statBonus { get; set; }
        public string? statBonusType { get; set; }
        public int? statBonusDuration { get; set; }
        
        // Other action properties
        public int? multiHitCount { get; set; }
        public double? multiHitDamagePercent { get; set; }
        public int? selfDamagePercent { get; set; }
        public int? rollBonus { get; set; }
        public bool? skipNextTurn { get; set; }
        public bool? repeatLastAction { get; set; }
    }

    // Data class for character save/load
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
        public int ComboStep { get; set; }
        public int ComboBonus { get; set; }
        public int TempComboBonus { get; set; }
        public int TempComboBonusTurns { get; set; }
        public double DamageReduction { get; set; }
        public List<Item> Inventory { get; set; } = new List<Item>();
        public Item? Head { get; set; }
        public Item? Body { get; set; }
        public Item? Weapon { get; set; }
        public Item? Feet { get; set; }
    }
} 