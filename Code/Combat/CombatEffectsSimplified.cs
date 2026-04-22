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

        /// <summary>Poison, burn, and bleed from weapon stats or weapon <c>StatusEffects</c> apply only on a critical hit.</summary>
        private static bool ShouldApplyEquipmentDoTFromHit(CombatEvent? combatEvent) =>
            combatEvent != null && combatEvent.IsCritical;

        /// <summary>
        /// When an action has no <see cref="Action.Triggers"/> conditions, poison/burn/bleed from the action itself
        /// also require a critical hit. If the sheet defines trigger conditions, those gate the whole bundle as before.
        /// </summary>
        private static bool ShouldApplyUnconditionalActionDoTFromHit(Action action, CombatEvent? combatEvent)
        {
            var conditions = action.Triggers?.TriggerConditions;
            if (conditions != null && conditions.Count > 0)
                return true;
            return combatEvent?.IsCritical ?? false;
        }

        private static bool IsDoTStatusEffectType(string effectType) =>
            string.Equals(effectType, "poison", StringComparison.OrdinalIgnoreCase)
            || string.Equals(effectType, "burn", StringComparison.OrdinalIgnoreCase)
            || string.Equals(effectType, "bleed", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Applies status effects from an action to the target
        /// </summary>
        /// <param name="action">The action being performed</param>
        /// <param name="attacker">The attacking Actor</param>
        /// <param name="target">The target Actor</param>
        /// <param name="results">List to add effect messages to</param>
        /// <param name="combatEvent">Optional combat event for conditional status application</param>
        /// <returns>True if any effects were applied</returns>
        public static bool ApplyStatusEffects(Action action, Actor attacker, Actor target, List<string> results, CombatEvent? combatEvent = null)
        {
            bool effectsApplied = false;
            
            // First, apply guaranteed status effects from modifications (for characters only, Attack actions only)
            // Then, check for weapon modification status effect chances (for characters only)
            if (attacker is Character characterAttacker)
            {
                effectsApplied |= ApplyModificationStatusEffects(characterAttacker, target, action, results, combatEvent);
                effectsApplied |= ApplyWeaponModificationStatusEffects(characterAttacker, target, action, results, combatEvent);
            }
            
            // Then apply status effects from the action itself
            var effectTypes = StatusEffectActionResolver.GetEffectTypesFromAction(action);
            
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
                    if (IsDoTStatusEffectType(effectType) && !ShouldApplyUnconditionalActionDoTFromHit(action, combatEvent))
                        continue;
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
        private static bool ApplyModificationStatusEffects(Character attacker, Actor target, Action action, List<string> results, CombatEvent? combatEvent)
        {
            if (action.Type != ActionType.Attack) return false;
            
            bool effectsApplied = false;
            var statusEffects = attacker.GetModificationStatusEffects();
            
            foreach (var effectName in statusEffects)
            {
                if (IsDoTStatusEffectType(effectName) && !ShouldApplyEquipmentDoTFromHit(combatEvent))
                    continue;
                var tempAction = StatusEffectActionResolver.CreateActionWithStatusEffect(effectName);
                if (tempAction != null && _effectRegistry.ApplyEffect(effectName.ToLower(), target, tempAction, results))
                {
                    effectsApplied = true;
                }
            }
            
            return effectsApplied;
        }

        /// <summary>
        /// Applies status effects from weapon modifications based on chance
        /// </summary>
        /// <param name="attacker">The attacking character</param>
        /// <param name="target">The target Actor</param>
        /// <param name="action">The action being performed (used for effect handlers)</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>True if any effects were applied</returns>
        private static bool ApplyWeaponModificationStatusEffects(Character attacker, Actor target, Action action, List<string> results, CombatEvent? combatEvent)
        {
            bool effectsApplied = false;
            bool applyWeaponDots = ShouldApplyEquipmentDoTFromHit(combatEvent);

            double poisonPct = attacker.GetWeaponPoisonPercentPerHit();
            if (applyWeaponDots && poisonPct > 0)
            {
                var tempPoison = new Action { CausesPoison = true, PoisonPercentToAdd = poisonPct };
                if (_effectRegistry.ApplyEffect("poison", target, tempPoison, results))
                    effectsApplied = true;
            }

            int burnAmt = attacker.GetWeaponBurnPerHit();
            if (applyWeaponDots && burnAmt > 0)
            {
                var tempBurn = new Action { CausesBurn = true, BurnAmountToAdd = burnAmt };
                if (_effectRegistry.ApplyEffect("burn", target, tempBurn, results))
                    effectsApplied = true;
            }

            int bleedAmt = attacker.GetWeaponBleedPerHit();
            if (applyWeaponDots && bleedAmt > 0)
            {
                var tempBleed = new Action { CausesBleed = true, BleedAmountToAdd = bleedAmt };
                if (_effectRegistry.ApplyEffect("bleed", target, tempBleed, results))
                    effectsApplied = true;
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
        /// Poison + burn damage from a single status tick (same <see cref="Actor"/> <c>TakeDamage</c> call).
        /// </summary>
        public readonly record struct StatusEffectDamageBreakdown(int PoisonDamage, int BurnDamage)
        {
            public int TotalDamage => PoisonDamage + BurnDamage;
        }

        /// <summary>
        /// Processes poison then burn for an actor (global tick). Use <see cref="ProcessStatusEffectsWithBreakdown"/> when the UI needs per-type HP delta coloring.
        /// </summary>
        public static int ProcessStatusEffects(Actor actor, List<string> results) =>
            ProcessStatusEffectsWithBreakdown(actor, results).TotalDamage;

        /// <summary>
        /// Processes all active status effects for an Actor at the start of their turn, reporting poison vs burn amounts separately.
        /// </summary>
        public static StatusEffectDamageBreakdown ProcessStatusEffectsWithBreakdown(Actor actor, List<string> results)
        {
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            int poison = ProcessPoisonPercentDamage(actor, currentTime, results);
            int burn = ProcessBurnDamage(actor, currentTime, results);
            return new StatusEffectDamageBreakdown(poison, burn);
        }

        /// <summary>Bleed pulse when the afflicted actor has completed a turn (including stunned skip).</summary>
        public static int ProcessBleedAfterActorTurn(Actor actor, List<string> results)
        {
            if (actor.BleedIntensity <= 0 && actor.PendingBleedFromHits <= 0)
                return 0;
            int damage = actor.ProcessBleedOnAction();
            if (damage > 0)
            {
                var builder = new ColoredTextBuilder();
                DamageFormatter.AddActorTakesDamage(builder, actor.Name, EntityColorHelper.GetActorColor(actor), damage, "bleed");
                results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
            }
            if (actor.BleedIntensity > 0 || actor.PendingBleedFromHits > 0)
            {
                var builder = new ColoredTextBuilder();
                DamageFormatter.AddEffectStacksRemain(builder, "bleed", ColorPalette.Error, actor.BleedIntensity + actor.PendingBleedFromHits);
                results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
            }
            else
            {
                var builder = new ColoredTextBuilder();
                DamageFormatter.AddActorNoLongerAffected(builder, actor.Name, EntityColorHelper.GetActorColor(actor), "bleeding", ColorPalette.Error);
                results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
            }
            return damage > 0 ? damage : 0;
        }

        private static int ProcessPoisonPercentDamage(Actor actor, double currentTime, List<string> results)
        {
            if (actor.PoisonPercentOfMaxHealth <= 0)
                return 0;
            int damage = actor.ProcessPoison(currentTime);
            if (damage > 0)
            {
                var builder = new ColoredTextBuilder();
                DamageFormatter.AddActorTakesDamage(builder, actor.Name, EntityColorHelper.GetActorColor(actor), damage, "poison");
                results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
            }
            if (actor.PoisonPercentOfMaxHealth > 0)
            {
                var builder = new ColoredTextBuilder();
                DamageFormatter.AddPoisonPercentRemain(builder, ColorPalette.Green, actor.PoisonPercentOfMaxHealth);
                results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
            }
            else
            {
                var builder = new ColoredTextBuilder();
                DamageFormatter.AddActorNoLongerAffected(builder, actor.Name, EntityColorHelper.GetActorColor(actor), "poisoned", ColorPalette.Green);
                results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
            }
            return damage > 0 ? damage : 0;
        }

        private static int ProcessBurnDamage(Actor actor, double currentTime, List<string> results)
        {
            if (actor.BurnIntensity <= 0 && actor.PendingBurnFromHits <= 0)
                return 0;
            int damage = actor.ProcessBurn(currentTime);
            if (damage > 0)
            {
                var builder = new ColoredTextBuilder();
                DamageFormatter.AddActorTakesDamage(builder, actor.Name, EntityColorHelper.GetActorColor(actor), damage, "burn");
                results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
            }
            int displayIntensity = actor.BurnIntensity + actor.PendingBurnFromHits;
            if (displayIntensity > 0)
            {
                var builder = new ColoredTextBuilder();
                DamageFormatter.AddEffectStacksRemain(builder, "burn", ColorPalette.Orange, displayIntensity);
                results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
            }
            else
            {
                var builder = new ColoredTextBuilder();
                DamageFormatter.AddActorNoLongerAffected(builder, actor.Name, EntityColorHelper.GetActorColor(actor), "burning", ColorPalette.Orange);
                results.Add(ColoredTextRenderer.RenderAsMarkup(builder.Build()));
            }
            return damage > 0 ? damage : 0;
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
                    target.QueueBleedFromHit(1);
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
            bool hadEffects = Actor.PoisonPercentOfMaxHealth > 0 || Actor.BurnIntensity > 0 || Actor.PendingBurnFromHits > 0
                             || Actor.BleedIntensity > 0 || Actor.PendingBleedFromHits > 0
                             || Actor.IsStunned || Actor.IsWeakened;
            
            if (hadEffects)
            {
                Actor.ClearAllTempEffects();
                results.Add($"[{Actor.Name}]'s temporary effects are cleared!");
            }
        }
    }
}


