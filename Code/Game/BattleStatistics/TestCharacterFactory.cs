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
            // New agility formula: speed = baseAttackTime * (1.0 - (agility - 1) / 99.0)
            // Solving for agility: agility = 1 + (1.0 - speed/baseAttackTime) * 99.0
            double speedMultiplier = attackSpeed / baseAttackTime;
            int targetAgility = (int)Math.Max(1, Math.Min(100, 1 + (1.0 - speedMultiplier) * 99.0));
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
        /// Creates a test enemy with the given configuration. <paramref name="level"/> scales direct stats for Action Lab / simulations (1–99).
        /// </summary>
        public static Enemy CreateTestEnemy(BattleConfiguration config, int battleIndex, int level = 1)
        {
            level = Math.Clamp(level, 1, 99);
            var (hp, damage, armor, attackSpeed) = ScaleLabDirectEnemyStats(config, level);

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
                        level: level,
                        maxHealth: hp,
                        damage: damage,
                        armor: armor,
                        attackSpeed: attackSpeed,
                        primaryAttribute: PrimaryAttribute.Strength,
                        isLiving: enemyData.IsLiving,
                        archetype: archetype,
                        useDirectStats: true);

                    var realEnemy = EnemyLoader.CreateEnemy(enemyType, level);
                    if (realEnemy != null && realEnemy.Weapon != null)
                        enemy.Weapon = realEnemy.Weapon;

                    return enemy;
                }
            }

            return new Enemy(
                name: $"TestEnemy_{battleIndex}",
                level: level,
                maxHealth: hp,
                damage: damage,
                armor: armor,
                attackSpeed: attackSpeed,
                primaryAttribute: PrimaryAttribute.Strength,
                isLiving: true,
                archetype: EnemyArchetype.Berserker,
                useDirectStats: true);
        }

        /// <summary>
        /// Per-level scaling for lab / battle-stat test dummies (direct-stat enemies), aligned loosely with world tuning.
        /// </summary>
        private static (int hp, int damage, int armor, double attackSpeed) ScaleLabDirectEnemyStats(BattleConfiguration config, int level)
        {
            var tuning = GameConfiguration.Instance;
            int hpPer = tuning.Character.EnemyHealthPerLevel;
            if (hpPer <= 0)
                hpPer = 10;
            int attrPer = tuning.Attributes.EnemyAttributesPerLevel;
            if (attrPer <= 0)
                attrPer = 2;
            double armorPer = tuning.EnemyScaling?.EnemyArmorPerLevel ?? 1.0;

            int lv = level - 1;
            int hp = Math.Max(1, config.EnemyHealth + lv * hpPer);
            int damage = Math.Max(0, config.EnemyDamage + lv * Math.Max(1, attrPer / 2));
            int armor = Math.Max(0, config.EnemyArmor + (int)(lv * armorPer));
            double spd = Math.Max(0.1, config.EnemyAttackSpeed * (1.0 - 0.004 * lv));
            return (hp, damage, armor, spd);
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

