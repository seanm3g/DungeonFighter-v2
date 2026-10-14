using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public static int CountEquipped(Character? hero, string? material)
        {
            string key = MaterialBuildData.CanonicalMaterialName(material);
            if (key.Length == 0 || hero == null)
                return 0;
            int count = 0;
            foreach (var item in EnumerateEquipped(hero))
            {
                if (item == null)
                    continue;
                string itemMat = ItemMaterialRules.RemapLegacyMaterial(item.Material);
                if (string.Equals(itemMat, key, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
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

        /// <summary>Zeros consecutive-connect tracking for a new fight. Does not clear keyword currency.</summary>
        public static void ResetFightConnects(Character? hero)
        {
            if (hero != null)
                hero.Effects.MaterialConsecutiveConnects = 0;
        }

        /// <summary>Clears dungeon-run keyword currency (CRISIS, DRAG, …) and consecutive-connect tracking.</summary>
        public static void ClearDungeonBanks(Character? hero)
        {
            hero?.Effects.ClearMaterialKeywordBank();
        }

        public static void NotifyHeroTurnStart(Character? hero, List<string>? messages = null)
        {
            if (hero == null || hero is Enemy)
                return;
            var evt = new CombatEvent(CombatEventType.TurnStarted, hero) { Target = hero };
            TryMintFromEvent(hero, evt, action: null, messages);
        }

        /// <param name="gainedFocus">The hero gained Focus from any source in this resolution (feeds ON_FOCUS).</param>
        /// <param name="appliedSlow">The hero slowed the target from any source in this resolution (feeds ON_SLOW).</param>
        /// <param name="takeHitOnly">The event is a foe's hit on the hero: only take-hit WHENs (Bone RAGE) may mint. Without
        /// this, a foe's connect also matched the hero's ON CONNECT / ON HIT / ON ATTACK sets.</param>
        public static bool TryMintFromEvent(Character? hero, CombatEvent? combatEvent, Action? action = null, List<string>? messages = null,
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

                int feedCount = CountEquipped(hero, build.FeedMaterial);
                int amount = MaterialBuildData.ComputeMintAmount(unlock, feedCount);
                if (amount <= 0)
                    continue;
                amount = Math.Max(1, (int)Math.Round(amount * CharmBonusController.GetMintMultiplier(hero)));
                amount += SkillEffectRouter.SynthesisFeedBonus(hero, build.Material);
                hero.Effects.AddMaterialKeyword(build.Keyword, amount);
                int total = hero.Effects.GetMaterialKeyword(build.Keyword);
                messages?.Add(FormatFeedCombatLine(build.Synthesis, build.Keyword, amount, total));
                any = true;
            }
            return any;
        }

        /// <summary>True when <paramref name="line"/> is a material-feed combat-block follow-up.</summary>
        public static bool IsFeedCombatLine(string? line)
        {
            if (string.IsNullOrEmpty(line))
                return false;
            return line.IndexOf(" feeds ", StringComparison.OrdinalIgnoreCase) >= 0
                && line.IndexOf("+", StringComparison.Ordinal) >= 0;
        }

        /// <summary>Parenthetical combat-block line: <c>(ON HIT feeds +1 GRAZE → 2)</c>.</summary>
        public static string FormatFeedCombatLine(string? synthesis, string keyword, int amount, int total)
        {
            string whenLabel = MaterialBuildData.FormatWhenLabel(synthesis);
            if (string.IsNullOrWhiteSpace(whenLabel))
                whenLabel = "WHEN";
            string key = (keyword ?? "").Trim().ToUpperInvariant();
            if (key.Length == 0)
                key = "CURRENCY";

            var builder = new ColoredTextBuilder();
            builder.Add("     (", Colors.Gray);
            builder.Add(whenLabel, ColorPalette.Info);
            builder.Add(" feeds ", Colors.White);
            builder.Add($"+{amount}", ColorPalette.Success);
            builder.Add(" ", Colors.White);
            builder.Add(key, ColorPalette.Gold);
            if (total > 0)
            {
                builder.Add(" → ", Colors.Gray);
                builder.Add(total.ToString(), ColorPalette.Success);
            }
            builder.Add(")", Colors.Gray);
            return ColoredTextRenderer.RenderAsMarkup(builder.Build());
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
                hero.Effects.AddMaterialKeyword(keyword, -bank);
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
            yield return "2 synth+convert, 3 +feed, 5 xfeed";
            yield return $"WHEN {build.Synthesis}";
            if (!string.IsNullOrWhiteSpace(build.ConvertAction))
                yield return $"Convert: {build.ConvertAction}" + (n >= 2 ? " (unlocked)" : " (needs 2)");
            if (hero != null && !string.IsNullOrWhiteSpace(build.Keyword))
                yield return $"{build.Keyword}: {GetKeyword(hero, build.Keyword)}";
        }

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
