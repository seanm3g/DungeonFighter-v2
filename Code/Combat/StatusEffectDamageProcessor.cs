using RPGGame.Combat.Formatting;
using RPGGame.UI.ColorSystem;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Processes status effects (poison, burn, etc.) for actors.
    /// Extracted from CombatEffectsSimplified to reduce size and improve Single Responsibility Principle compliance.
    /// </summary>
    public static class StatusEffectProcessor
    {
        /// <summary>
        /// Processes all active status effects for an Actor at the start of their turn
        /// </summary>
        /// <param name="actor">The Actor to process effects for</param>
        /// <param name="results">List to add effect messages to</param>
        /// <returns>Total damage dealt by effects</returns>
        public static int ProcessStatusEffects(Actor actor, List<string> results)
        {
            int totalEffectDamage = 0;
            double currentTime = GameTicker.Instance.GetCurrentGameTime();
            
            // Process poison damage
            if (actor.PoisonStacks > 0)
            {
                int poisonDamage = actor.ProcessPoison(currentTime);
                if (poisonDamage > 0)
                {
                    totalEffectDamage += poisonDamage;
                    string damageType = actor.GetDamageTypeText();
                    
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    var actorColor = EntityColorHelper.GetActorColor(actor);
                    DamageFormatter.AddActorTakesDamage(builder, actor.Name, actorColor, poisonDamage, damageType);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
                
                // Check if effect ended (regardless of whether damage was dealt)
                if (actor.PoisonStacks > 0)
                {
                    string damageType = actor.GetDamageTypeText();
                    
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    ColorPalette effectColor = damageType == "bleed" ? ColorPalette.Error : ColorPalette.Green;
                    DamageFormatter.AddEffectStacksRemain(builder, damageType, effectColor, actor.PoisonStacks);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
                else
                {
                    string damageType = actor.GetDamageTypeText();
                    string effectEndMessage = damageType == "bleed" ? "bleeding" : "poisoned";
                    
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    var actorColor = EntityColorHelper.GetActorColor(actor);
                    ColorPalette effectColor = damageType == "bleed" ? ColorPalette.Error : ColorPalette.Green;
                    DamageFormatter.AddActorNoLongerAffected(builder, actor.Name, actorColor, effectEndMessage, effectColor);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
            }
            
            // Process burn damage
            if (actor.BurnStacks > 0)
            {
                int burnDamage = actor.ProcessBurn(currentTime);
                if (burnDamage > 0)
                {
                    totalEffectDamage += burnDamage;
                    
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    var actorColor = EntityColorHelper.GetActorColor(actor);
                    DamageFormatter.AddActorTakesDamage(builder, actor.Name, actorColor, burnDamage, "burn");
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
                
                // Check if effect ended (regardless of whether damage was dealt)
                if (actor.BurnStacks > 0)
                {
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    DamageFormatter.AddEffectStacksRemain(builder, "burn", ColorPalette.Orange, actor.BurnStacks);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
                else
                {
                    // Use ColoredTextBuilder for proper spacing
                    var builder = new ColoredTextBuilder();
                    var actorColor = EntityColorHelper.GetActorColor(actor);
                    DamageFormatter.AddActorNoLongerAffected(builder, actor.Name, actorColor, "burning", ColorPalette.Orange);
                    var coloredText = builder.Build();
                    
                    // Convert to markup string for results list
                    results.Add(ColoredTextRenderer.RenderAsMarkup(coloredText));
                }
            }
            
            return totalEffectDamage;
        }
    }
}
