using System;
using System.Collections.Generic;
using System.Globalization;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>One player-facing line for an external (class / material / WHILE_EQUIPPED) action-card bonus.</summary>
    public readonly struct ActionCardBonusLine
    {
        public string Text { get; }
        public bool Beneficial { get; }

        public ActionCardBonusLine(string text, bool beneficial)
        {
            Text = text ?? "";
            Beneficial = beneficial;
        }
    }

    /// <summary>
    /// Standing bonuses that currently apply to a strip action: class passives, material convert scale,
    /// and WHILE_EQUIPPED same-swing tag amps. Used for card text and preview math.
    /// </summary>
    public sealed class ActionCardExternalBonusSnapshot
    {
        public double DamageModPercent { get; set; }
        public double SpeedModPercent { get; set; }
        public double AmpModPercent { get; set; }
        public double MultiHitMod { get; set; }
        public double ConvertMultiplier { get; set; } = 1.0;
        public List<ActionCardBonusLine> Lines { get; } = new();
    }

    public static class ActionCardExternalBonusCollector
    {
        /// <summary>Collects standing class / material / equipped-tag bonuses for one strip (or pool) action.</summary>
        public static ActionCardExternalBonusSnapshot Collect(Character? character, Action? action, int comboSlotIndex)
        {
            var snap = new ActionCardExternalBonusSnapshot();
            if (character == null || character is Enemy || action == null)
                return snap;

            foreach (var bonus in SkillEffectRouter.Instance.CollectActionCardBonuses(character, action, comboSlotIndex))
                AddSkillBonus(snap, bonus);

            double convert = MaterialSetController.GetConvertDamageMultiplier(character, action);
            if (convert > 1.0001)
            {
                snap.ConvertMultiplier = convert;
                string? convertLine = MaterialSetController.FormatConvertScaleLine(character, action);
                if (!string.IsNullOrWhiteSpace(convertLine))
                    snap.Lines.Add(new ActionCardBonusLine(convertLine, true));
            }

            foreach (var (source, mechanicId, magnitude) in EquippedItemTriggerApplicator.CollectWhileEquippedSameSwingPreview(character, action, comboSlotIndex))
                AddItemSameSwing(snap, source, mechanicId, magnitude);

            return snap;
        }

        /// <summary>Player-facing lines for the action card / tooltip Stats band.</summary>
        public static List<ActionCardBonusLine> BuildLines(Character? character, Action? action, int comboSlotIndex) =>
            Collect(character, action, comboSlotIndex).Lines;

        private static void AddSkillBonus(ActionCardExternalBonusSnapshot snap, SkillActionCardBonus bonus)
        {
            if (Math.Abs(bonus.Value) < 0.0001 || string.IsNullOrWhiteSpace(bonus.SourceName))
                return;

            switch ((bonus.StatKey ?? "").Trim().ToLowerInvariant())
            {
                case "dmg":
                case "damage":
                    snap.DamageModPercent += bonus.Value;
                    snap.Lines.Add(new ActionCardBonusLine($"{bonus.SourceName} {FormatSignedPercent(bonus.Value)} dmg", bonus.Value >= 0));
                    break;
                case "spd":
                case "speed":
                    snap.SpeedModPercent += bonus.Value;
                    snap.Lines.Add(new ActionCardBonusLine($"{bonus.SourceName} {FormatSignedPercent(bonus.Value)} spd", bonus.Value >= 0));
                    break;
                case "amp":
                    snap.AmpModPercent += bonus.Value;
                    snap.Lines.Add(new ActionCardBonusLine($"{bonus.SourceName} {FormatSignedPercent(bonus.Value)} amp", bonus.Value >= 0));
                    break;
                case "multihit":
                    snap.MultiHitMod += bonus.Value;
                    snap.Lines.Add(new ActionCardBonusLine($"{bonus.SourceName} Multihit {FormatSignedNumber(bonus.Value)}", bonus.Value >= 0));
                    break;
            }
        }

        private static void AddItemSameSwing(ActionCardExternalBonusSnapshot snap, string source, string mechanicId, double magnitude)
        {
            if (Math.Abs(magnitude) < 0.0001)
                return;
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId ?? "");
            string label = string.IsNullOrWhiteSpace(source) ? "Gear" : source;
            switch (id)
            {
                case "hero_action_damage":
                    snap.DamageModPercent += magnitude;
                    snap.Lines.Add(new ActionCardBonusLine($"{label} {FormatSignedPercent(magnitude)} dmg", magnitude >= 0));
                    break;
                case "hero_action_speed":
                    snap.SpeedModPercent += magnitude;
                    snap.Lines.Add(new ActionCardBonusLine($"{label} {FormatSignedPercent(magnitude)} spd", magnitude >= 0));
                    break;
                case "hero_action_amp":
                    snap.AmpModPercent += magnitude;
                    snap.Lines.Add(new ActionCardBonusLine($"{label} {FormatSignedPercent(magnitude)} amp", magnitude >= 0));
                    break;
            }
        }

        private static string FormatSignedPercent(double value)
        {
            string n = value.ToString("0.#", CultureInfo.InvariantCulture);
            return value >= 0 ? $"+{n}%" : $"{n}%";
        }

        private static string FormatSignedNumber(double value)
        {
            string n = value.ToString("0.#", CultureInfo.InvariantCulture);
            return value >= 0 ? $"+{n}" : n;
        }
    }
}
