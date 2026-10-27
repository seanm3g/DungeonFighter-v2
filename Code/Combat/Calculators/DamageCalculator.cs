using System;
using System.Threading;
using System.Diagnostics;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Sequence;
using RPGGame.Data;
using RPGGame.Diagnostics;

namespace RPGGame.Combat.Calculators
{
    /// <summary>
    /// Handles damage calculations including raw damage, final damage, and damage reduction.
    /// Damage is not cached: inputs (stats, armor, tags, effects) change independently of object identity.
    /// </summary>
    public static class DamageCalculator
    {
        [ThreadStatic]
        private static CombatSequenceDamageTrace? _sequenceTrace;

        /// <summary>Start capturing formula pieces for the live sequence HUD (one swing, this thread).</summary>
        public static void BeginSequenceTrace() => _sequenceTrace = new CombatSequenceDamageTrace();

        /// <summary>Take the captured formula, if any, and stop recording.</summary>
        public static CombatSequenceDamageTrace? TakeSequenceTrace()
        {
            var trace = _sequenceTrace;
            _sequenceTrace = null;
            return trace;
        }
        /// <summary>
        /// Legacy hook for callers that invalidate when equipment or stats change; no-op (caching removed).
        /// </summary>
        public static void InvalidateCache(Actor actor)
        {
        }

        /// <summary>
        /// Legacy hook for tests; no-op (caching removed).
        /// </summary>
        public static void ClearAllCaches()
        {
        }

        /// <summary>
        /// Clears on game start (legacy); no-op (caching removed).
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Legacy cache statistics; always zero (caching removed).
        /// </summary>
        public static (int rawHits, int rawMisses, int finalHits, int finalMisses, double rawHitRate, double finalHitRate) GetCacheStats()
        {
            return (0, 0, 0, 0, 0.0, 0.0);
        }

        /// <summary>
        /// Calculates raw damage before armor reduction
        /// </summary>
        private sealed class MultiHitSwing
        {
            public double Fraction;
        }

        private static readonly AsyncLocal<MultiHitSwing?> _multiHitSwing = new();

        /// <summary>True while a multi-hit swing is resolving its ticks (see <see cref="BeginMultiHitSwing"/>).</summary>
        public static bool InMultiHitSwing => _multiHitSwing.Value != null;

        /// <summary>
        /// Starts a multi-hit swing: each tick keeps its fractional damage for the next tick and skips the per-hit
        /// minimum/raw floors, so N hits of X% sum to about N×X% of one hit. The caller floors the swing total once
        /// (<see cref="SwingMinimumDamage"/>).
        /// </summary>
        public static IDisposable BeginMultiHitSwing()
        {
            var previous = _multiHitSwing.Value;
            _multiHitSwing.Value = new MultiHitSwing();
            return new SwingScope(previous);
        }

        /// <summary>Floor applied once to a whole multi-hit swing's damage.</summary>
        public static int SwingMinimumDamage => Math.Max(1, GameConfiguration.Instance.Combat.MinimumDamage);

        private sealed class SwingScope : IDisposable
        {
            private readonly MultiHitSwing? _previous;
            public SwingScope(MultiHitSwing? previous) => _previous = previous;
            public void Dispose() => _multiHitSwing.Value = _previous;
        }

        /// <summary>
        /// Swing + combo-slot multiplier as shown in combat roll info: matches the AMP_MOD leg inside
        /// <see cref="CalculateRawDamage"/> (queued sheet amp on the attacker) without mutating other effect state.
        /// For combo actions, sheet AMP_MOD compounds against the technique baseline when the slot multiplier is 1.0
        /// (e.g. opener exponent 0), so the log matches the AMP stat line + queued percent.
        /// </summary>
        public static double GetDisplayedComboMultiplier(Actor attacker, double swingComboSlotMultiplier, Action? action = null)
        {
            double m = swingComboSlotMultiplier;
            if (attacker is Character character && character.Effects.ConsumedAmpModPercent != 0)
            {
                double baseline = swingComboSlotMultiplier;
                if (action?.IsComboAction == true)
                    baseline = Math.Max(baseline, character.GetComboAmplifier());
                m = baseline * (1.0 + character.Effects.ConsumedAmpModPercent / 100.0);
            }
            if (attacker is Character skillAmpCharacter && skillAmpCharacter.Effects.ConsumedSkillAmpPercent != 0)
                m *= 1.0 + skillAmpCharacter.Effects.ConsumedSkillAmpPercent / 100.0;
            return m;
        }

