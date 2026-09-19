using System.Collections.Generic;
using RPGGame.Combat.Calculators;
using RPGGame.Combat.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Builds player-facing sequence HUD steps from an already-resolved swing.
    /// Does not re-run combat math.
    /// </summary>
    public static class CombatSequenceBuilder
    {
        internal static List<CombatSequenceStep> From(ActionExecutionResult result, Actor source, Actor? target)
        {
            var steps = new List<CombatSequenceStep>();
            if (result?.SelectedAction == null)
                return steps;

            Actor? displayTarget = result.EffectiveTarget ?? target;
            AppendSwing(steps, result, source, displayTarget);

            if (result.NestedRetriggerResults != null)
            {
                foreach (var nested in result.NestedRetriggerResults)
                {
                    if (nested?.SelectedAction == null)
                        continue;
                    Actor? nestedTarget = nested.EffectiveTarget ?? displayTarget;
                    AppendSwing(steps, nested, source, nestedTarget);
                }
            }

            return steps;
        }

        /// <summary>
        /// Environment hazards: Setup + Action + optional Defense + Damage (or a no-effect Action).
        /// </summary>
        public static List<CombatSequenceStep> FromEnvironmental(
            string actionName,
            int damage,
            int? defenseFace,
            Actor? target,
            int? attackFace = null)
        {
            var steps = new List<CombatSequenceStep>();
            string name = string.IsNullOrWhiteSpace(actionName) ? "hazard" : actionName;
            steps.Add(new CombatSequenceStep(
                CombatSequenceStepKind.Attacker,
                "ATTACKER",
                Plain("Hazard", ColorPalette.Info)));
            steps.Add(new CombatSequenceStep(
                CombatSequenceStepKind.Action,
                "ACTION",
                Plain(name, ColorPalette.Success)));

            if (damage <= 0 && !defenseFace.HasValue)
                return steps;

            if (defenseFace.HasValue && target != null)
            {
                steps.Add(BuildDefenseStep(
                    defenseFace.Value,
                    attackFace ?? DefenseBlockCalculator.NeutralAttackFace,
                    target,
                    action: null));
            }

            if (damage > 0)
            {
                steps.Add(new CombatSequenceStep(
                    CombatSequenceStepKind.Damage,
                    "DAMAGE",
                    Plain(damage.ToString(), ColorPalette.Damage),
                    CombatSequenceCue.HealthBar));
            }

            return steps;
        }

        private static void AppendSwing(
            List<CombatSequenceStep> steps,
            ActionExecutionResult result,
            Actor source,
            Actor? target)
        {
            var selected = result.SelectedAction!;
            steps.Add(BuildAttackerStep(source));
            steps.Add(BuildRollStep(result));
            steps.Add(BuildOutcomeStep(result));
            steps.Add(BuildActionStep(selected, result.Hit));

            if (result.Hit
                && target != null
                && target is Character heroTarget
                && heroTarget is not Enemy
                && (selected.Type == ActionType.Attack || selected.Type == ActionType.Spell))
            {
                steps.Add(BuildDefenseStep(result.DefenseFace ?? 0, result.ModifiedBaseRoll, target, selected));
            }

            if (result.Hit && selected.Type == ActionType.Heal)
            {
                var healBeats = CombatSequenceMathBeats.ForHeal(result.HealAmount);
                steps.Add(new CombatSequenceStep(
                    CombatSequenceStepKind.Heal,
                    "HEAL",
                    Last(healBeats),
                    CombatSequenceCue.HealthBar,
                    healBeats));
            }
            else if (result.Hit
                && result.Damage > 0
                && (selected.Type == ActionType.Attack || selected.Type == ActionType.Spell))
            {
                var damageBeats = CombatSequenceMathBeats.ForDamage(result, selected);
                steps.Add(new CombatSequenceStep(
                    CombatSequenceStepKind.Damage,
                    "DAMAGE",
                    Last(damageBeats),
                    CombatSequenceCue.HealthBar,
                    damageBeats));
            }

            if (result.StatusEffectMessages != null && result.StatusEffectMessages.Count > 0)
            {
                string first = result.StatusEffectMessages[0];
                if (!string.IsNullOrWhiteSpace(first))
                {
                    steps.Add(new CombatSequenceStep(
                        CombatSequenceStepKind.Effect,
                        "EFFECTS",
                        Plain(first.Trim(), ColorPalette.Info)));
                }
            }
        }

        private static CombatSequenceStep BuildAttackerStep(Actor source)
        {
            var builder = new ColoredTextBuilder();
            EntityColorHelper.AppendActorNameColored(builder, source);
            return new CombatSequenceStep(CombatSequenceStepKind.Attacker, "ATTACKER", builder.Build());
        }

        private static CombatSequenceStep BuildActionStep(Action selected, bool hit)
        {
            if (string.IsNullOrWhiteSpace(selected.Name))
            {
                return hit
                    ? new CombatSequenceStep(CombatSequenceStepKind.Action, "ACTION", Plain("hit", ColorPalette.Success))
                    : new CombatSequenceStep(CombatSequenceStepKind.Action, "ACTION", Plain("miss", ColorPalette.Miss));
            }

            return new CombatSequenceStep(
                CombatSequenceStepKind.Action,
                "ACTION",
                Plain(selected.Name, ColorPalette.Success));
        }

        private static CombatSequenceStep BuildRollStep(ActionExecutionResult result)
        {
            var beats = CombatSequenceMathBeats.ForRoll(result);
            return new CombatSequenceStep(
                CombatSequenceStepKind.Roll,
                "ROLL",
                Last(beats),
                CombatSequenceCue.ThresholdBar,
                beats);
        }

        private static CombatSequenceStep BuildOutcomeStep(ActionExecutionResult result)
        {
            var beats = CombatSequenceMathBeats.ForOutcome(result);
            return new CombatSequenceStep(
                CombatSequenceStepKind.Outcome,
                "OUTCOME",
                Last(beats),
                CombatSequenceCue.StripFlashAndSfx,
                beats);
        }

        private static CombatSequenceStep BuildDefenseStep(
            int defenseFace,
            int attackFace,
            Actor target,
            Action? action)
        {
            var beats = CombatSequenceMathBeats.ForDefense(defenseFace, attackFace, target, action);
            return new CombatSequenceStep(CombatSequenceStepKind.Defense, "DEFENSE", Last(beats), mathBeats: beats);
        }

        private static List<ColoredText> Last(List<List<ColoredText>> beats) =>
            beats.Count > 0 ? beats[beats.Count - 1] : new List<ColoredText>();

        private static List<ColoredText> Plain(string text, ColorPalette palette)
        {
            var builder = new ColoredTextBuilder();
            builder.Add(text, palette);
            return builder.Build();
        }

        internal static void SnapshotHealthHolds(ActionExecutionResult result, Actor source, Actor? target)
        {
            if (result == null)
                return;
            result.HealthBarHolds.Clear();
            var selected = result.SelectedAction;
            if (selected == null)
                return;

            Actor? combatTarget = result.EffectiveTarget ?? target;
            if (selected.Type == ActionType.Heal)
            {
                Actor recipient = combatTarget ?? source;
                AddHold(result, recipient);
                return;
            }

            if (selected.Type != ActionType.Attack && selected.Type != ActionType.Spell)
                return;

            if (selected.Target == TargetType.Self)
                AddHold(result, source);
            else
            {
                AddHold(result, combatTarget);
                if (selected.Target == TargetType.SelfAndTarget)
                    AddHold(result, source);
            }
        }

        private static void AddHold(ActionExecutionResult result, Actor? actor)
        {
            if (actor == null)
                return;
            string? id = HealthBarEntityId.ForActor(actor);
            if (id == null)
                return;
            result.HealthBarHolds.Add((id, ActionUtilities.GetEntityHealth(actor)));
        }
    }
}
