using System.Text.Json;
using System.Linq;

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
        
        // Enemy roll penalty from actions like Arcane Shield
        public int EnemyRollPenalty { get; set; } = 0;
        public int EnemyRollPenaltyTurns { get; set; } = 0;
        
        // Slow debuff from environmental effects
        public double SlowMultiplier { get; set; } = 1.0;
        public int SlowTurns { get; set; } = 0;
        
        // Poison debuff from environmental effects
        public int PoisonDamage { get; set; } = 0;
        public int PoisonStacks { get; set; } = 0; // Number of poison stacks
        public double LastPoisonTick { get; set; } = 0.0; // Last time poison ticked
        public bool IsBleeding { get; set; } = false; // Whether the damage is from bleeding (vs poison)
        
        // Shield buff from Arcane SHIELD
        public bool HasShield { get; set; } = false; // Whether character has active shield
        public int LastShieldReduction { get; set; } = 0; // Amount of damage reduced by last shield use
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
        public double LengthReduction { get; set; } = 0.0; // For Taunt
        public int LengthReductionTurns { get; set; } = 0; // Length reduction duration
        public double ComboAmplifierMultiplier { get; set; } = 1.0; // For Pretty Boy Swag
        public int ComboAmplifierTurns { get; set; } = 0; // Amplifier duration
        
        // Divine reroll system
        public int RerollCharges { get; set; } = 0; // Number of rerolls available
        public bool UsedRerollThisTurn { get; set; } = false; // Track if reroll was used this turn
        
        
        // Turn system constants
        public const double DEFAULT_ACTION_LENGTH = 1.0; // Basic attack length defines one turn

        // Class points system
        public int BarbarianPoints { get; set; } = 0; // Mace weapon
        public int WarriorPoints { get; set; } = 0;   // Sword weapon
        public int RoguePoints { get; set; } = 0;     // Dagger weapon
        public int WizardPoints { get; set; } = 0;    // Wand weapon

        public Character(string? name = null, int level = 1)
            : base(name ?? FlavorText.GenerateCharacterName())
        {
            Level = level;
            
            var tuning = TuningConfig.Instance;
            
            // Initialize attributes based on tuning config
            Strength = tuning.Attributes.PlayerBaseAttributes.Strength + (level - 1) * tuning.Attributes.PlayerAttributesPerLevel;
            Agility = tuning.Attributes.PlayerBaseAttributes.Agility + (level - 1) * tuning.Attributes.PlayerAttributesPerLevel;
            Technique = tuning.Attributes.PlayerBaseAttributes.Technique + (level - 1) * tuning.Attributes.PlayerAttributesPerLevel;
            Intelligence = tuning.Attributes.PlayerBaseAttributes.Intelligence + (level - 1) * tuning.Attributes.PlayerAttributesPerLevel;

            // Initialize health based on tuning config
            MaxHealth = tuning.Character.PlayerBaseHealth + (level - 1) * tuning.Character.HealthPerLevel;
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
            
            // Add class-specific actions based on class points
            AddBarbarianActions();
            AddWarriorActions();
            AddRogueActions();
            AddWizardActions();
        }

        private void LoadClassActionsFromJson(WeaponType weaponType)
        {
            try
            {
                string[] possiblePaths = {
                    Path.Combine("GameData", "Actions.json"),
                    Path.Combine("..", "GameData", "Actions.json"),
                    Path.Combine("..", "..", "GameData", "Actions.json"),
                    Path.Combine("DF4 - CONSOLE", "GameData", "Actions.json"),
                    Path.Combine("..", "DF4 - CONSOLE", "GameData", "Actions.json")
                };

                string? foundPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    var allActions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (allActions != null)
                    {
                        // Define which actions belong to which class
                        var classActions = GetClassActions(weaponType);
                        
                        foreach (var actionData in allActions)
                        {
                            if (classActions.Contains(actionData.Name))
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
                string[] possiblePaths = {
                    Path.Combine("GameData", "Actions.json"),
                    Path.Combine("..", "GameData", "Actions.json"),
                    Path.Combine("..", "..", "GameData", "Actions.json"),
                    Path.Combine("DF4 - CONSOLE", "GameData", "Actions.json"),
                    Path.Combine("..", "DF4 - CONSOLE", "GameData", "Actions.json")
                };

                string? foundPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
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
                    Console.WriteLine($"Actions.json file not found. Tried paths: {string.Join(", ", possiblePaths)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading actions from JSON: {ex.Message}");
            }
        }


        private Action CreateActionFromData(ActionData data)
        {
            // Enhance description with modifiers
            string enhancedDescription = EnhanceActionDescription(data);
            
            var action = new Action(
                name: data.Name,
                type: ParseActionType(data.Type),
                targetType: TargetType.SingleTarget,
                baseValue: data.BaseValue,
                range: data.Range,
                cooldown: data.Cooldown,
                description: enhancedDescription,
                comboOrder: data.ComboOrder,
                damageMultiplier: data.DamageMultiplier,
                length: data.Length,
                causesBleed: data.CausesBleed,
                causesWeaken: data.CausesWeaken,
                isComboAction: data.IsComboAction,
                comboBonusAmount: data.ComboBonusAmount,
                comboBonusDuration: data.ComboBonusDuration
            );
            
            // Set additional properties
            action.RollBonus = data.RollBonus;
            action.StatBonus = data.StatBonus;
            action.StatBonusType = data.StatBonusType;
            action.StatBonusDuration = data.StatBonusDuration;
            action.MultiHitCount = data.MultiHitCount;
            action.MultiHitDamagePercent = data.MultiHitDamagePercent;
            action.SelfDamagePercent = data.SelfDamagePercent;
            action.SkipNextTurn = data.SkipNextTurn;
            action.RepeatLastAction = data.RepeatLastAction;
                
            return action;
        }

        private string EnhanceActionDescription(ActionData data)
        {
            var modifiers = new List<string>();
            
            // Add roll bonus information
            if (data.RollBonus != 0)
            {
                string rollText = data.RollBonus > 0 ? $"+{data.RollBonus}" : data.RollBonus.ToString();
                modifiers.Add($"Roll: {rollText}");
            }
            
            // Add damage multiplier information
            if (data.DamageMultiplier != 1.0)
            {
                modifiers.Add($"Damage: {data.DamageMultiplier:F1}x");
            }
            
            // Add combo bonus information
            if (data.ComboBonusAmount > 0 && data.ComboBonusDuration > 0)
            {
                modifiers.Add($"Combo: +{data.ComboBonusAmount} for {data.ComboBonusDuration} turns");
            }
            
            // Add status effect information
            if (data.CausesBleed)
            {
                modifiers.Add("Causes Bleed");
            }
            
            if (data.CausesWeaken)
            {
                modifiers.Add("Causes Weaken");
            }
            
            // Add multi-hit information
            if (data.MultiHitCount > 1)
            {
                modifiers.Add($"Multi-hit: {data.MultiHitCount} attacks");
            }
            
            // Add self-damage information
            if (data.SelfDamagePercent > 0)
            {
                modifiers.Add($"Self-damage: {data.SelfDamagePercent}%");
            }
            
            // Add stat bonus information
            if (data.StatBonus > 0 && !string.IsNullOrEmpty(data.StatBonusType))
            {
                string durationText = data.StatBonusDuration == -1 ? "dungeon" : $"{data.StatBonusDuration} turns";
                modifiers.Add($"+{data.StatBonus} {data.StatBonusType} ({durationText})");
            }
            
            // Add special effects
            if (data.SkipNextTurn)
            {
                modifiers.Add("Skips next turn");
            }
            
            if (data.RepeatLastAction)
            {
                modifiers.Add("Repeats last action");
            }
            
            // Combine base description with modifiers
            string result = data.Description;
            if (modifiers.Count > 0)
            {
                result += $" | {string.Join(", ", modifiers)}";
            }
            
            return result;
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
            
            // Store current health percentage before equipping
            double healthPercentage = GetHealthPercentage();
            int oldMaxHealth = GetEffectiveMaxHealth();
            
            Item? previousItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    previousItem = Head;
                    // Remove old armor actions before equipping new armor
                    if (previousItem != null)
                    {
                        RemoveArmorActions(previousItem);
                    }
                    Head = item;
                    // Add armor actions if applicable
                    AddArmorActions(item);
                    break;
                case "body": 
                    previousItem = Body;
                    // Remove old armor actions before equipping new armor
                    if (previousItem != null)
                    {
                        RemoveArmorActions(previousItem);
                    }
                    Body = item;
                    // Add armor actions if applicable
                    AddArmorActions(item);
                    break;
                case "weapon": 
                    previousItem = Weapon;
                    // Remove old weapon actions before equipping new weapon
                    if (previousItem is WeaponItem oldWeapon)
                    {
                        RemoveWeaponActions(oldWeapon);
                    }
                    Weapon = item;
                    // Add weapon-specific actions
                    if (item is WeaponItem weapon)
                    {
                        AddWeaponActions(weapon);
                    }
                    break;
                case "feet": 
                    previousItem = Feet;
                    // Remove old armor actions before equipping new armor
                    if (previousItem != null)
                    {
                        RemoveArmorActions(previousItem);
                    }
                    Feet = item;
                    // Add armor actions if applicable
                    AddArmorActions(item);
                    break;
            }
            
            // Check if max health increased and adjust current health accordingly
            int newMaxHealth = GetEffectiveMaxHealth();
            if (newMaxHealth > oldMaxHealth)
            {
                // If max health increased, heal to full health
                CurrentHealth = newMaxHealth;
            }
            else if (newMaxHealth < oldMaxHealth)
            {
                // If max health decreased, maintain health percentage but cap at new max
                int newCurrentHealth = (int)(newMaxHealth * healthPercentage);
                CurrentHealth = Math.Min(newCurrentHealth, newMaxHealth);
            }
            
            // Update combo sequence after equipment change (only remove actions, don't add defaults)
            UpdateComboSequenceAfterGearChange();
            
            // Apply roll bonuses from the new item
            ApplyRollBonusesFromGear(item);
            
            // Update reroll charges from Divine modifications
            UpdateRerollCharges();
            
            return previousItem;
        }

        public Item? UnequipItem(string slot)
        {
            // Store current health percentage before unequipping
            double healthPercentage = GetHealthPercentage();
            int oldMaxHealth = GetEffectiveMaxHealth();
            
            Item? unequippedItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    unequippedItem = Head;
                    if (unequippedItem != null)
                    {
                    }
                    Head = null;
                    // Remove armor actions if applicable
                    RemoveArmorActions(unequippedItem);
                    break;
                case "body": 
                    unequippedItem = Body;
                    if (unequippedItem != null)
                    {
                    }
                    Body = null;
                    // Remove armor actions if applicable
                    RemoveArmorActions(unequippedItem);
                    break;
                case "weapon": 
                    unequippedItem = Weapon;
                    // Remove weapon actions before setting Weapon to null
                    if (unequippedItem is WeaponItem weaponToRemove)
                    {
                        RemoveWeaponActions(weaponToRemove);
                    }
                    Weapon = null;
                    break;
                case "feet": 
                    unequippedItem = Feet;
                    if (unequippedItem != null)
                    {
                    }
                    Feet = null;
                    // Remove armor actions if applicable
                    RemoveArmorActions(unequippedItem);
                    break;
            }
            
            // Check if max health changed and adjust current health accordingly
            int newMaxHealth = GetEffectiveMaxHealth();
            if (newMaxHealth < oldMaxHealth)
            {
                // If max health decreased, maintain health percentage but cap at new max
                int newCurrentHealth = (int)(newMaxHealth * healthPercentage);
                CurrentHealth = Math.Min(newCurrentHealth, newMaxHealth);
            }
            else if (newMaxHealth > oldMaxHealth)
            {
                // If max health increased (unlikely when unequipping), maintain health percentage
                int newCurrentHealth = (int)(newMaxHealth * healthPercentage);
                CurrentHealth = Math.Min(newCurrentHealth, newMaxHealth);
            }
            
            // Remove roll bonuses from the unequipped item
            if (unequippedItem != null)
            {
                RemoveRollBonusesFromGear(unequippedItem);
            }
            
            // Update reroll charges from Divine modifications
            UpdateRerollCharges();
            
            // Update combo sequence after equipment change (only remove actions, don't add defaults)
            UpdateComboSequenceAfterGearChange();
            
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
            var tuning = TuningConfig.Instance;
            MaxHealth += tuning.Character.HealthPerLevel;
            CurrentHealth = MaxHealth;
            
            // Award class point and stat increases based on equipped weapon
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
                
                // Increase stats based on weapon class: +3 for weapon class, +1 for others
                switch (weapon.WeaponType)
                {
                    case WeaponType.Mace: // Barbarian - Strength
                        Strength += 3;
                        Agility += 1;
                        Technique += 1;
                        Intelligence += 1;
                        break;
                    case WeaponType.Sword: // Warrior - Agility
                        Strength += 1;
                        Agility += 3;
                        Technique += 1;
                        Intelligence += 1;
                        break;
                    case WeaponType.Dagger: // Rogue - Technique
                        Strength += 1;
                        Agility += 1;
                        Technique += 3;
                        Intelligence += 1;
                        break;
                    case WeaponType.Wand: // Wizard - Intelligence
                        Strength += 1;
                        Agility += 1;
                        Technique += 1;
                        Intelligence += 3;
                        break;
                    default:
                        // Fallback: equal increases if unknown weapon type
                        Strength += 2;
                        Agility += 2;
                        Technique += 2;
                        Intelligence += 2;
                        break;
                }
                
                AwardClassPoint(weapon.WeaponType);
                Console.WriteLine($"\n*** LEVEL UP! ***");
                Console.WriteLine($"You reached level {Level}!");
                Console.WriteLine($"Gained +1 {className} class point!");
                Console.WriteLine($"Stats increased: {GetStatIncreaseMessage(weapon.WeaponType)}");
                Console.WriteLine($"Current class: {GetCurrentClass()}");
                Console.WriteLine($"You are now known as: {GetFullNameWithQualifier()}");
                // Show only classes with points > 0
                var classPointsInfo = new List<string>();
                if (BarbarianPoints > 0) classPointsInfo.Add($"Barbarian({BarbarianPoints})");
                if (WarriorPoints > 0) classPointsInfo.Add($"Warrior({WarriorPoints})");
                if (RoguePoints > 0) classPointsInfo.Add($"Rogue({RoguePoints})");
                if (WizardPoints > 0) classPointsInfo.Add($"Wizard({WizardPoints})");
                
                if (classPointsInfo.Count > 0)
                {
                    Console.WriteLine($"Class Points: {string.Join(" ", classPointsInfo)}");
                    Console.WriteLine($"Next Upgrades: {GetClassUpgradeInfo()}");
                }
                Console.WriteLine();
            }
            else
            {
                // No weapon equipped - equal stat increases
                Strength += 2;
                Agility += 2;
                Technique += 2;
                Intelligence += 2;
                
                Console.WriteLine($"\n*** LEVEL UP! ***");
                Console.WriteLine($"You reached level {Level}!");
                Console.WriteLine("No weapon equipped - equal stat increases (+2 all stats)");
                Console.WriteLine();
            }
        }

        private int XPToNextLevel()
        {
            var tuning = TuningConfig.Instance;
            return (int)(tuning.Progression.BaseXPToLevel2 * Math.Pow(tuning.Progression.XPScalingFactor, Level - 1));
        }

        private string GetStatIncreaseMessage(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Mace => "+3 STR, +1 AGI, +1 TEC, +1 INT",    // Barbarian - Strength
                WeaponType.Sword => "+1 STR, +3 AGI, +1 TEC, +1 INT",   // Warrior - Agility
                WeaponType.Dagger => "+1 STR, +1 AGI, +3 TEC, +1 INT",  // Rogue - Technique
                WeaponType.Wand => "+1 STR, +1 AGI, +1 TEC, +3 INT",    // Wizard - Intelligence
                _ => "+2 all stats"
            };
        }

        public void TakeDamage(int amount)
        {
            TakeDamageWithNotifications(amount);
        }
        
        public List<string> TakeDamageWithNotifications(int amount)
        {
            int originalAmount = amount;
            bool shieldUsed = false;
            
            // Apply shield damage reduction (50% reduction) if shield is active
            if (HasShield)
            {
                int reducedAmount = (int)(amount * 0.5); // Reduce damage by half
                int shieldReduction = amount - reducedAmount;
                amount = reducedAmount;
                HasShield = false; // Consume the shield
                shieldUsed = true;
                
                // Display shield message
                Console.WriteLine($"[{Name}]'s Arcane Shield reduces damage by {shieldReduction}!");
            }
            
            // Apply damage reduction if active
            if (DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - DamageReduction));
            }
            
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            
            // Store shield usage for display purposes
            if (shieldUsed)
            {
                LastShieldReduction = originalAmount - amount;
            }
            
            // Check for health milestones and leadership changes
            return Combat.CheckHealthMilestones(this, amount);
        }
        
        /// <summary>
        /// Calculates damage with shield reduction but doesn't apply it
        /// </summary>
        /// <param name="amount">Original damage amount</param>
        /// <returns>Tuple of (final damage, shield reduction amount, shield was used)</returns>
        public (int finalDamage, int shieldReduction, bool shieldUsed) CalculateDamageWithShield(int amount)
        {
            int originalAmount = amount;
            bool shieldUsed = false;
            int shieldReduction = 0;
            
            // Apply shield damage reduction (50% reduction) if shield is active
            if (HasShield)
            {
                int reducedAmount = (int)(amount * 0.5); // Reduce damage by half
                shieldReduction = amount - reducedAmount;
                amount = reducedAmount;
                shieldUsed = true;
            }
            
            // Apply damage reduction if active
            if (DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - DamageReduction));
            }
            
            return (amount, shieldReduction, shieldUsed);
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
            if (action.IsComboAction)
            {
                ComboSequence.Add(action);
                // Reassign combo orders to be sequential starting from 1
                ReorderComboSequence();
            }
        }
        
        public void RemoveFromCombo(Action action)
        {
            var actionToRemove = ComboSequence.FirstOrDefault(comboAction => comboAction.Name == action.Name);
            if (actionToRemove != null)
            {
                ComboSequence.Remove(actionToRemove);
                // Reset the action's combo order since it's no longer in the combo
                actionToRemove.ComboOrder = 0;
                // Reassign combo orders to be sequential starting from 1
                ReorderComboSequence();
            }
        }

        public override void RemoveAction(Action action)
        {
            // Remove from action pool (base implementation)
            base.RemoveAction(action);
            
            // Also remove from combo sequence if it's there
            RemoveFromCombo(action);
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
            // Clear existing combo sequence
            ComboSequence.Clear();
            
            // Add the two weapon actions to the combo by default
            if (Weapon is WeaponItem weapon)
            {
                var weaponActions = GetGearActions(weapon);
                foreach (var actionName in weaponActions)
                {
                    // Find the action in the action pool and add it to combo
                    var action = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                    if (action.action != null && action.action.IsComboAction)
                    {
                        AddToCombo(action.action);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the combo sequence when gear changes - only removes actions from unequipped gear
        /// without adding any default actions back
        /// </summary>
        private void UpdateComboSequenceAfterGearChange()
        {
            // Remove actions from combo sequence that are no longer in the action pool
            var actionsToRemove = new List<Action>();
            foreach (var comboAction in ComboSequence)
            {
                // Check if this action is still in the action pool
                var stillInPool = ActionPool.Any(a => a.action.Name == comboAction.Name);
                if (!stillInPool)
                {
                    actionsToRemove.Add(comboAction);
                }
            }
            
            // Remove the actions that are no longer available
            foreach (var actionToRemove in actionsToRemove)
            {
                RemoveFromCombo(actionToRemove);
            }
            
            // Reorder the remaining combo sequence
            ReorderComboSequence();
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

        public void UpdateTempEffects(double actionLength = DEFAULT_ACTION_LENGTH)
        {
            // Calculate how many turns this action represents
            double turnsPassed = CalculateTurnsFromActionLength(actionLength);
            
            // Update temporary stat bonuses
            if (TempStatBonusTurns > 0)
            {
                TempStatBonusTurns = Math.Max(0, TempStatBonusTurns - (int)Math.Ceiling(turnsPassed));
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
                StunTurnsRemaining = Math.Max(0, StunTurnsRemaining - (int)Math.Ceiling(turnsPassed));
                if (StunTurnsRemaining == 0)
                    IsStunned = false;
            }

            // Update length reduction
            if (LengthReductionTurns > 0)
            {
                LengthReductionTurns = Math.Max(0, LengthReductionTurns - (int)Math.Ceiling(turnsPassed));
                if (LengthReductionTurns == 0)
                    LengthReduction = 0.0;
            }

            // Update enemy roll penalty
            if (EnemyRollPenaltyTurns > 0)
            {
                EnemyRollPenaltyTurns = Math.Max(0, EnemyRollPenaltyTurns - (int)Math.Ceiling(turnsPassed));
                if (EnemyRollPenaltyTurns == 0)
                    EnemyRollPenalty = 0;
            }
            
            // Update roll penalty (for effects like Dust Cloud)
            if (RollPenaltyTurns > 0)
            {
                RollPenaltyTurns = Math.Max(0, RollPenaltyTurns - (int)Math.Ceiling(turnsPassed));
                if (RollPenaltyTurns == 0)
                    RollPenalty = 0;
            }

            // Update combo amplifier multiplier
            if (ComboAmplifierTurns > 0)
            {
                ComboAmplifierTurns = Math.Max(0, ComboAmplifierTurns - (int)Math.Ceiling(turnsPassed));
                if (ComboAmplifierTurns == 0)
                    ComboAmplifierMultiplier = 1.0;
            }

            // Update extra damage decay (per turn, not per action)
            if (ExtraDamage > 0)
            {
                ExtraDamage = Math.Max(0, ExtraDamage - (int)Math.Ceiling(turnsPassed)); // Decay per turn
            }

            // Update damage reduction decay (per turn, not per action)
            if (DamageReduction > 0)
            {
                DamageReduction = Math.Max(0.0, DamageReduction - (0.1 * Math.Ceiling(turnsPassed))); // Decay per turn
            }

            // Update weaken debuff
            if (WeakenTurns > 0)
            {
                WeakenTurns = Math.Max(0, WeakenTurns - (int)Math.Ceiling(turnsPassed));
                if (WeakenTurns == 0)
                {
                    IsWeakened = false;
                }
            }

            // Update slow debuff
            if (SlowTurns > 0)
            {
                SlowTurns = Math.Max(0, SlowTurns - (int)Math.Ceiling(turnsPassed));
                if (SlowTurns == 0)
                {
                    SlowMultiplier = 1.0; // Reset to normal speed
                }
            }

            // Reset reroll usage for next turn
            ResetRerollUsage();
        }

        public int GetEffectiveStrength()
        {
            return Strength + TempStrengthBonus + GetEquipmentStatBonus("STR") + GetModificationGodlikeBonus();
        }

        public int GetEffectiveAgility()
        {
            return Agility + TempAgilityBonus + GetEquipmentStatBonus("AGI");
        }

        public int GetEffectiveTechnique()
        {
            return Technique + TempTechniqueBonus + GetEquipmentStatBonus("TEC");
        }

        public int GetEffectiveIntelligence()
        {
            return Intelligence + TempIntelligenceBonus + GetEquipmentStatBonus("INT");
        }

        /// <summary>
        /// Gets the total stat bonus from all equipped items for a specific stat type
        /// </summary>
        private int GetEquipmentStatBonus(string statType)
        {
            int totalBonus = 0;
            
            // Check all equipped items
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var statBonus in item.StatBonuses)
                    {
                        // Check for exact stat type match
                        if (statBonus.StatType == statType)
                        {
                            totalBonus += (int)statBonus.Value;
                        }
                        // Check for "ALL" stat type which applies to all stats
                        else if (statBonus.StatType == "ALL")
                        {
                            totalBonus += (int)statBonus.Value;
                        }
                    }
                }
            }
            
            return totalBonus;
        }

        /// <summary>
        /// Gets the total stat bonus from all equipped items for a specific stat type (double version)
        /// </summary>
        private double GetEquipmentStatBonusDouble(string statType)
        {
            double totalBonus = 0.0;
            
            // Check all equipped items
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var statBonus in item.StatBonuses)
                    {
                        // Check for exact stat type match
                        if (statBonus.StatType == statType)
                        {
                            totalBonus += statBonus.Value;
                        }
                        // Check for "ALL" stat type which applies to all stats
                        else if (statBonus.StatType == "ALL")
                        {
                            totalBonus += statBonus.Value;
                        }
                    }
                }
            }
            
            return totalBonus;
        }

        /// <summary>
        /// Gets the total damage bonus from all equipped items
        /// </summary>
        public int GetEquipmentDamageBonus()
        {
            return GetEquipmentStatBonus("Damage");
        }

        /// <summary>
        /// Gets the total health bonus from all equipped items
        /// </summary>
        public int GetEquipmentHealthBonus()
        {
            return GetEquipmentStatBonus("Health");
        }

        /// <summary>
        /// Gets the total roll bonus from all equipped items
        /// </summary>
        public int GetEquipmentRollBonus()
        {
            return GetEquipmentStatBonus("RollBonus");
        }

        /// <summary>
        /// Gets the total Magic Find bonus from all equipped items
        /// </summary>
        public int GetMagicFind()
        {
            return GetEquipmentStatBonus("MagicFind") + GetModificationMagicFind();
        }

        /// <summary>
        /// Gets the total Magic Find bonus from modifications on equipped items
        /// </summary>
        public int GetModificationMagicFind()
        {
            int totalMagicFind = 0;
            
            // Check all equipped items for magicFind modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "magicFind")
                        {
                            totalMagicFind += (int)modification.RolledValue; // Use RolledValue as the bonus amount
                        }
                    }
                }
            }
            
            return totalMagicFind;
        }

        /// <summary>
        /// Gets the total attack speed bonus from all equipped items
        /// </summary>
        public double GetEquipmentAttackSpeedBonus()
        {
            return GetEquipmentStatBonusDouble("AttackSpeed");
        }

        /// <summary>
        /// Gets the total health regeneration bonus from all equipped items
        /// </summary>
        public int GetEquipmentHealthRegenBonus()
        {
            return GetEquipmentStatBonus("HealthRegen");
        }

        /// <summary>
        /// Gets the total armor from all equipped items
        /// </summary>
        public int GetTotalArmor()
        {
            int totalArmor = 0;
            
            // Add armor from equipped items
            if (Head is HeadItem head) totalArmor += head.GetTotalArmor();
            if (Body is ChestItem chest) totalArmor += chest.GetTotalArmor();
            if (Feet is FeetItem feet) totalArmor += feet.GetTotalArmor();
            
            // Add any additional armor from stat bonuses on other items
            totalArmor += GetEquipmentStatBonus("Armor");
            
            return totalArmor;
        }

        /// <summary>
        /// Gets the total number of reroll charges from Divine modifications
        /// </summary>
        public int GetTotalRerollCharges()
        {
            int totalRerolls = 0;
            
            // Check all equipped items for Divine modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "reroll")
                        {
                            totalRerolls++;
                        }
                    }
                }
            }
            
            return totalRerolls;
        }

        // Track Divine reroll charges used this combat
        public int RerollChargesUsed { get; set; } = 0;

        /// <summary>
        /// Gets the remaining Divine reroll charges for this combat
        /// </summary>
        public int GetRemainingRerollCharges()
        {
            return Math.Max(0, GetTotalRerollCharges() - RerollChargesUsed);
        }

        /// <summary>
        /// Uses one Divine reroll charge if available
        /// </summary>
        public bool UseRerollCharge()
        {
            if (GetRemainingRerollCharges() > 0)
            {
                RerollChargesUsed++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets Divine reroll charges for a new combat
        /// </summary>
        public void ResetRerollCharges()
        {
            RerollChargesUsed = 0;
        }


        /// <summary>
        /// Gets the total roll bonus from modifications on equipped items
        /// </summary>
        public int GetModificationRollBonus()
        {
            int totalRollBonus = 0;
            
            // Check all equipped items for rollBonus modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "rollBonus")
                        {
                            totalRollBonus += (int)modification.RolledValue; // Use RolledValue as the bonus amount
                        }
                    }
                }
            }
            
            return totalRollBonus;
        }

        /// <summary>
        /// Gets the total damage bonus from modifications on equipped items
        /// </summary>
        public int GetModificationDamageBonus()
        {
            int totalDamageBonus = 0;
            
            // Check all equipped items for damage modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "damage")
                        {
                            totalDamageBonus += (int)modification.RolledValue; // Use RolledValue as the bonus amount
                        }
                    }
                }
            }
            
            return totalDamageBonus;
        }

        /// <summary>
        /// Gets the total speed multiplier from modifications on equipped items
        /// </summary>
        public double GetModificationSpeedMultiplier()
        {
            double totalSpeedMultiplier = 1.0;
            
            // Check all equipped items for speedMultiplier modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "speedMultiplier")
                        {
                            totalSpeedMultiplier *= modification.RolledValue; // Multiply speed multipliers
                        }
                    }
                }
            }
            
            return totalSpeedMultiplier;
        }

        /// <summary>
        /// Gets the total damage multiplier from modifications on equipped items
        /// </summary>
        public double GetModificationDamageMultiplier()
        {
            double totalDamageMultiplier = 1.0;
            
            // Check all equipped items for damageMultiplier modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "damageMultiplier")
                        {
                            totalDamageMultiplier *= modification.RolledValue; // Multiply damage multipliers
                        }
                    }
                }
            }
            
            return totalDamageMultiplier;
        }

        /// <summary>
        /// Gets the total lifesteal percentage from modifications on equipped items
        /// </summary>
        public double GetModificationLifesteal()
        {
            double totalLifesteal = 0.0;
            
            // Check all equipped items for lifesteal modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "lifesteal")
                        {
                            totalLifesteal += modification.RolledValue; // Add lifesteal percentages
                        }
                    }
                }
            }
            
            return totalLifesteal;
        }

        /// <summary>
        /// Gets the total STR bonus from godlike modifications on equipped items
        /// </summary>
        public int GetModificationGodlikeBonus()
        {
            int totalGodlikeBonus = 0;
            
            // Check all equipped items for godlike modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "godlike")
                        {
                            totalGodlikeBonus += (int)modification.RolledValue; // Use RolledValue as the bonus amount
                        }
                    }
                }
            }
            
            return totalGodlikeBonus;
        }

        /// <summary>
        /// Gets the total bleed chance from modifications on equipped items
        /// </summary>
        public double GetModificationBleedChance()
        {
            double totalBleedChance = 0.0;
            
            // Check all equipped items for bleedChance modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "bleedChance")
                        {
                            totalBleedChance += modification.RolledValue; // Use RolledValue as the chance amount
                        }
                    }
                }
            }
            
            return totalBleedChance;
        }

        /// <summary>
        /// Gets the total unique action chance from modifications on equipped items
        /// </summary>
        public double GetModificationUniqueActionChance()
        {
            double totalUniqueActionChance = 0.0;
            
            // Check all equipped items for uniqueActionChance modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "uniqueActionChance")
                        {
                            totalUniqueActionChance += modification.RolledValue; // Use RolledValue as the chance amount
                        }
                    }
                }
            }
            
            return totalUniqueActionChance;
        }

        /// <summary>
        /// Gets available unique actions based on weapon type and class
        /// </summary>
        public List<Action> GetAvailableUniqueActions()
        {
            var uniqueActions = new List<Action>();
            var allActions = ActionLoader.GetAllActions();
            
            // Get weapon-specific unique actions
            if (Weapon is WeaponItem weapon)
            {
                string weaponType = weapon.WeaponType.ToString().ToLower();
                var weaponUniqueActions = allActions.Where(action => 
                    action.Tags.Contains("unique") && 
                    action.Tags.Contains("weapon") && 
                    action.Tags.Contains(weaponType)
                ).ToList();
                uniqueActions.AddRange(weaponUniqueActions);
            }
            
            // Get class-specific unique actions
            var classUniqueActions = allActions.Where(action => 
                action.Tags.Contains("unique") && 
                action.Tags.Contains("class")
            ).ToList();
            uniqueActions.AddRange(classUniqueActions);
            
            return uniqueActions;
        }

        /// <summary>
        /// Gets the total armor spike damage from equipped armor
        /// </summary>
        public double GetArmorSpikeDamage()
        {
            double totalSpikeDamage = 0.0;
            
            // Check all equipped items for armor spike statuses
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var armorStatus in item.ArmorStatuses)
                    {
                        if (armorStatus.Effect == "armorSpikes")
                        {
                            totalSpikeDamage += armorStatus.Value;
                        }
                    }
                }
            }
            
            return totalSpikeDamage;
        }

        /// <summary>
        /// Gets all armor statuses from equipped items
        /// </summary>
        public List<ArmorStatus> GetEquippedArmorStatuses()
        {
            var allStatuses = new List<ArmorStatus>();
            
            // Check all equipped items for armor statuses
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    allStatuses.AddRange(item.ArmorStatuses);
                }
            }
            
            return allStatuses;
        }

        /// <summary>
        /// Checks if the character has autoSuccess modifications
        /// </summary>
        public bool HasAutoSuccess()
        {
            // Check all equipped items for autoSuccess modifications
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "autoSuccess")
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Updates reroll charges based on equipped Divine modifications
        /// </summary>
        public void UpdateRerollCharges()
        {
            RerollCharges = GetTotalRerollCharges();
        }

        /// <summary>
        /// Attempts to use a reroll charge
        /// </summary>
        /// <returns>True if reroll was used, false if no charges available</returns>
        public bool UseReroll()
        {
            if (RerollCharges > 0 && !UsedRerollThisTurn)
            {
                RerollCharges--;
                UsedRerollThisTurn = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets reroll usage for the next turn
        /// </summary>
        public void ResetRerollUsage()
        {
            UsedRerollThisTurn = false;
            // Refresh reroll charges from equipment
            UpdateRerollCharges();
        }

        /// <summary>
        /// Clears all temporary effects (poison, bleed, stun, weaken, slow, roll penalty)
        /// </summary>
        public void ClearAllTempEffects()
        {
            // Clear poison/bleed effects
            PoisonDamage = 0;
            PoisonStacks = 0;
            IsBleeding = false;
            LastPoisonTick = 0.0;
            
            // Clear stun effects
            IsStunned = false;
            StunTurnsRemaining = 0;
            
            // Clear weaken effects
            IsWeakened = false;
            WeakenTurns = 0;
            
            // Clear slow effects
            SlowTurns = 0;
            SlowMultiplier = 1.0;
            
            // Clear roll penalty effects
            RollPenalty = 0;
            RollPenaltyTurns = 0;
        }

        /// <summary>
        /// Applies a weaken debuff to the character
        /// </summary>
        /// <param name="turns">Number of turns the weaken effect lasts</param>
        public void ApplyWeaken(int turns)
        {
            IsWeakened = true;
            WeakenTurns = turns;
        }
        
        /// <summary>
        /// Calculates how many turns an action length represents
        /// </summary>
        /// <param name="actionLength">The length of the action</param>
        /// <returns>Number of turns the action represents</returns>
        public static double CalculateTurnsFromActionLength(double actionLength)
        {
            return actionLength / DEFAULT_ACTION_LENGTH;
        }
        
        /// <summary>
        /// Calculates action length from number of turns
        /// </summary>
        /// <param name="turns">Number of turns</param>
        /// <returns>Action length equivalent</returns>
        public static double CalculateActionLengthFromTurns(double turns)
        {
            return turns * DEFAULT_ACTION_LENGTH;
        }

        /// <summary>
        /// Gets the effective max health including equipment bonuses
        /// </summary>
        public int GetEffectiveMaxHealth()
        {
            return MaxHealth + GetEquipmentHealthBonus();
        }

        public double GetHealthPercentage()
        {
            return (double)CurrentHealth / GetEffectiveMaxHealth();
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
            // Add special Barbarian class action when they have at least 5 points
            if (BarbarianPoints >= 5)
            {
                var berserkerRage = ActionLoader.GetAction("BERSERKER RAGE");
                if (berserkerRage != null)
                {
                    AddAction(berserkerRage, 0.0);
                }
            }
            // Future thresholds: 10, 20, 40, 80, 100 points for additional abilities
        }

        private void AddWarriorActions()
        {
            // Add special Warrior class actions when they have at least 5 points
            if (WarriorPoints >= 5)
            {
                
                var heroicStrike = ActionLoader.GetAction("HEROIC STRIKE");
                if (heroicStrike != null)
                {
                    AddAction(heroicStrike, 0.0);
                }
                
                var whirlwind = ActionLoader.GetAction("WHIRLWIND");
                if (whirlwind != null)
                {
                    AddAction(whirlwind, 0.0);
                }
            }
            else
            {
            }
        }

        private void AddRogueActions()
        {
            // Add special Rogue class action when they have at least 5 points
            if (RoguePoints >= 5)
            {
                var shadowStrike = ActionLoader.GetAction("SHADOW STRIKE");
                if (shadowStrike != null)
                {
                    AddAction(shadowStrike, 0.0);
                }
            }
        }

        private void AddWizardActions()
        {
            // Only add wizard actions if the character is actually a wizard class
            // A wizard class requires: 1) Wand equipped, 2) Wizard points > 0, 3) Wizard is primary class
            bool isWizardClass = IsWizardClass();
            
            if (isWizardClass)
            {
                // Add FIREBALL as a basic wizard spell (available at 3+ wizard points)
                if (WizardPoints >= 3)
                {
                    var fireball = ActionLoader.GetAction("FIREBALL");
                    if (fireball != null)
                    {
                        AddAction(fireball, 0.0);
                    }
                }
                
                // Add special Wizard class action when they have at least 5 points
                if (WizardPoints >= 5)
                {
                    var meteor = ActionLoader.GetAction("METEOR");
                    if (meteor != null)
                    {
                        AddAction(meteor, 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the character is a wizard class
        /// Requires: 1) Wand equipped, 2) Wizard points > 0, 3) Wizard is primary class
        /// </summary>
        private bool IsWizardClass()
        {
            // Must have a wand equipped
            if (!(Weapon is WeaponItem weapon) || weapon.WeaponType != WeaponType.Wand)
            {
                return false;
            }
            
            // Must have wizard points
            if (WizardPoints <= 0)
            {
                return false;
            }
            
            // Wizard must be the primary class (most points)
            var classes = new List<(string name, int points)>
            {
                ("Barbarian", BarbarianPoints),
                ("Warrior", WarriorPoints),
                ("Rogue", RoguePoints),
                ("Wizard", WizardPoints)
            };
            
            // Sort by points descending - wizard must be first (highest points)
            classes.Sort((a, b) => b.points.CompareTo(a.points));
            return classes[0].name == "Wizard";
        }

        /// <summary>
        /// Gets the next class upgrade threshold for a given class
        /// </summary>
        public int GetNextClassThreshold(string className)
        {
            int currentPoints = className switch
            {
                "Barbarian" => BarbarianPoints,
                "Warrior" => WarriorPoints,
                "Rogue" => RoguePoints,
                "Wizard" => WizardPoints,
                _ => 0
            };

            // Define upgrade thresholds
            int[] thresholds = { 5, 10, 20, 40, 80, 100 };
            
            foreach (int threshold in thresholds)
            {
                if (currentPoints < threshold)
                {
                    return threshold;
                }
            }
            
            return -1; // Max level reached
        }

        /// <summary>
        /// Gets a string showing next class upgrade thresholds
        /// </summary>
        public string GetClassUpgradeInfo()
        {
            var upgradeInfo = new List<string>();
            
            var classes = new[] { ("Barbarian", BarbarianPoints), ("Warrior", WarriorPoints), ("Rogue", RoguePoints), ("Wizard", WizardPoints) };
            
            // Filter to only show classes with at least 1 point, then sort by points (highest first) and take top 2
            var classesWithPoints = classes.Where(c => c.Item2 > 0);
            var sortedClasses = classesWithPoints.OrderByDescending(c => c.Item2).Take(2);
            
            foreach (var (className, points) in sortedClasses)
            {
                int nextThreshold = GetNextClassThreshold(className);
                if (nextThreshold > 0)
                {
                    int pointsNeeded = nextThreshold - points;
                    upgradeInfo.Add($"{className}: {pointsNeeded} to go");
                }
                else
                {
                    upgradeInfo.Add($"{className}: MAX");
                }
            }
            
            return string.Join(" | ", upgradeInfo);
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

        public string GetFullNameWithQualifier()
        {
            string currentClass = GetCurrentClass();
            int primaryClassPoints = GetPrimaryClassPoints();
            string qualifier = FlavorText.GetClassQualifier(currentClass, primaryClassPoints);
            return $"{Name} {qualifier}";
        }

        private int GetPrimaryClassPoints()
        {
            var classes = new List<(string name, int points)>
            {
                ("Barbarian", BarbarianPoints),
                ("Warrior", WarriorPoints),
                ("Rogue", RoguePoints),
                ("Wizard", WizardPoints)
            };

            // Sort by points descending and return the highest
            classes.Sort((a, b) => b.points.CompareTo(a.points));
            return classes[0].points;
        }

        private string GetClassTier(string baseClass, int points)
        {
            if (points >= 100)
                return $"Legendary {baseClass}";
            if (points >= 80)
                return $"Epic {baseClass}";
            if (points >= 40)
                return $"Grand {baseClass}";
            if (points >= 20)
                return $"Master {baseClass}";
            if (points >= 10)
                return $"Expert {baseClass}";
            if (points >= 5)
                return $"Adept {baseClass}";
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
            
            
            // If there are actions to add, load them
            if (gearActions.Count > 0)
            {
                foreach (var actionName in gearActions)
                {
                    // Load gear actions from JSON to ensure they're properly configured
                    LoadGearActionFromJson(actionName);
                }
                
                // Apply roll bonuses from stat bonuses to all actions
                ApplyRollBonusesFromGear(gear);
            }
        }

        private List<string> GetGearActions(Item gear)
        {
            if (gear is WeaponItem weapon)
            {
                // Get weapon actions from Actions.json based on weapon type
                return GetWeaponActionsFromJson(weapon.WeaponType);
            }
            else if (gear is HeadItem || gear is ChestItem || gear is FeetItem)
            {
                // Only get armor actions if this armor should have special actions
                if (HasSpecialArmorActions(gear))
                {
                    // Use the gear's assigned action if it has one
                    if (!string.IsNullOrEmpty(gear.GearAction))
                    {
                        return new List<string> { gear.GearAction };
                    }
                    else
                    {
                        // Fallback to random selection for gear without assigned actions
                        return GetRandomArmorActionFromJson(gear);
                    }
                }
                else
                {
                    // Basic starter armor should have no actions
                    return new List<string>();
                }
            }
            
            return new List<string>();
        }

        private void LoadGearActionFromJson(string actionName)
        {
            try
            {
                string[] possiblePaths = {
                    Path.Combine("GameData", "Actions.json"),
                    Path.Combine("..", "GameData", "Actions.json"),
                    Path.Combine("..", "..", "GameData", "Actions.json"),
                    Path.Combine("DF4 - CONSOLE", "GameData", "Actions.json"),
                    Path.Combine("..", "DF4 - CONSOLE", "GameData", "Actions.json")
                };

                string? foundPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }
                
                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    var allActions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (allActions != null)
                    {
                        var actionData = allActions.FirstOrDefault(a => a.Name == actionName);
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
                            else
                            {
                            }
                        }
                        else
                        {
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Actions.json not found when loading gear action {actionName}. Tried paths: {string.Join(", ", possiblePaths)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading gear action {actionName} from JSON: {ex.Message}");
            }
        }

        private void RemoveWeaponActions(WeaponItem? weapon = null)
        {
            // Remove weapon-specific gear actions using the same logic as GetWeaponActionsFromJson
            var weaponToRemove = weapon ?? Weapon as WeaponItem;
            if (weaponToRemove != null)
            {
                var weaponActions = GetWeaponActionsFromJson(weaponToRemove.WeaponType);
                
                foreach (var actionName in weaponActions)
                {
                    var actionToRemove = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                    if (actionToRemove.action != null)
                    {
                        RemoveAction(actionToRemove.action);
                    }
                }
            }
        }

        private void RemoveClassActions()
        {
            // Remove class-specific actions (core combo actions)
            var classActions = new[] { 
                "TAUNT", "JAB", "STUN", "CRIT", "SHIELD BASH", "DEFENSIVE STANCE", 
                "BERSERK", "BLOOD FRENZY", "PRECISION STRIKE", "QUICK REFLEXES",
                "FOCUS", "READ BOOK", "HEROIC STRIKE", "WHIRLWIND", "BERSERKER RAGE", "SHADOW STRIKE"
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

        private List<string> GetWeaponActionsFromJson(WeaponType weaponType)
        {
            var weaponTag = weaponType.ToString().ToLower();
            var allActions = ActionLoader.GetAllActions();
            
            // For mace weapons, return the specific mace actions
            if (weaponType == WeaponType.Mace)
            {
                return new List<string> { "CRUSHING BLOW", "SHIELD BREAK", "THUNDER CLAP" };
            }
            
            // For other weapon types, use the original logic
            var weaponActions = allActions
                .Where(action => action.Tags.Contains("weapon") && 
                                action.Tags.Contains(weaponTag) &&
                                !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();
                
            return weaponActions;
        }

        private List<string> GetRandomArmorActionFromJson(Item armor)
        {
            // All armor types get random actions for variety
            var randomAction = GetRandomArmorActionName();
            if (!string.IsNullOrEmpty(randomAction))
            {
                return new List<string> { randomAction };
            }
            
            // Fallback to random selection for unknown armor types
            var allActions = ActionLoader.GetAllActions();
            
            // Get all armor actions, excluding environmental actions
            var armorActions = allActions
                .Where(action => action.Tags.Contains("armor") && 
                                !action.Tags.Contains("environment"))
                .Select(action => action.Name)
                .ToList();

            // If no armor actions found, get any non-environmental combo actions
            if (armorActions.Count == 0)
            {
                armorActions = allActions
                    .Where(action => action.IsComboAction && 
                                    !action.Tags.Contains("environment") &&
                                    !action.Tags.Contains("enemy") &&
                                    !action.Tags.Contains("weapon"))
                    .Select(action => action.Name)
                    .ToList();
            }

            // Return ONE random armor action (not all of them)
            if (armorActions.Count > 0)
            {
                var fallbackAction = armorActions[Random.Shared.Next(armorActions.Count)];
                return new List<string> { fallbackAction };
            }
            
            return new List<string>();
        }

        private string? GetRandomArmorActionName()
        {
            // Get ALL combo actions (not just armor-tagged ones) for maximum variety
            var allActions = ActionLoader.GetAllActions();
            var availableActions = allActions
                .Where(action => action.IsComboAction && 
                               !action.Tags.Contains("environment") &&
                               !action.Tags.Contains("enemy") &&
                               !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();

            // Return completely random action if any are available
            if (availableActions.Count > 0)
            {
                return availableActions[Random.Shared.Next(availableActions.Count)];
            }
            
            return null; // No action if none available
        }

        private void AddArmorActions(Item armor)
        {
            // Add gear-specific actions for armor
            AddGearActions(armor);
        }

        public void AddEnvironmentActions(Environment environment)
        {
            // Add environment actions to the player's action pool
            if (environment != null && environment.ActionPool.Count > 0)
            {
                foreach (var (action, probability) in environment.ActionPool)
                {
                    // Add environment actions with lower probability (they're situational)
                    AddAction(action, probability * 0.5); // 50% of environment's probability
                }
            }
        }

        public void ClearEnvironmentActions()
        {
            // Remove all actions that have the "environment" tag
            var actionsToRemove = new List<Action>();
            foreach (var (action, probability) in ActionPool)
            {
                if (action.Tags.Contains("environment"))
                {
                    actionsToRemove.Add(action);
                }
            }
            
            foreach (var action in actionsToRemove)
            {
                RemoveAction(action);
            }
        }

        /// <summary>
        /// Determines if an armor piece should have special actions
        /// Only special armor pieces (looted gear with modifications, stat bonuses, etc.) should have actions
        /// Default/starter armor should have no actions
        /// </summary>
        private bool HasSpecialArmorActions(Item armor)
        {
            // Check if this armor piece has special properties that indicate it should have actions
            
            // 1. Check if it has modifications (indicates special gear)
            if (armor.Modifications.Count > 0)
            {
                return true;
            }
            
            // 2. Check if it has stat bonuses (indicates special gear)
            if (armor.StatBonuses.Count > 0)
            {
                return true;
            }
            
            // 3. Check if it has action bonuses (legacy system)
            if (armor.ActionBonuses.Count > 0)
            {
                return true;
            }
            
            // 4. Check if it's not basic starter gear by name
            string[] basicGearNames = { "Leather Helmet", "Leather Armor", "Leather Boots", "Cloth Hood", "Cloth Robes", "Cloth Shoes" };
            if (basicGearNames.Contains(armor.Name))
            {
                return false; // Basic starter gear should have no actions
            }
            
            // 5. If it has a special name or properties, it might be special gear
            // Only return true if it's clearly special gear, otherwise be conservative
            return false;
        }

        private void ApplyRollBonusesFromGear(Item gear)
        {
            // Find all roll bonus stat bonuses from the gear
            int totalRollBonus = 0;
            foreach (var statBonus in gear.StatBonuses)
            {
                if (statBonus.StatType == "RollBonus")
                {
                    totalRollBonus += (int)statBonus.Value;
                }
            }
            
            // Apply the roll bonus to all actions in the action pool
            if (totalRollBonus > 0)
            {
                foreach (var actionEntry in ActionPool)
                {
                    actionEntry.action.RollBonus += totalRollBonus;
                }
            }
        }

        private void RemoveRollBonusesFromGear(Item gear)
        {
            // Find all roll bonus stat bonuses from the gear
            int totalRollBonus = 0;
            foreach (var statBonus in gear.StatBonuses)
            {
                if (statBonus.StatType == "RollBonus")
                {
                    totalRollBonus += (int)statBonus.Value;
                }
            }
            
            // Remove the roll bonus from all actions in the action pool
            if (totalRollBonus > 0)
            {
                foreach (var actionEntry in ActionPool)
                {
                    actionEntry.action.RollBonus -= totalRollBonus;
                }
            }
        }

        /// <summary>
        /// Calculates the combo amplification multiplier based on Technique
        /// Formula: Linear scaling from ComboAmplifierAtTech5 (at TEC=5) to ComboAmplifierMax (at TEC=100)
        /// </summary>
        public double GetComboAmplifier()
        {
            var tuning = TuningConfig.Instance;
            
            // Clamp Technique to valid range
            int clampedTech = Math.Max(1, Math.Min(tuning.ComboSystem.ComboAmplifierMaxTech, GetEffectiveTechnique()));
            
            // Linear interpolation between ComboAmplifierAtTech5 and ComboAmplifierMax
            double techRange = tuning.ComboSystem.ComboAmplifierMaxTech - 5;
            double ampRange = tuning.ComboSystem.ComboAmplifierMax - tuning.ComboSystem.ComboAmplifierAtTech5;
            
            if (clampedTech <= 5)
            {
                return tuning.ComboSystem.ComboAmplifierAtTech5;
            }
            
            double techProgress = (clampedTech - 5) / techRange;
            return tuning.ComboSystem.ComboAmplifierAtTech5 + (ampRange * techProgress);
        }

        /// <summary>
        /// Sets Technique for testing purposes
        /// </summary>
        public void SetTechniqueForTesting(int value)
        {
            Technique = value;
        }

        /// <summary>
        /// Gets the current combo amplification for the current combo step
        /// </summary>
        public double GetCurrentComboAmplification()
        {
            var comboActions = GetComboActions();
            if (comboActions.Count == 0) return 1.0;
            
            int currentStep = ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            return Math.Pow(baseAmp, currentStep);
        }

        /// <summary>
        /// Gets combo information for display
        /// </summary>
        public string GetComboInfo()
        {
            var comboActions = GetComboActions();
            if (comboActions.Count == 0) return "No combo actions available";
            
            int currentStep = ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            double currentAmp = Math.Pow(baseAmp, currentStep + 1);
            
            // Only return amplification info, combo step removed per user request
            return $"Amplification: {currentAmp:F2}x";
        }

        /// <summary>
        /// Calculates the number of attacks this character can make per turn based on their attack speed
        /// </summary>
        /// <returns>Number of attacks (minimum 1)</returns>
        public int GetAttacksPerTurn()
        {
            double attackSpeed = GetTotalAttackSpeed();
            int attacks = (int)Math.Floor(attackSpeed);
            return Math.Max(1, attacks); // Always at least 1 attack
        }

        /// <summary>
        /// Calculates the total attack speed including weapon and agility bonuses
        /// </summary>
        /// <returns>Total attack speed value</returns>
        public double GetTotalAttackSpeed()
        {
            var tuning = TuningConfig.Instance;
            
            // New system: Base 10s, agility reduces time, weapon adds modifier
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            double agilityReduction = GetEffectiveAgility() * tuning.Combat.AgilitySpeedReduction;
            double weaponModifier = (Weapon is WeaponItem w) ? w.GetAttackSpeedModifier() : 0.0;
            double equipmentSpeedBonus = GetEquipmentAttackSpeedBonus();
            
            // Calculate final attack time: base - agility reduction + weapon modifier - equipment speed bonus
            double finalAttackTime = baseAttackTime - agilityReduction + weaponModifier - equipmentSpeedBonus;
            
            // Apply slow debuff if active
            if (SlowTurns > 0)
            {
                finalAttackTime *= SlowMultiplier;
            }
            
            // Apply speed multiplier modifications (like Ethereal)
            double speedMultiplier = GetModificationSpeedMultiplier();
            finalAttackTime /= speedMultiplier; // Divide by multiplier to make attacks faster
            
            // Apply minimum cap
            return Math.Max(tuning.Combat.MinimumAttackTime, finalAttackTime);
        }

        /// <summary>
        /// Applies a slow debuff to the character
        /// </summary>
        /// <param name="slowMultiplier">Multiplier for attack speed (higher = slower)</param>
        /// <param name="duration">Duration in turns</param>
        public void ApplySlow(double slowMultiplier, int duration)
        {
            SlowMultiplier = slowMultiplier;
            SlowTurns = duration;
        }
        
        /// <summary>
        /// Reduces slow debuff duration by 1 turn
        /// </summary>
        public void ReduceSlowTurns()
        {
            if (SlowTurns > 0)
            {
                SlowTurns--;
                if (SlowTurns <= 0)
                {
                    SlowMultiplier = 1.0; // Reset to normal speed
                }
            }
        }
        
        /// <summary>
        /// Applies a poison debuff to the character
        /// </summary>
        /// <param name="damage">Damage per tick</param>
        /// <param name="stacks">Number of poison stacks to add</param>
        /// <param name="isBleeding">Whether this is bleeding damage (vs poison)</param>
        public void ApplyPoison(int damage, int stacks = 1, bool isBleeding = false)
        {
            PoisonDamage = damage;
            PoisonStacks += stacks; // Add stacks (can stack multiple times)
            LastPoisonTick = GameTicker.Instance.GetCurrentGameTime(); // Set to current time so first tick happens after interval
            IsBleeding = isBleeding; // Track whether this is bleeding or poison
        }
        
        /// <summary>
        /// Applies a shield buff that reduces the next incoming attack damage by half
        /// </summary>
        public void ApplyShield()
        {
            HasShield = true;
        }
        
        /// <summary>
        /// Consumes the shield and returns whether it was active
        /// </summary>
        /// <returns>True if shield was active and consumed, false otherwise</returns>
        public bool ConsumeShield()
        {
            if (HasShield)
            {
                HasShield = false;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Processes poison damage based on time intervals (every 10 seconds)
        /// </summary>
        /// <param name="currentTime">Current game time in seconds</param>
        /// <returns>Damage taken from poison this tick</returns>
        public int ProcessPoison(double currentTime)
        {
            if (PoisonStacks > 0)
            {
                var poisonConfig = TuningConfig.Instance.Poison;
                
                // Check if it's time for a poison tick (based on config)
                if (currentTime - LastPoisonTick >= poisonConfig.TickInterval)
                {
                    int totalDamage = PoisonDamage * PoisonStacks;
                    TakeDamage(totalDamage);
                    LastPoisonTick = currentTime;
                    
                    // Reduce poison stacks by 1
                    PoisonStacks--;
                    if (PoisonStacks <= 0)
                    {
                        PoisonDamage = 0; // Reset poison
                        PoisonStacks = 0;
                        IsBleeding = false; // Reset bleeding flag
                    }
                    return totalDamage;
                }
            }
            return 0;
        }
        
        /// <summary>
        /// Gets the damage type text for display purposes
        /// </summary>
        /// <returns>"poison" or "bleed" based on IsBleeding flag</returns>
        public string GetDamageTypeText()
        {
            return IsBleeding ? "bleed" : "poison";
        }

        /// <summary>
        /// Calculates the Intelligence roll bonus (every 10 INT = +1 to rolls by default)
        /// </summary>
        /// <returns>Roll bonus from Intelligence</returns>
        public int GetIntelligenceRollBonus()
        {
            var tuning = TuningConfig.Instance;
            return GetEffectiveIntelligence() / tuning.Attributes.IntelligenceRollBonusPer; // Every X points of INT gives +1 to rolls
        }

        private void RemoveArmorActions(Item? armor)
        {
            if (armor == null) return;
            
            // Use the same logic as GetGearActions to determine which actions to remove
            var armorActions = GetGearActions(armor);
            
            foreach (var actionName in armorActions)
            {
                var actionToRemove = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                if (actionToRemove.action != null)
                {
                    RemoveAction(actionToRemove.action);
                }
            }
        }

        /// <summary>
        /// Removes actions associated with any item (weapon or armor) from the action pool
        /// </summary>
        public void RemoveItemActions(Item? item)
        {
            if (item == null) return;

            if (item is WeaponItem weapon)
            {
                // Remove weapon-specific actions using the same logic as GetGearActions
                var weaponActions = GetWeaponActionsFromJson(weapon.WeaponType);
                
                foreach (var actionName in weaponActions)
                {
                    var actionToRemove = ActionPool.FirstOrDefault(a => a.action.Name == actionName);
                    if (actionToRemove.action != null)
                    {
                        RemoveAction(actionToRemove.action);
                    }
                }
            }
            else if (item is HeadItem || item is ChestItem || item is FeetItem)
            {
                // Remove armor actions
                RemoveArmorActions(item);
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
                
                // Use the saved name, but fix "Unknown Unknown" names
                if (saveData.Name == "Unknown Unknown")
                {
                    character.Name = FlavorText.GenerateCharacterName();
                }
                else
                {
                    character.Name = saveData.Name; // Preserve the saved name
                }
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting save file: {ex.Message}");
            }
        }
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