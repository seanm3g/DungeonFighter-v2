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
            if (action.CausesFortify) effects.Add("fortify");
            if (action.CausesFocus) effects.Add("focus");
            if (action.CausesExpose) effects.Add("expose");
            if (action.CausesHPRegen) effects.Add("hpregen");
            if (action.CausesArmorBreak) effects.Add("armorbreak");
            if (action.CausesPierce) effects.Add("pierce");
            if (action.CausesReflect) effects.Add("reflect");
            if (action.CausesSilence) effects.Add("silence");
            if (action.CausesStatDrain) effects.Add("statdrain");
            if (action.CausesAbsorb) effects.Add("absorb");
            if (action.CausesTemporaryHP) effects.Add("temporaryhp");
            if (action.CausesConfusion) effects.Add("confusion");
            if (action.CausesCleanse) effects.Add("cleanse");
            if (action.CausesMark) effects.Add("mark");
            if (action.CausesDisrupt) effects.Add("disrupt");
            return effects;
        }

        /// <summary>
        /// Creates an Action with a specific status effect flag set for use by the effect registry.
        /// </summary>
        /// <param name="effectName">The name of the status effect</param>
        /// <returns>Action with the status effect flag set, or null if effect name is invalid</returns>
        public static Action? CreateActionWithStatusEffect(string effectName)
        {
            var action = new Action();
            var lower = (effectName ?? "").ToLower();
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
                case "fortify": action.CausesFortify = true; break;
                case "focus": action.CausesFocus = true; break;
                case "expose": action.CausesExpose = true; break;
                case "hpregen": action.CausesHPRegen = true; break;
                case "armorbreak": action.CausesArmorBreak = true; break;
                case "pierce": action.CausesPierce = true; break;
                case "reflect": action.CausesReflect = true; break;
                case "silence": action.CausesSilence = true; break;
                case "statdrain": action.CausesStatDrain = true; break;
                case "absorb": action.CausesAbsorb = true; break;
                case "temporaryhp": action.CausesTemporaryHP = true; break;
                case "confusion": action.CausesConfusion = true; break;
                case "cleanse": action.CausesCleanse = true; break;
                case "mark": action.CausesMark = true; break;
                case "disrupt": action.CausesDisrupt = true; break;
                default: return null;
            }
            return action;
        }
    }
}
