using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Resolves effect types from an action and creates temporary actions for status effects.
    /// Extracted from CombatEffectsSimplified for single responsibility and testability.
    /// </summary>
    public static class StatusEffectActionResolver
    {
        private static readonly HashSet<string> RemovedEffectTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "reflect", "cleanse"
        };

        /// <summary>
        /// Gets all effect types that an action can cause.
        /// </summary>
        public static List<string> GetEffectTypesFromAction(Action action)
        {
            var effects = new List<string>();
            if (action.CausesBleed) effects.Add("bleed");
            if (action.CausesWeaken) effects.Add("weaken");
            if (action.CausesSlow) effects.Add("slow");
            if (action.CausesPoison) effects.Add("poison");
            if (action.CausesStun) effects.Add("stun");
            if (action.CausesBurn) effects.Add("burn");
            if (action.CausesVulnerability) effects.Add("vulnerability");
            if (action.CausesHarden) effects.Add("harden");
            if (action.CausesFocus) effects.Add("focus");
            if (action.CausesExpose) effects.Add("expose");
            if (action.CausesHPRegen) effects.Add("hpregen");
            if (action.CausesArmorBreak) effects.Add("armorbreak");
            if (action.CausesPierce) effects.Add("pierce");
            if (action.CausesSilence) effects.Add("silence");
            if (action.CausesStatDrain) effects.Add("statdrain");
            if (action.CausesAbsorb) effects.Add("absorb");
            if (action.CausesTemporaryHP) effects.Add("temporaryhp");
            if (action.CausesConfusion) effects.Add("confusion");
            if (action.CausesMark) effects.Add("mark");
            if (action.CausesDisrupt) effects.Add("disrupt");
            if (action.CausesFortify) effects.Add("fortify");

            if (action.Advanced?.SelfTargetEffects != null)
            {
                foreach (string effect in action.Advanced.SelfTargetEffects)
                {
                    if (string.IsNullOrWhiteSpace(effect))
                        continue;
                    string normalized = effect.Trim().ToLowerInvariant();
                    if (RemovedEffectTypes.Contains(normalized))
                        continue;
                    if (!effects.Contains(normalized))
                        effects.Add(normalized);
                }
            }

            return effects;
        }

        /// <summary>
        /// Creates an Action with a specific status effect flag set for use by the effect registry.
        /// </summary>
        public static Action? CreateActionWithStatusEffect(string effectName)
        {
            var lower = (effectName ?? "").ToLower();
            if (RemovedEffectTypes.Contains(lower))
                return null;

            var action = new Action();
            switch (lower)
            {
                case "bleed": action.CausesBleed = true; break;
                case "weaken": action.CausesWeaken = true; break;
                case "slow": action.CausesSlow = true; break;
                case "poison": action.CausesPoison = true; break;
                case "burn": action.CausesBurn = true; break;
                case "stun": action.CausesStun = true; break;
                case "vulnerability": action.CausesVulnerability = true; break;
                case "harden": action.CausesHarden = true; break;
                case "focus": action.CausesFocus = true; break;
                case "expose": action.CausesExpose = true; break;
                case "hpregen": action.CausesHPRegen = true; break;
                case "armorbreak": action.CausesArmorBreak = true; break;
                case "pierce": action.CausesPierce = true; break;
                case "silence": action.CausesSilence = true; break;
                case "statdrain": action.CausesStatDrain = true; break;
                case "absorb": action.CausesAbsorb = true; break;
                case "temporaryhp": action.CausesTemporaryHP = true; break;
                case "confusion": action.CausesConfusion = true; break;
                case "mark": action.CausesMark = true; break;
                case "disrupt": action.CausesDisrupt = true; break;
                case "fortify": action.CausesFortify = true; break;
                default: return null;
            }
            return action;
        }

        /// <summary>
        /// Returns the action instance handlers should read flags from. Self-target-only sheet effects may
        /// appear in <see cref="AdvancedMechanicsProperties.SelfTargetEffects"/> without enemy-target Causes* flags.
        /// </summary>
        public static Action ResolveActionForEffectApplication(Action action, string effectType)
        {
            string normalizedEffect = effectType ?? "";
            if (RemovedEffectTypes.Contains(normalizedEffect.ToLowerInvariant()))
                return action;
            if (ActionHasEffectFlag(action, normalizedEffect))
                return action;
            return CreateActionWithStatusEffect(normalizedEffect) ?? action;
        }

        private static bool ActionHasEffectFlag(Action action, string effectType)
        {
            return (effectType ?? "").ToLowerInvariant() switch
            {
                "bleed" => action.CausesBleed,
                "weaken" => action.CausesWeaken,
                "slow" => action.CausesSlow,
                "poison" => action.CausesPoison,
                "stun" => action.CausesStun,
                "burn" => action.CausesBurn,
                "vulnerability" => action.CausesVulnerability,
                "harden" => action.CausesHarden,
                "focus" => action.CausesFocus,
                "expose" => action.CausesExpose,
                "hpregen" => action.CausesHPRegen,
                "armorbreak" => action.CausesArmorBreak,
                "pierce" => action.CausesPierce,
                "silence" => action.CausesSilence,
                "statdrain" => action.CausesStatDrain,
                "absorb" => action.CausesAbsorb,
                "temporaryhp" => action.CausesTemporaryHP,
                "confusion" => action.CausesConfusion,
                "mark" => action.CausesMark,
                "disrupt" => action.CausesDisrupt,
                "fortify" => action.CausesFortify,
                _ => false
            };
        }
    }
}
