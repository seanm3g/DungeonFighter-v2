using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>Runtime context used to filter enemy templates by placement metadata from <c>Enemies.json</c>.</summary>
    public readonly record struct EnemySpawnContext(string? RegionId, string? Biome, string? Location);

    /// <summary>Which spawn tier won the roll for this encounter pick.</summary>
    public enum EnemySpawnTier
    {
        Common,
        UncommonBiome,
        UncommonRegion,
        UncommonLocation,
        RareLocation,
        Anywhere
    }

    /// <summary>Filters and tier-rolls enemy rows by rarity + region/biome/location.</summary>
    public static class EnemySpawnFilter
    {
        /// <summary>Sheet sentinel values that mean "any" for region, biome, or location columns.</summary>
        public static bool IsPlacementWildcard(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;
            var t = value.Trim();
            return t.Equals("n/a", StringComparison.OrdinalIgnoreCase)
                || t.Equals("na", StringComparison.OrdinalIgnoreCase)
                || t.Equals("any", StringComparison.OrdinalIgnoreCase)
                || t == "*"
                || t.Equals("general", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Builds spawn context from a generated room plus dungeon travel metadata.</summary>
        public static EnemySpawnContext BuildSpawnContext(Environment room, string? spawnRegionId, string dungeonTheme)
        {
            var roomData = RoomLoader.GetRoomData(room.Name);
            string? region = !string.IsNullOrWhiteSpace(roomData?.Region)
                ? roomData!.Region.Trim()
                : !string.IsNullOrWhiteSpace(spawnRegionId)
                    ? spawnRegionId.Trim()
                    : null;
            string? biome = !string.IsNullOrWhiteSpace(roomData?.Biome)
                ? roomData!.Biome.Trim()
                : dungeonTheme;
            string? location = roomData?.GetLocationKey();
            if (string.IsNullOrWhiteSpace(location))
                location = room.Name;
            return new EnemySpawnContext(region, biome, location);
        }

        public static List<EnemyData> Filter(IEnumerable<EnemyData> pool, EnemySpawnContext ctx, TravelRegion? resolvedRegion = null)
        {
            return pool.Where(e => MatchesPlacement(e, ctx, resolvedRegion)).ToList();
        }

        public static bool MatchesPlacement(EnemyData enemy, EnemySpawnContext ctx, TravelRegion? resolvedRegion = null)
        {
            if (!MatchesRegion(enemy.Region, ctx.RegionId, resolvedRegion))
                return false;
            if (!MatchesTokenList(enemy.Biome, ctx.Biome))
                return false;
            if (!MatchesTokenList(enemy.Location, ctx.Location))
                return false;
            return true;
        }

        /// <summary>
        /// Rolls a spawn tier then picks uniformly from matching enemies.
        /// Default weights: 50% Common, 10% Uncommon+biome, 15% Uncommon+region, 15% Uncommon+location,
        /// 5% Rare+location, 5% anywhere (within <paramref name="pool"/>).
        /// </summary>
        public static EnemyData PickByTieredSpawnRoll(
            IReadOnlyList<EnemyData> pool,
            EnemySpawnContext ctx,
            TravelRegion? resolvedRegion,
            Random rng,
            EnemySpawnTierWeightsConfig? weights = null)
        {
            if (pool.Count == 0)
                throw new ArgumentException("Cannot pick from empty enemy pool.", nameof(pool));
            if (pool.Count == 1)
                return pool[0];

            weights ??= ResolveSpawnTierWeights(ctx, resolvedRegion);
            var tier = RollSpawnTier(rng, weights);
            var candidates = GetCandidatesForTier(pool, tier, ctx, resolvedRegion);
            if (candidates.Count == 0)
                candidates = GetCandidatesForTier(pool, EnemySpawnTier.Anywhere, ctx, resolvedRegion);
            if (candidates.Count == 0)
                candidates = GetCandidatesForTier(pool, EnemySpawnTier.Common, ctx, resolvedRegion);
            if (candidates.Count == 0)
                candidates = pool.ToList();

            return candidates[rng.Next(candidates.Count)];
        }

        public static EnemySpawnTierWeightsConfig ResolveSpawnTierWeights(
            EnemySpawnContext ctx,
            TravelRegion? resolvedRegion,
            EnemySystemConfig? enemySystem = null)
        {
            enemySystem ??= GameConfiguration.Instance?.EnemySystem;
            enemySystem?.EnsureSanitizedDefaults();
            var settlement = SettlementTypeResolver.Resolve(ctx.Location, resolvedRegion, enemySystem);
            return enemySystem?.GetSpawnTierWeights(settlement)
                   ?? EnemySpawnTierWeightsConfig.CreateDefaults();
        }

        public static EnemySpawnTier RollSpawnTier(Random rng, EnemySpawnTierWeightsConfig weights) =>
            ResolveSpawnTierFromRoll(rng.Next(100), weights);

        /// <summary>Maps a 0–99 roll to a spawn tier using cumulative percent weights.</summary>
        public static EnemySpawnTier ResolveSpawnTierFromRoll(int roll, EnemySpawnTierWeightsConfig weights)
        {
            roll = Math.Clamp(roll, 0, 99);
            int acc = 0;

            acc += Math.Max(0, weights.CommonPercent);
            if (roll < acc) return EnemySpawnTier.Common;

            acc += Math.Max(0, weights.UncommonBiomePercent);
            if (roll < acc) return EnemySpawnTier.UncommonBiome;

            acc += Math.Max(0, weights.UncommonRegionPercent);
            if (roll < acc) return EnemySpawnTier.UncommonRegion;

            acc += Math.Max(0, weights.UncommonLocationPercent);
            if (roll < acc) return EnemySpawnTier.UncommonLocation;

            acc += Math.Max(0, weights.RareLocationPercent);
            if (roll < acc) return EnemySpawnTier.RareLocation;

            return EnemySpawnTier.Anywhere;
        }

        public static List<EnemyData> GetCandidatesForTier(
            IReadOnlyList<EnemyData> pool,
            EnemySpawnTier tier,
            EnemySpawnContext ctx,
            TravelRegion? resolvedRegion)
        {
            return tier switch
            {
                EnemySpawnTier.Common => pool.Where(e => HasRarity(e, "Common")).ToList(),
                EnemySpawnTier.UncommonBiome => pool.Where(e =>
                    HasRarity(e, "Uncommon") && MatchesBiomePlacement(e, ctx)).ToList(),
                EnemySpawnTier.UncommonRegion => pool.Where(e =>
                    HasRarity(e, "Uncommon") && MatchesRegionPlacement(e, ctx, resolvedRegion)).ToList(),
                EnemySpawnTier.UncommonLocation => pool.Where(e =>
                    HasRarity(e, "Uncommon") && MatchesLocationPlacement(e, ctx)).ToList(),
                EnemySpawnTier.RareLocation => pool.Where(e =>
                    HasRarity(e, "Rare") && MatchesLocationPlacement(e, ctx)).ToList(),
                EnemySpawnTier.Anywhere => pool.ToList(),
                _ => pool.ToList()
            };
        }

        public static bool MatchesBiomePlacement(EnemyData enemy, EnemySpawnContext ctx)
        {
            if (IsPlacementWildcard(enemy.Biome) || string.IsNullOrWhiteSpace(ctx.Biome))
                return false;
            return MatchesTokenList(enemy.Biome, ctx.Biome);
        }

        public static bool MatchesRegionPlacement(EnemyData enemy, EnemySpawnContext ctx, TravelRegion? resolvedRegion)
        {
            if (IsPlacementWildcard(enemy.Region) || string.IsNullOrWhiteSpace(enemy.Region))
                return false;
            if (string.IsNullOrWhiteSpace(ctx.RegionId) && resolvedRegion == null)
                return false;
            return MatchesRegion(enemy.Region, ctx.RegionId, resolvedRegion);
        }

        public static bool MatchesLocationPlacement(EnemyData enemy, EnemySpawnContext ctx)
        {
            if (IsPlacementWildcard(enemy.Location) || string.IsNullOrWhiteSpace(enemy.Location)
                || string.IsNullOrWhiteSpace(ctx.Location))
                return false;
            return MatchesTokenList(enemy.Location, ctx.Location);
        }

        public static bool HasRarity(EnemyData enemy, string rarity) =>
            string.Equals(ResolveRarityName(enemy.Rarity), rarity.Trim(), StringComparison.OrdinalIgnoreCase);

        public static bool MatchesRegion(string? filterCell, string? regionId, TravelRegion? resolvedRegion)
        {
            if (IsPlacementWildcard(filterCell))
                return true;

            var tokens = GameDataTagHelper.ParseCommaSeparatedTags(filterCell);
            if (tokens.Count == 0)
                return true;
            if (tokens.All(IsPlacementWildcard))
                return true;

            var matchAgainst = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(regionId))
                matchAgainst.Add(regionId.Trim());
            if (resolvedRegion != null)
            {
                if (!string.IsNullOrWhiteSpace(resolvedRegion.Id))
                    matchAgainst.Add(resolvedRegion.Id.Trim());
                if (!string.IsNullOrWhiteSpace(resolvedRegion.DisplayName))
                    matchAgainst.Add(resolvedRegion.DisplayName.Trim());
                if (!string.IsNullOrWhiteSpace(resolvedRegion.Theme))
                    matchAgainst.Add(resolvedRegion.Theme.Trim());
            }

            if (matchAgainst.Count == 0)
                return true;

            foreach (var token in tokens)
            {
                if (IsPlacementWildcard(token))
                    return true;
                if (matchAgainst.Contains(token))
                    return true;
            }

            return false;
        }

        /// <summary>Empty filter cell matches any context value; empty context matches any enemy cell.</summary>
        public static bool MatchesTokenList(string? filterCell, string? contextValue)
        {
            if (IsPlacementWildcard(filterCell))
                return true;
            if (string.IsNullOrWhiteSpace(contextValue))
                return true;

            var tokens = GameDataTagHelper.ParseCommaSeparatedTags(filterCell);
            if (tokens.Count == 0)
                return true;

            string ctx = contextValue.Trim();
            foreach (var token in tokens)
            {
                if (IsPlacementWildcard(token))
                    return true;
                if (string.Equals(token, ctx, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static string ResolveRarityName(string? rarity) =>
            string.IsNullOrWhiteSpace(rarity) ? "Common" : rarity.Trim();
    }
}
