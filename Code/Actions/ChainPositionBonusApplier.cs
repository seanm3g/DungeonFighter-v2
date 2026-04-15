using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Applies <see cref="ChainPositionBonusEntry"/> when chain-position mods are enabled on the action.
    /// </summary>
    public static class ChainPositionBonusApplier
    {
        public static bool IsModifyChainPositionEnabled(ComboRoutingProperties? routing)
        {
            if (routing == null) return false;
            var s = (routing.ModifyBasedOnChainPosition ?? "").Trim();
            if (string.IsNullOrEmpty(s)) return false;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) || s == "0")
                return false;
            return true;
        }

        /// <summary>
        /// Integer accuracy bonus from chain entries (hero <c>Accuracy</c> / legacy <c>RollBonus</c>;
        /// enemy <c>EnemyAccuracy</c> / legacy <c>EnemyRollBonus</c>).
        /// </summary>
        public static int GetChainAccuracyDelta(Actor attacker, Action? action, List<Action> comboActions, int comboStep)
        {
            if (action == null || !IsModifyChainPositionEnabled(action.ComboRouting))
                return 0;

            var list = action.ComboRouting.ChainPositionBonuses;
            if (list == null || list.Count == 0)
                return 0;

            bool isEnemy = attacker is Enemy;
            int total = 0;
            foreach (var e in list)
            {
                var param = (e.ModifiesParam ?? "").Trim();
                if (isEnemy)
                {
                    if (!IsEnemyAccuracyParam(param))
                        continue;
                }
                else
                {
                    if (!IsHeroAccuracyParam(param))
                        continue;
                }

                double pos = GetPositionFactor(e.PositionBasis, attacker, action, comboActions, comboStep);
                total += (int)Math.Round(e.Value * pos);
            }

            return total;
        }

        private static bool IsHeroAccuracyParam(string param) =>
            string.Equals(param, "Accuracy", StringComparison.OrdinalIgnoreCase)
            || string.Equals(param, "RollBonus", StringComparison.OrdinalIgnoreCase)
            || string.Equals(param, "ROLL", StringComparison.OrdinalIgnoreCase);

        private static bool IsEnemyAccuracyParam(string param) =>
            string.Equals(param, "EnemyAccuracy", StringComparison.OrdinalIgnoreCase)
            || string.Equals(param, "EnemyRollBonus", StringComparison.OrdinalIgnoreCase)
            || string.Equals(param, "ROLL", StringComparison.OrdinalIgnoreCase);

        /// <summary>UI label for <see cref="ChainPositionBonusEntry.ModifiesParam"/> (maps legacy RollBonus → Accuracy).</summary>
        public static string GetDisplayNameForModifiesParam(string? modifiesParam)
        {
            var p = (modifiesParam ?? "").Trim();
            if (string.Equals(p, "RollBonus", StringComparison.OrdinalIgnoreCase)) return "Accuracy";
            if (string.Equals(p, "EnemyRollBonus", StringComparison.OrdinalIgnoreCase)) return "EnemyAccuracy";
            if (string.Equals(p, "ROLL", StringComparison.OrdinalIgnoreCase)) return "Accuracy";
            return p;
        }

        /// <summary>
        /// Adjusts combo damage multiplier from chain Damage entries: flat (#) adds to multiplier; percent (%) compounds.
        /// </summary>
        public static double AdjustComboDamageMultiplier(double baseComboMult, Actor source, Action action, List<Action> comboActions, int comboStep)
        {
            if (action == null || !IsModifyChainPositionEnabled(action.ComboRouting))
                return baseComboMult;

            var list = action.ComboRouting.ChainPositionBonuses;
            if (list == null || list.Count == 0)
                return baseComboMult;

            double additive = 0;
            double multiplicative = 1.0;
            foreach (var e in list)
            {
                if (!string.Equals((e.ModifiesParam ?? "").Trim(), "Damage", StringComparison.OrdinalIgnoreCase))
                    continue;
                double pos = GetPositionFactor(e.PositionBasis, source, action, comboActions, comboStep);
                var kind = (e.ValueKind ?? "#").Trim();
                if (string.Equals(kind, "%", StringComparison.OrdinalIgnoreCase))
                    multiplicative *= 1.0 + (e.Value / 100.0) * pos;
                else
                    additive += e.Value * pos;
            }

            return (baseComboMult + additive) * multiplicative;
        }

        public static int GetMultiHitDelta(Actor source, Action action, List<Action> comboActions, int comboStep)
        {
            if (action == null || !IsModifyChainPositionEnabled(action.ComboRouting))
                return 0;

            var list = action.ComboRouting.ChainPositionBonuses;
            if (list == null || list.Count == 0)
                return 0;

            int total = 0;
            foreach (var e in list)
            {
                if (!string.Equals((e.ModifiesParam ?? "").Trim(), "MultiHit", StringComparison.OrdinalIgnoreCase))
                    continue;
                double pos = GetPositionFactor(e.PositionBasis, source, action, comboActions, comboStep);
                total += (int)Math.Round(e.Value * pos);
            }

            return Math.Max(0, total);
        }

        private static double GetPositionFactor(string? positionBasis, Actor source, Action action, List<Action> comboActions, int comboStep)
        {
            var safeCombo = comboActions ?? new List<Action>();
            int n = safeCombo.Count;
            if (n <= 0)
                return 0;

            var basis = (positionBasis ?? "").Trim();
            if (string.Equals(basis, "ComboSlotIndex1", StringComparison.OrdinalIgnoreCase))
                return (comboStep % n) + 1;

            if (string.Equals(basis, "AmpTier", StringComparison.OrdinalIgnoreCase))
                return ActionUtilities.GetComboAmplificationExponent(source, action, safeCombo);

            // Explicit 0-based slot index as coefficient (opener = 0, second slot = 1, …).
            if (string.Equals(basis, "ComboSlotIndex0", StringComparison.OrdinalIgnoreCase))
                return comboStep % n;

            // Unset / legacy empty: 1-based slot (first slot = 1, second = 2, …) so a flat bonus applies on the opener.
            return (comboStep % n) + 1;
        }
    }
}
