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

        /// <summary>Convert damage multiplier from keyword bank + 3/5-stack feed counts. Never below 1.</summary>
        public static double GetConvertDamageMultiplier(Character? hero, Action? action)
        {
            if (hero == null || action == null)
                return 1.0;

            MaterialBuildData? build = MaterialBuildsLoader.FindByConvertAction(action.Name);
            string materialOverride = action.MaterialScale ?? "";
            string keywordOverride = action.KeywordScale ?? "";

            if (build == null && string.IsNullOrWhiteSpace(materialOverride) && string.IsNullOrWhiteSpace(keywordOverride))
                return 1.0;

            string material = !string.IsNullOrWhiteSpace(materialOverride)
                ? MaterialBuildData.CanonicalMaterialName(materialOverride)
                : (build?.Material ?? "");
            string keyword = !string.IsNullOrWhiteSpace(keywordOverride)
                ? keywordOverride.Trim().ToUpperInvariant()
                : (build?.Keyword ?? "");
            string feedMaterial = build?.FeedMaterial ?? material;

            int unlock = CountEquipped(hero, material);
            if (unlock < MaterialBuildData.StackUnlockCount && build != null)
                return 1.0;

            int feed = CountEquipped(hero, feedMaterial);
            int bank = GetKeyword(hero, keyword);

            double v = Math.Max(1, bank);
            if (unlock >= MaterialBuildData.StackAdditiveCount)
                v = bank + feed;
            if (unlock >= MaterialBuildData.StackMultiplyCount)
                v *= Math.Max(1, feed);

            string formula = (action.ScaleFormula ?? "").Trim();
            if (formula.Equals("keyword", StringComparison.OrdinalIgnoreCase)
                || formula.Equals("bank", StringComparison.OrdinalIgnoreCase))
                v = Math.Max(1, bank);
            else if (formula.Equals("material", StringComparison.OrdinalIgnoreCase)
                     || formula.Equals("count", StringComparison.OrdinalIgnoreCase))
                v = Math.Max(1, unlock);

            return Math.Max(1.0, v);
        }

        /// <summary>
        /// Compact convert-scale label for action cards (e.g. <c>CRISIS ×4</c>). Null when this action is not scaled.
        /// </summary>
        public static string? FormatConvertScaleLine(Character? hero, Action? action)
        {
            if (hero == null || action == null)
                return null;
            double mult = GetConvertDamageMultiplier(hero, action);
            if (mult <= 1.0001)
                return null;

            MaterialBuildData? build = MaterialBuildsLoader.FindByConvertAction(action.Name);
            string keyword = !string.IsNullOrWhiteSpace(action.KeywordScale)
                ? action.KeywordScale.Trim().ToUpperInvariant()
                : (build?.Keyword ?? "");
            string material = !string.IsNullOrWhiteSpace(action.MaterialScale)
                ? MaterialBuildData.CanonicalMaterialName(action.MaterialScale)
                : (build?.Material ?? "");

            string qty = mult.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(keyword))
                return $"{keyword} ×{qty}";
            if (!string.IsNullOrWhiteSpace(material))
                return $"{material} ×{qty}";
            return $"Convert ×{qty}";
        }

        public static IEnumerable<string> FormatSetStatusLines(Character? hero, Item? item)
        {
            if (item == null)
                yield break;
            string material = ItemMaterialRules.RemapLegacyMaterial(item.Material);
            var build = MaterialBuildsLoader.FindByMaterial(material);
            if (build == null)
            {
                if (!string.IsNullOrWhiteSpace(material))
                    yield return $"{material}: no material build";
                yield break;
            }

            int n = hero != null ? CountEquipped(hero, build.Material) : 0;
            yield return $"{build.Material} {n}/5  (2 synth+convert, 3 +feed, 5 xfeed)";
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
