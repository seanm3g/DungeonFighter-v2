using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Applies learned skill-tree passives/rules/masteries via CombatEventBus.
    /// Fight-scoped state resets when a battle narrative starts.
    /// </summary>
    public sealed partial class SkillEffectRouter
    {
        public static SkillEffectRouter Instance { get; } = new();

        private Character? _boundCharacter;
        private bool _subscribed;

        private int _painMemoryStored;
        private bool _ironDisciplineUsed;
        private bool _ripostePending;
        private bool _riposteArmorAbsorbed;
        private int? _lastSpeedBand;
        private bool _commandersOpeningUsed;
        private bool _flowStateArmed;
        private int _distinctConnectStreak;
        private string? _lastConnectedActionName;
        private Actor? _shadowMarkTarget;
        private int _loadedCharges;
        private int _theorems;
        private int _spellMemoryStacks;
        private string? _spellMemoryTag;
        private int _technicalDebt;
        private HashSet<string> _debtTags = new(StringComparer.OrdinalIgnoreCase);
        private bool _unbrokenLineHoldAvailable = true;
        private int _dungeonAnalysisBand = -1;
        private Dictionary<int, int> _roomRollBands = new();
        private bool _grandEquationUsed;
        private bool _rewriteFateArmed;
        private int _rewriteFateRoll;
        private bool _perfectCrimePending;
        private int _mobileBulwarkMoves;
        private int _ironHideArmor;

        private SkillEffectRouter() { }

        public void RefreshForCharacter(Character? character)
        {
            _boundCharacter = character;
            EnsureSubscribed();
        }

        public void ResetFightState()
        {
            _painMemoryStored = 0;
            _ironDisciplineUsed = false;
            _ripostePending = false;
            _riposteArmorAbsorbed = false;
            _lastSpeedBand = null;
            _commandersOpeningUsed = false;
            _flowStateArmed = false;
            _distinctConnectStreak = 0;
            _lastConnectedActionName = null;
            _shadowMarkTarget = null;
            _loadedCharges = 0;
            _theorems = 0;
            _spellMemoryStacks = 0;
            _spellMemoryTag = null;
            _technicalDebt = 0;
            _debtTags.Clear();
            _unbrokenLineHoldAvailable = true;
            _grandEquationUsed = false;
            _rewriteFateArmed = false;
            _rewriteFateRoll = 0;
            _perfectCrimePending = false;
            _mobileBulwarkMoves = 0;
            _ironHideArmor = 0;
            ResetHybridFightState();
        }

        public void OnRoomCleared(Character hero)
        {
            if (!Has(hero, "dungeon_analysis")) return;
            if (_roomRollBands.Count == 0) return;
            _dungeonAnalysisBand = _roomRollBands.OrderByDescending(kv => kv.Value).First().Key;
            _roomRollBands.Clear();
        }

        private void EnsureSubscribed()
        {
            var bus = CombatEventBus.Instance;
            // New games clear the bus (StartNewGameCommand / MainMenuHandler); subscribing only once left every on-hit
            // skill dead for the rest of the session. Re-subscribe whenever our handler is gone.
            if (_subscribed && bus.HasSubscriber(CombatEventType.ActionHit, OnHit)) return;
            bus.Unsubscribe(CombatEventType.ActionMiss, OnMiss);
            bus.Unsubscribe(CombatEventType.ActionHit, OnHit);
            bus.Unsubscribe(CombatEventType.ActionCritical, OnHit);
            bus.Unsubscribe(CombatEventType.EnemyDied, OnKill);
            bus.Unsubscribe(CombatEventType.ComboEnded, OnComboEnded);
            bus.Unsubscribe(CombatEventType.ActionExecuted, OnActionExecuted);
            bus.Subscribe(CombatEventType.ActionMiss, OnMiss);
            bus.Subscribe(CombatEventType.ActionHit, OnHit);
            bus.Subscribe(CombatEventType.ActionCritical, OnHit);
            bus.Subscribe(CombatEventType.EnemyDied, OnKill);
            bus.Subscribe(CombatEventType.ComboEnded, OnComboEnded);
            bus.Subscribe(CombatEventType.ActionExecuted, OnActionExecuted);
            _subscribed = true;
        }

        /// <summary>Rank scaled by node power (rounded, at least 1 once learned): what rank-based magnitudes use.</summary>
        private int GetRank(Character? c, string customEffectId) => SkillRanks.GetScaledRank(c, customEffectId);

        private bool Has(Character? c, string customEffectId) => GetRank(c, customEffectId) > 0;

        /// <summary>Rank x the node's <see cref="SkillTreeNodeDefinition.Power"/>: what per-rank magnitudes multiply by.</summary>
        private double GetPower(Character? c, string customEffectId) => SkillRanks.GetPower(c, customEffectId);

        /// <summary>Flat armor from Bone Temper while Harden is active.</summary>
        public int GetSkillArmorBonus(Character? c)
        {
            if (c == null) return 0;
            int armor = ReferenceEquals(c, _boundCharacter) ? _ironHideArmor : 0;
            int rank = GetRank(c, "bone_temper");
            if (rank > 0 && (c.HardenStacks ?? 0) > 0)
                armor += rank;
            return armor;
        }

        /// <summary>
        /// Standing attribute from rite-of-passage passives.
        /// Puberty / Commission / Initiation / Apprenticeship: a flat +10 to STR / AGI / TEC / INT
        /// (times node power, per rank).
        /// Folded into <see cref="Character.GetEffectiveStrength"/> and the other effective getters.
        /// </summary>
        public int GetSkillAttributeBonus(Character? c, string? statCode)
        {
            if (c == null || string.IsNullOrWhiteSpace(statCode)) return 0;
            string code = statCode.Trim().ToUpperInvariant();
            // Rite of Passage (+10), Kinship (+1 per class piece), Gut Instinct, Empty Fury, streak and slot stats are
            // rule nodes now (SkillTrees.json crossWire: WHILE -> STR 10, WHILE -> STR 1 PER WORN barbarian, ...).
            return code is "STR" or "AGI" or "TEC" or "INT"
                ? (int)Math.Round(CrossWires.Passive(c, code))
                : 0;
        }

        private Character? AsBoundHero(Actor? source)
        {
            if (_boundCharacter != null && ReferenceEquals(source, _boundCharacter))
                return _boundCharacter;
            if (source is Character c && c is not Enemy)
                return c;
            return null;
        }

        private static void ScheduleRetrigger(Actor source, string mechanicId, string? arg = null)
        {
            RetriggerScheduler.TrySchedule(mechanicId, arg, null, new Action { Name = "skill_retrigger" }, source, new List<string>());
        }

        private static void MarkTarget(Actor target, int turns = 3)
        {
            target.IsMarked = true;
            target.MarkTurns = Math.Max(target.MarkTurns, turns);
        }

        private static void ClearMark(Actor target)
        {
            target.IsMarked = false;
            target.MarkTurns = 0;
        }

        private void OnMiss(CombatEvent evt)
        {
            var hero = AsBoundHero(evt.Source);
            if (hero == null) return;

            ApplyDiceRollPassives(hero, evt);
            ApplyDaggerCritMissSalvage(hero, evt);

            if (Has(hero, "iron_discipline") && !_ironDisciplineUsed)
            {
                _ironDisciplineUsed = true;
                hero.Effects.ComboModeActive = true;
                hero.FocusStacks = 0;
                hero.FocusTurns = 0;
            }

            if (Has(hero, "ghost_step"))
                CombatTriggerContext.AddMissSalvageCharges(hero, GetRank(hero, "ghost_step"));

            if (Has(hero, "unbroken_line") && _unbrokenLineHoldAvailable)
            {
                _unbrokenLineHoldAvailable = false;
                hero.Effects.ComboModeActive = true;
                hero.Effects.SetTempRollBonus(Math.Max(hero.Effects.GetTempRollBonus(), 2 * GetRank(hero, "unbroken_line")), 1);
            }
        }

        private void OnHit(CombatEvent evt)
        {
            var hero = AsBoundHero(evt.Source);
            if (hero == null) return;
            using var skillPin = SkillRanks.Pin(hero.Progression);

            TrackConnectStreak(hero, evt);
            ApplyDiceRollPassives(hero, evt);
            ApplyClassTreeOnHit(hero, evt);
            ApplyHybridOnHit(hero, evt);
            ApplyShadowcraft(hero, evt);
            // Venom Mace, Envenomed Edge, Hemorrhage: GameData/Conversions.json (phase early).
            ConversionTable.Apply(hero, evt.Target, evt.IsCritical, "early");
            ApplyMaceMastery(hero, evt);
            ApplyRiposteConsume(hero, evt);
            ApplyCommandersOpening(hero, evt);
            ApplyCadenceKeeper(hero, evt);
            ApplySwordMastery(hero, evt);
            ApplySnakeEyes(hero, evt);
            ApplyPrimeTheory(hero, evt);
            ApplySpellMemory(hero, evt);
            ApplyTechnicalDebt(hero, evt);
            // Kindle, Thermal Shock: GameData/Conversions.json (phase late).
            ConversionTable.Apply(hero, evt.Target, evt.IsCritical, "late");
            ApplyDungeonAnalysisAdvantage(hero, evt);
            ApplyGrandEquation(hero, evt);
            ApplyLivingAlloy(hero, evt);
            ApplyPainMemoryConsume(hero, evt);
            ApplyToxicLedger(hero, evt);
            ApplyFlowState(hero, evt);
            ApplyRewriteFateSpend(hero, evt);
            ApplyPerfectCrimeTransfer(hero, evt);
            ApplyMobileBulwark(hero, evt);
            ApplyQuickFingers(hero, evt);
            ApplyFieldBrief(hero, evt);
            ApplyBackChannel(hero, evt);
            ApplyDataDrivenRankMods(hero, evt);

            if (evt.NaturalRollValue > 0)
            {
                int band = evt.NaturalRollValue <= 7 ? 0 : evt.NaturalRollValue <= 14 ? 1 : 2;
                _roomRollBands[band] = _roomRollBands.GetValueOrDefault(band) + 1;
            }
        }

        private void OnKill(CombatEvent evt)
        {
            var hero = AsBoundHero(evt.Source);
            if (hero == null) return;

            if (Has(hero, "no_witnesses") && _shadowMarkTarget != null &&
                (evt.Target == null || ReferenceEquals(evt.Target, _shadowMarkTarget)))
            {
                CombatTriggerContext.AddMissSalvageCharges(hero, GetRank(hero, "no_witnesses"));
                ScheduleRetrigger(hero, "retrigger_opener");
            }

            if (Has(hero, "perfect_crime") && evt.Target != null)
                _perfectCrimePending = true;
        }

        private void OnComboEnded(CombatEvent evt)
        {
            var hero = AsBoundHero(evt.Source);
            if (hero == null) return;

            if (Has(hero, "second_wind"))
            {
                int rank = GetRank(hero, "second_wind");
                int heal = Math.Max(1, (int)(hero.MaxHealth * 0.08 * GetPower(hero, "second_wind")));
                hero.Heal(heal);
                int focus = hero.FocusStacks ?? 0;
                if (focus > 0)
                {
                    hero.FortifyStacks = (hero.FortifyStacks ?? 0) + focus * rank;
                    hero.FortifyTurns = Math.Max(hero.FortifyTurns, 3);
                    hero.FocusStacks = 0;
                }
            }

            _mobileBulwarkMoves = 0;
        }

        private void OnActionExecuted(CombatEvent evt)
        {
            if (evt.Target is Character heroDef && heroDef is not Enemy &&
                (_boundCharacter == null || ReferenceEquals(heroDef, _boundCharacter)))
            {
                if (Has(heroDef, "riposte"))
                    _ripostePending = true;
            }
        }

        public void NotifyHeroTookDamage(Character hero, int postArmorDamage, bool armorAbsorbedAny)
        {
            if (hero is Enemy) return;
            if (_boundCharacter == null || !ReferenceEquals(hero, _boundCharacter))
                _boundCharacter = hero;

            if (Has(hero, "pain_memory") && postArmorDamage > 0)
            {
                int rank = GetRank(hero, "pain_memory");
                int add = Math.Max(0, (int)(postArmorDamage * 0.25 * rank));
                int cap = Math.Max(1, (int)(hero.MaxHealth * 0.20 * rank));
                _painMemoryStored = Math.Min(cap, _painMemoryStored + add);
            }

            if (Has(hero, "riposte"))
            {
                _ripostePending = true;
                _riposteArmorAbsorbed = armorAbsorbedAny;
            }

            if (Has(hero, "shield_geometry") && postArmorDamage <= 0 && armorAbsorbedAny)
                hero.Effects.ComboStep = 0;

            if (Has(hero, "guard_duty") && armorAbsorbedAny)
            {
                int rank = GetRank(hero, "guard_duty");
                hero.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
                {
                    new() { Type = "DAMAGE_MOD", Value = 5 * rank }
                });
            }

        }

        private void TrackConnectStreak(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "flow_state")) return;
            string name = evt.Action?.Name ?? "";
            if (!string.Equals(name, _lastConnectedActionName, StringComparison.OrdinalIgnoreCase))
            {
                _distinctConnectStreak++;
                _lastConnectedActionName = name;
            }
            if (_distinctConnectStreak >= 3)
            {
                _flowStateArmed = true;
                _distinctConnectStreak = 0;
                hero.Effects.RerollCharges = Math.Max(hero.Effects.RerollCharges, 1);
            }
        }

        private void ApplyFlowState(Character hero, CombatEvent evt)
        {
            if (!_flowStateArmed || !Has(hero, "flow_state")) return;
            _flowStateArmed = false;
            CombatTriggerContext.SetCritFaceMin(hero, 20);
        }

        private void ApplyShadowcraft(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "shadowcraft") || evt.Target == null) return;
            if (_shadowMarkTarget == null)
            {
                _shadowMarkTarget = evt.Target;
                MarkTarget(evt.Target);
            }
            else if (!ReferenceEquals(_shadowMarkTarget, evt.Target))
            {
                ClearMark(_shadowMarkTarget);
                _shadowMarkTarget = evt.Target;
                MarkTarget(evt.Target);
            }
        }

        private void ApplyPerfectCrimeTransfer(Character hero, CombatEvent evt)
        {
            if (!_perfectCrimePending || !Has(hero, "perfect_crime") || evt.Target == null) return;
            _perfectCrimePending = false;
            MarkTarget(evt.Target, 2);
            if (evt.Target.BleedIntensity <= 0)
                evt.Target.QueueBleedFromHit(2);
        }

        /// <summary>Strip slots filled beyond the first (Empty Fury, Concentrated Chaos reward long strips).</summary>
        private static int CountExtraFilledActionSlots(Character hero) =>
            Math.Max(0, (hero.GetComboActions()?.Count ?? 0) - 1);

        private void AddDamageMod(Character hero, double value)
        {
            if (value <= 0) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "DAMAGE_MOD", Value = value }
            });
        }

        private void AddSpeedMod(Character hero, double value)
        {
            if (value <= 0) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "SPEED_MOD", Value = value }
            });
        }

        private void AddAmpMod(Character hero, double value)
        {
            if (value <= 0) return;
            hero.Effects.ConsumedSkillAmpPercent += value;
        }

        /// <summary>
        /// Barbarian natural-roll passive: Defense = natural roll x power until the hero next acts. (Warrior speed, Rogue
        /// damage and Wizard AMP from the roll are rule nodes: ONATTACK -> NEXT SPEED 1% PER ROLL.)
        /// </summary>
        private void ApplyDiceRollPassives(Character hero, CombatEvent evt)
        {
            int roll = evt.NaturalRollValue;
            _ironHideArmor = 0;
            if (roll <= 0) return;

            double armor = GetPower(hero, "iron_hide_roll");
            if (armor > 0)
                _ironHideArmor = (int)Math.Round(roll * armor);
        }

        /// <summary>Dagger crit miss: bank a miss-salvage charge ("worst faces get one salvage").</summary>
        private void ApplyDaggerCritMissSalvage(Character hero, CombatEvent evt)
        {
            if (!evt.IsCriticalMiss || !Has(hero, "dagger_critmiss_reroll")) return;
            if (hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != WeaponType.Dagger) return;
            CombatTriggerContext.AddMissSalvageCharges(hero, 1);
        }

        private void ApplyMaceMastery(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "mace_mastery") || evt.Target == null) return;
            if (!ExposeStatus.IsArmorStripped(evt.Target)) return; // "armor broken" = Expose tier 2 (Armor Break folded in)
            bool bludgeon = evt.Action?.Tags?.Any(t =>
                string.Equals(t, "bludgeon", StringComparison.OrdinalIgnoreCase)) == true;
            if (!bludgeon) return;
            int rank = GetRank(hero, "mace_mastery");
            hero.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
            {
                new() { Type = "DAMAGE_MOD", Value = 25 * rank }
            });
        }

        private void ApplyRiposteConsume(Character hero, CombatEvent evt)
        {
            if (!_ripostePending || !Has(hero, "riposte")) return;
            _ripostePending = false;
            int rank = GetRank(hero, "riposte");
            hero.PierceTurns = Math.Max(hero.PierceTurns, rank);
            if (_riposteArmorAbsorbed)
                hero.Effects.ConsumedMultiHitMod += rank;
            _riposteArmorAbsorbed = false;
        }

        private void ApplyCommandersOpening(Character hero, CombatEvent evt)
        {
            if (_commandersOpeningUsed || !Has(hero, "commanders_opening")) return;
            _commandersOpeningUsed = true;
            var tm = RollModificationManager.GetThresholdManager();
            if (evt.IsCritical)
                tm.AdjustCriticalHitThreshold(hero, 2);
            else
                tm.AdjustComboThreshold(hero, 2);
        }

        private void ApplyCadenceKeeper(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "cadence_keeper") || evt.Action == null) return;
            double len = evt.Action.Length <= 0 ? 1.0 : evt.Action.Length;
            int band = len < 0.95 ? -1 : len > 1.05 ? 1 : 0;
            if (_lastSpeedBand != null && _lastSpeedBand.Value != band)
            {
                // rank x power Focus per alternation; the fraction is a chance (it was at least 1 every time).
                double focus = GetPower(hero, "cadence_keeper");
                int stacks = (int)Math.Floor(focus) + (RandomUtility.NextDouble() < focus - Math.Floor(focus) ? 1 : 0);
                if (stacks <= 0) { _lastSpeedBand = band; return; }
                hero.FocusStacks = (hero.FocusStacks ?? 0) + stacks;
                hero.FocusTurns = Math.Max(hero.FocusTurns, 2);
            }
            else if (_lastSpeedBand != null && _lastSpeedBand.Value == band && (hero.FocusStacks ?? 0) > 0)
            {
                hero.FocusStacks = (hero.FocusStacks ?? 0) - 1;
            }
            _lastSpeedBand = band;
        }

        private void ApplySwordMastery(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "sword_mastery") || evt.Target == null) return;
            int maxStep = Math.Max(0, hero.GetComboActions().Count - 1);
            if ((evt.Target.ExposeStacks ?? 0) > 0)
                hero.Effects.ComboStep = Math.Min(hero.Effects.ComboStep + 1, maxStep);
            else if (evt.Target.IsMarked)
                hero.Effects.ComboStep = Math.Max(0, hero.Effects.ComboStep - 1);
        }

        private void ApplyMobileBulwark(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "mobile_bulwark")) return;
            _mobileBulwarkMoves++;
            if (_mobileBulwarkMoves >= 2)
            {
                _mobileBulwarkMoves = 0;
                hero.FortifyStacks = (hero.FortifyStacks ?? 0) + GetRank(hero, "mobile_bulwark");
                hero.FortifyTurns = Math.Max(hero.FortifyTurns, 2);
            }
        }

        private void ApplySnakeEyes(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "snake_eyes")) return;
            int face = evt.NaturalRollValue;
            if (face == 2 || face == 12)
            {
                _loadedCharges++;
                if (_loadedCharges >= 2)
                {
                    _loadedCharges = 0;
                    CombatTriggerContext.SetPendingReplaceRollFace(hero, 20);
                }
            }
        }

        private void ApplyPrimeTheory(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "prime_theory")) return;
            int face = evt.NaturalRollValue;
            if (face is 2 or 3 or 5 or 7 or 11 or 13 or 17 or 19)
            {
                _theorems++;
                if (_theorems >= 3)
                {
                    _theorems = 0;
                    CombatTriggerContext.SetCritFaceMin(hero, 20);
                }
            }
        }

        private void ApplySpellMemory(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "spell_memory") || evt.Action?.Tags == null || evt.Action.Tags.Count == 0)
                return;
            string tag = evt.Action.Tags[0];
            if (string.Equals(tag, _spellMemoryTag, StringComparison.OrdinalIgnoreCase))
            {
                _spellMemoryStacks++;
                AddAmpMod(hero, 12 * GetPower(hero, "spell_memory"));
            }
            else
            {
                if (_spellMemoryStacks > 0)
                    AddAmpMod(hero, 12 * _spellMemoryStacks * GetPower(hero, "spell_memory"));
                _spellMemoryStacks = 0;
                _spellMemoryTag = tag;
            }
        }

        private void ApplyTechnicalDebt(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "technical_debt") || evt.Action?.Tags == null) return;
            foreach (var tag in evt.Action.Tags)
            {
                if (_debtTags.Add(tag))
                    _technicalDebt++;
                else if (_technicalDebt > 0 && evt.Target != null)
                {
                    ExposeStatus.Apply(evt.Target, _technicalDebt, 2);
                    _technicalDebt = 0;
                    _debtTags.Clear();
                }
            }
        }

        private void ApplyDungeonAnalysisAdvantage(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "dungeon_analysis") || _dungeonAnalysisBand < 0) return;
            int band = evt.NaturalRollValue <= 7 ? 0 : evt.NaturalRollValue <= 14 ? 1 : 2;
            if (band != _dungeonAnalysisBand)
            {
                hero.Effects.SetTempRollBonus(Math.Max(hero.Effects.GetTempRollBonus(), 3), 1);
                _dungeonAnalysisBand = -1;
            }
        }

        private void ApplyGrandEquation(Character hero, CombatEvent evt)
        {
            if (_grandEquationUsed || !Has(hero, "grand_equation")) return;
            int slot = hero.Effects.ComboStep + 1;
            if (evt.NaturalRollValue + slot == 21)
            {
                _grandEquationUsed = true;
                ScheduleRetrigger(hero, "retrigger_opener");
            }
        }

        private void ApplyLivingAlloy(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "living_alloy") || !evt.IsCritical) return;
            if ((hero.HardenStacks ?? 0) > 0)
            {
                hero.HardenStacks = (hero.HardenStacks ?? 0) - 1;
                ScheduleRetrigger(hero, "retrigger_slot", (hero.Effects.ComboStep + 1).ToString());
            }
            else if ((hero.FortifyStacks ?? 0) > 0)
            {
                hero.FortifyStacks = (hero.FortifyStacks ?? 0) - 1;
                ScheduleRetrigger(hero, "retrigger_slot", (hero.Effects.ComboStep + 1).ToString());
            }
        }

        private void ApplyPainMemoryConsume(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "pain_memory") || _painMemoryStored <= 0 || evt.Action == null) return;
            bool finisher = evt.Action.Tags?.Any(t =>
                string.Equals(t, "finisher", StringComparison.OrdinalIgnoreCase)) == true
                || evt.Action.ComboRouting?.IsFinisher == true;
            if (!finisher) return;
            hero.Effects.ExtraDamage += _painMemoryStored;
            _painMemoryStored = 0;
        }

        private void ApplyToxicLedger(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "toxic_ledger") || evt.Target == null || evt.Action == null) return;
            bool finisher = evt.Action.Tags?.Any(t =>
                string.Equals(t, "finisher", StringComparison.OrdinalIgnoreCase)) == true
                || evt.Action.ComboRouting?.IsFinisher == true;
            if (!finisher) return;
            int kinds = 0;
            if (evt.Target.PoisonPercentOfMaxHealth > 0) kinds++;
            if (evt.Target.BurnIntensity > 0) kinds++;
            if (evt.Target.BleedIntensity > 0) kinds++;
            if (evt.Target.AcidIntensity > 0) kinds++;
            // The hit event fires before damage is rolled, so the bonus rides on this hit as a damage mod.
            if (kinds > 0)
                AddDamageMod(hero, 10.0 * kinds * GetPower(hero, "toxic_ledger"));
        }

        private void ApplyDataDrivenRankMods(Character hero, CombatEvent evt)
        {
            if (hero.Progression == null) return;
            WeaponType? path = hero.Progression.GetPrimaryClassWeaponType();
            if (path == null) return;
            if (hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != path.Value)
                return;

            int dmgMod = 0;
            int heal = 0;
            foreach (var kv in hero.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0) continue;
                var node = SkillTreeService.Trees.GetNode(kv.Key);
                if (node == null) continue;
                // Only apply pack mods from the equipped primary path tree.
                var owner = SkillTreeService.Trees.GetTreeForWeapon(path.Value);
                if (owner == null || !owner.Nodes.Any(n =>
                        string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase)))
                    continue;
                int rank = kv.Value + CrossWires.SkillRankBonus(hero, kv.Key);
                if (node.DamageModPerRank > 0)
                    dmgMod += node.DamageModPerRank * rank;
                if (node.HealOnHitPerRank > 0)
                    heal += node.HealOnHitPerRank * rank;
            }

            if (dmgMod > 0)
            {
                hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
                {
                    new() { Type = "DAMAGE_MOD", Value = dmgMod }
                });
            }
            if (heal > 0)
                hero.Heal(heal);
        }

        private void ApplyQuickFingers(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "quick_fingers");
            if (rank <= 0 || evt.Target == null) return;
            int heroAgi = hero.GetEffectiveAgility();
            int foeAgi = evt.Target is Character foe ? foe.GetEffectiveAgility() : 0;
            if (heroAgi <= foeAgi) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "DAMAGE_MOD", Value = rank }
            });
        }

        private void ApplyFieldBrief(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "field_brief");
            if (rank <= 0 || !evt.IsCritical) return;
            hero.FortifyStacks = (hero.FortifyStacks ?? 0) + rank;
            hero.FortifyTurns = Math.Max(hero.FortifyTurns, 2);
        }

        private void ApplyBackChannel(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "back_channel");
            if (rank <= 0 || evt.Target == null || !evt.Target.IsMarked) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "DAMAGE_MOD", Value = 3 * rank }
            });
        }

        private void ApplyRewriteFateSpend(Character hero, CombatEvent evt)
        {
            if (!_rewriteFateArmed) return;
            _rewriteFateArmed = false;
            CombatTriggerContext.SetPendingReplaceRollFace(hero, _rewriteFateRoll);
        }

        public void ArmRewriteFate(Character hero, int? forcedFace = null)
        {
            int spent = Math.Max(1, _theorems);
            _theorems = 0;
            _rewriteFateRoll = forcedFace ?? Math.Min(20, 5 + 5 * spent);
            _rewriteFateArmed = true;
            RefreshForCharacter(hero);
        }

        public int GetPainMemoryStored() => _painMemoryStored;
        public int GetTheorems() => _theorems;
        public int GetLoadedCharges() => _loadedCharges;
    }
}
