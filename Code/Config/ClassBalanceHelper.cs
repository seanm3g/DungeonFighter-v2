namespace RPGGame
{
    /// <summary>Resolves per-weapon class balance multipliers from tuning config.</summary>
    public static class ClassBalanceHelper
    {
        public static ClassMultipliers GetMultipliersForWeapon(WeaponType weaponType, ClassBalanceConfig? classBalance = null)
        {
            classBalance ??= GameConfiguration.Instance.ClassBalance;
            return weaponType switch
            {
                WeaponType.Mace => classBalance.Barbarian,
                WeaponType.Sword => classBalance.Warrior,
                WeaponType.Dagger => classBalance.Rogue,
                WeaponType.Wand => classBalance.Wizard,
                _ => new ClassMultipliers { HealthMultiplier = 1, DamageMultiplier = 1, SpeedMultiplier = 1 }
            };
        }

        /// <summary>Class damage multiplier; with <paramref name="level"/> &gt; 0 it includes the class's level damage curve.</summary>
        public static double GetDamageMultiplier(WeaponType? weaponType, int level = 0)
        {
            if (weaponType == null) return 1.0;
            var multipliers = GetMultipliersForWeapon(weaponType.Value);
            double m = multipliers.DamageMultiplier > 0 ? multipliers.DamageMultiplier : 1.0;
            if (level > 0 && multipliers.DamageCurve != null)
                m *= multipliers.DamageCurve.MultiplierAt(level);
            return m;
        }

        public static double GetSpeedMultiplier(WeaponType? weaponType)
        {
            if (weaponType == null) return 1.0;
            var m = GetMultipliersForWeapon(weaponType.Value).SpeedMultiplier;
            return m > 0 ? m : 1.0;
        }
    }
}
