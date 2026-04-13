import pathlib, re, json

ROOT = pathlib.Path(r"d:\Code Projects\github projects\DungeonFighter-v2")

def patch_game_configuration():
    p = ROOT / "Code" / "Game" / "GameConfiguration.cs"
    t = p.read_text(encoding="utf-8")
    if "ClassPresentationConfig" not in t:
        t = t.replace(
            "public ClassBalanceConfig ClassBalance { get; set; } = new();\n\n        // Combat-related configurations",
            "public ClassBalanceConfig ClassBalance { get; set; } = new();\n        public ClassPresentationConfig ClassPresentation { get; set; } = new();\n\n        // Combat-related configurations",
        )
    if "ClassPresentation =" not in t:
        t = t.replace(
            "ClassBalance = config.ClassBalance;\n\n                        // Combat-related configurations",
            "ClassBalance = config.ClassBalance;\n                        ClassPresentation = config.ClassPresentation != null\n                            ? config.ClassPresentation.EnsureNormalized()\n                            : new ClassPresentationConfig().EnsureNormalized();\n\n                        // Combat-related configurations",
        )
    p.write_text(t, encoding="utf-8")

character_progression_cs = r'''using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public class CharacterProgression
    {
        public int Level { get; set; }

        private int _xp = 0;
        public int XP
        {
            get => _xp;
            set => _xp = Math.Max(0, value);
        }

        public int BarbarianPoints { get; set; } = 0;
        public int WarriorPoints { get; set; } = 0;
        public int RoguePoints { get; set; } = 0;
        public int WizardPoints { get; set; } = 0;

        public CharacterProgression(int level = 1)
        {
            Level = level;
            XP = 0;
        }

        private ClassPresentationConfig Pres => GameConfiguration.Instance.ClassPresentation.EnsureNormalized();

        public void AddXP(int amount)
        {
            XP += amount;
            while (XP >= XPToNextLevel())
            {
                XP -= XPToNextLevel();
                LevelUp();
            }
        }

        public void LevelUp() => Level++;

        public int GetXPRequiredForNextLevel() => XPToNextLevel();

        private int XPToNextLevel()
        {
            var tuning = GameConfiguration.Instance;
            int averageXPPerDungeonAtLevel1 = tuning.Progression.EnemyXPBase + 25;
            return Level * Level * averageXPPerDungeonAtLevel1;
        }

        public void AwardClassPoint(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Mace:
                    BarbarianPoints++;
                    break;
                case WeaponType.Sword:
                    WarriorPoints++;
                    break;
                case WeaponType.Dagger:
                    RoguePoints++;
                    break;
                case WeaponType.Wand:
                    WizardPoints++;
                    break;
            }
        }

        public void RemoveClassPoint(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Mace:
                    BarbarianPoints = Math.Max(0, BarbarianPoints - 1);
                    break;
                case WeaponType.Sword:
                    WarriorPoints = Math.Max(0, WarriorPoints - 1);
                    break;
                case WeaponType.Dagger:
                    RoguePoints = Math.Max(0, RoguePoints - 1);
                    break;
                case WeaponType.Wand:
                    WizardPoints = Math.Max(0, WizardPoints - 1);
                    break;
            }
        }

        public int GetClassPoints(WeaponType weaponType) =>
            Pres.GetClassPoints(weaponType, BarbarianPoints, WarriorPoints, RoguePoints, WizardPoints);

        public List<(WeaponType Path, int Points)> GetClassPathsSortedByPoints()
        {
            var rows = ClassPresentationConfig.ClassWeaponOrder
                .Select(wt => (Path: wt, Points: GetClassPoints(wt)))
                .OrderByDescending(x => x.Points)
                .ThenBy(x => Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, x.Path))
                .ToList();
            return rows;
        }

        public WeaponType? GetPrimaryClassWeaponType()
        {
            var sorted = GetClassPathsSortedByPoints();
            return sorted[0].Points > 0 ? sorted[0].Path : (WeaponType?)null;
        }

        public string GetCurrentClass()
        {
            var cfg = Pres;
            var sorted = GetClassPathsSortedByPoints();
            if (sorted[0].Points == 0)
                return cfg.DefaultNoPointsClassName;

            var primary = sorted[0];
            string primaryTitle = cfg.FormatRankedTitle(primary.Path, primary.Points);

            var secondary = sorted[1];
            if (secondary.Points > 0)
            {
                int bandP = cfg.GetTierBandIndex(primary.Points);
                int bandS = cfg.GetTierBandIndex(secondary.Points);
                if (bandP != bandS)
                {
                    string secondaryTitle = cfg.FormatRankedTitle(secondary.Path, secondary.Points);
                    return primaryTitle + "-" + secondaryTitle;
                }
            }

            return primaryTitle;
        }

        public string GetFullNameWithQualifier(string characterName)
        {
            string currentClass = GetCurrentClass();
            int primaryClassPoints = GetPrimaryClassPoints();
            var wt = GetPrimaryClassWeaponType();
            string qualifier = FlavorText.GetClassQualifier(wt, primaryClassPoints, currentClass);
            return $"{characterName} {qualifier}";
        }

        private int GetPrimaryClassPoints()
        {
            var sorted = GetClassPathsSortedByPoints();
            return sorted[0].Points;
        }

        public int GetNextClassThreshold(string className)
        {
            var wt = TryResolveWeaponTypeFromName(className);
            if (wt == null) return 0;
            return GetNextClassThreshold(wt.Value);
        }

        public int GetNextClassThreshold(WeaponType weaponType)
        {
            int currentPoints = GetClassPoints(weaponType);
            return Pres.GetNextThreshold(currentPoints);
        }

        private WeaponType? TryResolveWeaponTypeFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string n = name.Trim();
            var cfg = Pres;
            foreach (WeaponType wt in ClassPresentationConfig.ClassWeaponOrder)
            {
                if (string.Equals(cfg.GetDisplayName(wt), n, StringComparison.OrdinalIgnoreCase))
                    return wt;
            }
            return n.ToLowerInvariant() switch
            {
                "barbarian" => WeaponType.Mace,
                "warrior" => WeaponType.Sword,
                "rogue" => WeaponType.Dagger,
                "wizard" => WeaponType.Wand,
                _ => null
            };
        }

        public string GetClassUpgradeInfo()
        {
            var cfg = Pres;
            var upgradeInfo = new List<string>();
            var sorted = GetClassPathsSortedByPoints()
                .Where(x => x.Points > 0)
                .Take(2);
            foreach (var (path, points) in sorted)
            {
                string label = cfg.GetDisplayName(path);
                int nextThreshold = cfg.GetNextThreshold(points);
                if (nextThreshold > 0)
                {
                    int pointsNeeded = nextThreshold - points;
                    upgradeInfo.Add($"{label}: {pointsNeeded} to go");
                }
                else
                {
                    upgradeInfo.Add($"{label}: MAX");
                }
            }
            return string.Join(" | ", upgradeInfo);
        }

        public bool IsWizardClass(WeaponType? weaponType)
        {
            if (weaponType != WeaponType.Wand) return false;
            if (WizardPoints <= 0) return false;
            var sorted = GetClassPathsSortedByPoints();
            return sorted[0].Path == WeaponType.Wand;
        }

        public void DisplayProgressionInfo(string characterName)
        {
            var cfg = Pres;
            Console.WriteLine($"=== CHARACTER INFORMATION ===");
            Console.WriteLine($"Name: {characterName}");
            Console.WriteLine($"Class: {GetCurrentClass()}");
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"XP: {XP}");
            Console.WriteLine();
            Console.WriteLine("=== CLASS POINTS ===");
            Console.WriteLine($"{cfg.GetDisplayName(WeaponType.Mace)} (Mace): {BarbarianPoints}");
            Console.WriteLine($"{cfg.GetDisplayName(WeaponType.Sword)} (Sword): {WarriorPoints}");
            Console.WriteLine($"{cfg.GetDisplayName(WeaponType.Dagger)} (Dagger): {RoguePoints}");
            Console.WriteLine($"{cfg.GetDisplayName(WeaponType.Wand)} (Wand): {WizardPoints}");
            Console.WriteLine();
        }

        public static string BuildClassSystemSettingsSummary(ClassPresentationConfig? preview = null)
        {
            var cfg = (preview ?? GameConfiguration.Instance.ClassPresentation).EnsureNormalized();
            var nl = Environment.NewLine;
            var t = cfg.TierThresholds;
            string th = $"{t[0]}, {t[1]}, and {t[2]}";
            return
                "Base classes — earn class points by fighting with the matching weapon type:" + nl +
                $"  {cfg.GetDisplayName(WeaponType.Mace)} (Mace)" + nl +
                $"  {cfg.GetDisplayName(WeaponType.Sword)} (Sword)" + nl +
                $"  {cfg.GetDisplayName(WeaponType.Dagger)} (Dagger)" + nl +
                $"  {cfg.GetDisplayName(WeaponType.Wand)} (Wand)" + nl + nl +
                $"Default class: if every base class has 0 points, you are shown as {cfg.DefaultNoPointsClassName}." + nl + nl +
                $"Per-class point tiers (thresholds are {th} points):" + nl +
                $"  Below {t[0]} (with at least 1 point on that class): Novice" + nl +
                $"  {t[0]}+ points: Adept (class name)" + nl +
                $"  {t[1]}+ points: Expert (class name)" + nl +
                $"  {t[2]}+ points: Master (class name)" + nl + nl +
                "Hybrid classes:" + nl +
                "  Your two highest classes by points are considered. If the second has at least 1 point and " +
                "its tier band (pre-first threshold vs each named tier) differs from the primary, your title " +
                "combines both with joiner \"-\" (for example, \"" +
                $"Adept {cfg.GetDisplayName(WeaponType.Mace)}-Adept {cfg.GetDisplayName(WeaponType.Sword)}\"). " +
                "Otherwise only the primary tier name is used.";
        }
    }
}
'''

def write_character_progression():
    (ROOT / "Code" / "Entity" / "CharacterProgression.cs").write_text(character_progression_cs, encoding="utf-8")

print("patched")
