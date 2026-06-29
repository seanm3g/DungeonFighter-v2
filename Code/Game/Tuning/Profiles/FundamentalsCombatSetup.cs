using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.ActionInteractionLab;
using RPGGame.BattleStatistics;
using RPGGame.Config;
using RPGGame.Entity.Services;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// Builds a no-gear (weapon-only) lab snapshot for fundamentals combat sims.
    /// </summary>
    public static class FundamentalsCombatSetup
    {
        /// <summary>Default loader enemy for fundamentals sims (uses global 1× health scaling via <see cref="EnemyLoader"/>).</summary>
        public const string DefaultFundamentalsEnemyType = "Goblin";

        /// <summary>Hero stat multiplier for fundamentals sims (1.0 = normal level stats).</summary>
        public const double FundamentalsHeroPowerScale = 1.0;

        public static LabCombatSnapshot BuildSnapshot(SimulationProfileConfig config)
        {
            ActionLoader.ReloadActions();
            EnemyLoader.LoadEnemies();

            int level = Math.Clamp(config.PlayerLevel, 1, 99);
            var character = new Character("FundamentalsHero", level);
            StripNonWeaponGear(character);
            ApplyFundamentalsHeroPowerScale(character);

            var weaponType = ParseWeaponType(config.WeaponType);
            var weapon = CreateBaselineWeapon(weaponType);
            character.EquipItem(weapon, "weapon");
            character.InitializeDefaultCombo();

            var strip = character.GetComboActions().Select(a => a.Name).ToList();
            if (strip.Count == 0)
                throw new InvalidOperationException("Fundamentals hero has no combo actions after InitializeDefaultCombo.");

            string forced = config.ForcedCatalogAction?.Trim() ?? strip[0];
            if (ActionLoader.GetAction(forced) == null)
                throw new InvalidOperationException($"Unknown forced catalog action '{forced}'.");

            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(character);

            string enemyType = ResolveEnemyType(config);

            return new LabCombatSnapshot(
                json,
                labPanelStrDelta: 0,
                labPanelAgiDelta: 0,
                labPanelTecDelta: 0,
                labPanelIntDelta: 0,
                labPanelLevelDelta: 0,
                labPanelArmorDelta: 0,
                sessionEnemyLoaderType: enemyType,
                enemyLevel: Math.Clamp(config.EnemyLevel, 1, 99),
                comboStripActionNames: strip,
                selectedCatalogActionName: forced,
                labEnemyBattleConfig: null);
        }

        private static string ResolveEnemyType(SimulationProfileConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.EnemyType))
            {
                string requested = config.EnemyType.Trim();
                if (EnemyLoader.CreateEnemy(requested, level: 1) != null)
                    return requested;
                throw new InvalidOperationException($"Unknown fundamentals enemy type '{requested}'.");
            }

            var types = EnemyLoader.GetAllEnemyTypes();
            string? match = types.FirstOrDefault(t =>
                string.Equals(t, DefaultFundamentalsEnemyType, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;

            match = types.FirstOrDefault();
            if (match != null)
                return match;

            throw new InvalidOperationException("No enemies loaded for fundamentals simulation.");
        }

        private static void ApplyFundamentalsHeroPowerScale(Character character)
        {
            if (Math.Abs(FundamentalsHeroPowerScale - 1.0) < 0.0001)
                return;

            double scale = FundamentalsHeroPowerScale;
            character.Stats.Strength = Math.Max(1, (int)(character.Stats.Strength * scale));
            character.Stats.Agility = Math.Max(1, (int)(character.Stats.Agility * scale));
            character.Stats.Technique = Math.Max(1, (int)(character.Stats.Technique * scale));
            character.Stats.Intelligence = Math.Max(1, (int)(character.Stats.Intelligence * scale));
        }

        private static void StripNonWeaponGear(Character character)
        {
            foreach (string slot in new[] { "weapon", "head", "body", "legs", "feet" })
            {
                try { character.UnequipItem(slot); }
                catch { /* slot may already be empty */ }
            }
        }

        private static WeaponType ParseWeaponType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return WeaponType.Sword;
            return Enum.TryParse<WeaponType>(value.Trim(), true, out var wt) ? wt : WeaponType.Sword;
        }

        private static WeaponItem CreateBaselineWeapon(WeaponType weaponType)
        {
            int baseDamage = 1;
            var scaling = GameConfiguration.Instance.WeaponScaling?.StartingWeaponDamage;
            if (scaling != null)
            {
                baseDamage = weaponType switch
                {
                    WeaponType.Mace => scaling.Mace,
                    WeaponType.Sword => scaling.Sword,
                    WeaponType.Dagger => scaling.Dagger,
                    WeaponType.Wand => scaling.Wand,
                    _ => baseDamage
                };
            }

            return new WeaponItem(
                name: $"Fundamentals {weaponType}",
                tier: 1,
                baseDamage: Math.Max(1, baseDamage),
                baseAttackSpeed: 1.0,
                weaponType: weaponType);
        }
    }
}
