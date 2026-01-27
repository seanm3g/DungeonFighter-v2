using System;
using System.Collections.Generic;
using RPGGame.Combat.Formatting;
using RPGGame.UI.ColorSystem;
using RPGGame.Combat.Events;

namespace RPGGame
{
    /// <summary>
    /// Simplified combat effects handler using the effect registry pattern
    /// Replaces the original CombatEffects.cs with cleaner, more maintainable code
    /// </summary>
    public static class CombatEffectsSimplified
    {
        private static readonly EffectHandlerRegistry _effectRegistry = new EffectHandlerRegistry();

        /// <summary>
        /// Applies status effects from an action to the target
        /// </summary>
        /// <param name="action">The action being performed</param>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="target">The target Actor</param>
        /// <param name="results">List to add effect messages to</param>
        /// <param name="combatEvent">Optional combat event for conditional status application</param>
        /// <returns>True if any effects were applied</returns>
        public static bool ApplyStatusEffects(Action action, Actor attacker, Actor target, List<string> results, Combat.Events.CombatEvent? combatEvent = null)
        {
            bool effectsApplied = false;
            
            // First, apply guaranteed status effects from modifications (for characters only, Attack actions only)
            // Then, check for weapon modification status effect chances (for characters only)
            if (attacker is Character characterAttacker)
            {
                effectsApplied |= ApplyModificationStatusEffects(characterAttacker, target, action, results);
                effectsApplied |= ApplyWeaponModificationStatusEffects(characterAttacker, target, action, results);
            }
            
            // Then apply status effects from the action itself
            var effectTypes = GetEffectTypesFromAction(action);
            
            // Check if status effects should be conditionally applied
            bool shouldApplyEffects = true;
            if (action.Triggers.TriggerConditions != null && action.Triggers.TriggerConditions.Count > 0 && combatEvent != null)
            {
                // Check if any condition matches the current event
                shouldApplyEffects = false;
                foreach (var conditionStr in action.Triggers.TriggerConditions)
                {
                    var upper = conditionStr.ToUpper();
                    bool matches = upper switch
                    {
                        "ONMISS" => combatEvent.IsMiss || combatEvent.Type == CombatEventType.ActionMiss,
                        "ONHIT" or "ONNORMALHIT" => combatEvent.Type == CombatEventType.ActionHit && !combatEvent.IsCombo && !combatEvent.IsCritical,
                        "ONCOMBO" or "ONCOMBOHIT" => combatEvent.IsCombo,
                        "ONCRITICAL" or "ONCRITICALHIT" => combatEvent.IsCritical,
                        _ => false
                    };
                    if (matches)
                    {
                        shouldApplyEffects = true;
                        break; // At least one condition matches
                    }
                }
            }
            
            if (shouldApplyEffects)
            {
                foreach (var effectType in effectTypes)
                {
                    if (_effectRegistry.ApplyEffect(effectType, target, action, results))
                    {
                        effectsApplied = true;
                    }
                }
            }
            
            return effectsApplied;
        }

        /// <summary>
        /// Applies guaranteed status effects from modifications to Attack actions
        /// </summary>
        /// <param name="attacker">The attacking character</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action being performed</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if any effects were applied</returns>
        private static bool ApplyModificationStatusEffects(Character attacker, Actor target, Action action, List<string> results)
        {
            if (action.Type != ActionType.Attack) return false;
            
            bool effectsApplied = false;
            var statusEffects = attacker.GetModificationStatusEffects();
            
            foreach (var effectName in statusEffects)
            {
                // Create temporary action with the status effect flag set
                var tempAction = CreateActionWithStatusEffect(effectName);
                if (tempAction != null && _effectRegistry.ApplyEffect(effectName.ToLower(), target, tempAction, results))
                {
                    effectsApplied = true;
                }
            }
            
            return effectsApplied;
        }

