using System;

using System.Collections.Generic;

using System.Linq;

using RPGGame;

using RPGGame.Config;

using RPGGame.Tuning.Profiles;



namespace RPGGame.Tuning.Suggesters

{

    public static class PlaythroughAdjustmentSuggester

    {

        private const double MaxStartingActionDamageMultiplier = 1.5;

        private const int MaxStartingClassPointsBonus = 3;

        private const int MaxStartingWeaponDamage = 12;



        public static List<TuningSuggestion> Suggest(

            ClassPlaythroughBatchResult batch,

            TuningAnalysis analysis,

            PlaythroughAnalysisTargets? targets = null)

        {

            targets ??= new PlaythroughAnalysisTargets();

            var suggestions = new List<TuningSuggestion>();

            if (batch.ClassAggregates.Count == 0)

                return suggestions;



            var config = GameConfiguration.Instance;

            config.EarlyGame ??= new EarlyGameConfig();

            config.EarlyGame.EnsureValidDefaults();

            config.WeaponScaling ??= new WeaponScalingConfig();

            config.WeaponScaling.EnsureSanitizedDefaults();



            var multipliers = config.EnemySystem.GlobalMultipliers;

            double meanLevel = batch.ClassAggregates.Average(a => a.MeanFinalLevel);

            double meanDungeons = batch.ClassAggregates.Average(a => a.MeanDungeonsCompleted);

            double levelSpread = batch.ClassAggregates.Max(a => a.MeanFinalLevel) - batch.ClassAggregates.Min(a => a.MeanFinalLevel);



            bool tooHard = meanLevel < targets.MinMeanFinalLevel || meanDungeons < targets.MinMeanDungeonsCompleted;

            bool parityIssue = levelSpread > targets.MaxLevelSpread || batch.HasParityWarnings;

            var weakest = batch.ClassAggregates.OrderBy(a => a.MeanFinalLevel).ThenBy(a => a.MeanDungeonsCompleted).First();

            var strongest = batch.ClassAggregates.OrderByDescending(a => a.MeanFinalLevel).First();



            if (tooHard)

            {

                double levelGap = Math.Max(0, targets.MinMeanFinalLevel - meanLevel);

                double dungeonGap = Math.Max(0, targets.MinMeanDungeonsCompleted - meanDungeons);

                double severity = Math.Clamp((levelGap + dungeonGap) / 3.0, 0.05, 0.25);



                suggestions.Add(new TuningSuggestion

                {

                    Id = "playthrough_reduce_enemy_health",

                    Priority = severity >= 0.15

                        ? BalanceTuningGoals.TuningPriority.Critical

                        : BalanceTuningGoals.TuningPriority.High,

                    Category = "global",

                    Target = "All Enemies",

                    Parameter = "HealthMultiplier",

                    CurrentValue = multipliers.HealthMultiplier,

                    SuggestedValue = multipliers.HealthMultiplier * (1.0 - severity),

                    AdjustmentMagnitude = severity * 100,

                    Reason = $"Playthrough survival low — avg level {meanLevel:F1} (target ≥{targets.MinMeanFinalLevel:F1}), avg dungeons {meanDungeons:F1} (target ≥{targets.MinMeanDungeonsCompleted:F1}).",

                    Impact = $"Reduce enemy health multiplier by {severity * 100:F0}% to ease early playthroughs"

                });



                suggestions.AddRange(BuildEarlyGameSuggestionsForClass(

                    weakest,

                    severity,

                    BalanceTuningGoals.TuningPriority.High,

                    "Playthrough runs end too early"));



                int currentHealth = config.Character.PlayerBaseHealth;

                int healthBoost = Math.Max(1, (int)Math.Ceiling(currentHealth * severity * 0.5));

                suggestions.Add(new TuningSuggestion

                {

                    Id = "playthrough_increase_player_health",

                    Priority = BalanceTuningGoals.TuningPriority.Medium,

                    Category = "player",

                    Target = "Player",

                    Parameter = "BaseHealth",

                    CurrentValue = currentHealth,

                    SuggestedValue = currentHealth + healthBoost,

                    AdjustmentMagnitude = (healthBoost / (double)currentHealth) * 100,

                    Reason = $"Playthrough runs end too early (avg level {meanLevel:F1}). Boost player survivability.",

                    Impact = $"Increase player base health by {healthBoost} ({currentHealth} → {currentHealth + healthBoost})"

                });

            }

            else if (parityIssue)

            {

                suggestions.AddRange(BuildEarlyGameSuggestionsForClass(

                    weakest,

                    severity: Math.Clamp(levelSpread / 4.0, 0.05, 0.15),

                    BalanceTuningGoals.TuningPriority.High,

                    $"Class parity spread — {weakest.ClassDisplayName} avg level {weakest.MeanFinalLevel:F1} vs {strongest.ClassDisplayName} {strongest.MeanFinalLevel:F1} (spread {levelSpread:F1})"));



                suggestions.Add(new TuningSuggestion

                {

                    Id = "playthrough_parity_class_damage",

                    Priority = BalanceTuningGoals.TuningPriority.Medium,

                    Category = "class_balance",

                    Target = EarlyGameBalanceHelper.GetClassBalanceKey(weakest.WeaponType),

                    Parameter = "DamageMultiplier",

                    CurrentValue = EarlyGameBalanceHelper.GetClassMultipliers(

                        EarlyGameBalanceHelper.GetClassBalanceKey(weakest.WeaponType)).DamageMultiplier,

                    SuggestedValue = Math.Min(

                        3.0,

                        EarlyGameBalanceHelper.GetClassMultipliers(

                            EarlyGameBalanceHelper.GetClassBalanceKey(weakest.WeaponType)).DamageMultiplier * 1.08),

                    AdjustmentMagnitude = 8,

                    Reason = $"{weakest.ClassDisplayName} trails {strongest.ClassDisplayName} by {levelSpread:F1} mean levels at death.",

                    Impact = $"Raise {weakest.ClassDisplayName} combat damage multiplier ~8% without buffing all classes"

                });

            }

            else if (meanLevel > targets.MaxMeanFinalLevel)

            {

                suggestions.Add(new TuningSuggestion

                {

                    Id = "playthrough_increase_enemy_health",

                    Priority = BalanceTuningGoals.TuningPriority.Medium,

                    Category = "global",

                    Target = "All Enemies",

                    Parameter = "HealthMultiplier",

                    CurrentValue = multipliers.HealthMultiplier,

                    SuggestedValue = multipliers.HealthMultiplier * 1.05,

                    AdjustmentMagnitude = 5,

                    Reason = $"Playthrough progression high — avg level {meanLevel:F1} exceeds target max {targets.MaxMeanFinalLevel:F1}.",

                    Impact = "Increase enemy health multiplier by 5% to restore challenge"

                });

            }



            return suggestions;

        }



