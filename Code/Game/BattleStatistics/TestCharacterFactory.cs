using System;
using System.Linq;
using RPGGame.Entity;
using RPGGame.Items;
using RPGGame.Data;
using RPGGame.Config;
using RPGGame.Utils;
using RPGGame;

namespace RPGGame.BattleStatistics
{
    /// <summary>
    /// Factory for creating test characters and enemies with specific configurations
    /// </summary>
    public static class TestCharacterFactory
    {
        /// <summary>
        /// Creates a test character with custom stats using real game systems
        /// </summary>
        public static Character CreateTestCharacter(string name, int damage, double attackSpeed, int armor, int health)
        {
            var character = new Character(name, level: 1);
            
            character.MaxHealth = health;
            character.CurrentHealth = health;
            
            var tuning = GameConfiguration.Instance;
            
            WeaponItem? weapon = LoadRealWeapon(WeaponType.Sword);
            if (weapon == null)
            {
                weapon = new WeaponItem("Test Sword", 1, 5, 1.0, WeaponType.Sword);
            }
            
            int baseStrength = Math.Max(1, damage / 2);
            character.Strength = baseStrength;
            
            int currentWeaponDamage = weapon.GetTotalDamage();
            int targetWeaponDamage = Math.Max(1, damage - baseStrength);
            weapon.BaseDamage = Math.Max(1, targetWeaponDamage - weapon.BonusDamage);
            weapon.BaseAttackSpeed = attackSpeed;
            
            character.EquipItem(weapon, "weapon");
            character.InitializeDefaultCombo();
            
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            int targetAgility = (int)Math.Max(1, (baseAttackTime - attackSpeed) / tuning.Combat.AgilitySpeedReduction);
            character.Agility = targetAgility;
            
            if (armor > 0)
            {
                int headArmor = Math.Max(1, armor / 3);
                int chestArmor = Math.Max(1, armor / 2);
                int feetArmor = Math.Max(1, armor - headArmor - chestArmor);
                
                var headItem = new HeadItem("Test Helmet", 1, headArmor);
                var chestItem = new ChestItem("Test Chest", 1, chestArmor);
                var feetItem = new FeetItem("Test Boots", 1, feetArmor);
                
                character.EquipItem(headItem, "head");
                character.EquipItem(chestItem, "body");
                character.EquipItem(feetItem, "feet");
            }
            
            return character;
        }

        /// <summary>
        /// Creates a test character with a specific weapon type
        /// </summary>
        public static Character CreateTestCharacterWithWeapon(string name, WeaponType weaponType, int level)
        {
            var character = new Character(name, level);

            int baseDamage = 1;
            var weaponScaling = GameConfiguration.Instance.WeaponScaling;
            if (weaponScaling?.StartingWeaponDamage != null)
            {
                baseDamage = weaponType switch
                {
                    WeaponType.Mace => weaponScaling.StartingWeaponDamage.Mace,
                    WeaponType.Sword => weaponScaling.StartingWeaponDamage.Sword,
                    WeaponType.Dagger => weaponScaling.StartingWeaponDamage.Dagger,
                    WeaponType.Wand => weaponScaling.StartingWeaponDamage.Wand,
                    _ => baseDamage
                };
            }

            var weapon = new WeaponItem(
                name: $"{weaponType} Test Weapon",
                tier: 1,
                baseDamage: baseDamage,
                baseAttackSpeed: 1.0,
                weaponType: weaponType
            );

            character.EquipItem(weapon, "weapon");
            character.Actions.AddClassActions(character, character.Progression, weaponType);

            return character;
        }

        /// <summary>
        /// Creates a test enemy with the given configuration
        /// </summary>
        public static Enemy CreateTestEnemy(BattleConfiguration config, int battleIndex)
        {
            var allEnemyTypes = EnemyLoader.GetAllEnemyTypes();
            
            if (allEnemyTypes.Count > 0)
            {
                var enemyType = allEnemyTypes[0];
                var enemyData = EnemyLoader.GetEnemyData(enemyType);
                
                if (enemyData != null)
                {
                    EnemyArchetype archetype = EnemyArchetype.Berserker;
                    if (Enum.TryParse<EnemyArchetype>(enemyData.Archetype, true, out var parsedArchetype))
                    {
                        archetype = parsedArchetype;
                    }
                    
                    var enemy = new Enemy(
                        name: enemyData.Name,
                        level: 1,
                        maxHealth: config.EnemyHealth,
                        damage: config.EnemyDamage,
                        armor: config.EnemyArmor,
                        attackSpeed: config.EnemyAttackSpeed,
                        primaryAttribute: PrimaryAttribute.Strength,
                        isLiving: enemyData.IsLiving,
                        archetype: archetype,
                        useDirectStats: true
                    );
                    
                    var realEnemy = EnemyLoader.CreateEnemy(enemyType, level: 1);
                    if (realEnemy != null && realEnemy.Weapon != null)
                    {
                        enemy.Weapon = realEnemy.Weapon;
                    }
                    
                    return enemy;
                }
            }
            
            // Fallback: create enemy with direct stats
            return new Enemy(
                name: $"TestEnemy_{battleIndex}",
                level: 1,
                maxHealth: config.EnemyHealth,
                damage: config.EnemyDamage,
                armor: config.EnemyArmor,
                attackSpeed: config.EnemyAttackSpeed,
                primaryAttribute: PrimaryAttribute.Strength,
                isLiving: true,
                archetype: EnemyArchetype.Berserker,
                useDirectStats: true
            );
        }

        /// <summary>
        /// Creates a test environment
        /// </summary>
        public static Environment CreateTestEnvironment()
        {
            return new Environment(
                name: "Test Room",
                description: "Testing environment",
                isHostile: false,
                theme: "neutral"
            );
        }

        /// <summary>
        /// Loads a real weapon from Weapons.json
        /// </summary>
        private static WeaponItem? LoadRealWeapon(WeaponType preferredType)
        {
            try
            {
                var weaponDataList = JsonLoader.LoadJsonList<WeaponData>("Weapons.json");
                if (weaponDataList == null || weaponDataList.Count == 0)
                    return null;
                
                var preferredWeapon = weaponDataList.FirstOrDefault(w => 
                    Enum.TryParse<WeaponType>(w.Type, true, out var wt) && wt == preferredType);
                
                var weaponData = preferredWeapon ?? weaponDataList[0];
                
                if (Enum.TryParse<WeaponType>(weaponData.Type, true, out var weaponType))
                {
                    return new WeaponItem(
                        weaponData.Name,
                        weaponData.Tier,
                        weaponData.BaseDamage,
                        weaponData.AttackSpeed,
                        weaponType
                    );
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"TestCharacterFactory: Error loading weapon: {ex.Message}");
            }
            
            return null;
        }
    }
}

