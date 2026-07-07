using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Merges ACTION cadence bonus deposits additively by normalized bonus type.
    /// Multiple grants stack into one bank redeemed on the next hit+combo.
    /// </summary>
    public static class ActionCadenceBonusBank
    {
        public static void MergeAdditively(List<ActionAttackBonusItem> bank, IEnumerable<ActionAttackBonusItem>? incoming, int stackTimes = 1)
        {
            if (bank == null || incoming == null || stackTimes < 1)
                return;

            foreach (var b in incoming)
            {
                if (b == null) continue;
                string type = ActionAttackBonusItem.NormalizeBonusType(b.Type);
                if (string.IsNullOrEmpty(type)) continue;

                double delta = b.Value * stackTimes;
                var existing = bank.FirstOrDefault(x =>
                    string.Equals(ActionAttackBonusItem.NormalizeBonusType(x.Type), type, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                    existing.Value += delta;
                else
                    bank.Add(new ActionAttackBonusItem { Type = type, Value = delta });
            }
        }

        public static List<ActionAttackBonusItem> Copy(IReadOnlyList<ActionAttackBonusItem> bank)
        {
            if (bank == null || bank.Count == 0)
                return new List<ActionAttackBonusItem>();
            return bank.Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value }).ToList();
        }
    }
}
