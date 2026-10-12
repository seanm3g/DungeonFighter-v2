using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Events;

namespace RPGGame
{
    /// <summary>
    /// Hybrid-tree effects: unique actions (+25% of the hybrid's bonus per 10 of its secondary attribute or of its
    /// hybrid tags, × node power) and accumulation (+1 attribute / tag per connect this fight).
    /// </summary>
    public sealed partial class SkillEffectRouter
    {
        private enum HybridBonus { Damage, Speed, Amp, Accuracy, Combo, Status }

        /// <summary>Percent of the hybrid bonus per 10 units of the scaling source (the design board's +25%).</summary>
        public const double HybridBonusPerTen = 25.0;

        private static readonly (string Action, string Hybrid, bool FromTags, HybridBonus Bonus)[] HybridActions =
        {
            ("BODY BLOW", "Warbrute", false, HybridBonus.Damage),
            ("WARBRUTE SMASH", "Warbrute", true, HybridBonus.Damage),
            ("PROWESS RUSH", "Ravager", false, HybridBonus.Speed),
            ("RAVAGER FLURRY", "Ravager", true, HybridBonus.Speed),
            ("WILLFIRE", "Ragecaller", false, HybridBonus.Amp),
            ("RAGECALLER HOWL", "Ragecaller", true, HybridBonus.Amp),
            ("FINESSE THRUST", "Duelist", false, HybridBonus.Accuracy),
            ("DUELIST FEINT", "Duelist", true, HybridBonus.Accuracy),
            ("WITS ARC", "Spellblade", false, HybridBonus.Combo),
            ("SPELLBLADE WEAVE", "Spellblade", true, HybridBonus.Combo),
            ("MIND HEX", "Hexblade", false, HybridBonus.Status),
            ("HEXBLADE CURSE", "Hexblade", true, HybridBonus.Status)
        };

        private readonly Dictionary<string, double> _hybridAttributeAccum = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _hybridTagAccum = new(StringComparer.OrdinalIgnoreCase);
        private int _pendingComboSteps;

        public static string HybridActionEffect(HybridInfo h, bool fromTags) =>
            $"{h.Tag}_action_{(fromTags ? "tag" : "attribute")}";

        public int GetHybridAttributeAccumulation(Character c, HybridInfo h) =>
            ReferenceEquals(c, _boundCharacter) ? (int)Math.Floor(_hybridAttributeAccum.GetValueOrDefault(h.Key)) : 0;

        public int GetHybridTagAccumulation(Character c, HybridInfo h) =>
            ReferenceEquals(c, _boundCharacter) ? (int)Math.Floor(_hybridTagAccum.GetValueOrDefault(h.Key)) : 0;

        /// <summary>Combo-threshold steps banked by a Spellblade action, spent on the hero's next attack.</summary>
        public int ConsumePendingComboSteps(Character hero)
        {
            if (!ReferenceEquals(hero, _boundCharacter) || _pendingComboSteps <= 0) return 0;
            int steps = _pendingComboSteps;
            _pendingComboSteps = 0;
            return steps;
        }

        private void ResetHybridFightState()
        {
            _hybridAttributeAccum.Clear();
            _hybridTagAccum.Clear();
            _pendingComboSteps = 0;
        }

        private void ApplyHybridOnHit(Character hero, CombatEvent evt)
        {
            foreach (var h in HybridTrees.All)
            {
                double attr = GetPower(hero, h.AccumulateAttributeEffect);
                if (attr > 0)
                    _hybridAttributeAccum[h.Key] = _hybridAttributeAccum.GetValueOrDefault(h.Key) + attr;
                double tag = GetPower(hero, h.AccumulateTagEffect);
                if (tag > 0)
                    _hybridTagAccum[h.Key] = _hybridTagAccum.GetValueOrDefault(h.Key) + tag;
            }

            string? name = evt.Action?.Name;
            if (string.IsNullOrWhiteSpace(name)) return;
            foreach (var (action, hybridKey, fromTags, bonus) in HybridActions)
            {
                if (!string.Equals(action, name, StringComparison.OrdinalIgnoreCase)) continue;
                var h = HybridTrees.ByKey(hybridKey);
                if (h == null) return;
                double power = GetPower(hero, HybridActionEffect(h, fromTags));
                if (power <= 0) return;
                int source = fromTags ? HybridTrees.TagCount(hero, h) : HybridTrees.SecondaryAttribute(hero, h);
                double pct = HybridBonusPerTen * Math.Floor(source / 10.0) * power;
                if (pct <= 0) return;
                int steps = Math.Max(1, (int)Math.Round(pct / HybridBonusPerTen));
                switch (bonus)
                {
                    case HybridBonus.Damage: AddDamageMod(hero, pct); break;
                    case HybridBonus.Speed: AddSpeedMod(hero, pct); break;
                    case HybridBonus.Amp: AddAmpMod(hero, pct); break;
                    case HybridBonus.Accuracy:
                        hero.Effects.SetTempRollBonus(Math.Max(hero.Effects.GetTempRollBonus(), steps), 1);
                        break;
                    case HybridBonus.Combo:
                        _pendingComboSteps += steps;
                        break;
                    case HybridBonus.Status:
                        if (evt.Target != null)
                        {
                            evt.Target.VulnerabilityStacks = (evt.Target.VulnerabilityStacks ?? 0) + steps;
                            evt.Target.VulnerabilityTurns = Math.Max(evt.Target.VulnerabilityTurns, 2);
                            if (evt.Target is Character weakened)
                                weakened.ApplyWeaken(2);
                        }
                        break;
                }
                return;
            }
        }
    }
}