        /// <summary>
        /// Creates an Action object with a specific status effect flag set
        /// </summary>
        /// <param name="effectName">The name of the status effect</param>
        /// <returns>Action with the status effect flag set, or null if effect name is invalid</returns>
        private static Action? CreateActionWithStatusEffect(string effectName)
        {
            var action = new Action();
            var lower = effectName.ToLower();
            
            // Map status effect names to Action properties
            switch (lower)
            {
                case "bleed": action.CausesBleed = true; break;
                case "weaken": action.CausesWeaken = true; break;
                case "slow": action.CausesSlow = true; break;
                case "poison": action.CausesPoison = true; break;
                case "burn": action.CausesBurn = true; break;
                case "stun": action.CausesStun = true; break;
                case "vulnerability": action.CausesVulnerability = true; break;
                case "harden": action.CausesHarden = true; break;
                case "fortify": action.CausesFortify = true; break;
                case "focus": action.CausesFocus = true; break;
                case "expose": action.CausesExpose = true; break;
                case "hpregen": action.CausesHPRegen = true; break;
                case "armorbreak": action.CausesArmorBreak = true; break;
                case "pierce": action.CausesPierce = true; break;
                case "reflect": action.CausesReflect = true; break;
                case "silence": action.CausesSilence = true; break;
                case "statdrain": action.CausesStatDrain = true; break;
                case "absorb": action.CausesAbsorb = true; break;
                case "temporaryhp": action.CausesTemporaryHP = true; break;
                case "confusion": action.CausesConfusion = true; break;
                case "cleanse": action.CausesCleanse = true; break;
                case "mark": action.CausesMark = true; break;
                case "disrupt": action.CausesDisrupt = true; break;
                default: return null;
            }
            
            return action;
        }

        /// <summary>
        /// Applies status effects from weapon modifications based on chance
        /// </summary>
        /// <param name="attacker">The attacking character</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action being performed (used for effect handlers)</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if any effects were applied</returns>
        private static bool ApplyWeaponModificationStatusEffects(Character attacker, Actor target, Action action, List<string> results)
        {
            bool effectsApplied = false;
            
            // Check poison chance
            double poisonChance = attacker.GetModificationPoisonChance();
            if (poisonChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < poisonChance)
                {
                    // Create a temporary action with CausesPoison set to true for the effect handler
                    var tempAction = new Action
                    {
                        CausesPoison = true
                    };
                    if (_effectRegistry.ApplyEffect("poison", target, tempAction, results))
                    {
                        effectsApplied = true;
                    }
                }
            }
            
            // Check burn chance
            double burnChance = attacker.GetModificationBurnChance();
            if (burnChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < burnChance)
                {
                    var tempAction = new Action
                    {
                        CausesBurn = true
                    };
                    if (_effectRegistry.ApplyEffect("burn", target, tempAction, results))
                    {
                        effectsApplied = true;
                    }
                }
            }
            
