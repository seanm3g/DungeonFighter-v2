using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// One-time / incremental fixes when loading <see cref="EnemyData"/> from JSON (legacy <c>overrides</c> object, etc.).
    /// </summary>
    public static class EnemyDataPostLoad
    {
        /// <summary>
        /// Applies legacy <c>overrides</c> (captured in <see cref="EnemyData.ExtensionData"/> when the typed property was removed)
        /// then folds root stat keys. Call after <see cref="System.Text.Json.JsonSerializer.Deserialize{T}"/> on each enemy.
        /// </summary>
        public static void Apply(EnemyData enemy)
        {
            MigrateOverridesObjectFromExtensionData(enemy);
            EnemyDataLegacyRootStats.FoldExtensionDataIntoTypedFields(enemy);
        }

        private static void MigrateOverridesObjectFromExtensionData(EnemyData enemy)
        {
            if (enemy.ExtensionData == null || !enemy.ExtensionData.TryGetValue("overrides", out var ovEl))
                return;

            if (ovEl.ValueKind == JsonValueKind.Object
                && ovEl.TryGetProperty("health", out var h)
                && h.ValueKind == JsonValueKind.Number)
            {
                double hv = h.GetDouble();
                if (enemy.BaseHealth.HasValue)
                    enemy.BaseHealth = enemy.BaseHealth.Value * hv;
                else if (enemy.HealthGrowthPerLevel.HasValue)
                    enemy.HealthGrowthPerLevel = enemy.HealthGrowthPerLevel.Value * hv;
            }

            enemy.ExtensionData.Remove("overrides");
            if (enemy.ExtensionData.Count == 0)
                enemy.ExtensionData = null;
        }
    }
}
