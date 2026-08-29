using System;
using System.Collections.Generic;
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

        public static bool TryMintFromEvent(Character? hero, CombatEvent? combatEvent, Action? action = null, List<string>? messages = null)
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
                if (!WhenMatches(build.WhenToken, subject, combatEvent, hero))
                    continue;

                int feedCount = CountEquipped(hero, build.FeedMaterial);
                int amount = MaterialBuildData.ComputeMintAmount(unlock, feedCount);
                if (amount <= 0)
                    continue;
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
        public static int GetConvertDamageBonus(Character? hero, Action? action)
        {
            if (hero == null || action == null)
                return 0;

            MaterialBuildData? build = MaterialBuildsLoader.FindByConvertAction(action.Name);
            string materialOverride = action.MaterialScale ?? "";
            string keywordOverride = action.KeywordScale ?? "";

            if (build == null && string.IsNullOrWhiteSpace(materialOverride) && string.IsNullOrWhiteSpace(keywordOverride))
                return 0;

            string material = !string.IsNullOrWhiteSpace(materialOverride)
                ? MaterialBuildData.CanonicalMaterialName(materialOverride)
                : (build?.Material ?? "");
            string keyword = !string.IsNullOrWhiteSpace(keywordOverride)
                ? keywordOverride.Trim().ToUpperInvariant()
                : (build?.Keyword ?? "");

            int unlock = CountEquipped(hero, material);
            if (unlock < MaterialBuildData.StackUnlockCount && build != null)
                return 0;

            int bank = GetKeyword(hero, keyword);
            int units = Math.Max(0, bank);

            string formula = (action.ScaleFormula ?? "").Trim();
            if (formula.Equals("material", StringComparison.OrdinalIgnoreCase)
                || formula.Equals("count", StringComparison.OrdinalIgnoreCase))
                units = Math.Max(0, unlock);

            if (units <= 0)
                return 0;
            return units * MaterialBuildData.ConvertDamagePerKeyword;
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
            string keyword = !string.IsNullOrWhiteSpace(action.KeywordScale)
                ? action.KeywordScale.Trim().ToUpperInvariant()
                : (build?.Keyword ?? "");
            string material = !string.IsNullOrWhiteSpace(action.MaterialScale)
                ? MaterialBuildData.CanonicalMaterialName(action.MaterialScale)
                : (build?.Material ?? "");

            if (!string.IsNullOrWhiteSpace(keyword))
                return $"{keyword} +{bonus}";
            if (!string.IsNullOrWhiteSpace(material))
                return $"{material} +{bonus}";
            return $"Convert +{bonus}";
        }

        /// <summary>
        /// Equipped MATERIAL BUILDS rows at 2+ pieces (a set the hero has started forming).
        /// Class-less materials with no build row are omitted.
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
            return list;
        }

        /// <summary>Compact HUD line, e.g. <c>Iron 2/5</c>.</summary>
        public static string FormatFormingSetHudLine(string material, int count)
        {
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

        private static bool WhenMatches(string whenToken, Action action, CombatEvent combatEvent, Character hero)
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
                    && action.CausesSlow;
            if (token == "ONFOCUS")
                return !combatEvent.IsMiss
                    && combatEvent.Type == CombatEventType.ActionHit
                    && action.CausesFocus;

            return ActionTriggerGate.MatchesConditionToken(whenToken, action, combatEvent);
        }
    }
}
