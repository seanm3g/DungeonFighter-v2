using System;
using System.Collections.Generic;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Skills that fire when the hero lands a status on an enemy (<see cref="CombatEventType.StatusEffectApplied"/>,
    /// published once per status a hit adds or stacks higher; see <see cref="Combat.StatusApplySnapshot"/>).
    /// Each grants the class's primary attribute for the rest of the dungeon, per rank x node power.
    /// </summary>
    public sealed partial class SkillEffectRouter
    {
        /// <summary>customEffectId, statuses that trigger it, buff code, base points per rank.</summary>
        private static readonly (string Effect, string[] Statuses, string StatCode, double PerRank)[] StatusTriggerSkills =
        {
            ("blood_frenzy", new[] { "bleed", "stun" }, DynamicAttributeCategoryResolver.CodeStrength, 6),
            ("pinning_drill", new[] { "slow", "weaken" }, DynamicAttributeCategoryResolver.CodeAgility, 8),
            ("toxic_insight", new[] { "poison", "expose" }, DynamicAttributeCategoryResolver.CodeTechnique, 1),
            ("kindled_mind", new[] { "burn", "expose" }, DynamicAttributeCategoryResolver.CodeIntelligence, 1),
        };

        /// <summary>Attribute points status-trigger skills have granted, never reset (sim fight logs diff it).</summary>
        public static long StatusSkillPointsGranted { get; private set; }

        private readonly Dictionary<string, double> _statusTriggerCarry = new(StringComparer.OrdinalIgnoreCase);

        private void ResetStatusTriggerFightState() => _statusTriggerCarry.Clear();

        private void OnStatusApplied(CombatEvent evt)
        {
            var hero = AsBoundHero(evt.Source);
            if (hero == null || string.IsNullOrEmpty(evt.StatusEffectType) || evt.Target is not Enemy) return;
            using var skillPin = SkillRanks.Pin(hero.Progression);
            foreach (var (effect, statuses, statCode, perRank) in StatusTriggerSkills)
            {
                if (Array.IndexOf(statuses, evt.StatusEffectType!.ToLowerInvariant()) < 0) continue;
                double power = GetPower(hero, effect);
                if (power <= 0) continue;
                GrantFightStat(hero, effect, statCode, perRank * power);
            }
        }

        /// <summary>Whole points go on the fight buff now; fractions carry to the next trigger in the same fight.</summary>
        private void GrantFightStat(Character hero, string effect, string statCode, double points)
        {
            double total = _statusTriggerCarry.GetValueOrDefault(effect) + points;
            int whole = (int)Math.Floor(total);
            _statusTriggerCarry[effect] = total - whole;
            if (whole <= 0) return;
            StatusSkillPointsGranted += whole;
            hero.DungeonCadenceBuffs.MergeAdditively(new[] { new ActionAttackBonusItem { Type = statCode, Value = whole } });
        }
    }
}
