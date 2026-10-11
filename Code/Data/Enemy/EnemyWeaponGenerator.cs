using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Generates weapons for enemies
    /// </summary>
    public static class EnemyWeaponGenerator
    {
        private static List<WeaponData>? _weaponData;

        /// <summary>
        /// Generates a common-tier weapon for an enemy
        /// </summary>
        /// <param name="enemyName">The name of the enemy</param>
        /// <param name="enemyLevel">The level of the enemy</param>
        /// <returns>A common-tier weapon appropriate for the enemy</returns>
        public static WeaponItem GenerateCommonWeaponForEnemy(string enemyName, int enemyLevel)
        {
            // Load weapon data if not already loaded
            if (_weaponData == null)
            {
                LoadWeaponData();
            }

            // Get only tier 1 (common) weapons
            var commonWeapons = _weaponData?.Where(w => w.Tier == 1).ToList() ?? new List<WeaponData>();

            if (!commonWeapons.Any())
            {
                // Fallback to basic weapon if no common weapons found
                return new WeaponItem($"{enemyName} Weapon", 1, 6, 0.0, WeaponType.Sword);
            }

            // Select a random common weapon
            var selectedWeapon = commonWeapons[RandomUtility.Next(commonWeapons.Count)];

            // Generate the weapon item
            var weapon = ItemGenerator.GenerateWeaponItem(selectedWeapon);

            // Enemy weapon damage: its own knob when set, otherwise the hero starting weapon damage (legacy coupling).
            int enemyWeaponDamage = GameConfiguration.Instance?.EnemySystem?.BaselineStats?.WeaponDamage ?? 0;
            var weaponScaling = GameConfiguration.Instance?.WeaponScaling;
            if (enemyWeaponDamage > 0)
            {
                weapon.BaseDamage = enemyWeaponDamage;
            }
            else if (weaponScaling?.StartingWeaponDamage != null)
            {
                double configDamage = weapon.WeaponType switch
                {
                    WeaponType.Mace => weaponScaling.StartingWeaponDamage.Mace,
                    WeaponType.Sword => weaponScaling.StartingWeaponDamage.Sword,
                    WeaponType.Dagger => weaponScaling.StartingWeaponDamage.Dagger,
                    WeaponType.Wand => weaponScaling.StartingWeaponDamage.Wand,
                    _ => weapon.BaseDamage
                };

                // Override the base damage with the tuned value
                weapon.BaseDamage = (int)configDamage;
            }

            // Ensure it's marked as common rarity
            weapon.Rarity = "Common";

            return weapon;
        }

        /// <summary>
        /// Loads weapon data from JSON file
        /// </summary>
        private static void LoadWeaponData()
        {
            string? filePath = JsonLoader.FindGameDataFile("Weapons.json");
            if (filePath != null)
            {
                _weaponData = JsonLoader.LoadJsonList<WeaponData>(filePath);
            }
            else
            {
                UIManager.WriteLine("Error loading weapon data: Weapons.json not found", UIMessageType.System);
                _weaponData = new List<WeaponData>();
            }
        }
    }
}

