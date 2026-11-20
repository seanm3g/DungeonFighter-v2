using System;

namespace RPGGame
{
    /// <summary>
    /// Skill-tree helpers left after the one rule engine (step 7, 2026-11-20): the attribute bridge for rule attributes,
    /// card previews and convert text. The hybrid trees, the conversion table and per-rank pack mods are coded rules on
    /// the engine (<see cref="RPGGame.Combat.Rules.SkillRules"/>); every other node is a written rule (<c>crossWire</c>).
    /// Nothing here subscribes to the combat bus.
    /// </summary>
    public sealed partial class SkillEffectRouter
    {
        public static SkillEffectRouter Instance { get; } = new();

        private SkillEffectRouter() { }

        /// <summary>New fight: hybrid accumulation and banked combo steps restart.</summary>
        public void ResetFightState() => RPGGame.Combat.Rules.SkillRules.ResetFight();

        /// <summary>
        /// Standing attribute from rule nodes (Rite of Passage, Kinship, …: <c>WHILE -> STR 10</c>), folded into
        /// <see cref="Character.GetEffectiveStrength"/> and the other effective getters.
        /// </summary>
        public int GetSkillAttributeBonus(Character? c, string? statCode)
        {
            if (c == null || string.IsNullOrWhiteSpace(statCode)) return 0;
            string code = statCode.Trim().ToUpperInvariant();
            return code is "STR" or "AGI" or "TEC" or "INT"
                ? (int)Math.Round(CrossWires.Passive(c, code))
                : 0;
        }
    }
}
