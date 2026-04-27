using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Combat.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Turn
{
    /// <summary>
    /// Processes damage over time and status effects for combat entities
    /// </summary>
    public static class StatusEffectProcessor
    {
        private static int GetCharacterCurrentHealth(Actor entity) =>
            entity is Character ch ? ch.CurrentHealth : 0;

        /// <summary>Avoids an extra blank line when the prior parsed line already ends with a newline.</summary>
        private static bool ColoredListEndsWithLineBreak(List<ColoredText> segments)
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                string? t = segments[i]?.Text;
                if (string.IsNullOrEmpty(t)) continue;
                char c = t[t.Length - 1];
                return c == '\n' || c == '\r';
            }
            return false;
        }

        /// <summary>
        /// Processes damage over time effects for an entity
        /// </summary>
        /// <param name="entity">The entity to process effects for</param>
        /// <param name="results">Output list for effect messages</param>
        /// <returns>Total damage dealt</returns>
        public static int ProcessEntityStatusEffects(Actor entity, List<string> results)
        {
            return CombatEffectsSimplified.ProcessStatusEffects(entity, results);
        }

        /// <summary>
        /// Processes damage over time effects for both player and enemy
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="enemy">The enemy</param>
        public static void ProcessDamageOverTimeEffects(Character player, Enemy enemy)
        {
            bool blankLineAdded = false;
            
            // Process effects for player
            ProcessEntityEffects(player, ref blankLineAdded);
            
            // Use IsAlive (HP > 0), not IsLiving. IsLiving is a template flag (undead/elemental) used for poison/bleed
            // immunity in Enemy overrides — burn still ticks on those enemies; skipping here left burn stacked with no ticks.
            if (enemy.IsAlive)
            {
                ProcessEntityEffects(enemy, ref blankLineAdded);
            }
        }

        /// <summary>Bleed resolves when the afflicted actor finishes their combat turn (including stun).</summary>
        public static void ProcessBleedAfterActorResolvedTurn(Actor entity)
        {
            var results = new List<string>();
            int damage = CombatEffectsSimplified.ProcessBleedAfterActorTurn(entity, results);
            if (damage > 0)
            {
                string? barId = HealthBarEntityId.ForActor(entity);
                int hpBefore = GetCharacterCurrentHealth(entity);

                if (entity is Character character)
                    character.TakeDamage(damage);
                else if (entity is Enemy enemy)
                    enemy.TakeDamage(damage);

                int actual = hpBefore - GetCharacterCurrentHealth(entity);
                if (barId != null)
                    HealthBarDeltaDamageHint.RecordAfterMitigation(barId, 0, 0, damage, damage, actual);
            }
            if (results.Count > 0)
            {
                if (!RPGGame.TurnManager.DisableCombatUIOutput)
                    TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.PoisonDamage, entity.Name);
                var combinedSegments = new List<ColoredText>();
                for (int i = 0; i < results.Count; i++)
                {
                    if (i > 0 && !ColoredListEndsWithLineBreak(combinedSegments))
                        combinedSegments.Add(new ColoredText(System.Environment.NewLine, Colors.White));
                    combinedSegments.AddRange(ColoredTextParser.Parse(results[i]));
                }
                BlockDisplayManager.DisplaySystemBlock(combinedSegments);
                TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage, entity.Name);
            }
        }

        private static void ProcessEntityEffects(Actor entity, ref bool blankLineAdded)
        {
            var results = new List<string>();
            var breakdown = CombatEffectsSimplified.ProcessStatusEffectsWithBreakdown(entity, results);
            int damage = breakdown.TotalDamage;

            if (damage > 0)
            {
                string? barId = HealthBarEntityId.ForActor(entity);
                int hpBefore = GetCharacterCurrentHealth(entity);

                // TakeDamage is defined on Character and Enemy, not Actor
                if (entity is Character character)
                {
                    character.TakeDamage(damage);
                }
                else if (entity is Enemy enemy)
                {
                    enemy.TakeDamage(damage);
                }

                int actual = hpBefore - GetCharacterCurrentHealth(entity);
                if (barId != null)
                    HealthBarDeltaDamageHint.RecordAfterMitigation(
                        barId,
                        breakdown.PoisonDamage,
                        breakdown.BurnDamage,
                        bleedRequested: 0,
                        requestedTotal: damage,
                        actualHpLost: actual);
            }
            
            if (results.Count > 0)
            {
                // Apply spacing for poison damage (context-aware)
                // Always call ApplySpacingBefore to let the spacing system handle rules
                // The spacing system will add blank lines between consecutive poison damage blocks
                if (!RPGGame.TurnManager.DisableCombatUIOutput)
                {
                    TextSpacingSystem.ApplySpacingBefore(TextSpacingSystem.BlockType.PoisonDamage, entity.Name);
                    blankLineAdded = true;
                }
                
                // Combine ALL status effect messages into a single block
                // Parse each message separately and combine with explicit newlines to ensure proper line breaks
                var combinedSegments = new List<ColoredText>();
                for (int i = 0; i < results.Count; i++)
                {
                    if (i > 0 && !ColoredListEndsWithLineBreak(combinedSegments))
                    {
                        // Add explicit newline segment between messages to ensure proper line breaks
                        // This prevents the "no longer affected" message from wrapping onto the previous line
                        combinedSegments.Add(new ColoredText(System.Environment.NewLine, Colors.White));
                    }
                    
                    // Parse each message separately
                    var messageSegments = ColoredTextParser.Parse(results[i]);
                    combinedSegments.AddRange(messageSegments);
                }
                
                BlockDisplayManager.DisplaySystemBlock(combinedSegments);
                // Record as poison damage block for spacing system (only once for the entire block)
                TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage, entity.Name);
            }
        }
    }
}

