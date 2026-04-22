using System.Collections.Generic;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Historical <c>Enemies.json</c> root keys (<c>strength</c>, <c>agility</c>, …) land in <see cref="EnemyData.ExtensionData"/>.
    /// They are folded into <see cref="EnemyData.GrowthPerLevel"/> (attribute scaling is growth-only; no enemy overrides).
    /// </summary>
    public static class EnemyDataLegacyRootStats
    {
        private static readonly string[] LegacyKeys = { "strength", "agility", "technique", "intelligence" };

        public static void FoldExtensionDataIntoTypedFields(EnemyData enemy)
        {
            if (enemy.ExtensionData == null || enemy.ExtensionData.Count == 0)
                return;

            enemy.GrowthPerLevel ??= new EnemyAttributeSet();

            foreach (var key in LegacyKeys)
            {
                if (!enemy.ExtensionData.TryGetValue(key, out var el))
                    continue;
                if (el.ValueKind != JsonValueKind.Number)
                {
                    enemy.ExtensionData.Remove(key);
                    continue;
                }

                double v = el.GetDouble();
                enemy.ExtensionData.Remove(key);

                switch (key)
                {
                    case "strength":
                        if (!enemy.GrowthPerLevel.Strength.HasValue)
                            enemy.GrowthPerLevel.Strength = v;
                        break;
                    case "agility":
                        if (!enemy.GrowthPerLevel.Agility.HasValue)
                            enemy.GrowthPerLevel.Agility = v;
                        break;
                    case "technique":
                        if (!enemy.GrowthPerLevel.Technique.HasValue)
                            enemy.GrowthPerLevel.Technique = v;
                        break;
                    case "intelligence":
                        if (!enemy.GrowthPerLevel.Intelligence.HasValue)
                            enemy.GrowthPerLevel.Intelligence = v;
                        break;
                }
            }

            if (enemy.ExtensionData.Count == 0)
                enemy.ExtensionData = null;
        }
    }
}
