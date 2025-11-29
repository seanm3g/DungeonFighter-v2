using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Collects all messages for an action block into a structured list
    /// </summary>
    public static class BlockMessageCollector
    {
        /// <summary>
        /// Collects all messages for an action block into a structured list
        /// </summary>
        public static List<(List<ColoredText> segments, UIMessageType messageType)> CollectActionBlockMessages(
            List<ColoredText>? actionText,
            List<ColoredText>? rollInfo,
            List<List<ColoredText>>? statusEffects,
            List<ColoredText>? criticalMissNarrative,
            List<List<ColoredText>>? narratives)
        {
            var messageGroups = new List<(List<ColoredText> segments, UIMessageType messageType)>();
            
            // Add action text
            if (actionText != null && actionText.Count > 0)
            {
                messageGroups.Add((actionText, UIMessageType.Combat));
            }
            
            // Add roll info
            if (rollInfo != null && rollInfo.Count > 0)
            {
                messageGroups.Add((rollInfo, UIMessageType.RollInfo));
            }
            
            // Add critical miss narrative
            if (criticalMissNarrative != null && criticalMissNarrative.Count > 0)
            {
                messageGroups.Add((criticalMissNarrative, UIMessageType.System));
            }
            
            // Add status effects
            if (statusEffects != null)
            {
                foreach (var effect in statusEffects)
                {
                    if (effect != null && effect.Count > 0)
                    {
                        messageGroups.Add((effect, UIMessageType.EffectMessage));
                    }
                }
            }
            
            // Add all narratives (all part of the same turn block)
            if (narratives != null)
            {
                foreach (var narrative in narratives)
                {
                    if (narrative != null && narrative.Count > 0)
                    {
                        messageGroups.Add((narrative, UIMessageType.System));
                    }
                }
            }
            
            return messageGroups;
        }
    }
}

