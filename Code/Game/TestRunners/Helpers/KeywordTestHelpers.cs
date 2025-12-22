using System;
using RPGGame.UI.Avalonia;
using KeywordColorSystem = RPGGame.UI.ColorSystem.KeywordColorSystem;

namespace RPGGame.Game.TestRunners.Helpers
{
    /// <summary>
    /// Helper class for keyword coloring tests
    /// </summary>
    public static class KeywordTestHelpers
    {
        public static (int passed, int failed) TestKeywordSystemAccessibility(CanvasUICoordinator uiCoordinator)
        {
            var colored = KeywordColorSystem.Colorize("Player deals 25 damage to Enemy");
            if (colored != null && colored.Count > 0)
            {
                uiCoordinator.WriteLine($"  ✓ Keyword system accessible");
                uiCoordinator.WriteLine($"    Generated {colored.Count} colored segments");
                return (1, 0);
            }
            uiCoordinator.WriteLine($"  ✗ Keyword system not accessible");
            return (0, 1);
        }

        public static (int passed, int failed) TestKeywordGroup(string[] keywords, CanvasUICoordinator uiCoordinator)
        {
            int passed = 0, failed = 0;
            foreach (var keyword in keywords)
            {
                var colored = KeywordColorSystem.Colorize($"Player {keyword} Enemy");
                if (colored != null && colored.Count > 0) passed++;
                else { failed++; uiCoordinator.WriteLine($"  ✗ Failed to colorize text with '{keyword}'"); }
            }
            return (passed, failed);
        }

        public static string[] GetDamageKeywords() => new[] { "damage", "hit", "strike", "attack" };
        public static string[] GetStatusKeywords() => new[] { "poison", "fire", "ice", "stun" };
    }
}