        /// <param name="roll">Total attack roll (modified d20 + roll bonuses) used for combo/normal damage bands.</param>
        /// <param name="rollBonusForCritEval">Same roll bonus as combat resolution; crit damage uses
        /// <see cref="CombatCalculator.GetCritThresholdEvaluationRoll"/> so accuracy does not inflate crits.</param>
        public static int CalculateRawDamage(Actor attacker, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int roll = 0, int rollBonusForCritEval = 0)
        {
            // Get base damage from attacker
            int baseDamage = 0;
            // Enemy must be checked before Character (Enemy : Character). Otherwise direct-stat lab enemies
            // and attribute-based enemy damage paths are skipped.
            if (attacker is Enemy enemy)
            {
                baseDamage = enemy.Identity?.ReferenceAttributeDamage ?? enemy.GetAttributeDamageBonus();

                if (enemy.Weapon is WeaponItem weapon)
                    baseDamage += weapon.GetTotalDamage();

                if (enemy.Effects.ConsumedWeaponDamageFlat != 0)
                    baseDamage += (int)Math.Round(enemy.Effects.ConsumedWeaponDamageFlat);
            }
            else if (attacker is Character character)
            {
                baseDamage = character.GetAttributeDamageBonus();

                // Add weapon damage if attacker has a weapon
                if (character.Weapon is WeaponItem weapon)
                {
                    baseDamage += weapon.GetTotalDamage();
                }

                // Flat WEAPON_DAMAGE from cadence (HERO BASE → WEAPON DAMAGE)
                if (character.Effects.ConsumedWeaponDamageFlat != 0)
                    baseDamage += (int)Math.Round(character.Effects.ConsumedWeaponDamageFlat);

                // Add equipment damage bonus
                baseDamage += character.GetEquipmentDamageBonus();

                // Add modification damage bonus
                baseDamage += character.GetModificationDamageBonus();
            }

            // Ensure base damage is at least 1 to prevent zero damage issues
            // This can happen if stats/equipment are not properly initialized
            if (baseDamage <= 0)
            {
                baseDamage = 1;
            }

            // Apply action damage multiplier if action is provided
            double actionMultiplier = action?.DamageMultiplier ?? 1.0;

            if (attacker is Character earlyGameCharacter)
            {
                double startingActionMult = EarlyGameBalanceHelper.GetStartingActionDamageMultiplier(earlyGameCharacter, action);
                actionMultiplier *= startingActionMult;
            }

            // Apply consumed DAMAGE_MOD from ACTION/ABILITY keyword (next action/ability only)
            if (attacker is Character damageModCharacter && damageModCharacter.Effects.ConsumedDamageModPercent != 0)
                actionMultiplier *= (1.0 + damageModCharacter.Effects.ConsumedDamageModPercent / 100.0);

            // Rogue DEFENSE Counter: one-shot DAMAGE_MOD % from last take-hit
            if (attacker is Character counterHero && counterHero is not Enemy)
            {
                double counterPct = counterHero.Effects.ConsumePendingDefenseCounterDamagePct();
                if (counterPct != 0)
                    actionMultiplier *= (1.0 + counterPct / 100.0);
            }

            // Apply consumed AMP_MOD from ACTION/ABILITY keyword (next action/ability only; % bonus, multiply).
            // Combo: sheet amp applies on top of technique baseline when slot mult is 1.0 (opener tier), matching HUD + combat log.
            if (attacker is Character ampModCharacter && ampModCharacter.Effects.ConsumedAmpModPercent != 0)
            {
                double baseline = comboAmplifier;
                if (action?.IsComboAction == true)
                    baseline = Math.Max(baseline, ampModCharacter.GetComboAmplifier());
                comboAmplifier = baseline * (1.0 + ampModCharacter.Effects.ConsumedAmpModPercent / 100.0);
            }

            // Skill-tree AMP (First Glyph, Pure Prefer): a plain % on the current multiplier.
            if (attacker is Character skillAmpHero && skillAmpHero.Effects.ConsumedSkillAmpPercent != 0)
                comboAmplifier *= 1.0 + skillAmpHero.Effects.ConsumedSkillAmpPercent / 100.0;

            // INT potency (combat.intelligenceAmpPerPoint): INT scales the hero's AMP.
            if (attacker is Character intHero && intHero is not Enemy)
                comboAmplifier *= intHero.GetIntelligenceAmpMultiplier();

            // Apply next attack damage multiplier (for Follow Through and similar effects)
            // Consume it so it only applies once
            double nextAttackMultiplier = 1.0;
            if (attacker is Character characterAttacker)
            {
                nextAttackMultiplier = characterAttacker.Effects.ConsumeNextTurnDamageMultiplier();
            }

            // Calculate total damage before armor
            double totalDamage = (baseDamage * actionMultiplier * comboAmplifier * damageMultiplier * nextAttackMultiplier);
            if (attacker is Character strengthHero)
            {
                double strMult = strengthHero.GetStrengthDamageMultiplier();
                totalDamage *= strMult;
                if (_sequenceTrace != null)
                    _sequenceTrace.StrengthMultiplier = strMult;
                totalDamage *= SpeedCalculator.GetSpeedOverflowDamageMultiplier(strengthHero);
                // Retrigger swings re-resolve late, high-AMP strip slots; combat.retriggerDamageMultiplier trims them.
                if (strengthHero is not Enemy && RPGGame.Actions.Execution.ActionExecutionFlow.IsResolvingRetrigger)
                    totalDamage *= GameConfiguration.Instance.Combat.GetRetriggerDamageMultiplier();
            }

            // Opt-in enemy damage curve by level (enemySystem.damageCurve).
            if (attacker is Enemy curvedEnemy)
            {
                totalDamage *= GameConfiguration.Instance.EnemySystem?.DamageCurve?.MultiplierAt(curvedEnemy.Level) ?? 1.0;
                totalDamage *= curvedEnemy.Identity is { } identity
                    ? identity.HitDamage * identity.Calibration
                    : curvedEnemy.GetArchetypeDamageMultiplier();
                totalDamage *= curvedEnemy.EncounterDamageMultiplier;
            }

            // Get combat configuration
            var combatConfig = GameConfiguration.Instance.Combat;
            var combatBalance = GameConfiguration.Instance.CombatBalance;

            // Apply roll-based damage scaling
            bool appliedCritDamage = false;
            if (roll > 0)
            {
                // Get threshold from threshold manager if available, otherwise use config
                int criticalThreshold = combatConfig.CriticalHitThreshold;
                if (attacker != null)
                {
                    criticalThreshold = RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(attacker);
                }

                // Match ActionExecutionFlow / MultiHitProcessor: crit uses crit-eval roll (excludes full roll bonus), not attack total.
                int critEvalRoll = attacker != null
                    ? CombatCalculator.GetCritThresholdEvaluationRoll(roll, rollBonusForCritEval, attacker.RollPenalty)
                    : roll;

                // Natural 20+ on the swing total still forces crit damage when bonuses push the die (legacy safety).
                if (critEvalRoll >= criticalThreshold || roll >= 20)
                {
                    appliedCritDamage = true;
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        if (!ActionExecutor.DisableCombatDebugOutput)
                        {
                        }
                    }
                    totalDamage *= combatBalance.CriticalHitDamageMultiplier;
                    if (_sequenceTrace != null)
                    {
                        _sequenceTrace.RollBand = "crit";
                        _sequenceTrace.RollBandMultiplier = combatBalance.CriticalHitDamageMultiplier;
                    }
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        if (!ActionExecutor.DisableCombatDebugOutput)
                        {
                        }
                    }
                }
                else
                {
                    // Enhanced damage scaling based on roll using RollSystem configuration
                    // Only apply when not a critical hit (crit-eval below threshold and total roll below 20)
                    var rollSystem = GameConfiguration.Instance.RollSystem;
                    var rollMultipliers = combatBalance.RollDamageMultipliers;
                    if (roll >= rollSystem.ComboThreshold.Min)
                    {
                        // Safeguard: if multiplier is 0 or invalid, default to 1.0 to prevent zeroing damage
                        double comboMultiplier = rollMultipliers.ComboRollDamageMultiplier;
                        if (comboMultiplier <= 0)
                        {
                            comboMultiplier = 1.0;
                        }
                        totalDamage *= comboMultiplier;
                        if (_sequenceTrace != null)
                        {
                            _sequenceTrace.RollBand = "action roll";
                            _sequenceTrace.RollBandMultiplier = comboMultiplier;
                        }
                    }
                    else if (roll >= rollSystem.BasicAttackThreshold.Min) // Normal attack range (6-13)
                    {
                        // Safeguard: if multiplier is 0 or invalid, default to 1.0 to prevent zeroing damage
                        double basicMultiplier = rollMultipliers.BasicRollDamageMultiplier;
                        if (basicMultiplier <= 0)
                        {
                            basicMultiplier = 1.0;
                        }
                        totalDamage *= basicMultiplier;
                        if (_sequenceTrace != null)
                        {
                            _sequenceTrace.RollBand = "normal hit";
                            _sequenceTrace.RollBandMultiplier = basicMultiplier;
                        }
                    }
                }
            }

