using System;
using RPGGame.Utils;

namespace RPGGame.Tuning
{
    public static class TuningSuggestionApplier
    {
        public static bool Apply(TuningSuggestion suggestion)
        {
            try
            {
                switch (suggestion.Category)
                {
                    case "global":
                        if (suggestion.Parameter == "HealthMultiplier" || suggestion.Parameter == "health")
                        {
                            return BalanceTuningConsole.AdjustGlobalEnemyMultiplier("health", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "DamageMultiplier" || suggestion.Parameter == "damage")
                        {
                            return BalanceTuningConsole.AdjustGlobalEnemyMultiplier("damage", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "ArmorMultiplier" || suggestion.Parameter == "armor")
                        {
                            return BalanceTuningConsole.AdjustGlobalEnemyMultiplier("armor", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "SpeedMultiplier" || suggestion.Parameter == "speed")
                        {
                            return BalanceTuningConsole.AdjustGlobalEnemyMultiplier("speed", suggestion.SuggestedValue);
                        }
                        break;

                    case "player":
                        if (suggestion.Parameter == "BaseStrength")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseAttribute("strength", (int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaseAgility")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseAttribute("agility", (int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaseTechnique")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseAttribute("technique", (int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaseIntelligence")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseAttribute("intelligence", (int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaseHealth")
                        {
                            return BalanceTuningConsole.AdjustPlayerBaseHealth((int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "AttributesPerLevel")
                        {
                            return BalanceTuningConsole.AdjustPlayerAttributesPerLevel((int)suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "HealthPerLevel")
                        {
                            return BalanceTuningConsole.AdjustPlayerHealthPerLevel((int)suggestion.SuggestedValue);
                        }
                        break;

                    case "enemy_baseline":
                        if (suggestion.Parameter == "BaselineStrength")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("strength", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineAgility")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("agility", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineTechnique")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("technique", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineIntelligence")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("intelligence", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineHealth")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("health", suggestion.SuggestedValue);
                        }
                        else if (suggestion.Parameter == "BaselineArmor")
                        {
                            return BalanceTuningConsole.AdjustEnemyBaselineStat("armor", suggestion.SuggestedValue);
                        }
                        break;

                    case "archetype":
                        ScrollDebugLogger.Log($"TuningSuggestionApplier: Archetype adjustments not yet fully implemented");
                        break;

                    case "weapon":
                        return BalanceTuningConsole.AdjustWeaponScaling(suggestion.Target, "damage", suggestion.SuggestedValue);

                    case "enemy":
                        ScrollDebugLogger.Log($"TuningSuggestionApplier: Enemy-specific adjustments require Enemies.json modification");
                        break;
                }

                return false;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"TuningSuggestionApplier: Error applying suggestion: {ex.Message}");
                return false;
            }
        }
    }
}

