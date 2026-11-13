using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Region / dungeon enemy affixes (<c>EnemyAffixes.json</c>; design in
    /// <c>Documentation/ENEMY_ROOM_AFFIX_DESIGN_2026-11-12.md</c>). Every enemy carries its home region's family at the
    /// current region's rank; Rare enemies (or a Lair room) add their home dungeon's affix; the final boss becomes the
    /// dungeon's Demon. Levers reuse existing mechanics only: on-hit statuses, starting statuses, HP / damage / speed
    /// scaling and lifesteal.
    /// </summary>
    public static class EnemyAffixRuntime
    {
        public const string LeverOnHit = "onhit";
        public const string LeverStart = "start";
        public const string LeverHealth = "hp";
        public const string LeverDamage = "damage";
        public const string LeverSpeed = "speed";
        public const string LeverLifesteal = "lifesteal";
        public const string LeverTaken = "taken";
        public const string LeverEvasion = "evasion";
        public const string LeverExtraAction = "extraaction";

        /// <summary>
        /// Adds a spawn prefix's effects (EnemyPrefixes.json) as affixes named after the prefix, then reapplies HP /
        /// lifesteal. Ancient's extra action is one more signature template from the enemy's dungeon kit.
        /// </summary>
        public static void ApplyPrefixEffects(Enemy enemy, EnemyPrefixData prefix)
        {
            if (enemy == null || prefix == null || prefix.Effects.Count == 0 || !AffixFeatures.Prefix)
                return;
            foreach (var effect in prefix.Effects)
            {
                var affix = new EnemyAffixDef
                {
                    Name = prefix.Name,
                    Lever = effect.Lever,
                    Status = effect.Status,
                    Chance = effect.Chance,
                    Stacks = effect.Stacks,
                    Value = effect.Value,
                    BelowHealth = effect.BelowHealth,
                    Weapons = effect.Weapons.ToList(),
                    Description = prefix.Description
                };
                enemy.Affixes.Add(affix);
                if (Is(affix, LeverExtraAction))
                    AddSignatureActions(enemy, Math.Max(1, affix.Stacks));
            }
            ApplyStaticLevers(enemy);
        }

        /// <summary>Adds <paramref name="count"/> signature templates the enemy does not run yet (its dungeon kit, else Crush).</summary>
        private static void AddSignatureActions(Enemy enemy, int count)
        {
            var kit = AffixDataLoader.Enemy.DungeonFor(enemy.HomeDungeonTheme)?.Signature ?? new List<string> { "ENEMY CRUSH" };
            for (int i = 0; i < count; i++)
            {
                string? template = kit.FirstOrDefault(t => !enemy.ActionPool.Any(e =>
                    string.Equals(e.action.Name, SignatureName(t), StringComparison.OrdinalIgnoreCase)));
                if (template == null)
                    return;
                var action = EnemyActionCatalog.TryCreate(template);
                if (action == null)
                    return;
                action.Name = SignatureName(template);
                enemy.AddAction(action, 1.0);
            }
            enemy.InitializeActionSequence();
        }

        private static string SignatureName(string template)
        {
            string core = template.StartsWith("ENEMY ", StringComparison.OrdinalIgnoreCase) ? template[6..] : template;
            return core.Length > 1 ? char.ToUpperInvariant(core[0]) + core[1..].ToLowerInvariant() : core.ToUpperInvariant();
        }

        /// <summary>Incoming damage multiplier from taken affixes whose weapon filter matches the attacker's weapon.</summary>
        public static double DamageTakenMultiplier(Enemy enemy, Actor? attacker)
        {
            string? weapon = (attacker as Character)?.Weapon is WeaponItem w ? w.WeaponType.ToString() : null;
            double bonus = 0;
            foreach (var affix in enemy.Affixes)
            {
                if (!Is(affix, LeverTaken))
                    continue;
                if (affix.Weapons.Count > 0 && (weapon == null
                    || !affix.Weapons.Any(x => string.Equals(x, weapon, StringComparison.OrdinalIgnoreCase))))
                    continue;
                bonus += affix.Value;
            }
            return Math.Max(0.1, 1.0 + bonus);
        }

        /// <summary>Hit faces a hero loses against this enemy (Tiny).</summary>
        public static int Evasion(Enemy enemy) =>
            enemy.Affixes.Where(a => Is(a, LeverEvasion)).Sum(a => Math.Max(0, a.Stacks));

        /// <summary>One line per affix for the HUD ("Kindled II: 20% chance to Burn on hit"); prefix effects share one line.</summary>
        public static List<string> DescribeAffixes(Enemy enemy)
        {
            var lines = new List<string>();
            foreach (var affix in enemy.Affixes)
            {
                string line = string.IsNullOrWhiteSpace(affix.Description) ? affix.Name : $"{affix.Name}: {affix.Description}";
                if (!lines.Contains(line))
                    lines.Add(line);
            }
            if (enemy.PendingPhaseAction != null)
                lines.Add($"At {AffixDataLoader.Enemy.Demon.PhaseAtHealth * 100:0}% HP: adds {enemy.PendingPhaseAction.Name}");
            return lines;
        }

        /// <summary>Region cycle rank 1-5 from the region id ("forest" = 1, "fire-3" = 3); falls back to the level band.</summary>
        public static int RegionRank(TravelRegion? region, int level)
        {
            string id = region?.Id ?? "";
            int dash = id.LastIndexOf('-');
            if (dash > 0 && int.TryParse(id[(dash + 1)..], out int n) && n > 0)
                return n;
            if (region != null && !string.IsNullOrWhiteSpace(id))
                return 1;
            return Math.Clamp((Math.Max(1, level) - 1) / 20 + 1, 1, 5);
        }

        /// <summary>
        /// Gives a freshly spawned room enemy its home identity: home region family at the current rank, home dungeon
        /// affix when Rare (or <paramref name="forceDungeonAffix"/>), its dungeon's weapon material, and the HP /
        /// lifesteal levers. General enemies (no home region) take the current region's family.
        /// </summary>
        public static void ApplyHome(Enemy enemy, EnemyData? data, TravelRegion? currentRegion, bool forceDungeonAffix = false, bool isWanderer = false)
        {
            if (enemy == null)
                return;
            var file = AffixDataLoader.Enemy;
            bool regional = data != null && !EnemySpawnFilter.IsPlacementWildcard(data.Region);
            string? homeRegion = regional ? data!.Region : currentRegion?.Theme;
            string? homeDungeon = regional ? data!.Biome : null;
            var identity = file.DungeonFor(homeDungeon);

            enemy.IsWanderer = isWanderer;
            enemy.HomeDungeonTheme = identity?.Theme;
            enemy.Affixes.Clear();
            enemy.RegionAffixName = null;

            var family = file.FamilyFor(homeRegion);
            if (family != null && AffixFeatures.Region)
            {
                var regionAffix = family.AtRank(RegionRank(currentRegion, enemy.Level));
                enemy.Affixes.Add(regionAffix);
                enemy.RegionAffixName = regionAffix.Name;
            }

            bool rare = string.Equals(data?.Rarity, "rare", StringComparison.OrdinalIgnoreCase);
            if ((rare || forceDungeonAffix) && identity?.Affix != null && AffixFeatures.Dungeon)
                enemy.Affixes.Add(identity.Affix);

            if (identity != null && enemy.Weapon != null && !string.IsNullOrWhiteSpace(identity.Material))
                enemy.Weapon.Material = identity.Material;

            ApplyStaticLevers(enemy);
        }

        /// <summary>
        /// Tools that create one enemy outside a dungeon (Action Lab, Settings quick sims): its home affixes at the rank
        /// its level falls in. Returns <paramref name="enemy"/> so calls can wrap <c>EnemyLoader.CreateEnemy</c>.
        /// </summary>
        public static Enemy? WithHomeAffixes(Enemy? enemy)
        {
            if (enemy != null)
                ApplyHome(enemy, EnemyLoader.GetEnemyData(enemy.BaseName), currentRegion: null);
            return enemy;
        }

        /// <summary>
        /// Turns the final boss into its dungeon's Demon: region family at rank + bonus, the dungeon affix, two signature
        /// actions up front (pool capped at <see cref="EnemyDemonConfig.MaxActions"/>), the third signature held for the
        /// second phase, the dungeon's weapon material and its title.
        /// </summary>
        public static void MakeDemon(Enemy boss, string? dungeonTheme, TravelRegion? currentRegion)
        {
            if (boss == null)
                return;
            var file = AffixDataLoader.Enemy;
            var identity = file.DungeonFor(dungeonTheme);
            if (identity == null)
                return;
            var demon = file.Demon;

            boss.Affixes.Clear();
            boss.RegionAffixName = null;
            var family = file.FamilyFor(identity.Region);
            if (family != null && AffixFeatures.Region)
            {
                var regionAffix = family.AtRank(RegionRank(currentRegion, boss.Level) + Math.Max(0, demon.RegionRankBonus));
                boss.Affixes.Add(regionAffix);
                boss.RegionAffixName = regionAffix.Name;
            }
            if (identity.Affix != null && AffixFeatures.Dungeon)
                boss.Affixes.Add(identity.Affix);
            boss.HomeDungeonTheme = identity.Theme;
            if (boss.Weapon != null && !string.IsNullOrWhiteSpace(identity.Material))
                boss.Weapon.Material = identity.Material;

            var signature = identity.Signature
                .Select(CreateSignatureAction)
                .Where(a => a != null)
                .Cast<Action>()
                .ToList();
            var opening = signature.Take(2).ToList();
            boss.PendingPhaseAction = signature.Count > 2 ? signature[2] : null;
            boss.InSecondPhase = false;

            var merged = boss.ActionPool
                .Where(entry => !opening.Any(s => string.Equals(s.Name, entry.action.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            int cap = Math.Max(opening.Count, demon.MaxActions);
            boss.ActionPool.Clear();
            foreach (var action in opening)
                boss.ActionPool.Add((action, 1.0));
            foreach (var entry in merged.Take(Math.Max(0, cap - opening.Count)))
                boss.ActionPool.Add(entry);
            boss.InitializeActionSequence();

            ApplyStaticLevers(boss);
            boss.AppendTitle(identity.Demon);
        }

        /// <summary>The Demon's version of a signature template ("ENEMY REND" → "Infernal Rend").</summary>
        public static Action? CreateSignatureAction(string templateName)
        {
            var action = EnemyActionCatalog.TryCreate(templateName);
            if (action == null)
                return null;
            string core = templateName.StartsWith("ENEMY ", StringComparison.OrdinalIgnoreCase) ? templateName[6..] : templateName;
            core = core.Length > 1 ? char.ToUpperInvariant(core[0]) + core[1..].ToLowerInvariant() : core.ToUpperInvariant();
            action.Name = $"Infernal {core}";
            return action;
        }

        /// <summary>HP and lifesteal: applied when affixes are (re)built. HP rescales from what earlier affixes applied.</summary>
        private static void ApplyStaticLevers(Enemy enemy)
        {
            // Affix HP plus the patch's per-dungeon HP scale (enemySystem.dungeonScale, by home dungeon).
            double hp = (1.0 + enemy.Affixes.Where(a => Is(a, LeverHealth)).Sum(a => a.Value)) * DungeonScale(enemy).health;
            double ratio = hp / Math.Max(1e-6, enemy.AppliedAffixHealthMultiplier);
            enemy.ScaleMaxHealth(ratio);
            enemy.AppliedAffixHealthMultiplier = hp;

            double lifesteal = enemy.Affixes.Where(a => Is(a, LeverLifesteal)).Sum(a => a.Value) * 100.0;
            if (lifesteal > 0)
            {
                foreach (var (action, _) in enemy.ActionPool)
                {
                    if (action.Advanced != null && action.Advanced.LifestealPercent < lifesteal)
                        action.Advanced.LifestealPercent = lifesteal;
                }
                if (enemy.PendingPhaseAction?.Advanced is { } pending && pending.LifestealPercent < lifesteal)
                    pending.LifestealPercent = lifesteal;
            }
        }

        /// <summary>Statuses the enemy starts each fight with: its start affixes plus the room's start status.</summary>
        public static List<(string status, int stacks)> StartStatuses(Enemy enemy, Environment? room)
        {
            var list = enemy.Affixes
                .Where(a => Is(a, LeverStart) && !string.IsNullOrWhiteSpace(a.Status))
                .Select(a => (a.Status.Trim().ToLowerInvariant(), Math.Max(1, a.Stacks)))
                .ToList();
            if (room?.RoomAffix is { } roomAffix && !string.IsNullOrWhiteSpace(roomAffix.EnemyStartStatus))
                list.Add((roomAffix.EnemyStartStatus.Trim().ToLowerInvariant(), Math.Max(1, roomAffix.EnemyStartStacks)));
            return list;
        }

        /// <summary>
        /// Statuses an enemy's landed hit applies, with their amount (each on-hit affix rolls its chance; the phase
        /// strengthens the region one). Amount = the affix's value (poison: % of max HP per tick; 0 = the status default).
        /// </summary>
        public static List<(string status, double amount)> RollOnHitStatuses(Enemy enemy, Random random)
        {
            var result = new List<(string status, double amount)>();
            foreach (var affix in enemy.Affixes)
            {
                if (!Is(affix, LeverOnHit) || string.IsNullOrWhiteSpace(affix.Status))
                    continue;
                double chance = Math.Min(1.0, affix.Chance * PhaseFactor(enemy, affix));
                if (chance > 0 && random.NextDouble() < chance)
                    result.Add((affix.Status.Trim().ToLowerInvariant(), affix.Value));
            }
            return result;
        }

        /// <summary>Outgoing damage multiplier from damage affixes (HP-gated ones only below their threshold).</summary>
        public static double DamageMultiplier(Enemy enemy)
        {
            double bonus = 0;
            foreach (var affix in enemy.Affixes)
            {
                if (!Is(affix, LeverDamage) || !BelowHealthMet(enemy, affix))
                    continue;
                bonus += affix.Value * PhaseFactor(enemy, affix);
            }
            return (1.0 + bonus) * DungeonScale(enemy).damage;
        }

        /// <summary>Swing time multiplier from speed affixes (+20% speed → ×1/1.2).</summary>
        public static double AttackTimeMultiplier(Enemy enemy)
        {
            double speed = enemy.Affixes
                .Where(a => Is(a, LeverSpeed) && BelowHealthMet(enemy, a))
                .Sum(a => a.Value * PhaseFactor(enemy, a));
            return Math.Abs(speed) > 1e-9 ? 1.0 / Math.Max(0.1, 1.0 + speed) : 1.0;
        }

        /// <summary>The patch's damage / HP scale for the enemy's home dungeon (enemySystem.dungeonScale).</summary>
        public static (double damage, double health) DungeonScale(Enemy enemy) =>
            GameConfiguration.Instance.EnemySystem?.DungeonScaleFor(enemy.HomeDungeonTheme) ?? (1.0, 1.0);

        private static bool BelowHealthMet(Enemy enemy, EnemyAffixDef affix) =>
            affix.BelowHealth <= 0 || enemy.MaxHealth <= 0 || enemy.CurrentHealth <= enemy.MaxHealth * affix.BelowHealth;

        /// <summary>Demon second phase: at or below the phase HP the held signature action joins its sequence.</summary>
        public static bool CheckPhase(Enemy enemy)
        {
            if (enemy == null || !enemy.IsBoss || enemy.InSecondPhase || enemy.MaxHealth <= 0)
                return false;
            double at = AffixDataLoader.Enemy.Demon.PhaseAtHealth;
            if (at <= 0 || enemy.CurrentHealth > enemy.MaxHealth * at)
                return false;
            enemy.InSecondPhase = true;
            if (enemy.PendingPhaseAction != null)
            {
                enemy.AddAction(enemy.PendingPhaseAction, 1.0);
                enemy.PendingPhaseAction = null;
                enemy.InitializeActionSequence();
            }
            return true;
        }

        private static double PhaseFactor(Enemy enemy, EnemyAffixDef affix) =>
            enemy.InSecondPhase && affix.Name == enemy.RegionAffixName
                ? Math.Max(1.0, AffixDataLoader.Enemy.Demon.PhaseRegionMultiplier)
                : 1.0;

        private static bool Is(EnemyAffixDef affix, string lever) =>
            string.Equals(affix.Lever?.Trim(), lever, StringComparison.OrdinalIgnoreCase);
    }
}
