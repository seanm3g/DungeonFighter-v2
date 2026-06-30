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
                        return BalanceTuningConsole.AdjustArchetype(
                            suggestion.Target,
                            suggestion.Parameter,
                            suggestion.SuggestedValue);

                    case "weapon":
                        return BalanceTuningConsole.AdjustWeaponScaling(suggestion.Target, "damage", suggestion.SuggestedValue);

                    case "enemy":
                        return BalanceTuningConsole.AdjustEnemyOverride(
                            suggestion.Target,
                            suggestion.Parameter,
                            suggestion.SuggestedValue);

                    case "roll_feel":
                        if (suggestion.Parameter == "rollFeelVarianceCompression")
                        {
                            RollFeelVarianceCompression.Apply(suggestion.SuggestedValue);
                            return true;
                        }
                        var param = CombatTuningParameterRegistry.GetById(suggestion.Parameter);
                        if (param != null)
                        {
                            param.SetValue(suggestion.SuggestedValue);
                            return true;
                        }
                        break;

                    case "dungeon_scaling":
                        var scaling = GameConfiguration.Instance.DungeonScaling;
                        if (scaling == null)
                            break;
                        if (suggestion.Parameter == "EnemyCountPerRoom")
                        {
                            scaling.EnemyCountPerRoom = (int)suggestion.SuggestedValue;
                            return true;
                        }
                        if (suggestion.Parameter == "RoomCountPerLevel")
                        {
                            scaling.RoomCountPerLevel = suggestion.SuggestedValue;
                            return true;
                        }
                        break;

                    case "enemy_scaling":
                        return BalanceTuningConsole.AdjustEnemyScalingPerLevel(
                            suggestion.Parameter.ToLower(),
                            suggestion.SuggestedValue);

                    case "enemy_progression":
                        return BalanceTuningConsole.AdjustEnemyProgressionScale(
                            suggestion.Parameter,
                            suggestion.SuggestedValue);

                    case "starting_weapon":
                        if (EarlyGameBalanceHelper.TryParseWeaponType(suggestion.Target, out var startWeapon)
                            && suggestion.Parameter.Equals("StartingWeaponDamage", StringComparison.OrdinalIgnoreCase))
                        {
                            return AdjustmentExecutor.AdjustStartingWeaponDamage(
                                startWeapon, (int)Math.Round(suggestion.SuggestedValue));
                        }
                        break;

                    case "early_game":
                        if (!EarlyGameBalanceHelper.TryParseWeaponType(suggestion.Target, out var earlyWeapon))
                            break;
                        if (suggestion.Parameter.Equals("StartingClassPointsBonus", StringComparison.OrdinalIgnoreCase))
                        {
                            return AdjustmentExecutor.AdjustStartingClassPointsBonus(
                                earlyWeapon, (int)Math.Round(suggestion.SuggestedValue));
                        }
                        if (suggestion.Parameter.Equals("StartingActionDamageMultiplier", StringComparison.OrdinalIgnoreCase))
                        {
                            return AdjustmentExecutor.AdjustStartingActionDamageMultiplier(
                                earlyWeapon, suggestion.SuggestedValue);
                        }
                        break;

                    case "class_balance":
                        return AdjustmentExecutor.AdjustClassBalanceMultiplier(
                            suggestion.Target,
                            suggestion.Parameter,
                            suggestion.SuggestedValue);
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