            // Check bleed chance
            double bleedChance = attacker.GetModificationBleedChance();
            if (bleedChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < bleedChance)
                {
                    var tempAction = new Action
                    {
                        CausesBleed = true
                    };
                    if (_effectRegistry.ApplyEffect("bleed", target, tempAction, results))
                    {
                        effectsApplied = true;
                    }
                }
            }
            
            // Check freeze chance
            double freezeChance = attacker.GetModificationFreezeChance();
            if (freezeChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < freezeChance)
                {
                    // Apply freeze effect (50% speed reduction for 5 turns)
                    var freezeConfig = GameConfiguration.Instance.StatusEffects.Freeze;
                    if (target is Character targetCharacter)
                    {
                        targetCharacter.ApplySlow(freezeConfig.SpeedReduction, (int)freezeConfig.Duration);
                        string actorPattern = target is Enemy ? "enemy" : "player";
                        results.Add($"     {{{{actorPattern}}|" + $"{target.Name}" + "}} is " + $"{{{{frozen|frozen}}}}!");
                        effectsApplied = true;
                    }
                    else if (target is Enemy targetEnemy)
                    {
                        // For enemies, we can't easily apply slow without modifying the base class
                        // Just add a message for now
                        results.Add($"     {target.Name} is {{frozen|frozen}}!");
                        effectsApplied = true;
                    }
                }
            }
            
            // Check stun chance
            double stunChance = attacker.GetModificationStunChance();
            if (stunChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < stunChance)
                {
                    var tempAction = new Action
                    {
                        CausesStun = true
                    };
                    if (_effectRegistry.ApplyEffect("stun", target, tempAction, results))
                    {
                        effectsApplied = true;
                    }
                }
            }
            
            return effectsApplied;
        }

        /// <summary>
        /// Gets all effect types that an action can cause
        /// </summary>
        private static List<string> GetEffectTypesFromAction(Action action)
        {
            var effects = new List<string>();
            
            // Basic status effects
            if (action.CausesBleed) effects.Add("bleed");
            if (action.CausesWeaken) effects.Add("weaken");
            if (action.CausesSlow) effects.Add("slow");
            if (action.CausesPoison) effects.Add("poison");
            if (action.CausesStun) effects.Add("stun");
            if (action.CausesBurn) effects.Add("burn");
            
            // Advanced status effects (Phase 2)
            if (action.CausesVulnerability) effects.Add("vulnerability");
            if (action.CausesHarden) effects.Add("harden");
            if (action.CausesFortify) effects.Add("fortify");
            if (action.CausesFocus) effects.Add("focus");
            if (action.CausesExpose) effects.Add("expose");
            if (action.CausesHPRegen) effects.Add("hpregen");
            if (action.CausesArmorBreak) effects.Add("armorbreak");
            if (action.CausesPierce) effects.Add("pierce");
            if (action.CausesReflect) effects.Add("reflect");
            if (action.CausesSilence) effects.Add("silence");
            if (action.CausesStatDrain) effects.Add("statdrain");
            if (action.CausesAbsorb) effects.Add("absorb");
            if (action.CausesTemporaryHP) effects.Add("temporaryhp");
            if (action.CausesConfusion) effects.Add("confusion");
            if (action.CausesCleanse) effects.Add("cleanse");
            if (action.CausesMark) effects.Add("mark");
            if (action.CausesDisrupt) effects.Add("disrupt");
            
            return effects;
        }

        /// <summary>
        /// Processes all active status effects for an Actor at the start of their turn
        /// </summary>
        /// <param name="Actor">The Actor to process effects for</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>Total damage dealt by effects</returns>
        public static int ProcessStatusEffects(Actor Actor, List<string> results)
        {
            int totalEffectDamage = 0;
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            
            // Process poison damage
            if (Actor.PoisonStacks > 0)
            {
                int poisonDamage = Actor.ProcessPoison(currentTime);
                if (poisonDamage > 0)
                {
                    totalEffectDamage += poisonDamage;
                    string damageType = Actor.GetDamageTypeText();
                    
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    var actorColor = EntityColorHelper.GetActorColor(Actor);
                    DamageFormatter.AddActorTakesDamage(builder, Actor.Name, actorColor, poisonDamage, damageType);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
                
                // Check if effect ended (regardless of whether damage was dealt)
                if (Actor.PoisonStacks > 0)
                {
                    string damageType = Actor.GetDamageTypeText();
                    
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    ColorPalette effectColor = damageType == "bleed" ? ColorPalette.Error : ColorPalette.Green;
                    DamageFormatter.AddEffectStacksRemain(builder, damageType, effectColor, Actor.PoisonStacks);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
                else
                {
                    string damageType = Actor.GetDamageTypeText();
                    string effectEndMessage = damageType == "bleed" ? "bleeding" : "poisoned";
                    
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    var actorColor = EntityColorHelper.GetActorColor(Actor);
                    ColorPalette effectColor = damageType == "bleed" ? ColorPalette.Error : ColorPalette.Green;
                    DamageFormatter.AddActorNoLongerAffected(builder, Actor.Name, actorColor, effectEndMessage, effectColor);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
            }
            
            // Process burn damage
            if (Actor.BurnStacks > 0)
            {
                int burnDamage = Actor.ProcessBurn(currentTime);
                if (burnDamage > 0)
                {
                    totalEffectDamage += burnDamage;
                    
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    var actorColor = EntityColorHelper.GetActorColor(Actor);
                    DamageFormatter.AddActorTakesDamage(builder, Actor.Name, actorColor, burnDamage, "burn");
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
                
                // Check if effect ended (regardless of whether damage was dealt)
                if (Actor.BurnStacks > 0)
                {
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    DamageFormatter.AddEffectStacksRemain(builder, "burn", ColorPalette.Orange, Actor.BurnStacks);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
                else
                {
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    var actorColor = EntityColorHelper.GetActorColor(Actor);
                    DamageFormatter.AddActorNoLongerAffected(builder, Actor.Name, actorColor, "burning", ColorPalette.Orange);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
            }
            
            return totalEffectDamage;
        }

        /// <summary>
        /// Checks if an Actor can act based on status effects
        /// </summary>
        /// <param name="Actor">The Actor to check</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if the Actor can act, false if stunned or otherwise incapacitated</returns>
        public static bool CanEntityAct(Actor Actor, List<string> results)
        {
            // Check for stun effect
            if (Actor.IsStunned)
            {
                // Use markup syntax with stunned template for proper colorization
                results.Add($"[{Actor.Name}] is {{stunned|stunned}} and cannot act!");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Applies environmental debuffs to entities using the effect registry
        /// </summary>
        /// <param name="source">The source of the debuff (usually environment)</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action causing the debuff</param>
        /// <param name="debuffType">Type of debuff to apply</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if debuff was applied</returns>
        public static bool ApplyEnvironmentalDebuff(Actor source, Actor target, Action action, string debuffType, List<string> results)
        {
            // Use the effect registry to apply environmental effects
            bool applied = _effectRegistry.ApplyEffect(debuffType, target, action, results);
            
            if (applied)
            {
                // Modify the result message to indicate environmental source
                if (results.Count > 0)
                {
                    string lastResult = results[results.Count - 1];
                    results[results.Count - 1] = lastResult.Replace($"[{target.Name}]", $"[{target.Name}]").Replace("!", " by the environment!");
                }
            }
            
            return applied;
        }

        /// <summary>
        /// Checks and applies bleed chance for critical hits
        /// </summary>
        /// <param name="attacker">The attacking character</param>
        /// <param name="target">The target Actor</param>
        /// <param name="results">List to add effect messages to</param>
        public static void CheckAndApplyBleedChance(Character attacker, Actor target, List<string> results)
        {
            // For now, use a simple bleed chance calculation
            // This would need to be implemented based on equipment/modifications
            double bleedChance = 0.1; // 10% base bleed chance
            
            if (bleedChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < bleedChance)
                {
                    target.IsBleeding = true;
                    results.Add($"[{target.Name}] starts bleeding from the wound!");
                }
            }
        }

        /// <summary>
        /// Gets the effect type description for an action using the registry
        /// </summary>
        /// <param name="action">The action to describe</param>
        /// <returns>Description of the effect type</returns>
        public static string GetEffectType(Action action)
        {
            return _effectRegistry.GetEffectType(action);
        }

        /// <summary>
        /// Calculates effect duration based on attacker's stats
        /// </summary>
        /// <param name="baseDuration">Base duration of the effect</param>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="target">The target Actor</param>
        /// <returns>Modified duration</returns>
        public static int CalculateEffectDuration(int baseDuration, Actor attacker, Actor target)
        {
            // Intelligence increases effect duration
            int intelligenceBonus = 0;
            if (attacker is Character attackerCharacter)
            {
                intelligenceBonus = attackerCharacter.GetEffectiveIntelligence() / 10; // +1 turn per 10 intelligence
            }
            
            // Target's intelligence provides resistance
            int resistance = 0;
            if (target is Character targetCharacter)
            {
                resistance = targetCharacter.GetEffectiveIntelligence() / 15; // -1 turn per 15 intelligence
            }
            
            int finalDuration = baseDuration + intelligenceBonus - resistance;
            return Math.Max(1, finalDuration); // Minimum 1 turn
        }

        /// <summary>
        /// Clears all temporary effects from an Actor
        /// </summary>
        /// <param name="Actor">The Actor to clear effects from</param>
        /// <param name="results">List to add effect messages to</param>
        public static void ClearAllTemporaryEffects(Actor Actor, List<string> results)
        {
            bool hadEffects = Actor.PoisonStacks > 0 || Actor.BurnStacks > 0 || Actor.IsBleeding || 
                             Actor.IsStunned || Actor.IsWeakened;
            
            if (hadEffects)
            {
                Actor.ClearAllTempEffects();
                results.Add($"[{Actor.Name}]'s temporary effects are cleared!");
            }
        }
    }
}


