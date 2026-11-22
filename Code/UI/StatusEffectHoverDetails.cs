using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Combat;

namespace RPGGame
{
    /// <summary>
    /// Long-form status-effect hover copy: what the status is, exactly how it ticks or stacks, its live numbers on
    /// this actor, what applied it, how it ends, and (for enemies) any tag immunity / weakness / resistance.
    /// </summary>
    public static class StatusEffectHoverDetails
    {
        /// <summary>Enemy whose right-panel status rows are hoverable this frame (set by the right panel renderer).</summary>
        public static Enemy? CurrentEnemy { get; set; }

        private static string Fmt(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static double TickSeconds()
        {
            double interval = GameConfiguration.Instance?.Poison?.TickInterval ?? 5.0;
            return interval > 0 ? interval : 5.0;
        }

        private static string Turns(int n) => n == 1 ? "1 turn" : $"{n} turns";

        /// <summary>Title-case display name for an effect id ("hpregen" → "HP Regen"). Vulnerability / Armor Break show as Exposed (folded in).</summary>
        public static string DisplayName(string effectId) => (effectId ?? "").ToLowerInvariant() switch
        {
            "armorbreak" or "vulnerability" or "expose" => "Exposed",
            "temporaryhp" => "Temporary HP",
            "mark" => "Marked",
            "" => "",
            string s => char.ToUpperInvariant(s[0]) + s.Substring(1)
        };

        /// <summary>
        /// Detail lines for one active status on <paramref name="actor"/>. <paramref name="effectId"/> is the canonical id
        /// (burn, bleed, expose, harden, …).
        /// </summary>
        /// <param name="hero">The player, so an enemy row can list the hero actions that inflict it (hero rows use the actor).</param>
        public static List<string> Describe(Actor? actor, string effectId, Character? hero = null)
        {
            var lines = new List<string>();
            string id = (effectId ?? "").Trim().ToLowerInvariant();
            if (id.Length == 0)
                return lines;

            string what = WhatItIs(id);
            if (!string.IsNullOrEmpty(what))
                lines.Add(what);

            string how = HoverEffectCopy.HowItWorks(id);
            string mechanics = Mechanics(actor, id);
            if (!string.IsNullOrEmpty(mechanics))
                lines.Add("How it works: " + mechanics);
            else if (!string.IsNullOrEmpty(how))
                lines.Add("How it works: " + how);

            string now = RightNow(actor, id);
            if (!string.IsNullOrEmpty(now))
                lines.Add("Right now: " + now);

            string source = actor?.GetStatusSource(id) ?? "";
            if (!string.IsNullOrWhiteSpace(source))
                lines.Add($"Applied by: {source}.");

            string comesFrom = DescribeSources(actor, id, hero);
            if (!string.IsNullOrEmpty(comesFrom))
                lines.Add(comesFrom);

            string ends = HowItEnds(id);
            if (!string.IsNullOrEmpty(ends))
                lines.Add("Ends: " + ends);

            if (actor is Enemy enemy)
            {
                string affinity = DescribeAffinity(enemy, id);
                if (!string.IsNullOrEmpty(affinity))
                    lines.Add(affinity);
            }
            return lines;
        }

        /// <summary>
        /// "Comes from:" line naming what can put <paramref name="id"/> on <paramref name="bearer"/> this fight: the bearer's
        /// own self-target actions, the other side's actions that hit with it, and the hero's weapon DoT mods.
        /// </summary>
        private static string DescribeSources(Actor? bearer, string id, Character? hero)
        {
            if (bearer == null)
                return "";
            bool bearerIsHero = bearer is Character && bearer is not Enemy;
            Character? player = bearerIsHero ? (Character)bearer : hero;
            Enemy? foe = bearer as Enemy ?? CurrentEnemy;

            var parts = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Scan(IEnumerable<Action> actions, bool wantSelf, string owner)
            {
                foreach (var action in actions)
                {
                    if (action == null || string.IsNullOrWhiteSpace(action.Name) || !seen.Add(owner + action.Name))
                        continue;
                    if (!StatusEffectActionResolver.GetEffectTypesFromAction(action)
                            .Any(e => string.Equals(e, id, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    if (HoverEffectCopy.IsSelfTarget(action, id) == wantSelf)
                        parts.Add($"{owner} {action.Name}");
                }
            }

            if (player != null)
            {
                var heroActions = player.GetComboActions().Concat(player.ActionPool.Select(e => e.action));
                Scan(heroActions, wantSelf: bearerIsHero, owner: "your");
            }
            if (foe != null)
                Scan(foe.ActionPool.Select(e => e.action), wantSelf: !bearerIsHero, owner: bearerIsHero ? "enemy's" : "its");

            if (!bearerIsHero && player != null)
            {
                string weapon = id switch
                {
                    "bleed" when player.GetWeaponBleedPerHit() > 0 => $"your weapon mods (+{player.GetWeaponBleedPerHit()} on crit)",
                    "burn" when player.GetWeaponBurnPerHit() > 0 => $"your weapon mods (+{player.GetWeaponBurnPerHit()} on crit)",
                    _ => ""
                };
                if (weapon.Length > 0)
                    parts.Add(weapon);
            }

            if (parts.Count == 0)
                return "Comes from: no action in your kit or this enemy's applies it (gear proc, skill, room or dungeon effect).";
            const int maxShown = 4;
            string list = string.Join(", ", parts.Take(maxShown));
            return parts.Count > maxShown ? $"Comes from: {list} +{parts.Count - maxShown} more." : $"Comes from: {list}.";
        }

        /// <summary>One-line affinity note for an enemy and status ("Weak to Burn ×2 — Wood and leaf burn readily (plant)").</summary>
        public static string DescribeAffinity(Enemy enemy, string effectId)
        {
            var r = StatusAffinity.Resolve(enemy, effectId);
            if (r.Sources.Count == 0 || Math.Abs(r.Multiplier - 1.0) < 0.0001)
                return "";
            // Only list rules that push the same way as the result (an immune hover should not cite a weakness note).
            var reasons = r.Sources.Where(s => r.IsImmune ? s.Multiplier <= 0
                : r.IsWeak ? s.Multiplier > 1.0
                : s.Multiplier < 1.0).ToList();
            if (reasons.Count == 0)
                reasons = r.Sources.ToList();
            string why = string.Join("; ", reasons.Select(s =>
                string.IsNullOrWhiteSpace(s.Note) ? s.Tag : $"{s.Note} ({s.Tag})"));
            string name = DisplayName(r.Status);
            if (r.IsImmune)
                return $"Immune to {name}: it is never applied. {why}.";
            if (r.IsWeak)
                return $"Weak to {name}: ticks deal ×{Fmt(r.Multiplier)} damage. {why}.";
            return $"Resists {name}: ticks deal ×{Fmt(r.Multiplier)} damage. {why}.";
        }

        /// <summary>Hover lines for an enemy affinity row ("Immune: Bleed, Poison").</summary>
        public static List<string> DescribeAffinityRow(Enemy enemy, string row)
        {
            var lines = new List<string>();
            string kind = row.Split(':')[0].Trim();
            lines.Add(kind switch
            {
                "Immune" => "Immune statuses are never applied to this enemy, so they never show in the log.",
                "Weak" => "Weak statuses deal extra damage each tick on this enemy.",
                "Resists" => "Resisted statuses deal reduced damage each tick on this enemy.",
                _ => "Status affinities come from this enemy's tags."
            });
            foreach (var a in StatusAffinity.DescribeAll(enemy))
            {
                bool match = kind switch
                {
                    "Immune" => a.IsImmune,
                    "Weak" => a.IsWeak,
                    "Resists" => a.IsResistant,
                    _ => true
                };
                if (match)
                    lines.Add(DescribeAffinity(enemy, a.Status));
            }
            var tags = StatusAffinity.EffectiveTags(enemy).Where(t => !t.StartsWith("has_", StringComparison.OrdinalIgnoreCase)).ToList();
            if (tags.Count > 0)
                lines.Add("Tags: " + string.Join(", ", tags));
            return lines;
        }

        private static string WhatItIs(string id) => id switch
        {
            "burn" => "Burn: flames that hurt every tick and slowly die down.",
            "bleed" => "Bleed: an open wound that hurts each time the victim acts.",
            "harden" => "Guard: braced to take hits.",
            "fortify" => "Fortified: armor reinforced.",
            "focus" => "Focused: strikes are lined up for extra damage.",
            "expose" or "vulnerability" or "armorbreak" => "Exposed: guard is down; Defense is reduced, and at full stacks armor is stripped.",
            "temporaryhp" => "Temporary HP: a buffer that is lost before real HP.",
            "mark" => "Marked: the next hit is lined up as a critical.",
            _ => ""
        };

        private static string Mechanics(Actor? actor, string id)
        {
            double tick = TickSeconds();
            double dotPerLevel = GameConfiguration.Instance?.Combat?.DotDamagePerLevel ?? 0;
            string levelNote = dotPerLevel > 0 ? $" Tick damage grows {Fmt(dotPerLevel * 100)}% per victim level above 1." : "";
            return id switch
            {
                "burn" => $"Every {Fmt(tick)}s it deals damage equal to its intensity, then intensity drops by 1. New burn adds intensity on the next tick.{levelNote}",
                "bleed" => $"When the victim finishes a turn it takes damage equal to intensity, then intensity drops by 1. Fast attackers bleed out faster.{levelNote}",
                _ => ""
            };
        }

        private static string RightNow(Actor? actor, string id)
        {
            if (actor == null)
                return "";
            switch (id)
            {
                case "burn":
                {
                    int n = actor.BurnIntensity + actor.PendingBurnFromHits;
                    return $"intensity {n} (≈{ScaleForActor(actor, "burn", Math.Max(actor.BurnIntensity, actor.PendingBurnFromHits))} damage next tick, about {SumDecay(n)} total if left alone).";
                }
                case "bleed":
                {
                    int n = actor.BleedIntensity + actor.PendingBleedFromHits;
                    return $"intensity {n} (≈{ScaleForActor(actor, "bleed", Math.Max(actor.BleedIntensity, actor.PendingBleedFromHits))} damage after their next turn, about {SumDecay(n)} total).";
                }
                case "harden":
                    return $"{actor.HardenStacks} stack(s), {Turns(actor.HardenTurns)} left.";
                case "fortify":
                    return $"{actor.FortifyStacks} stack(s), {Turns(actor.FortifyTurns)} left.";
                case "focus":
                    return $"{actor.FocusStacks} stack(s), {Turns(actor.FocusTurns)} left.";
                case "expose":
                case "vulnerability":
                case "armorbreak":
                    return $"{actor.ExposeStacks} stack(s), {Turns(actor.ExposeTurns)} left."
                        + (ExposeStatus.IsArmorStripped(actor) ? $" Armor stripped {(int)Math.Round(actor.ExposeArmorStrip * 100)}%." : "");
                case "temporaryhp":
                    return $"{actor.TemporaryHP} temporary HP, {Turns(actor.TemporaryHPTurns)} left.";
                case "mark":
                    return $"{Turns(actor.MarkTurns)} left.";
                default:
                    return "";
            }
        }

        private static int ScaleForActor(Actor actor, string id, int dmg) =>
            actor is Enemy e ? StatusAffinity.ScaleTick(e, id, dmg) : dmg;

        /// <summary>n + (n-1) + … + 1: total of a decaying intensity left alone.</summary>
        private static int SumDecay(int n) => n <= 0 ? 0 : n * (n + 1) / 2;

        private static string HowItEnds(string id) => id switch
        {
            "burn" or "bleed" => "when intensity decays to 0, or the fight ends.",
            "vulnerability" or "harden" or "fortify" or "focus" or "expose" or "armorbreak" or "temporaryhp"
                => "when its turns run out (counted on the victim's turns).",
            "mark" => "after the next hit lands, or when its turns run out.",
            _ => ""
        };
    }
}