            // Apply class damage multiplier for hero attackers
            if (attacker is Character classChar && classChar.Weapon is WeaponItem classWeapon)
            {
                double classMult = ClassBalanceHelper.GetDamageMultiplier(classWeapon.WeaponType);
                totalDamage *= classMult;
                if (_sequenceTrace != null)
                    _sequenceTrace.ClassMultiplier = classMult;
            }

            // Mark of Class: amplify primary class-tagged actions
            if (attacker is Character tagHero && tagHero is not Enemy && action != null)
            {
                double classTagMult = CharmBonusController.GetClassTagDamageMultiplier(tagHero);
                if (classTagMult > 1.0 && ActionHasHeroPrimaryClassTag(tagHero, action))
                    totalDamage *= classTagMult;
            }

            if (_sequenceTrace != null)
                _sequenceTrace.PreRoundTotal = totalDamage;

            var swing = _multiHitSwing.Value;
            int wholeDamage;
            if (swing != null)
            {
                double exact = totalDamage + swing.Fraction;
                wholeDamage = (int)Math.Floor(exact);
                swing.Fraction = exact - wholeDamage;
            }
            else
            {
                wholeDamage = (int)totalDamage;
            }

            int result = wholeDamage;

            if (attacker is Character convertHero && convertHero is not Enemy)
                result += MaterialSetController.GetConvertDamageBonus(convertHero, action, wholeDamage);

