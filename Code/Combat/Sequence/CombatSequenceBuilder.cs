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

        /// <summary>Stand-in action for a room turn that rolls under its strike threshold (it advances one length).</summary>
        public static readonly Action QuietRoomAction = new() { Name = "Quiet", Length = 1.0, IsComboAction = false };

        /// <summary>
        /// Environment hazards: Setup + Action + optional Defense + Damage / Effects (or a no-effect Action).
        /// With <paramref name="room"/> and <paramref name="action"/>, the room's d20 strike roll plays as ROLL + OUTCOME
        /// and the steps carry swing info, so the combat board tumbles the die like an actor's swing.
        /// </summary>
        public static List<CombatSequenceStep> FromEnvironmental(
            string actionName,
            int damage,
            int? defenseFace,
            Actor? target,
            int? attackFace = null,
            bool hasStatusEffects = false,
            Environment? room = null,
            Action? action = null)
        {
            var steps = new List<CombatSequenceStep>();
            string name = string.IsNullOrWhiteSpace(actionName) ? "hazard" : actionName;
            steps.Add(new CombatSequenceStep(
                CombatSequenceStepKind.Attacker,
                "ATTACKER",
                Plain("Hazard", ColorPalette.Info)));
            bool rolled = room != null && action != null && room.LastStrikeRoll > 0;
            if (rolled)
                AppendStrikeRoll(steps, room!, struck: true);
            steps.Add(new CombatSequenceStep(
                CombatSequenceStepKind.Action,
                "ACTION",
                Plain(name, ColorPalette.Success)));

            if (defenseFace.HasValue && target != null)
            {
                steps.Add(BuildDefenseStep(
                    defenseFace.Value,
                    attackFace ?? DefenseBlockCalculator.NeutralAttackFace,
                    target,
                    action: null));
            }

            // Status effects land on the target, then the damage number.
            if (hasStatusEffects)
            {
                steps.Add(new CombatSequenceStep(
                    CombatSequenceStepKind.Effect,
                    "EFFECTS",
                    Plain("status", ColorPalette.Info)));
            }

            if (damage > 0)
            {
                steps.Add(new CombatSequenceStep(
                    CombatSequenceStepKind.Damage,
                    "DAMAGE",
                    Plain(damage.ToString(), ColorPalette.Damage),
                    CombatSequenceCue.HealthBar));
            }

            if (rolled)
                AttachStrikeSwing(steps, room!, action!, struck: true, damage);
            return steps;
        }

        /// <summary>
        /// A room turn whose d20 strike roll came up short: ATTACKER, ROLL, OUTCOME (quiet), ACTION (nothing happens).
        /// </summary>
        public static List<CombatSequenceStep> FromEnvironmentalQuiet(Environment room)
        {
            var steps = new List<CombatSequenceStep>
            {
                new(CombatSequenceStepKind.Attacker, "ATTACKER", Plain("Hazard", ColorPalette.Info))
            };
            AppendStrikeRoll(steps, room, struck: false);
            steps.Add(new CombatSequenceStep(CombatSequenceStepKind.Action, "ACTION", Plain("quiet", ColorPalette.Gray)));
            AttachStrikeSwing(steps, room, QuietRoomAction, struck: false, damage: 0);
            return steps;
        }

        private static void AppendStrikeRoll(List<CombatSequenceStep> steps, Environment room, bool struck)
        {
            int need = room.LastStrikeRollThreshold;
            steps.Add(new CombatSequenceStep(
                CombatSequenceStepKind.Roll,
                "ROLL",
                Plain(room.LastStrikeRoll.ToString(), ColorPalette.White)));
            steps.Add(new CombatSequenceStep(
                CombatSequenceStepKind.Outcome,
                "OUTCOME",
                struck ? Plain($"strikes ({need}+)", ColorPalette.Success) : Plain($"quiet ({need}+)", ColorPalette.Gray)));
        }

        /// <summary>Room rolls hit on <c>LastStrikeRollThreshold</c>+ with no crit or combo bands.</summary>
        private static void AttachStrikeSwing(List<CombatSequenceStep> steps, Environment room, Action action, bool struck, int damage)
        {
            var thresholds = new CombatSequenceThresholds(21, 21, room.LastStrikeRollThreshold, 0);
            var swing = new CombatSequenceSwingInfo(room, action, room.LastStrikeRoll,
                struck ? CombatSequenceSwingOutcome.Hit : CombatSequenceSwingOutcome.Miss, isCritical: false, damage, thresholds);
            foreach (var step in steps)
                step.Swing = swing;
        }

        private static void AppendSwing(
            List<CombatSequenceStep> steps,
            ActionExecutionResult result,
            Actor source,
            Actor? target)
        {
            var selected = result.SelectedAction!;
            int firstStep = steps.Count;
            steps.Add(BuildAttackerStep(source));
            steps.Add(BuildRollStep(result));
            steps.Add(BuildOutcomeStep(result));
            steps.Add(BuildActionStep(selected, result.Hit));

            if (result.Hit
                && target is Character
                && (selected.Type == ActionType.Attack || selected.Type == ActionType.Spell))
            {
                steps.Add(BuildDefenseStep(result.DefenseFace ?? 0, result.ModifiedBaseRoll, target, selected));
            }

            // Status effects land on the target before the damage number (the board's beat order).
            if (result.StatusEffectMessages != null && result.StatusEffectMessages.Count > 0)
            {
                string first = result.StatusEffectMessages[0];
                if (!string.IsNullOrWhiteSpace(first))
                {
                    // Status lines carry color markup; parse it like the combat log does instead of printing raw codes.
                    var segments = TrimSegments(ColoredTextParser.Parse(first.Trim()));
                    if (segments.Count > 0)
                    {
                        steps.Add(new CombatSequenceStep(
                            CombatSequenceStepKind.Effect,
                            "EFFECTS",
                            segments));
                    }
                }
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

            var outcome = result.IsCombo && result.Hit
                ? CombatSequenceSwingOutcome.Combo
                : result.Hit ? CombatSequenceSwingOutcome.Hit : CombatSequenceSwingOutcome.Miss;
            int roll = result.AttackRoll != 0 ? result.AttackRoll : result.ModifiedBaseRoll + result.RollBonus;
            CombatSequenceThresholds? thresholds = null;
            if (result.ResolvedCritThreshold is int crit && result.ResolvedComboThreshold is int combo
                && result.ResolvedHitThreshold is int hit && result.ResolvedCritMissThreshold is int critMiss)
                thresholds = new CombatSequenceThresholds(crit, combo, hit + 1, critMiss);
            var swing = new CombatSequenceSwingInfo(source, selected, roll, outcome, result.IsCritical, result.Hit ? result.Damage : 0, thresholds,
                isCriticalMiss: !result.Hit && result.IsCriticalMiss);
            for (int i = firstStep; i < steps.Count; i++)
                steps[i].Swing = swing;
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
                // Plain HIT already lives in OUTCOME; leave ACTION blank so the cell is not redundant.
                if (hit)
                    return new CombatSequenceStep(CombatSequenceStepKind.Action, "ACTION", new List<ColoredText>());
                return new CombatSequenceStep(CombatSequenceStepKind.Action, "ACTION", Plain("miss", ColorPalette.Miss));
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

        private static List<ColoredText> TrimSegments(List<ColoredText> segments)
        {
            while (segments.Count > 0 && string.IsNullOrWhiteSpace(segments[0].Text))
                segments.RemoveAt(0);
            while (segments.Count > 0 && string.IsNullOrWhiteSpace(segments[segments.Count - 1].Text))
                segments.RemoveAt(segments.Count - 1);
            if (segments.Count > 0)
            {
                segments[0].Text = segments[0].Text.TrimStart();
                segments[segments.Count - 1].Text = segments[segments.Count - 1].Text.TrimEnd();
            }
            return segments;
        }

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
