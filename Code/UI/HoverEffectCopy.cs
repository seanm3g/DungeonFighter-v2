using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Player-facing "what this does" copy for status rows and action-pool hovers.
    /// Numbers follow the same combat knobs the handlers use.
    /// </summary>
    public static class HoverEffectCopy
    {
        private const int StackTurns = 3;

        public static List<string> ExplainStatusLine(Character? hero, string? displayLine)
        {
            var lines = new List<string>();
            string line = (displayLine ?? "").Trim();
            if (line.Length == 0)
                return lines;

            if (LeatherSetBonus.IsLuckyStatusLine(line))
            {
                lines.Add("Trigger: 3 or more Leather pieces equipped.");
                lines.Add("Attack rolls use 2d20 and keep the highest.");
                lines.Add("Lasts while the Leather set stays equipped.");
                return lines;
            }

            string? roomStatus = RPGGame.Combat.RoomStatusEffects.NameFromLine(line);
            if (roomStatus != null)
                return RPGGame.Combat.RoomStatusEffects.Explain(hero, roomStatus);

            var keyword = Regex.Match(line, @"^([A-Z][A-Z0-9]*) x\d+$");
            if (keyword.Success)
            {
                var build = MaterialBuildsLoader.FindByKeyword(keyword.Groups[1].Value);
                if (build != null)
                {
                    AppendKeywordExplanation(lines, hero, build);
                    return lines;
                }
            }

            string effectId = EffectIdFromHudLine(line);
            if (!string.IsNullOrEmpty(effectId))
            {
                lines.AddRange(StatusEffectHoverDetails.Describe(hero, effectId));
                return lines;
            }

            AppendLooseLine(lines, line);
            return lines;
        }

        /// <summary>Status row hover for any actor (enemy right-panel rows); hero-only lines fall back to the short copy.</summary>
        /// <param name="observer">The player, so enemy rows can list which of the hero's actions inflict the status.</param>
        public static List<string> ExplainActorStatusLine(Actor? actor, string? displayLine, Character? observer = null)
        {
            if (actor is Character hero && actor is not Enemy)
                return ExplainStatusLine(hero, displayLine);
            var lines = new List<string>();
            string line = (displayLine ?? "").Trim();
            string? roomStatus = RPGGame.Combat.RoomStatusEffects.NameFromLine(line);
            if (roomStatus != null)
                return RPGGame.Combat.RoomStatusEffects.Explain(actor, roomStatus);
            string effectId = EffectIdFromHudLine(line);
            if (!string.IsNullOrEmpty(effectId))
                lines.AddRange(StatusEffectHoverDetails.Describe(actor, effectId, observer));
            else
                AppendLooseLine(lines, line);
            return lines;
        }

        public static List<string> BuildActionDoesLines(Character? character, Action? action)
        {
            var lines = new List<string>();
            if (action == null)
                return lines;

            // D5.1: the card's role line first ("Payoff · applies 1 Bleed").
            string presented = PresentDescription(ActionRoles.CardLine(action, CombatActionStripBuilder.ResolveActionCardDescription(action)));
            if (!string.IsNullOrEmpty(presented))
                lines.Add(presented);

            foreach (string effectId in StatusEffectActionResolver.GetEffectTypesFromAction(action))
            {
                if (lines.Count >= 8)
                    break;
                AppendGrant(lines, action, effectId);
                if (character != null)
                    AppendAlreadyOnYou(lines, character, action, effectId);
            }

            AppendConvert(lines, character, action, presented);
            return lines;
        }

        private static void AppendKeywordExplanation(List<string> lines, Character? hero, MaterialBuildData build)
        {
            lines.Add($"Gained from your {build.Material} set (2+ pieces): {MaterialBuildData.DescribeWhen(build.WhenToken)}.");
            if (hero != null)
                lines.Add($"Each trigger now gives +{MaterialSetController.ComputeMintPerTrigger(hero, build)} (more with 3 and 5 pieces).");
            string source = hero?.Effects.GetMaterialKeywordSource(build.Keyword) ?? "";
            if (!string.IsNullOrEmpty(source))
                lines.Add($"Last trigger: {source}.");
            lines.Add(FormatKeywordPayoff(build, hero));
            lines.Add("Lasts until you leave the dungeon.");
            string rider = SkillEffectRouter.DescribeEquippedConvertMastery(hero, build.ConvertAction);
            if (!string.IsNullOrEmpty(rider))
                lines.Add(rider);
        }

        private static void AppendConvert(List<string> lines, Character? hero, Action action, string presented)
        {
            var build = MaterialBuildsLoader.FindByConvertAction(action.Name);
            if (build == null || string.IsNullOrWhiteSpace(build.Keyword))
                return;

            bool descriptionCovers = presented.IndexOf(build.Keyword, StringComparison.OrdinalIgnoreCase) >= 0;
            if (!descriptionCovers && lines.Count < 8)
                lines.Add(FormatKeywordPayoff(build, hero));
            if (hero != null && lines.Count < 8)
                lines.Add($"You have {MaterialSetController.GetKeyword(hero, build.Keyword)} {build.Keyword}.");
            string rider = SkillEffectRouter.DescribeEquippedConvertMastery(hero, action.Name);
            if (!string.IsNullOrEmpty(rider) && lines.Count < 8)
                lines.Add(rider);
        }

        public static string FormatKeywordPayoff(MaterialBuildData build, Character? hero)
        {
            string keyword = build.Keyword;
            string actionName = string.IsNullOrWhiteSpace(build.ConvertAction) ? "The convert" : build.ConvertAction;
            int perUnit = MaterialBuildData.ConvertDamagePerKeyword;
            var data = string.IsNullOrWhiteSpace(build.ConvertAction) ? null : ActionLoader.GetActionData(build.ConvertAction);
            if (data != null
                && int.TryParse(data.MaterialScale, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && parsed > 0)
                perUnit = parsed;

            string payoff;
            var combat = GameConfiguration.Instance?.Combat;
            string statRate = MaterialSetController.FormatConvertStatRate(keyword);
            if (!MaterialSetController.ConvertPaysDamage)
            {
                payoff = statRate.Length > 0
                    ? $"{actionName} spends all {keyword} for {statRate}."
                    : $"{actionName} spends all {keyword}.";
            }
            else if (combat != null && combat.MaterialConvertPercentPerUnit > 0)
            {
                double pct = perUnit * combat.MaterialConvertPercentPerUnit;
                if (combat.MaterialConvertPercentByKeyword != null
                    && combat.MaterialConvertPercentByKeyword.TryGetValue(keyword, out double byKeyword))
                    pct = byKeyword;
                payoff = $"{actionName} spends {keyword} for {pct.ToString("0.#", CultureInfo.InvariantCulture)}% of the hit per {keyword}.";
            }
            else
            {
                payoff = $"{actionName} spends {keyword} for +{perUnit} damage per {keyword}.";
            }

            if (MaterialSetController.ConvertPaysDamage && statRate.Length > 0)
                payoff += $" Also {statRate}.";
            if (hero != null)
            {
                double charm = CharmBonusController.GetConvertMultiplier(hero);
                if (charm > 1.001)
                    payoff += $" Charm ×{charm.ToString("0.##", CultureInfo.InvariantCulture)}.";
            }
            return payoff;
        }

        private static void AppendGrant(List<string> lines, Action action, string effectId)
        {
            string raw = (effectId ?? "").Trim().ToLowerInvariant();
            // Retired ids describe the core status they apply as (Weaken = Rattle, Stun = Stagger, ...); dropped ones nothing.
            string id = CoreStatuses.Resolve(raw) ?? "";
            if (id.Length == 0)
                return;
            bool self = IsSelfTarget(action, raw);
            string who = self ? "you" : "the target";
            bool triggered = action.Triggers?.TriggerConditions != null
                && action.Triggers.TriggerConditions.Any(c => !string.IsNullOrWhiteSpace(c));
            bool dot = raw is "bleed" or "poison" or "burn" or "acid";
            string when = dot && !triggered
                ? "On a critical hit, applies"
                : triggered
                    ? "When a trigger matches, applies"
                    : "Applies";
            string duration = DurationPhrase(id);
            string grant = string.IsNullOrEmpty(duration)
                ? $"{when} {DisplayName(id)} to {who}."
                : $"{when} {DisplayName(id)} to {who} for {duration}.";
            lines.Add(grant);
            string how = HowItWorks(id);
            if (!string.IsNullOrEmpty(how))
                lines.Add(how);
        }

        private static void AppendAlreadyOnYou(List<string> lines, Character hero, Action action, string effectId)
        {
            string id = (effectId ?? "").Trim().ToLowerInvariant();
            if (!IsSelfTarget(action, id) || lines.Count >= 8)
                return;
            string have = AlreadyOnYou(hero, id);
            if (!string.IsNullOrEmpty(have))
                lines.Add(have);
        }

        internal static bool IsSelfTarget(Action action, string effectId)
        {
            if (action.Target == TargetType.Self)
                return true;
            return action.Advanced?.SelfTargetEffects != null
                && action.Advanced.SelfTargetEffects.Any(e => string.Equals(e, effectId, StringComparison.OrdinalIgnoreCase));
        }

        private static string AlreadyOnYou(Character hero, string id) => id switch
        {
            "harden" when (hero.HardenStacks ?? 0) > 0 && hero.HardenTurns > 0
                => $"You already have Harden x{hero.HardenStacks} ({hero.HardenTurns} turns left).",
            "focus" when (hero.FocusStacks ?? 0) > 0 && hero.FocusTurns > 0
                => $"You already have Focus x{hero.FocusStacks} ({hero.FocusTurns} turns left).",
            "fortify" when (hero.FortifyStacks ?? 0) > 0 && hero.FortifyTurns > 0
                => $"You already have Fortify x{hero.FortifyStacks} ({hero.FortifyTurns} turns left).",
            "expose" or "vulnerability" or "armorbreak" when (hero.ExposeStacks ?? 0) > 0 && hero.ExposeTurns > 0
                => $"You already have Expose x{hero.ExposeStacks} ({hero.ExposeTurns} turns left).",
            _ => ""
        };

        public static string HowItWorks(string effectId)
        {
            var combat = GameConfiguration.Instance?.Combat;
            double harden = combat?.StatusHardenDefensePerStack ?? 0.15;
            double focus = combat?.StatusFocusDamagePerStack ?? 0.15;
            double expose = combat?.StatusExposeDefensePerStack ?? 0.15;
            int exposeCap = combat?.StatusMaxStacks ?? 3;
            double strip = combat?.StatusExposeArmorStripPerStack ?? 0.10;
            double stripMax = combat?.StatusExposeArmorStripMax ?? 0.30;
            string exposeText = $"Each stack removes {Pct(expose)} Defense. Below zero, hits hurt more."
                + (exposeCap > 0 && strip > 0
                    ? $" At {exposeCap} stacks, more Expose strips {Pct(strip)} of armor each (up to {Pct(stripMax)})."
                    : "");
            double fortify = combat?.StatusFortifyDefensePerStack ?? 0;

            return (effectId ?? "").Trim().ToLowerInvariant() switch
            {
                "harden" => $"Each stack adds {Pct(harden)} Defense, which reduces incoming damage.",
                "focus" => $"Each stack increases damage dealt by {Pct(focus)}.",
                "expose" or "vulnerability" or "armorbreak" => exposeText,
                "fortify" => fortify > 0
                    ? $"Each stack adds armor and {Pct(fortify)} Defense."
                    : "Each stack adds armor.",
                "bleed" => "Deals damage when the target acts, then fades.",
                "burn" => "Deals damage over time.",
                "temporaryhp" => "Grants 10 temporary HP per stack.",
                "mark" => "The next hit is a critical hit.",
                _ => ""
            };
        }

        private static string DurationPhrase(string id)
        {
            switch (id)
            {
                case "mark":
                    return "1 turn";
                case "bleed":
                case "burn":
                case "stagger":
                case "rattle":
                    return "";
                default:
                    return TurnPhrase(StackTurns);
            }
        }

        private static string TurnPhrase(int turns) => turns == 1 ? "1 turn" : $"{Math.Max(0, turns)} turns";

        private static string Pct(double fraction)
        {
            double percent = fraction * 100.0;
            string n = Math.Abs(percent % 1) < 0.05
                ? percent.ToString("0", CultureInfo.InvariantCulture)
                : percent.ToString("0.#", CultureInfo.InvariantCulture);
            return n + "%";
        }

        private static string DisplayName(string id) => id switch
        {
            "armorbreak" or "vulnerability" => "Expose",
            "temporaryhp" => "Temporary HP",
            CoreStatuses.Guard => "Guard",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id)
        };

        private static string EffectIdFromHudLine(string line)
        {
            if (Starts(line, "Bleed")) return "bleed";
            if (Starts(line, "Burn")) return "burn";
            if (Starts(line, "Crit miss")) return "";
            if (Starts(line, "Harden") || Starts(line, "Guard")) return "harden";
            if (Starts(line, "Fortify")) return "fortify";
            if (Starts(line, "Focus")) return "focus";
            if (Starts(line, "Expose")) return "expose";
            if (Starts(line, "Temp HP")) return "temporaryhp";
            if (Starts(line, "Marked")) return "mark";
            return "";
        }

        private static void AppendLooseLine(List<string> lines, string line)
        {
            if (line.IndexOf("Disadvantage", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                lines.Add("Roll 2d20 and keep the lowest.");
                return;
            }
            if (line.IndexOf("Advantage", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                lines.Add("Roll 2d20 and keep the highest.");
                return;
            }
            if (line.IndexOf("Accuracy", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                lines.Add("Added to the attack roll for the turns shown.");
                return;
            }
            if (Starts(line, "Rattle"))
            {
                lines.Add("Subtracted from its attack rolls for the turns shown.");
                return;
            }
            if (Starts(line, "Slow"))
            {
                lines.Add("Actions take longer for the turns shown.");
                return;
            }
            // Tempo statuses (TempoStatuses.cs): mechanics only, per-stack numbers from combat config.
            var combatCfg = GameConfiguration.Instance.Combat;
            if (Starts(line, "Stagger"))
            {
                lines.Add($"Its next action takes {Pct(combatCfg.StatusStaggerLengthPerStack)} longer per stack (it acts later), then Stagger is spent.");
                return;
            }
            if (Starts(line, "Haste"))
            {
                lines.Add($"Actions take {Pct(combatCfg.StatusHasteSpeedPerStack)} less time per stack; one stack falls off after each of its actions.");
                return;
            }
            if (line == "Shield")
            {
                lines.Add("The next hit deals half damage, then this is spent.");
                return;
            }
            if (line == "Skip turn")
            {
                lines.Add("You skip your next turn.");
                return;
            }
            if (line == "Guarantee hit")
            {
                lines.Add("Your next attack hits.");
                return;
            }
            if (Starts(line, "Extra atk"))
            {
                lines.Add("Extra attacks are queued.");
                return;
            }
            if (line == "Combo mode")
            {
                lines.Add("Combo routing is active.");
                return;
            }
            if (Starts(line, "Reroll"))
            {
                lines.Add("You can reroll an attack die.");
                return;
            }
            if (Starts(line, "Fight:"))
            {
                lines.Add("These bonuses last for the rest of the fight.");
                return;
            }
            if (Starts(line, "Dungeon:"))
            {
                lines.Add("These bonuses last until you leave the dungeon.");
                return;
            }
            if (Starts(line, "Next action") || Starts(line, "Action "))
            {
                lines.Add("Applies on that swing, then it is spent.");
                return;
            }
            if (line.IndexOf("TURN", StringComparison.Ordinal) >= 0)
            {
                lines.Add("Temporary bonus for the turns shown.");
                return;
            }
            if (Starts(line, "Crit miss"))
            {
                lines.Add("A critical-miss penalty is active for the turns shown.");
                return;
            }
            lines.Add("Active for the time shown on this row.");
        }

        public static string PresentDescription(string? raw)
        {
            string text = (raw ?? "").Trim();
            if (text.Length == 0)
                return "";
            int letters = 0;
            int upper = 0;
            foreach (char c in text)
            {
                if (!char.IsLetter(c))
                    continue;
                letters++;
                if (char.IsUpper(c))
                    upper++;
            }
            if (letters == 0 || upper != letters || text.IndexOf(' ') < 0)
                return text;
            string lower = text.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        private static bool Starts(string line, string word) =>
            line.StartsWith(word, StringComparison.OrdinalIgnoreCase)
            && (line.Length == word.Length || !char.IsLetter(line[word.Length]));
    }
}