            int convertFlat = result - wholeDamage;

            int maxCap = Math.Max(1, combatConfig.MaximumDamageCap);
            if (result > maxCap)
                result = maxCap;

            // Ensure raw damage is never 0 (safeguard against calculation errors); multi-hit ticks may be 0 and are floored per swing.
            if (result <= 0 && swing == null)
            {
                result = 1;
            }

            if (_sequenceTrace != null)
            {
                _sequenceTrace.BaseDamage = baseDamage;
                _sequenceTrace.ActionMultiplier = actionMultiplier;
                _sequenceTrace.Amp = comboAmplifier;
                _sequenceTrace.ConvertFlat = convertFlat;
                _sequenceTrace.Raw = result;
                _sequenceTrace.CritDamage = appliedCritDamage;
            }

            return result;
        }

        /// <summary>
        /// Calculates damage dealt by an attacker to a target
        /// </summary>
        public static int CalculateDamage(Actor attacker, Actor target, Action? action = null, double comboAmplifier = 1.0, double damageMultiplier = 1.0, int rollBonus = 0, int roll = 0, bool showWeakenedMessage = true, int? defenseFace = null, int? attackFace = null, int mitigationHitIndex = 0)
        {
            var sw = CombatHotPathMetrics.IsEnabled ? Stopwatch.StartNew() : null;

            // Calculate raw damage before armor
            int totalDamage = CalculateRawDamage(attacker, action, comboAmplifier, damageMultiplier, roll, rollBonus);
            double outgoing = StatusCombatModifiers.OutgoingDamageMultiplier(attacker)
                * AnimalSetController.GetOutgoingDamageMultiplier(attacker);
            if (outgoing != 1.0)
                totalDamage = (int)Math.Round(totalDamage * outgoing);

            // Apply tag-based damage modification
            if (action != null)
            {
                double tagModifier = Combat.TagDamageCalculator.GetDamageModifier(action, target);
                totalDamage = (int)(totalDamage * tagModifier);
            }

            if (_sequenceTrace != null)
                _sequenceTrace.BeforeArmor = totalDamage;

            bool pierce = IgnoresArmor(target, action);
            bool inSwing = InMultiHitSwing;
            int minimumDamage = inSwing ? 0 : Math.Max(1, GameConfiguration.Instance.Combat.MinimumDamage);
            int finalDamage;
            int reducedAmount;

            if (target is Character defender)
            {
                var mit = ClassDefenseCalculator.ApplyIncoming(defender, totalDamage, pierce, mitigationHitIndex);
                finalDamage = mit.Remaining;
                reducedAmount = mit.ReducedAmount;
                finalDamage = ApplyIncomingStatus(target, finalDamage);
                if (finalDamage > 0 && finalDamage < minimumDamage)
                    finalDamage = minimumDamage;
            }
            else
            {
                int targetArmor = DamageCalculator.ResolveTargetArmor(target, action);
                finalDamage = Math.Max(minimumDamage, totalDamage - targetArmor);
                reducedAmount = Math.Max(0, totalDamage - finalDamage);
                finalDamage = ApplyIncomingStatus(target, finalDamage);
                if (finalDamage <= 0 && !inSwing)
                    finalDamage = 1;
            }

            if (_sequenceTrace != null)
            {
                _sequenceTrace.Block = reducedAmount;
                _sequenceTrace.Final = finalDamage;
            }

            if (sw != null)
            {
                sw.Stop();
                CombatHotPathMetrics.RecordDamageCalculator(sw.Elapsed);
            }

            return finalDamage;
        }

        /// <summary>
        /// Calculates damage reduction from armor and other sources
        /// </summary>
        public static int ApplyDamageReduction(Actor target, int damage, Action? action = null, int? defenseFace = null, int? attackFace = null, int mitigationHitIndex = 0)
        {
            bool pierce = IgnoresArmor(target, action);
            int remaining;
            if (target is Character defender)
            {
                remaining = ClassDefenseCalculator.ApplyIncoming(defender, damage, pierce, mitigationHitIndex).Remaining;
            }
            else
            {
                int armorReduction = ResolveTargetArmor(target, action);
                remaining = damage - armorReduction;
            }

            double damageReductionMultiplier = 1.0;
            if (target.DamageReduction > 0)
            {
                damageReductionMultiplier = 1.0 - (target.DamageReduction / 100.0);
            }

            int finalDamage = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, (int)(remaining * damageReductionMultiplier));
            if (target is Character remainingHero && remaining <= 0)
                return 0;
            return finalDamage;
        }

        private static int ApplyIncomingStatus(Actor target, int damage)
        {
            double incoming = StatusCombatModifiers.IncomingDamageMultiplier(target)
                * AnimalSetController.GetIncomingDamageMultiplier(target);
            return incoming == 1.0 ? damage : (int)Math.Round(damage * incoming);
        }

        /// <summary>
        /// True when this swing ignores Defense DR (action pierce, or the target is pierced).
        /// </summary>
        public static bool IgnoresArmor(Actor? target, Action? action = null)
        {
            if (action?.CausesPierce == true)
                return true;
            return target != null && target.HasPierce && target.PierceTurns > 0;
        }

        /// <summary>
        /// Base Defense rating for the target (hero gear+/effects, enemy Armor minus combat shred).
        /// Returns 0 when <paramref name="action"/> has pierce or the target is under pierce.
        /// </summary>
        public static int ResolveTargetArmor(Actor target, Action? action = null)
        {
            if (target == null)
                return 0;
            if (IgnoresArmor(target, action))
                return 0;
            if (target is Character character)
                return Math.Max(0, character.GetMaxArmor());
            return 0;
        }

        /// <summary>True when the action carries the hero's highest class-point tag (barbarian/warrior/rogue/wizard).</summary>
        private static bool ActionHasHeroPrimaryClassTag(Character hero, Action action)
        {
            if (action?.Tags == null || action.Tags.Count == 0)
                return false;
            string primary = ResolvePrimaryClassTag(hero);
            if (primary.Length == 0)
                return false;
            return GameDataTagHelper.HasTag(action.Tags, primary);
        }

        private static string ResolvePrimaryClassTag(Character hero)
        {
            int barb = hero.BarbarianPoints;
            int war = hero.WarriorPoints;
            int rog = hero.RoguePoints;
            int wiz = hero.WizardPoints;
            int max = Math.Max(Math.Max(barb, war), Math.Max(rog, wiz));
            if (max <= 0)
                return "";
            if (barb == max) return "barbarian";
            if (war == max) return "warrior";
            if (rog == max) return "rogue";
            if (wiz == max) return "wizard";
            return "";
        }
    }
}
