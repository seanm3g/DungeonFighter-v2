using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Manages enemy archetype logic and attack profiles
    /// </summary>
    public static class ArchetypeManager
    {
        public static EnemyArchetype SuggestArchetypeForEnemy(string name, int strength, int agility, int technique, int intelligence)
        {
            int maxStat = System.Math.Max(System.Math.Max(strength, agility), System.Math.Max(technique, intelligence));

            if (maxStat == strength)
                return EnemyArchetype.Brute;
            if (maxStat == agility)
                return EnemyArchetype.Assassin;
            if (maxStat == technique)
                return EnemyArchetype.Berserker;
            return EnemyArchetype.Sage;
        }

        /// <summary>
        /// Default archetype profiles. <see cref="EnemyAttackProfile.SpeedMultiplier"/> multiplies attack time,
        /// so fast archetypes sit below 1.0.
        /// </summary>
        public static EnemyAttackProfile GetArchetypeProfile(EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Knight => Profile(EnemyArchetype.Knight, "Knight", armor: 1.2),
                EnemyArchetype.Assassin => Profile(EnemyArchetype.Assassin, "Assassin", speed: 1.0 / 1.2),
                EnemyArchetype.Berserker => Profile(EnemyArchetype.Berserker, "Berserker", damage: 1.2),
                EnemyArchetype.Acrobat => Profile(EnemyArchetype.Acrobat, "Acrobat", speed: 1.0 / 1.15),
                EnemyArchetype.Brute => Profile(EnemyArchetype.Brute, "Brute", health: 1.3),
                EnemyArchetype.Warlord => Profile(EnemyArchetype.Warlord, "Warlord", damage: 1.15, armor: 1.1),
                EnemyArchetype.Sage => Profile(EnemyArchetype.Sage, "Sage", damage: 1.1),
                EnemyArchetype.Duelist => Profile(EnemyArchetype.Duelist, "Duelist", speed: 1.0 / 1.1, damage: 1.05),
                EnemyArchetype.Trickster => Profile(EnemyArchetype.Trickster, "Trickster", speed: 1.0 / 1.15),
                _ => Profile(EnemyArchetype.Berserker, "Berserker")
            };
        }

        public static double GetArchetypeDamageMultiplier(EnemyAttackProfile attackProfile) =>
            GetCombatOverride(attackProfile.Archetype)?.Damage ?? attackProfile.DamageMultiplier;

        /// <summary>Attack-time multiplier for the archetype (patch <c>enemySystem.archetypeCombat</c> override, else profile default).</summary>
        public static double GetArchetypeAttackTimeMultiplier(EnemyAttackProfile attackProfile) =>
            GetCombatOverride(attackProfile.Archetype)?.AttackTime ?? attackProfile.SpeedMultiplier;

        private static ArchetypeCombatConfig? GetCombatOverride(EnemyArchetype archetype)
        {
            var table = GameConfiguration.Instance.EnemySystem?.ArchetypeCombat;
            return table != null && table.TryGetValue(archetype.ToString(), out var entry) ? entry : null;
        }

        private static EnemyAttackProfile Profile(
            EnemyArchetype archetype,
            string name,
            double speed = 1.0,
            double damage = 1.0,
            double health = 1.0,
            double armor = 1.0) =>
            new()
            {
                Archetype = archetype,
                Name = name,
                SpeedMultiplier = speed,
                DamageMultiplier = damage,
                HealthMultiplier = health,
                ArmorMultiplier = armor
            };
    }
}
