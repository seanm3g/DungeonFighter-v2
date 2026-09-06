using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    public enum CombatSequenceStepKind
    {
        Attacker,
        Roll,
        Outcome,
        Action,
        Defense,
        Damage,
        Heal,
        Effect
    }

    public enum CombatSequenceCue
    {
        None,
        ThresholdBar,
        StripFlashAndSfx,
        HealthBar
    }

    /// <summary>
    /// One named beat in the live combat-sequence HUD.
    /// <see cref="MathBeats"/> are sequential formula pieces shown in that column (die → keep → bonus, base → ×action → final).
    /// </summary>
    public sealed class CombatSequenceStep
    {
        public CombatSequenceStep(
            CombatSequenceStepKind kind,
            string title,
            List<ColoredText> result,
            CombatSequenceCue cue = CombatSequenceCue.None,
            List<List<ColoredText>>? mathBeats = null)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Result = result ?? new List<ColoredText>();
            Cue = cue;
            if (mathBeats != null && mathBeats.Count > 0)
                MathBeats = mathBeats;
            else
                MathBeats = new List<List<ColoredText>> { Result };
        }

        public CombatSequenceStepKind Kind { get; }
        public CombatVisualAction? VisualAction { get; set; }
        public string Title { get; }
        public List<ColoredText> Result { get; }
        public CombatSequenceCue Cue { get; }
        public IReadOnlyList<List<ColoredText>> MathBeats { get; }
    }
}
