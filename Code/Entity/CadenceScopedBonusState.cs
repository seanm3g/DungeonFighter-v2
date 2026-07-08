using System;
using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Additive cadence bonuses that persist for a scope (fight or dungeon run) without per-roll consumption.
    /// </summary>
    public sealed class CadenceScopedBonusState
    {
        private readonly List<ActionAttackBonusItem> _bonuses = new();

        public bool HasAny => _bonuses.Count > 0;

        public IReadOnlyList<ActionAttackBonusItem> Bonuses => _bonuses;

        public void Clear() => _bonuses.Clear();

        public void MergeAdditively(IEnumerable<ActionAttackBonusItem>? incoming, int stackTimes = 1)
        {
            ActionCadenceBonusBank.MergeAdditively(_bonuses, incoming, stackTimes);
        }

        public List<ActionAttackBonusItem> CopyBonuses() => ActionCadenceBonusBank.Copy(_bonuses);

        public int GetStatBonus(string statCode)
        {
            int sum = 0;
            foreach (var b in _bonuses)
            {
                if (string.Equals(ActionAttackBonusItem.NormalizeBonusType(b.Type), statCode, StringComparison.OrdinalIgnoreCase))
                    sum += (int)b.Value;
            }
            return sum;
        }

        public int StrengthBonus => GetStatBonus("STR");
        public int AgilityBonus => GetStatBonus("AGI");
        public int TechniqueBonus => GetStatBonus("TECH");
        public int IntelligenceBonus => GetStatBonus("INT");

        public double GetModifierPercent(string modType)
        {
            double sum = 0;
            foreach (var b in _bonuses)
            {
                if (string.Equals(ActionAttackBonusItem.NormalizeBonusType(b.Type), modType, StringComparison.OrdinalIgnoreCase))
                    sum += b.Value;
            }
            return sum;
        }
    }
}
