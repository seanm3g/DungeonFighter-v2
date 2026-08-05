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
    public sealed class SkillEffectRouter
    {
        public static SkillEffectRouter Instance { get; } = new();

        private Character? _boundCharacter;
        private bool _subscribed;

        private bool _bronzeSkinTriggered;
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
        private int _attackStreak;
        private int _nonMissStreak;
        private int _slotIntApplied;

        private static readonly string[] BarbarianMaterialTags =
            { "bone", "steel", "damascus", "barbarian" };
        private static readonly string[] WarriorMaterialTags =
            { "bronze", "gold", "mithril", "warrior" };
        private static readonly string[] RogueMaterialTags =
            { "glass", "obsidian", "shadow", "rogue" };
        private static readonly string[] WizardMaterialTags =
            { "willow", "silver", "crystal", "wizard" };
        private static readonly string[] ScrapPreferQualities =
            { "Battle Scarred", "New", "Masterwork" };
        private static readonly string[] ParadeDressQualities =
            { "Worn", "Like New", "Heirloom" };
        private static readonly string[] FragilePreferQualities =
            { "Broken", "Preowned", "Perfect" };
        private static readonly string[] PurePreferQualities =
            { "Second Hand", "Cosmic" };

        private SkillEffectRouter() { }

        public void RefreshForCharacter(Character? character)
        {
            _boundCharacter = character;
            EnsureSubscribed();
        }

        public void ResetFightState()
        {
            _bronzeSkinTriggered = false;
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
            _attackStreak = 0;
            _nonMissStreak = 0;
            _slotIntApplied = 0;
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
            if (_subscribed) return;
            var bus = CombatEventBus.Instance;
            bus.Subscribe(CombatEventType.ActionMiss, OnMiss);
            bus.Subscribe(CombatEventType.ActionHit, OnHit);
            bus.Subscribe(CombatEventType.ActionCritical, OnHit);
            bus.Subscribe(CombatEventType.EnemyDied, OnKill);
            bus.Subscribe(CombatEventType.ComboEnded, OnComboEnded);
            bus.Subscribe(CombatEventType.ActionExecuted, OnActionExecuted);
            _subscribed = true;
        }

        private int GetRank(Character? c, string customEffectId)
        {
            if (c?.Progression == null || string.IsNullOrWhiteSpace(customEffectId)) return 0;
            c.Progression.EnsureSkillTreeRootsGranted();
            int best = 0;
            foreach (var kv in c.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0) continue;
                var node = SkillTreeService.Trees.GetNode(kv.Key);
                if (node != null &&
                    string.Equals(node.CustomEffectId, customEffectId, StringComparison.OrdinalIgnoreCase))
                    best = Math.Max(best, kv.Value);
            }
            return best;
        }

        private bool Has(Character? c, string customEffectId) => GetRank(c, customEffectId) > 0;

        /// <summary>Flat armor from Bone Temper while Harden is active.</summary>
        public int GetSkillArmorBonus(Character? c)
        {
            int rank = GetRank(c, "bone_temper");
            if (rank <= 0 || c == null) return 0;
            return (c.HardenStacks ?? 0) > 0 ? rank : 0;
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

            ResetNonMissStreak(hero);

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

            TrackConnectStreak(hero, evt);
            ApplyShadowcraft(hero, evt);
            ApplyVenomMace(hero, evt);
            ApplyEnvenomedEdge(hero, evt);
            ApplyHemorrhage(hero, evt);
            ApplyBronzeAges(hero, evt);
            ApplyFirstFormation(hero, evt);
            ApplyFirstCut(hero, evt);
            ApplyFirstGlyph(hero, evt);
            ApplyEmptyFury(hero, evt);
            ApplyConsecutiveAgi(hero, evt);
            ApplyConsecutiveTec(hero, evt);
            ApplySlotInt(hero, evt);
            ApplySwordHitAgi(hero, evt);
            ApplyWandComboInt(hero, evt);
            ApplyScrapPrefer(hero, evt);
            ApplyParadeDress(hero, evt);
            ApplyFragilePrefer(hero, evt);
            ApplyPurePrefer(hero, evt);
            ApplyGutInstinct(hero, evt);
            ApplyMaceMastery(hero, evt);
            ApplyRiposteConsume(hero, evt);
            ApplyCommandersOpening(hero, evt);
            ApplyCadenceKeeper(hero, evt);
            ApplySwordMastery(hero, evt);
            ApplySnakeEyes(hero, evt);
            ApplyPrimeTheory(hero, evt);
            ApplySpellMemory(hero, evt);
            ApplyTechnicalDebt(hero, evt);
            ApplyKindle(hero, evt);
            ApplyThermalShock(hero, evt);
            ApplyDungeonAnalysisAdvantage(hero, evt);
            ApplyGrandEquation(hero, evt);
            ApplyLivingAlloy(hero, evt);
            ApplyPainMemoryConsume(hero, evt);
            ApplyToxicLedger(hero, evt);
            ApplyFlowState(hero, evt);
            ApplyRewriteFateSpend(hero, evt);
            ApplyPerfectCrimeTransfer(hero, evt);
            ApplyMobileBulwark(hero, evt);
            ApplyBloodPrice(hero, evt);
            ApplySteadyMarch(hero, evt);
            ApplyDeepCut(hero, evt);
            ApplyQuickFingers(hero, evt);
            ApplyManaBleed(hero, evt);
            ApplyFocusLens(hero, evt);
            ApplyBronzeKnuckle(hero, evt);
            ApplyPhalanx(hero, evt);
            ApplyMetronome(hero, evt);
            ApplyFieldBrief(hero, evt);
            ApplyBackChannel(hero, evt);
            ApplyToxinPressure(hero, evt);
            ApplyGreasePalm(hero, evt);
            ApplySigilBurn(hero, evt);
            ApplySoftEcho(hero, evt);
            ApplyLoadedOdds(hero, evt);
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
                int heal = Math.Max(1, (int)(hero.MaxHealth * 0.08 * rank));
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

            if (Has(hero, "bronze_skin") && !_bronzeSkinTriggered && postArmorDamage > 0)
            {
                _bronzeSkinTriggered = true;
                int rank = GetRank(hero, "bronze_skin");
                hero.HardenStacks = (hero.HardenStacks ?? 0) + 2 * rank;
                hero.HardenTurns = Math.Max(hero.HardenTurns, 3);
            }

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

            ResetAttackStreak(hero);
            ResetNonMissStreak(hero);
            ClearSlotIntBonus(hero);
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

        private void ApplyVenomMace(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "venom_mace") || evt.Target == null) return;
            if (evt.Target.PoisonPercentOfMaxHealth <= 0) return;
            evt.Target.ApplyPoisonPercent(-1.0);
            evt.Target.QueueAcidFromHit(1);
        }

        private void ApplyEnvenomedEdge(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "envenomed_edge") || evt.Target == null) return;
            if (evt.Target.PoisonPercentOfMaxHealth <= 0) return;
            int convert = evt.IsCritical ? 2 : 1;
            for (int i = 0; i < convert; i++)
            {
                if (evt.Target.PoisonPercentOfMaxHealth <= 0) break;
                evt.Target.ApplyPoisonPercent(-1.0);
                evt.Target.QueueBleedFromHit(2);
            }
        }

        private void ApplyHemorrhage(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "hemorrhage") || evt.Target == null) return;
            if (evt.Target.BleedIntensity <= 0) return;
            evt.Target.QueueBleedFromHit(Math.Max(1, evt.Target.BleedIntensity / 2));
            if (evt.IsCritical)
                evt.Target.ArmorBreakStacks = (evt.Target.ArmorBreakStacks ?? 0) + 1;
        }

        private void ApplyPerfectCrimeTransfer(Character hero, CombatEvent evt)
        {
            if (!_perfectCrimePending || !Has(hero, "perfect_crime") || evt.Target == null) return;
            _perfectCrimePending = false;
            MarkTarget(evt.Target, 2);
            if (evt.Target.BleedIntensity <= 0)
                evt.Target.QueueBleedFromHit(2);
        }

        private int CountBronzeItems(Character hero)
        {
            int count = 0;
            foreach (var item in EnumerateEquipped(hero))
            {
                if (item?.Tags == null) continue;
                if (item.Tags.Any(t => string.Equals(t, "bronze", StringComparison.OrdinalIgnoreCase)))
                    count++;
            }
            if (Has(hero, "living_alloy"))
            {
                count += hero.HardenStacks ?? 0;
                count += hero.FortifyStacks ?? 0;
            }
            return count;
        }

        private static IEnumerable<Item?> EnumerateEquipped(Character hero)
        {
            yield return hero.Equipment.Head;
            yield return hero.Equipment.Body;
            yield return hero.Equipment.Legs;
            yield return hero.Equipment.Feet;
            yield return hero.Equipment.Weapon;
        }

        private static int CountEquippedMatchingTags(Character hero, string[] tags)
        {
            int count = 0;
            foreach (var item in EnumerateEquipped(hero))
            {
                if (item == null) continue;
                if (!string.IsNullOrWhiteSpace(item.Material) &&
                    tags.Any(t => string.Equals(item.Material, t, StringComparison.OrdinalIgnoreCase)))
                {
                    count++;
                    continue;
                }
                if (item.Tags != null &&
                    item.Tags.Any(it => tags.Any(t => string.Equals(it, t, StringComparison.OrdinalIgnoreCase))))
                    count++;
            }
            return count;
        }

        private static int CountEquippedMatchingQualities(Character hero, string[] qualityNames)
        {
            int count = 0;
            foreach (var item in EnumerateEquipped(hero))
            {
                if (item?.Modifications == null) continue;
                if (item.Modifications.Any(m =>
                        string.Equals(m.PrefixCategory, "Quality", StringComparison.OrdinalIgnoreCase) &&
                        qualityNames.Any(q => string.Equals(m.Name, q, StringComparison.OrdinalIgnoreCase))))
                    count++;
            }
            return count;
        }

        private static int CountEmptyActionSlots(Character hero)
        {
            int max = ComboSequenceMaxHelper.GetEffectiveMax(hero);
            int filled = hero.GetComboActions()?.Count ?? 0;
            return Math.Max(0, max - filled);
        }

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
            hero.Effects.ConsumedAmpModPercent += value;
        }

        private void ApplyBronzeAges(Character hero, CombatEvent evt)
        {
            if (evt.Action == null) return;
            int slot = hero.Effects.ComboStep;

            // First Bronze Age: +8% damage per Barbarian-material item, per skill rank (no slot gate).
            int barbItems = CountEquippedMatchingTags(hero, BarbarianMaterialTags);
            if (barbItems > 0 && Has(hero, "first_bronze_age"))
            {
                int rank = GetRank(hero, "first_bronze_age");
                AddDamageMod(hero, 8.0 * barbItems * rank);
            }

            int bronze = CountBronzeItems(hero);
            if (bronze <= 0) return;

            if (Has(hero, "second_bronze_age") && slot == 1 && bronze >= 2)
            {
                // Flat stack: +1 full-power multihit per two Bronze items, times skill rank.
                hero.Effects.ConsumedMultiHitMod += (bronze / 2.0) * GetRank(hero, "second_bronze_age");
            }
            if (Has(hero, "third_bronze_age") && slot == 2)
            {
                int rank = GetRank(hero, "third_bronze_age");
                double speedPct = 6.0 * bronze * rank;
                double appliedSpeed = Math.Min(30.0, speedPct);
                AddSpeedMod(hero, appliedSpeed);
                if (speedPct > 30)
                {
                    int focus = (int)((speedPct - 30) / 6);
                    hero.FocusStacks = (hero.FocusStacks ?? 0) + Math.Max(1, focus);
                    hero.FocusTurns = Math.Max(hero.FocusTurns, 2);
                }
            }
        }

        private void ApplyFirstFormation(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "first_formation");
            if (rank <= 0 || evt.Action == null) return;
            int items = CountEquippedMatchingTags(hero, WarriorMaterialTags);
            if (items <= 0) return;
            AddSpeedMod(hero, 8.0 * items * rank);
        }

        private void ApplyFirstCut(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "first_cut");
            if (rank <= 0 || evt.Action == null) return;
            int items = CountEquippedMatchingTags(hero, RogueMaterialTags);
            if (items <= 0) return;
            AddDamageMod(hero, 8.0 * items * rank);
        }

        private void ApplyFirstGlyph(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "first_glyph");
            if (rank <= 0 || evt.Action == null) return;
            int items = CountEquippedMatchingTags(hero, WizardMaterialTags);
            if (items <= 0) return;
            AddAmpMod(hero, 8.0 * items * rank);
        }

        private void ApplyEmptyFury(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "empty_fury");
            if (rank <= 0) return;
            int empty = CountEmptyActionSlots(hero);
            if (empty <= 0) return;
            int bonus = 25 * empty * rank;
            hero.Stats.TempStrengthBonus = Math.Max(hero.Stats.TempStrengthBonus, bonus);
            hero.Stats.TempStatBonusTurns = Math.Max(hero.Stats.TempStatBonusTurns, 1);
        }

        private void ApplyConsecutiveAgi(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "consecutive_agi");
            if (rank <= 0) return;
            _attackStreak++;
            hero.Stats.TempAgilityBonus += 5 * rank;
            hero.Stats.TempStatBonusTurns = Math.Max(hero.Stats.TempStatBonusTurns, 1);
        }

        private void ApplyConsecutiveTec(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "consecutive_tec");
            if (rank <= 0) return;
            _nonMissStreak++;
            hero.Stats.TempTechniqueBonus += 5 * rank;
            hero.Stats.TempStatBonusTurns = Math.Max(hero.Stats.TempStatBonusTurns, 1);
        }

        private void ResetAttackStreak(Character hero)
        {
            if (_attackStreak <= 0) return;
            int rank = GetRank(hero, "consecutive_agi");
            if (rank > 0)
                hero.Stats.TempAgilityBonus = Math.Max(0, hero.Stats.TempAgilityBonus - 5 * rank * _attackStreak);
            _attackStreak = 0;
        }

        private void ResetNonMissStreak(Character hero)
        {
            if (_nonMissStreak <= 0) return;
            int rank = GetRank(hero, "consecutive_tec");
            if (rank > 0)
                hero.Stats.TempTechniqueBonus = Math.Max(0, hero.Stats.TempTechniqueBonus - 5 * rank * _nonMissStreak);
            _nonMissStreak = 0;
        }

        private void ApplySlotInt(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "slot_int");
            if (rank <= 0) return;
            ClearSlotIntBonus(hero);
            int slot = Math.Max(1, hero.Effects.ComboStep + 1);
            _slotIntApplied = 5 * slot * rank;
            hero.Stats.TempIntelligenceBonus += _slotIntApplied;
            hero.Stats.TempStatBonusTurns = Math.Max(hero.Stats.TempStatBonusTurns, 1);
        }

        private void ClearSlotIntBonus(Character hero)
        {
            if (_slotIntApplied <= 0) return;
            hero.Stats.TempIntelligenceBonus = Math.Max(0, hero.Stats.TempIntelligenceBonus - _slotIntApplied);
            _slotIntApplied = 0;
        }

        private void ApplySwordHitAgi(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "sword_hit_agi");
            if (rank <= 0) return;
            if (hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != WeaponType.Sword) return;
            hero.Stats.TempAgilityBonus += 5 * rank;
            hero.Stats.TempStatBonusTurns = Math.Max(hero.Stats.TempStatBonusTurns, 1);
        }

        private void ApplyWandComboInt(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "wand_combo_int");
            if (rank <= 0 || !evt.IsCombo) return;
            if (hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != WeaponType.Wand) return;
            hero.Stats.TempIntelligenceBonus += 5 * rank;
            hero.Stats.TempStatBonusTurns = Math.Max(hero.Stats.TempStatBonusTurns, 1);
        }

        private void ApplyScrapPrefer(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "scrap_prefer");
            if (rank <= 0 || evt.Action == null) return;
            int n = CountEquippedMatchingQualities(hero, ScrapPreferQualities);
            if (n <= 0) return;
            AddDamageMod(hero, 10.0 * n * rank);
        }

        private void ApplyParadeDress(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "parade_dress");
            if (rank <= 0 || evt.Action == null) return;
            int n = CountEquippedMatchingQualities(hero, ParadeDressQualities);
            if (n <= 0) return;
            AddDamageMod(hero, 10.0 * n * rank);
        }

        private void ApplyFragilePrefer(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "fragile_prefer");
            if (rank <= 0 || evt.Action == null) return;
            int n = CountEquippedMatchingQualities(hero, FragilePreferQualities);
            if (n <= 0) return;
            AddDamageMod(hero, 10.0 * n * rank);
        }

        private void ApplyPurePrefer(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "pure_prefer");
            if (rank <= 0 || evt.Action == null) return;
            int n = CountEquippedMatchingQualities(hero, PurePreferQualities);
            if (n <= 0) return;
            AddAmpMod(hero, 10.0 * n * rank);
        }

        private void ApplyGutInstinct(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "gut_instinct")) return;
            int rank = GetRank(hero, "gut_instinct");
            int bonus = (hero.GetEffectiveIntelligence() / 5) * rank;
            if (bonus > 0)
                hero.Stats.TempStrengthBonus += bonus;
        }

        private void ApplyMaceMastery(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "mace_mastery") || evt.Target == null) return;
            if ((evt.Target.ArmorBreakStacks ?? 0) <= 0) return;
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
                hero.FocusStacks = (hero.FocusStacks ?? 0) + GetRank(hero, "cadence_keeper");
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
                hero.Effects.ConsumedAmpModPercent += 12 * GetRank(hero, "spell_memory");
            }
            else
            {
                if (_spellMemoryStacks > 0)
                    hero.Effects.ConsumedAmpModPercent += 12 * _spellMemoryStacks * GetRank(hero, "spell_memory");
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
                    evt.Target.ExposeStacks = (evt.Target.ExposeStacks ?? 0) + _technicalDebt;
                    evt.Target.ExposeTurns = Math.Max(evt.Target.ExposeTurns, 2);
                    _technicalDebt = 0;
                    _debtTags.Clear();
                }
            }
        }

        private void ApplyKindle(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "kindle") || evt.Target == null) return;
            if (evt.Target.BurnIntensity <= 0) return;
            if (evt.Target.PoisonPercentOfMaxHealth <= 0) return;
            evt.Target.ApplyPoisonPercent(-1.0);
            evt.Target.QueueAcidFromHit(1);
            evt.Target.QueueBurnFromHit(1);
        }

        private void ApplyThermalShock(Character hero, CombatEvent evt)
        {
            if (!Has(hero, "thermal_shock") || evt.Target == null) return;
            bool burning = evt.Target.BurnIntensity > 0;
            bool slowed = evt.Target is Character c && c.Effects.SlowTurns > 0;
            if (burning && slowed)
            {
                evt.Target.IsStunned = true;
                evt.Target.StunTurnsRemaining = Math.Max(evt.Target.StunTurnsRemaining, 1);
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
            if (kinds > 0)
                hero.Effects.ExtraDamage += (int)(evt.Damage * 0.10 * kinds * GetRank(hero, "toxic_ledger"));
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
                if (node.DamageModPerRank > 0)
                    dmgMod += node.DamageModPerRank * kv.Value;
                if (node.HealOnHitPerRank > 0)
                    heal += node.HealOnHitPerRank * kv.Value;
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

        private void ApplyBloodPrice(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "blood_price");
            if (rank <= 0 || evt.Damage <= 0) return;
            bool mace = hero.Equipment.Weapon is WeaponItem w && w.WeaponType == WeaponType.Mace;
            if (!mace) return;
            hero.Heal(rank);
        }

        private void ApplySteadyMarch(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "steady_march");
            if (rank <= 0) return;
            bool sword = hero.Equipment.Weapon is WeaponItem w && w.WeaponType == WeaponType.Sword;
            if (!sword) return;
            hero.Stats.TempAgilityBonus += rank;
        }

        private void ApplyDeepCut(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "deep_cut");
            if (rank <= 0 || evt.Target == null) return;
            int stacks = evt.IsCritical ? rank * 2 : rank;
            evt.Target.QueueBleedFromHit(stacks);
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

        private void ApplyManaBleed(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "mana_bleed");
            if (rank <= 0 || evt.Damage <= 0 || evt.Action == null) return;
            bool spell = evt.Action.Tags?.Any(t =>
                string.Equals(t, "spell", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "wand", StringComparison.OrdinalIgnoreCase)) == true
                || (hero.Equipment.Weapon is WeaponItem w && w.WeaponType == WeaponType.Wand);
            if (!spell) return;
            hero.Heal(rank);
        }

        private void ApplyFocusLens(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "focus_lens");
            if (rank <= 0 || evt.Action == null) return;
            bool spell = evt.Action.Tags?.Any(t =>
                string.Equals(t, "spell", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "wand", StringComparison.OrdinalIgnoreCase)) == true
                || (hero.Equipment.Weapon is WeaponItem w && w.WeaponType == WeaponType.Wand);
            if (!spell) return;
            hero.Stats.TempTechniqueBonus += rank;
        }

        private void ApplyBronzeKnuckle(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "bronze_knuckle");
            if (rank <= 0 || (hero.HardenStacks ?? 0) <= 0) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "DAMAGE_MOD", Value = 3 * rank }
            });
        }

        private void ApplyPhalanx(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "phalanx");
            if (rank <= 0 || (hero.FortifyStacks ?? 0) <= 0) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "DAMAGE_MOD", Value = 2 * rank }
            });
        }

        private void ApplyMetronome(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "metronome");
            if (rank <= 0 || hero.Effects.ComboStep != 1) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "SPEED_MOD", Value = 4 * rank }
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

        private void ApplyToxinPressure(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "toxin_pressure");
            if (rank <= 0 || evt.Target == null) return;
            if (evt.Target.PoisonPercentOfMaxHealth <= 0) return;
            evt.Target.QueueBleedFromHit(rank);
        }

        private void ApplyGreasePalm(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "grease_palm");
            if (rank <= 0 || evt.NaturalRollValue <= 0 || evt.NaturalRollValue > 7) return;
            hero.Stats.TempTechniqueBonus += rank;
        }

        private void ApplySigilBurn(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "sigil_burn");
            if (rank <= 0 || evt.Target == null || evt.Target.BurnIntensity <= 0) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "DAMAGE_MOD", Value = 3 * rank }
            });
        }

        private void ApplySoftEcho(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "soft_echo");
            if (rank <= 0 || evt.Action == null) return;
            bool spell = evt.Action.Tags?.Any(t =>
                string.Equals(t, "spell", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "wand", StringComparison.OrdinalIgnoreCase)) == true
                || (hero.Equipment.Weapon is WeaponItem w && w.WeaponType == WeaponType.Wand);
            if (!spell) return;
            hero.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
            {
                new() { Type = "DAMAGE_MOD", Value = 2 * rank }
            });
        }

        private void ApplyLoadedOdds(Character hero, CombatEvent evt)
        {
            int rank = GetRank(hero, "loaded_odds");
            if (rank <= 0 || evt.NaturalRollValue < 15) return;
            hero.Stats.TempIntelligenceBonus += rank;
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