        private static IEnumerable<TuningSuggestion> BuildEarlyGameSuggestionsForClass(

            ClassPlaythroughAggregate aggregate,

            double severity,

            BalanceTuningGoals.TuningPriority priority,

            string reasonPrefix)

        {

            var config = GameConfiguration.Instance;

            var weaponType = aggregate.WeaponType;

            string weaponTarget = weaponType.ToString();

            double actionMult = EarlyGameBalanceHelper.GetStartingActionDamageMultiplier(weaponType);

            double suggestedActionMult = Math.Min(MaxStartingActionDamageMultiplier, actionMult * (1.0 + severity));

            if (suggestedActionMult > actionMult + 0.009)

            {

                yield return new TuningSuggestion

                {

                    Id = $"playthrough_starting_action_{weaponTarget.ToLowerInvariant()}",

                    Priority = priority,

                    Category = "early_game",

                    Target = weaponTarget,

                    Parameter = "StartingActionDamageMultiplier",

                    CurrentValue = actionMult,

                    SuggestedValue = Math.Round(suggestedActionMult, 3),

                    AdjustmentMagnitude = (suggestedActionMult - actionMult) * 100,

                    Reason = $"{reasonPrefix}; boost {aggregate.ClassDisplayName} starting-weapon actions through level {config.EarlyGame.StartingActionBonusLevelCap}.",

                    Impact = $"Starting actions for {aggregate.ClassDisplayName} deal {(suggestedActionMult - 1) * 100:F0}% more damage early game"

                };

            }



            int currentWeaponDamage = EarlyGameBalanceHelper.GetStartingWeaponDamageOverride(weaponType);

            if (currentWeaponDamage <= 0)

            {

                config.WeaponScaling!.StartingWeaponDamage.EnsureValidDefaults();

                currentWeaponDamage = weaponType switch

                {

                    WeaponType.Mace => config.WeaponScaling.StartingWeaponDamage.Mace,

                    WeaponType.Sword => config.WeaponScaling.StartingWeaponDamage.Sword,

                    WeaponType.Dagger => config.WeaponScaling.StartingWeaponDamage.Dagger,

                    WeaponType.Wand => config.WeaponScaling.StartingWeaponDamage.Wand,

                    _ => 1

                };

            }



            int weaponBoost = Math.Max(1, (int)Math.Ceiling(severity * 4));

            int suggestedWeaponDamage = Math.Min(MaxStartingWeaponDamage, currentWeaponDamage + weaponBoost);

            if (suggestedWeaponDamage > currentWeaponDamage)

            {

                yield return new TuningSuggestion

                {

                    Id = $"playthrough_starting_weapon_{weaponTarget.ToLowerInvariant()}",

                    Priority = priority,

                    Category = "starting_weapon",

                    Target = weaponTarget,

                    Parameter = "StartingWeaponDamage",

                    CurrentValue = currentWeaponDamage,

                    SuggestedValue = suggestedWeaponDamage,

                    AdjustmentMagnitude = weaponBoost,

                    Reason = $"{reasonPrefix}; {aggregate.ClassDisplayName} starter weapon underperforming (avg level {aggregate.MeanFinalLevel:F1}).",

                    Impact = $"Raise {aggregate.ClassDisplayName} starter weapon base damage {currentWeaponDamage} → {suggestedWeaponDamage}"

                };

            }



            int currentBonusPoints = EarlyGameBalanceHelper.GetStartingClassPointsBonus(weaponType);

            if (currentBonusPoints < MaxStartingClassPointsBonus)

            {

                yield return new TuningSuggestion

                {

                    Id = $"playthrough_starting_class_points_{weaponTarget.ToLowerInvariant()}",

                    Priority = BalanceTuningGoals.TuningPriority.Medium,

                    Category = "early_game",

                    Target = weaponTarget,

                    Parameter = "StartingClassPointsBonus",

                    CurrentValue = currentBonusPoints,

                    SuggestedValue = currentBonusPoints + 1,

                    AdjustmentMagnitude = 1,

                    Reason = $"{reasonPrefix}; grant {aggregate.ClassDisplayName} one extra starting class point to unlock path actions sooner.",

                    Impact = $"{aggregate.ClassDisplayName} begins with {currentBonusPoints + 1} bonus class point(s) for early unlocks"

                };

            }

        }

    }

}


