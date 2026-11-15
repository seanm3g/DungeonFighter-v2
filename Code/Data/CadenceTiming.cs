using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Optional timing on a cadence bundle (<c>cadenceBundlesJson[].timing</c>) and the bonus group built from it:
    /// when, where on the strip and in what shape a TURN / ACTION grant lands. Empty = the plain cadence.
    /// <list type="bullet">
    /// <item><c>slot</c> (ACTION): the strip slot the grant lands on: <c>1</c>..<c>n</c> (1-based), <c>first</c>, <c>last</c>.
    /// Default: the slot after this action.</item>
    /// <item><c>delay</c>: ACTION skips that many slots (<c>1</c> = the action after next); TURN waits that many rolls.</item>
    /// <item><c>shape</c>: <c>ramp</c> (×1, ×2, ×3 … over the duration), <c>decay</c> (×N … ×1), <c>charge</c>
    /// (ACTION only: all N copies on one action, the pre-2026-11-15 ACTION ×N behavior).</item>
    /// <item><c>every</c> (TURN): only every n-th roll of the duration gets the bonus.</item>
    /// <item><c>perSlot</c>: values × this action's 1-based strip slot.</item>
    /// <item><c>slotIf</c>: grant only when this action sits in a matching slot: <c>3</c>, <c>&gt;=3</c>, <c>&lt;=2</c>,
    /// <c>first</c>, <c>last</c>, <c>odd</c>, <c>even</c>.</item>
    /// <item><c>untilMiss</c> (TURN): a miss ends the grant early.</item>
    /// </list>
    /// </summary>
    public sealed class CadenceTiming
    {
        [JsonPropertyName("slot")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Slot { get; set; }

        [JsonPropertyName("delay")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Delay { get; set; }

        [JsonPropertyName("shape")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Shape { get; set; }

        [JsonPropertyName("every")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Every { get; set; }

        [JsonPropertyName("perSlot")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool PerSlot { get; set; }

        [JsonPropertyName("slotIf")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SlotIf { get; set; }

        [JsonPropertyName("untilMiss")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool UntilMiss { get; set; }

        [JsonIgnore]
        public bool IsDefault =>
            string.IsNullOrWhiteSpace(Slot) && Delay <= 0 && string.IsNullOrWhiteSpace(Shape)
            && Every <= 1 && !PerSlot && string.IsNullOrWhiteSpace(SlotIf) && !UntilMiss;

        [JsonIgnore] public bool IsRamp => ShapeIs("ramp");
        [JsonIgnore] public bool IsDecay => ShapeIs("decay");
        [JsonIgnore] public bool IsCharge => ShapeIs("charge");

        private bool ShapeIs(string s) => string.Equals(Shape?.Trim(), s, StringComparison.OrdinalIgnoreCase);

        /// <summary>Null when <paramref name="t"/> is null or default, so plain groups stay plain.</summary>
        public static CadenceTiming? CloneOrNull(CadenceTiming? t) =>
            t == null || t.IsDefault
                ? null
                : new CadenceTiming
                {
                    Slot = t.Slot, Delay = t.Delay, Shape = t.Shape, Every = t.Every,
                    PerSlot = t.PerSlot, SlotIf = t.SlotIf, UntilMiss = t.UntilMiss
                };

        /// <summary>
        /// Multiplier for the k-th application (1-based) of <paramref name="total"/>: ramp = k, decay = total − k + 1, else 1.
        /// </summary>
        public double ShapeFactor(int k, int total)
        {
            if (total < 1) total = 1;
            if (k < 1) k = 1;
            if (IsRamp) return k;
            if (IsDecay) return Math.Max(1, total - k + 1);
            return 1;
        }

        /// <summary><see cref="SlotIf"/> against this action's 0-based <paramref name="slot0"/> on a strip of <paramref name="count"/>.
        /// No condition = true; unknown slot (−1) fails any condition.</summary>
        public bool SlotIfHolds(int slot0, int count)
        {
            if (string.IsNullOrWhiteSpace(SlotIf))
                return true;
            if (slot0 < 0 || count <= 0)
                return false;
            int s = slot0 + 1;
            string c = SlotIf.Trim().ToLowerInvariant();
            switch (c)
            {
                case "first": return s == 1;
                case "last": return s == count;
                case "odd": return s % 2 == 1;
                case "even": return s % 2 == 0;
            }
            string op = c.StartsWith(">=") || c.StartsWith("<=") ? c[..2] : c.StartsWith(">") || c.StartsWith("<") || c.StartsWith("=") ? c[..1] : "=";
            string num = op == "=" && !c.StartsWith("=") ? c : c[op.Length..];
            if (!int.TryParse(num.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x))
                return false;
            return op switch
            {
                ">=" => s >= x,
                "<=" => s <= x,
                ">" => s > x,
                "<" => s < x,
                _ => s == x
            };
        }

        /// <summary>
        /// 0-based strip slots for an ACTION grant of <paramref name="count"/> applications from the action at
        /// <paramref name="sourceSlot0"/>: start at <see cref="Slot"/> (or the slot after the source), skip <see cref="Delay"/>,
        /// then one slot per application, wrapping the strip.
        /// </summary>
        public static List<int> ResolveActionSlots(CadenceTiming? timing, int sourceSlot0, int stripCount, int count)
        {
            var slots = new List<int>();
            if (stripCount <= 0 || count < 1)
                return slots;
            int start;
            string? slot = timing?.Slot?.Trim().ToLowerInvariant();
            if (slot == "first")
                start = 0;
            else if (slot == "last")
                start = stripCount - 1;
            else if (!string.IsNullOrEmpty(slot) && int.TryParse(slot, NumberStyles.Integer, CultureInfo.InvariantCulture, out int abs) && abs >= 1)
                start = Math.Min(abs, stripCount) - 1;
            else
                start = (Math.Max(0, sourceSlot0) + 1) % stripCount;
            start = (start + Math.Max(0, timing?.Delay ?? 0)) % stripCount;
            for (int k = 0; k < count; k++)
                slots.Add((start + k) % stripCount);
            return slots;
        }

        /// <summary>Copies <paramref name="items"/> scaled by <paramref name="factor"/>.</summary>
        public static List<ActionAttackBonusItem> Scale(IEnumerable<ActionAttackBonusItem>? items, double factor) =>
            items == null
                ? new List<ActionAttackBonusItem>()
                : items.Where(b => b != null).Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value * factor }).ToList();

        /// <summary>Short card text, e.g. "ramping", "in 2", "slot 3", "every 2nd", "until a miss".</summary>
        public string Describe()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(SlotIf)) parts.Add($"if in slot {SlotIf.Trim()}");
            if (!string.IsNullOrWhiteSpace(Slot)) parts.Add($"on slot {Slot.Trim()}");
            if (Delay > 0) parts.Add($"after {Delay}");
            if (IsRamp) parts.Add("ramping");
            if (IsDecay) parts.Add("fading");
            if (IsCharge) parts.Add("all at once");
            if (Every > 1) parts.Add($"every {Every}");
            if (PerSlot) parts.Add("x slot");
            if (UntilMiss) parts.Add("until a miss");
            return string.Join(", ", parts);
        }
    }
}
