using System;

namespace RPGGame
{
    /// <summary>
    /// Authored swing cadence from the ENEMIES sheet <c>tempo</c> column. Stretches identity-mode swing time while
    /// <see cref="EnemyIdentityProfile.HitDamage"/> (= AttackTime ÷ Health) scales hit size the opposite way, so damage per
    /// second is unchanged: a "heavy" enemy swings ~3 times in a typical 11-swing fight for big hits, a "swarm" ~20 times
    /// for small hits between the hero's attacks.
    /// </summary>
    public static class EnemyTempo
    {
        public const string Swarm = "swarm";
        public const string Fast = "fast";
        public const string Normal = "normal";
        public const string Slow = "slow";
        public const string Heavy = "heavy";

        /// <summary>Swing-time multiplier for a tempo label (unknown / blank = 1).</summary>
        public static double AttackTimeMultiplier(string? tempo) => (tempo ?? "").Trim().ToLowerInvariant() switch
        {
            Swarm => 0.55,
            Fast => 0.75,
            Slow => 1.6,
            Heavy => 3.5,
            _ => 1.0
        };

        /// <summary>Short HUD / tooltip description ("Heavy: few, crushing swings").</summary>
        public static string Describe(string? tempo) => (tempo ?? "").Trim().ToLowerInvariant() switch
        {
            Swarm => "Swarm: constant small hits between your attacks",
            Fast => "Fast: quick, lighter hits",
            Slow => "Slow: fewer, harder hits",
            Heavy => "Heavy: rare, crushing hits",
            _ => "Normal tempo"
        };
    }
}
