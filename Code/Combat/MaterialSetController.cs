using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Material-set engines: equipped counts, convert grants, and synthesis keyword minting.
    /// </summary>
    public static class MaterialSetController
    {
        public static IEnumerable<Item?> EnumerateEquipped(Character? hero)
        {
            if (hero?.Equipment == null)
                yield break;
            yield return hero.Equipment.Head;
            yield return hero.Equipment.Body;
            yield return hero.Equipment.Legs;
            yield return hero.Equipment.Feet;
            yield return hero.Equipment.Weapon;
        }

        /// <summary>Wildcard material: each equipped piece counts toward every other material the hero is wearing.</summary>
        public const string WildcardMaterial = "Unknown";

        /// <summary>
        /// Equipped pieces of <paramref name="material"/>. <see cref="WildcardMaterial"/> pieces join the count once
        /// at least one real piece of that material is worn (wildcards alone never form a set). Hybrid material nodes
        /// (e.g. Ragecaller Material) let the partner class's material pieces join a class material's count the same way.
        /// </summary>
        public static int CountEquipped(Character? hero, string? material)
        {
            string key = MaterialBuildData.CanonicalMaterialName(material);
            if (key.Length == 0 || hero == null)
                return 0;
            bool keyIsWildcard = string.Equals(key, WildcardMaterial, StringComparison.OrdinalIgnoreCase);
            var partnerMaterials = keyIsWildcard ? new List<string>() : HybridPartnerMaterials(hero, key);
            int count = 0;
            int joiners = 0;
            foreach (var item in EnumerateEquipped(hero))
            {
                if (item == null)
                    continue;
                string itemMat = ItemMaterialRules.RemapLegacyMaterial(item.Material);
                if (string.Equals(itemMat, key, StringComparison.OrdinalIgnoreCase))
                    count++;
                else if (!keyIsWildcard && string.Equals(itemMat, WildcardMaterial, StringComparison.OrdinalIgnoreCase))
                    joiners++;
                else if (partnerMaterials.Any(m => string.Equals(m, itemMat, StringComparison.OrdinalIgnoreCase)))
                    joiners++;
            }
            return count > 0 ? count + joiners : 0;
        }

        /// <summary>Materials of the hybrid partner classes of <paramref name="material"/>'s class (learned hybrid material nodes).</summary>
        private static List<string> HybridPartnerMaterials(Character hero, string material)
        {
            string? classTag = ClassTags.All.FirstOrDefault(t =>
                ClassTags.MaterialsOf(t).Any(m => string.Equals(m, material, StringComparison.OrdinalIgnoreCase)));
            if (classTag == null)
                return new List<string>();
            return HybridTrees.MergedPartners(hero, classTag).SelectMany(ClassTags.MaterialsOf).ToList();
        }

        public static IReadOnlyList<string> GetGrantedConvertActionNames(Character? hero)
        {
            var names = new List<string>();
            if (hero == null)
                return names;
            foreach (var build in MaterialBuildsLoader.GetAll())
            {
                if (string.IsNullOrWhiteSpace(build.ConvertAction))
                    continue;
                int n = CountEquipped(hero, build.Material);
                if (n < MaterialBuildData.StackUnlockCount)
                    continue;
                names.Add(build.ConvertAction);
            }
            return names;
        }

        /// <summary>
        /// Adds 2-stack convert actions to the hero pool and removes converts that are no longer granted.
        /// Inventory equip uses incremental pool updates, so this must run on every gear change (not only full rebuild).
        /// </summary>
        public static void SyncConvertActionsToPool(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return;

            var granted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in GetGrantedConvertActionNames(hero))
            {
                if (!string.IsNullOrWhiteSpace(name))
                    granted.Add(name.Trim());
            }

            var knownConverts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var build in MaterialBuildsLoader.GetAll())
            {
                if (!string.IsNullOrWhiteSpace(build.ConvertAction))
                    knownConverts.Add(build.ConvertAction.Trim());
            }

            var stale = new List<Action>();
            foreach (var entry in hero.ActionPool)
            {
                if (entry.action == null)
                    continue;
                if (!knownConverts.Contains(entry.action.Name))
                    continue;
                if (granted.Contains(entry.action.Name))
                    continue;
                stale.Add(entry.action);
            }

            foreach (var action in stale)
            {
                var combo = hero.GetComboActions();
                for (int i = combo.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(combo[i].Name, action.Name, StringComparison.OrdinalIgnoreCase))
                        hero.RemoveFromCombo(combo[i], ignoreWeaponRequirement: true);
                }
                hero.RemoveAllActionsByName(action.Name);
            }

            foreach (string actionName in granted)
            {
                bool exists = false;
                foreach (var entry in hero.ActionPool)
                {
                    if (entry.action != null &&
                        string.Equals(entry.action.Name, actionName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists)
                    continue;

                var loaded = TryCreateConvertAction(actionName);
                if (loaded != null)
                    hero.AddAction(loaded, 1.0);
            }
        }

        /// <summary>
        /// Convert grants are authored set unlocks. Prefer the gameplay lookup, then fall back to raw
        /// action data so a workshop active-set tier filter cannot hide BONE WRATH / IRON CULL / etc.
        /// </summary>
        private static Action? TryCreateConvertAction(string actionName)
        {
            var loaded = ActionLoader.GetAction(actionName);
            if (loaded != null)
                return loaded;
            var data = ActionLoader.GetActionData(actionName);
            if (data == null)
                return null;
            return ActionDataToActionMapper.CreateAction(data);
        }

        public static int GetKeyword(Character? hero, string? keyword)
        {
            if (hero == null || string.IsNullOrWhiteSpace(keyword))
                return 0;
            return hero.Effects.GetMaterialKeyword(keyword);
        }

        /// <summary>
        /// Standing swing speed % from material sets: worn pieces of unlocked sets
        /// (<see cref="CombatConfig.MaterialSetSpeedPercentPerPiece"/>) plus currency minted this fight
        /// (<see cref="CombatConfig.MaterialFightMintSpeedPercentPerUnit"/>, capped). Heroes only; 0 when off.
        /// </summary>
        public static double GetSetSpeedPercent(Actor? actor)
        {
            if (actor is not Character hero || hero is Enemy)
                return 0;
            var combat = GameConfiguration.Instance.Combat;
            double pct = 0;
            if (combat.MaterialSetSpeedPercentPerPiece is { Count: > 0 } perPiece)
                foreach (var (material, perPct) in perPiece)
                {
                    int worn = CountEquipped(hero, material);
                    if (worn >= MaterialBuildData.StackUnlockCount)
                        pct += worn * perPct;
                }
            if (combat.MaterialFightMintSpeedPercentPerUnit is { Count: > 0 } perUnit)
            {
                double bank = 0;
                foreach (var (keyword, perPct) in perUnit)
                    bank += GetFightMinted(hero, keyword) * perPct;
                if (combat.MaterialFightMintSpeedMaxPercent > 0)
                    bank = Math.Min(bank, combat.MaterialFightMintSpeedMaxPercent);
                pct += bank;
            }
            return Math.Max(0, pct);
        }

        /// <summary>
        /// Incoming damage multiplier from worn set pieces (<see cref="CombatConfig.MaterialSetDamageReductionPercentPerPiece"/>)
        /// and currency minted this fight (<see cref="CombatConfig.MaterialFightMintDamageReductionPercentPerUnit"/>,
        /// capped at <see cref="CombatConfig.MaterialFightMintDamageReductionMaxPercent"/>, default 90). Heroes only; 1 when off.
        /// </summary>
        public static double GetIncomingDamageMultiplier(Actor? actor)
        {
            if (actor is not Character hero || hero is Enemy)
                return 1.0;
            var combat = GameConfiguration.Instance.Combat;
            double pct = 0;
            if (combat.MaterialFightMintDamageReductionPercentPerUnit is { Count: > 0 } perUnit)
                foreach (var (keyword, perPct) in perUnit)
                    pct += GetFightMinted(hero, keyword) * perPct;
            if (combat.MaterialSetDamageReductionPercentPerPiece is { Count: > 0 } perPiece)
                foreach (var (material, perPct) in perPiece)
                {
                    int worn = CountEquipped(hero, material);
                    if (worn >= MaterialBuildData.StackUnlockCount)
                        pct += worn * perPct;
                }
            if (pct <= 0)
                return 1.0;
            double cap = combat.MaterialFightMintDamageReductionMaxPercent > 0 ? combat.MaterialFightMintDamageReductionMaxPercent : 90;
            pct = Math.Clamp(pct, 0, cap);
            return 1.0 - pct / 100.0;
        }

        /// <summary>
        /// Attribute points from worn set pieces (<see cref="CombatConfig.MaterialSetAttributePerPiece"/>) for
        /// <paramref name="statCode"/> (STR / AGI / TEC / INT): each unlocked set pays its class's stat. Heroes only.
        /// </summary>
        public static int GetSetAttributeBonus(Character? hero, string statCode)
        {
            if (hero == null || hero is Enemy)
                return 0;
            var perPiece = GameConfiguration.Instance.Combat.MaterialSetAttributePerPiece;
            if (perPiece is not { Count: > 0 })
                return 0;
            double points = 0;
            foreach (var (material, per) in perPiece)
            {
                var build = MaterialBuildsLoader.FindByMaterial(material);
                if (build == null || !string.Equals(ClassStatCode(build.AssociatedClass), statCode, StringComparison.OrdinalIgnoreCase))
                    continue;
                int worn = CountEquipped(hero, material);
                if (worn >= MaterialBuildData.StackUnlockCount)
                    points += worn * per;
            }
            return (int)Math.Round(points);
        }

        /// <summary>A class's primary stat as the attribute bonus code: BARBARIAN STR, WARRIOR AGI, ROGUE TEC, WIZARD INT.</summary>
        public static string ClassStatCode(string? associatedClass) => (associatedClass ?? "").Trim().ToUpperInvariant() switch
        {
            "BARBARIAN" => "STR",
            "WARRIOR" => "AGI",
            "ROGUE" => "TEC",
            "WIZARD" => "INT",
            _ => ""
        };

        // Primary attribute converts granted this fight (capped by MaterialConvertPrimaryStatMaxPerFight).
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Character, StrongBox<int>> _fightConvertStat = new();
        // Unrounded points earned this fight, so small banks at low per-unit rates (0.05 BEAT) still add up.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Character, StrongBox<double>> _fightConvertStatRaw = new();
        // The AGI share of those grants, which the hero AGI soft cap can exempt (see GetSetAndConvertAgility).
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Character, StrongBox<int>> _fightConvertAgility = new();

        /// <summary>AGI the hero has from class sets (always-on per piece) plus this fight's convert grants.</summary>
        public static int GetSetAndConvertAgility(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return 0;
            int convert = _fightConvertAgility.TryGetValue(hero, out var box) ? box.Value : 0;
            return GetSetAttributeBonus(hero, "AGI") + convert;
        }

        /// <summary>
        /// Convert payoff in stats: <paramref name="unitsSpent"/> × <see cref="CombatConfig.MaterialConvertPrimaryStatPerUnit"/>
        /// points of the material class's primary attribute (Barbarian STR, Warrior AGI, Rogue TEC, Wizard INT; the hero's
        /// top attribute when the currency has no set) for the rest of the fight. Fractions carry within the fight.
        /// Returns the points granted.
        /// </summary>
        public static int GrantConvertPrimaryStat(Character? hero, string? keyword, int unitsSpent)
        {
            if (hero == null || hero is Enemy || unitsSpent <= 0 || string.IsNullOrWhiteSpace(keyword))
                return 0;
            double per = GetConvertPrimaryStatPerUnit(keyword);
            if (per <= 0)
                return 0;
            // With damage off the charm's convert multiplier scales the stat payout instead.
            if (!ConvertPaysDamage)
                per *= CharmBonusController.GetConvertMultiplier(hero);
            var combat = GameConfiguration.Instance.Combat;
            var raw = _fightConvertStatRaw.GetOrCreateValue(hero);
            double before = raw.Value;
            raw.Value += unitsSpent * per;
            int points = (int)Math.Floor(raw.Value + 1e-9) - (int)Math.Floor(before + 1e-9);
            var granted = _fightConvertStat.GetOrCreateValue(hero);
            if (combat.MaterialConvertPrimaryStatMaxPerFight > 0)
                points = Math.Min(points, Math.Max(0, combat.MaterialConvertPrimaryStatMaxPerFight - granted.Value));
            if (points <= 0)
                return 0;
            granted.Value += points;
            string code = GetConvertStatCode(hero, keyword);
            // Cadence buffs spell Technique "TECH" (CadenceScopedBonusState.TechniqueBonus); a "TEC" buff is never read.
            string buffType = string.Equals(code, "TEC", StringComparison.OrdinalIgnoreCase)
                ? DynamicAttributeCategoryResolver.CodeTechnique : code;
            hero.FightCadenceBuffs.MergeAdditively(new[] { new ActionAttackBonusItem { Type = buffType, Value = points } });
            if (string.Equals(code, "AGI", StringComparison.OrdinalIgnoreCase))
                _fightConvertAgility.GetOrCreateValue(hero).Value += points;
            return points;
        }

        /// <summary>Primary attribute points per unit a convert of <paramref name="keyword"/> spends (0 = none).</summary>
        public static double GetConvertPrimaryStatPerUnit(string? keyword)
        {
            var perUnit = GameConfiguration.Instance.Combat?.MaterialConvertPrimaryStatPerUnit;
            if (string.IsNullOrWhiteSpace(keyword) || perUnit is not { Count: > 0 } || !perUnit.TryGetValue(keyword, out double per))
                return 0;
            return Math.Max(0, per);
        }

        /// <summary>Attribute code a convert of <paramref name="keyword"/> pays: its set's class stat, else the hero's top attribute.</summary>
        public static string GetConvertStatCode(Character? hero, string? keyword)
        {
            string code = ClassStatCode(MaterialBuildsLoader.FindByKeyword(keyword)?.AssociatedClass);
            if (code.Length > 0 || hero == null)
                return code.Length > 0 ? code : "STR";
            return DynamicAttributeCategoryResolver.GetRankedAttributeCodes(hero)[0];
        }

        /// <summary>Converts pay damage under the active rules (<see cref="CombatConfig.MaterialConvertPaysDamage"/>).</summary>
        public static bool ConvertPaysDamage => GameConfiguration.Instance.Combat?.MaterialConvertPaysDamage ?? true;

        // Currency minted in the current fight, by keyword: drives the fight-mint speed/defense knobs, which must not drop
        // when a convert spends the bank.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Character, Dictionary<string, int>> _fightMinted = new();

        /// <summary>Adds to this fight's minted tally for <paramref name="keyword"/> (the mint path calls it).</summary>
        public static void RecordFightMint(Character? hero, string? keyword, int amount)
        {
            if (hero == null || string.IsNullOrWhiteSpace(keyword) || amount <= 0)
                return;
            var tally = _fightMinted.GetOrCreateValue(hero);
            tally[keyword] = tally.GetValueOrDefault(keyword) + amount;
        }

        /// <summary>Currency of <paramref name="keyword"/> minted since the fight started (spending does not lower it).</summary>
        public static int GetFightMinted(Character? hero, string? keyword)
        {
            if (hero == null || string.IsNullOrWhiteSpace(keyword) || !_fightMinted.TryGetValue(hero, out var tally))
                return 0;
            foreach (var (k, v) in tally)
                if (string.Equals(k, keyword, StringComparison.OrdinalIgnoreCase))
                    return v;
            return 0;
        }

        /// <summary>Zeros consecutive-connect tracking and this fight's minted tally for a new fight. Does not clear keyword currency.</summary>
        public static void ResetFightConnects(Character? hero)
        {
            if (hero == null)
                return;
            hero.Effects.MaterialConsecutiveConnects = 0;
            _fightMinted.Remove(hero);
            _fightConvertStat.Remove(hero);
            _fightConvertStatRaw.Remove(hero);
            _fightConvertAgility.Remove(hero);
        }

        /// <summary>Clears dungeon-run keyword currency (CRISIS, DRAG, …) and consecutive-connect tracking.</summary>
        public static void ClearDungeonBanks(Character? hero)
        {
            hero?.Effects.ClearMaterialKeywordBank();
        }

        public static void NotifyHeroTurnStart(Character? hero)
        {
            if (hero == null || hero is Enemy)
                return;
            var evt = new CombatEvent(CombatEventType.TurnStarted, hero) { Target = hero };
            TryMintFromEvent(hero, evt, action: null);
        }

        /// <param name="gainedFocus">The hero gained Focus from any source in this resolution (feeds ON_FOCUS).</param>
        /// <param name="appliedSlow">The hero slowed the target from any source in this resolution (feeds ON_SLOW).</param>
        /// <param name="takeHitOnly">The event is a foe's hit on the hero: only take-hit WHENs (Bone RAGE) may mint. Without
        /// this, a foe's connect also matched the hero's ON CONNECT / ON HIT / ON ATTACK sets.</param>
        public static bool TryMintFromEvent(Character? hero, CombatEvent? combatEvent, Action? action = null,
            bool gainedFocus = false, bool appliedSlow = false, bool takeHitOnly = false)
        {
            if (hero == null || hero is Enemy || combatEvent == null)
                return false;

            bool any = false;
            Action subject = action ?? combatEvent.Action ?? new Action { Name = "MaterialSynth" };
            foreach (var build in MaterialBuildsLoader.GetAll())
            {
                int unlock = CountEquipped(hero, build.Material);
                if (unlock < MaterialBuildData.StackUnlockCount)
                    continue;
                if (string.IsNullOrWhiteSpace(build.WhenToken) || string.IsNullOrWhiteSpace(build.Keyword))
                    continue;
                if (takeHitOnly != IsTakeHitToken(build.WhenToken))
                    continue;
                if (!WhenMatches(build.WhenToken, subject, combatEvent, hero, gainedFocus, appliedSlow))
                    continue;

                int amount = ComputeMintPerTrigger(hero, build);
                if (amount <= 0)
                    continue;
                hero.Effects.AddMaterialKeyword(build.Keyword, amount);
                RecordFightMint(hero, build.Keyword, amount);
                string whenLabel = MaterialBuildData.FormatWhenLabel(build.Synthesis);
                string cause = string.IsNullOrWhiteSpace(subject.Name) || subject.Name == "MaterialSynth"
                    ? whenLabel
                    : $"{subject.Name} · {whenLabel}";
                hero.Effects.SetMaterialKeywordSource(build.Keyword, cause);
                any = true;
            }
            return any;
        }

        /// <summary>Keyword minted each time the set's WHEN fires with the hero's current gear, charm and skills (0 below 2 pieces).</summary>
        public static int ComputeMintPerTrigger(Character? hero, MaterialBuildData build)
        {
            if (hero == null)
                return 0;
            int amount = MaterialBuildData.ComputeMintAmount(CountEquipped(hero, build.Material), CountEquipped(hero, build.FeedMaterial));
            if (amount <= 0)
                return 0;
            amount = Math.Max(1, (int)Math.Round(amount * CharmBonusController.GetMintMultiplier(hero)));
            return amount + SkillEffectRouter.SynthesisFeedBonus(hero, build.Material);
        }

        /// <summary>
        /// Flat convert damage from the keyword bank. Feed 3/5 only changes how much currency is minted,
        /// not this payoff: 2 RAGE → +10. Formula <c>material</c>/<c>count</c> uses equipped count instead of bank.
        /// </summary>
        public static int GetConvertDamageBonus(Character? hero, Action? action) => GetConvertDamageBonus(hero, action, 0);

        /// <summary>Converts pay a percent of the hit (<see cref="CombatConfig.MaterialConvertPercentPerUnit"/> &gt; 0).</summary>
        public static bool IsPercentConvertMode => (GameConfiguration.Instance.Combat?.MaterialConvertPercentPerUnit ?? 0) > 0;

        /// <param name="hitDamage">The hit before the convert (percent mode pays a share of it; 0 = card preview, which
        /// shows the percent as damage on a 100-point hit).</param>
        public static int GetConvertDamageBonus(Character? hero, Action? action, int hitDamage)
        {
            if (hero == null || action == null)
                return 0;

            MaterialBuildData? build = MaterialBuildsLoader.FindByConvertAction(action.Name);
            int bonus = ComputeConvertDamageBonus(hero, action, build, hitDamage);
            if (bonus > 0 || build == null || string.IsNullOrWhiteSpace(build.Keyword))
                return bonus;

            // Sheet columns on a pool copy can point at the wrong material or an empty bank
            // (materialScale "5" used to be read as a material name). The set's own keyword is
            // what the card and the hit should scale with once that currency is actually stocked.
            if (CountEquipped(hero, build.Material) < MaterialBuildData.StackUnlockCount)
                return 0;
            if (GetKeyword(hero, build.Keyword) <= 0)
                return 0;

            var fromBuild = new Action { Name = action.Name };
            return ComputeConvertDamageBonus(hero, fromBuild, build, hitDamage);
        }

        private static int ComputeConvertDamageBonus(Character hero, Action action, MaterialBuildData? build, int hitDamage)
        {
            if (!ConvertPaysDamage)
                return 0;
            if (!TryResolveConvertScale(action, build, out string material, out string keyword, out int perUnit, out bool useCount))
                return 0;

            int unlock = CountEquipped(hero, material);
            if (unlock < MaterialBuildData.StackUnlockCount && build != null
                && !string.Equals(material, build.Material, StringComparison.OrdinalIgnoreCase))
            {
                int buildUnlock = CountEquipped(hero, build.Material);
                if (buildUnlock >= MaterialBuildData.StackUnlockCount)
                {
                    material = build.Material;
                    unlock = buildUnlock;
                    if (!useCount && GetKeyword(hero, keyword) <= 0 && GetKeyword(hero, build.Keyword) > 0)
                        keyword = build.Keyword;
                }
            }

            if (unlock < MaterialBuildData.StackUnlockCount && build != null)
                return 0;

            int units = useCount ? Math.Max(0, unlock) : Math.Max(0, GetKeyword(hero, keyword));
            if (units <= 0 && !useCount && build != null && GetKeyword(hero, build.Keyword) > 0
                && !string.Equals(keyword, build.Keyword, StringComparison.OrdinalIgnoreCase))
            {
                keyword = build.Keyword;
                units = GetKeyword(hero, keyword);
            }

            if (units <= 0)
                return 0;
            var combat = GameConfiguration.Instance.Combat;
            if (combat != null && combat.MaterialConvertMaxUnits > 0)
                units = Math.Min(units, combat.MaterialConvertMaxUnits);
            if (combat != null && combat.MaterialConvertPercentPerUnit > 0)
            {
                double perUnitPct = !useCount && combat.MaterialConvertPercentByKeyword != null
                    && combat.MaterialConvertPercentByKeyword.TryGetValue(keyword, out double byKeyword)
                    ? byKeyword
                    : perUnit * combat.MaterialConvertPercentPerUnit;
                double pct = units * perUnitPct / 100.0;
                double basis = hitDamage > 0 ? hitDamage : 100;
                return Math.Max(0, (int)Math.Round(basis * pct * CharmBonusController.GetConvertMultiplier(hero)));
            }
            int flat = units * perUnit;
            return Math.Max(0, (int)Math.Round(flat * CharmBonusController.GetConvertMultiplier(hero)));
        }

        /// <summary>
        /// Convert cash-out: a connecting convert action spends the whole bank it scaled from, so each payout is
        /// what was minted since the last cash-out. Count-formula converts read equipped pieces and spend nothing.
        /// </summary>
        public static int SpendConvertBank(Character? hero, Action? action)
        {
            if (hero == null || hero is Enemy || action == null)
                return 0;
            MaterialBuildData? build = MaterialBuildsLoader.FindByConvertAction(action.Name);
            if (!TryResolveConvertScale(action, build, out _, out string keyword, out _, out bool useCount)
                || useCount || string.IsNullOrWhiteSpace(keyword))
                return 0;
            int bank = GetKeyword(hero, keyword);
            if (bank > 0)
            {
                hero.Effects.AddMaterialKeyword(keyword, -bank);
                GrantConvertPrimaryStat(hero, keyword, bank);
            }
            return bank;
        }

        /// <summary>
        /// Reads an action's convert-scale columns. Sheet layout (ACTIONS DS/DT/DU): <c>materialScale</c> = bonus per
        /// keyword (number), <c>keywordScale</c> = effect (<c>DAMAGE</c>), <c>scaleFormula</c> = keyword or
        /// <c>material</c>/<c>count</c>. Legacy layout (material name / keyword / formula) still resolves.
        /// Unset columns fall back to the MATERIAL BUILDS row.
        /// </summary>
        private static bool TryResolveConvertScale(Action action, MaterialBuildData? build,
            out string material, out string keyword, out int perUnit, out bool useCount)
        {
            material = build?.Material ?? "";
            keyword = build?.Keyword ?? "";
            perUnit = MaterialBuildData.ConvertDamagePerKeyword;
            useCount = false;

            string materialScale = (action.MaterialScale ?? "").Trim();
            string keywordScale = (action.KeywordScale ?? "").Trim();
            string formula = (action.ScaleFormula ?? "").Trim();
            if (build == null && materialScale.Length == 0 && keywordScale.Length == 0)
                return false;

            if (int.TryParse(materialScale, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPerUnit))
                perUnit = Math.Max(0, parsedPerUnit);
            else if (materialScale.Length > 0)
                material = MaterialBuildData.CanonicalMaterialName(materialScale);

            if (keywordScale.Length > 0 && !keywordScale.Equals("DAMAGE", StringComparison.OrdinalIgnoreCase))
                keyword = keywordScale.ToUpperInvariant();

            if (formula.Equals("material", StringComparison.OrdinalIgnoreCase)
                || formula.Equals("count", StringComparison.OrdinalIgnoreCase))
                useCount = true;
            else if (formula.Length > 0
                && !formula.Equals("keyword", StringComparison.OrdinalIgnoreCase)
                && !formula.Equals("bank", StringComparison.OrdinalIgnoreCase))
                keyword = formula.ToUpperInvariant();
            return true;
        }

        /// <summary>
        /// Per-unit convert rate under the active rules, e.g. <c>+3.5% damage per GRAZE</c> (percent mode) or
        /// <c>+5 damage per GRAZE</c> (flat). Null when the action is not a keyword convert.
        /// </summary>
        public static string? FormatConvertRateText(Action? action)
        {
            if (action == null)
                return null;
            MaterialBuildData? build = MaterialBuildsLoader.FindByConvertAction(action.Name);
            if (!TryResolveConvertScale(action, build, out _, out string keyword, out int perUnit, out bool useCount)
                || useCount || string.IsNullOrWhiteSpace(keyword))
                return null;

            var combat = GameConfiguration.Instance.Combat;
            if (combat != null && combat.MaterialConvertPercentPerUnit > 0)
            {
                double pct = combat.MaterialConvertPercentByKeyword != null
                    && combat.MaterialConvertPercentByKeyword.TryGetValue(keyword, out double byKeyword)
                    ? byKeyword
                    : perUnit * combat.MaterialConvertPercentPerUnit;
                return WithStatRate($"+{pct.ToString("0.#", CultureInfo.InvariantCulture)}% damage per {keyword}", keyword);
            }
            return WithStatRate($"+{perUnit} damage per {keyword}", keyword);
        }

        /// <summary>Stat payout text, e.g. <c>+1 STR per 4 RAGE (rest of fight)</c>; empty when converts pay no stats.</summary>
        public static string FormatConvertStatRate(string? keyword)
        {
            double per = GetConvertPrimaryStatPerUnit(keyword);
            if (per <= 0)
                return "";
            string code = GetConvertStatCode(null, keyword);
            string rate = per >= 1
                ? $"+{per.ToString("0.##", CultureInfo.InvariantCulture)} {code} per {keyword}"
                : $"+1 {code} per {(1 / per).ToString("0.#", CultureInfo.InvariantCulture)} {keyword}";
            return rate + " (rest of fight)";
        }

        // Damage-off rules show only the stat payout; damage rules append it when the patch pays stats too.
        private static string WithStatRate(string damageText, string keyword)
        {
            string stat = FormatConvertStatRate(keyword);
            if (!ConvertPaysDamage)
                return stat.Length > 0 ? stat : $"Spends {keyword}";
            return stat.Length > 0 ? $"{damageText}, {stat}" : damageText;
        }

        /// <summary>
        /// Compact convert-scale label for action cards (e.g. <c>RAGE +10</c>). Null when this action is not scaled.
        /// </summary>
        public static string? FormatConvertScaleLine(Character? hero, Action? action)
        {
            if (hero == null || action == null)
                return null;
            int bonus = GetConvertDamageBonus(hero, action);
            if (bonus <= 0)
                return null;

            MaterialBuildData? build = MaterialBuildsLoader.FindByConvertAction(action.Name);
            TryResolveConvertScale(action, build, out string material, out string keyword, out _, out bool useCount);
            if (build != null && !useCount && GetKeyword(hero, keyword) <= 0 && GetKeyword(hero, build.Keyword) > 0)
                keyword = build.Keyword;
            if (build != null && CountEquipped(hero, material) < MaterialBuildData.StackUnlockCount
                && CountEquipped(hero, build.Material) >= MaterialBuildData.StackUnlockCount)
                material = build.Material;

            string unit = IsPercentConvertMode ? "%" : "";
            int stacks = !useCount && !string.IsNullOrWhiteSpace(keyword)
                ? GetKeyword(hero, keyword)
                : CountEquipped(hero, material);
            string stack = stacks > 0 ? $" x{stacks}" : "";
            if (!useCount && !string.IsNullOrWhiteSpace(keyword))
                return $"{keyword}{stack} +{bonus}{unit}";
            if (!string.IsNullOrWhiteSpace(material))
                return $"{material}{stack} +{bonus}{unit}";
            return $"Convert{stack} +{bonus}{unit}";
        }

        /// <summary>
        /// Equipped MATERIAL BUILDS rows at 2+ pieces (a set the hero has started forming),
        /// plus the class-less Leather luck set when 2+ Leather pieces are equipped.
        /// </summary>
        public static IReadOnlyList<(string Material, int Count)> GetFormingSets(Character? hero)
        {
            var list = new List<(string Material, int Count)>();
            if (hero == null)
                return list;
            foreach (var build in MaterialBuildsLoader.GetAll())
            {
                if (string.IsNullOrWhiteSpace(build.Material))
                    continue;
                int n = CountEquipped(hero, build.Material);
                if (n < MaterialBuildData.StackUnlockCount)
                    continue;
                list.Add((build.Material, n));
            }

            int leatherCount = LeatherSetBonus.CountEquipped(hero);
            if (leatherCount >= MaterialBuildData.StackUnlockCount
                && !list.Any(e => string.Equals(e.Material, LeatherSetBonus.MaterialName, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add((LeatherSetBonus.MaterialName, leatherCount));
            }

            return list;
        }

        /// <summary>Compact HUD line, e.g. <c>Iron 2/5</c> or <c>Leather 3/3</c>.</summary>
        public static string FormatFormingSetHudLine(string material, int count)
        {
            if (string.Equals(
                    MaterialBuildData.CanonicalMaterialName(material),
                    LeatherSetBonus.MaterialName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return LeatherSetBonus.FormatHudLine(count);
            }

            string name = MaterialBuildData.CanonicalMaterialName(material);
            if (name.Length == 0)
                name = (material ?? "").Trim();
            return $"{name} {count}/5";
        }

        public static IEnumerable<string> FormatFormingSetHudLines(Character? hero)
        {
            foreach (var (material, count) in GetFormingSets(hero))
                yield return FormatFormingSetHudLine(material, count);
        }

        public static IEnumerable<string> FormatSetStatusLines(Character? hero, Item? item)
        {
            if (item == null)
                yield break;
            foreach (var line in FormatSetStatusLinesForMaterial(hero, item.Material))
                yield return line;
        }

        public static IEnumerable<string> FormatSetStatusLinesForMaterial(Character? hero, string? material)
        {
            string key = ItemMaterialRules.RemapLegacyMaterial(material);
            if (string.Equals(key, LeatherSetBonus.MaterialName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var line in LeatherSetBonus.FormatStatusLines(hero))
                    yield return line;
                yield break;
            }

            var build = MaterialBuildsLoader.FindByMaterial(key);
            if (build == null)
            {
                if (!string.IsNullOrWhiteSpace(key))
                    yield return $"{key}: no material build";
                yield break;
            }

            int n = hero != null ? CountEquipped(hero, build.Material) : 0;
            yield return FormatFormingSetHudLine(build.Material, n);
            foreach (var line in DescribeSetGeneration(hero, build, n))
                yield return line;
        }

        /// <summary>How the set's keyword is generated (trigger, per-trigger amount by piece count) and how it is spent.</summary>
        private static IEnumerable<string> DescribeSetGeneration(Character? hero, MaterialBuildData build, int n)
        {
            string keyword = string.IsNullOrWhiteSpace(build.Keyword) ? "its keyword" : build.Keyword;
            string feed = string.IsNullOrWhiteSpace(build.FeedMaterial) ? build.Material : build.FeedMaterial;
            yield return $"Gain {keyword}: {MaterialBuildData.DescribeWhen(build.WhenToken)}.";

            if (n < MaterialBuildData.StackUnlockCount)
                yield return $"Now: nothing yet. Wear {MaterialBuildData.StackUnlockCount} {build.Material} pieces to start.";
            else if (hero != null)
                yield return $"Now: +{ComputeMintPerTrigger(hero, build)} {keyword} each time.";

            string convert = string.IsNullOrWhiteSpace(build.ConvertAction) ? "" : $", unlocks {build.ConvertAction}";
            int five = GameConfiguration.Instance.Combat?.MaterialFiveStackMintMultiplier ?? 0;
            string fiveText = five > 0 ? $"1 per {feed} piece × {five}" : $"{feed} pieces × {feed} pieces";
            yield return $"{Mark(n, MaterialBuildData.StackUnlockCount)}2 pieces: +1 each time{convert}.";
            yield return $"{Mark(n, MaterialBuildData.StackAdditiveCount)}3 pieces: +1 per {feed} piece worn.";
            yield return $"{Mark(n, MaterialBuildData.StackMultiplyCount)}5 pieces: {fiveText}.";

            if (!string.IsNullOrWhiteSpace(build.ConvertAction) && !string.IsNullOrWhiteSpace(build.Keyword))
            {
                yield return "Spend: " + HoverEffectCopy.FormatKeywordPayoff(build, hero)
                    + (n >= MaterialBuildData.StackUnlockCount ? "" : $" (needs {MaterialBuildData.StackUnlockCount} pieces)");
            }
            if (hero != null && !string.IsNullOrWhiteSpace(build.Keyword))
            {
                string last = hero.Effects.GetMaterialKeywordSource(build.Keyword);
                yield return $"Banked: {GetKeyword(hero, build.Keyword)} {keyword} (kept until you leave the dungeon)."
                    + (string.IsNullOrEmpty(last) ? "" : $" Last from: {last}.");
            }
        }

        private static string Mark(int have, int need) => have >= need ? "> " : "  ";

        private static bool IsTakeHitToken(string whenToken) =>
            ActionTriggerGate.NormalizeToken(whenToken) is "ONTAKEHIT" or "ONHEROHURT" or "ONRECEIVEHIT";

        private static bool WhenMatches(string whenToken, Action action, CombatEvent combatEvent, Character hero,
            bool gainedFocus = false, bool appliedSlow = false)
        {
            string token = ActionTriggerGate.NormalizeToken(whenToken);
            if (token == "ONTURN")
                return combatEvent.Type == CombatEventType.TurnStarted;
            if (token == "ONATTACK")
                return action.Type == ActionType.Attack
                    && (combatEvent.Type == CombatEventType.ActionHit
                        || combatEvent.Type == CombatEventType.ActionMiss
                        || combatEvent.Type == CombatEventType.ActionExecuted);
            if (token == "ONCONSECUTIVEATTACK")
                return hero.Effects.MaterialConsecutiveConnects >= 2
                    && combatEvent.Type == CombatEventType.ActionHit
                    && !combatEvent.IsMiss;
            if (token == "ONSLOW")
                return !combatEvent.IsMiss
                    && combatEvent.Type == CombatEventType.ActionHit
                    && (action.CausesSlow || appliedSlow);
            if (token == "ONFOCUS")
                return !combatEvent.IsMiss
                    && combatEvent.Type == CombatEventType.ActionHit
                    && (action.CausesFocus || gainedFocus);

            return ActionTriggerGate.MatchesConditionToken(whenToken, action, combatEvent);
        }
    }
}
