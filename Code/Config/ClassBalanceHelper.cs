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

        public static double GetDamageMultiplier(WeaponType? weaponType)
        {
            if (weaponType == null) return 1.0;
            var m = GetMultipliersForWeapon(weaponType.Value).DamageMultiplier;
            return m > 0 ? m : 1.0;
        }

        public static double GetSpeedMultiplier(WeaponType? weaponType)
        {
            if (weaponType == null) return 1.0;
            var m = GetMultipliersForWeapon(weaponType.Value).SpeedMultiplier;
            return m > 0 ? m : 1.0;
        }
    }
}
