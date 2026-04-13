using System;
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
            return ClassPresentationConfig.ClassWeaponOrder
                .Select(wt => (Path: wt, Points: GetClassPoints(wt)))
                .OrderByDescending(x => x.Points)
                .ThenBy(x => Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, x.Path))
                .ToList();
        }

        public WeaponType? GetPrimaryClassWeaponType()
        {
            var sorted = GetClassPathsSortedByPoints();
            return sorted[0].Points > 0 ? sorted[0].Path : (WeaponType?)null;
        }

        /// <summary>
        /// Class title derived from **weapon path points** (hybrids, ranked fragments). Used for action unlocks,
        /// save compatibility, and debug — not the HUD/menu attribute display name.
        /// </summary>
        public string GetWeaponPointsClassTitle()
        {
            var cfg = Pres;
            var sorted = GetClassPathsSortedByPoints();
            if (sorted[0].Points == 0)
                return cfg.DefaultNoPointsClassName;

            var primary = sorted[0];
            var secondary = sorted[1];
            if (secondary.Points > 0)
            {
                int bandP = cfg.GetTierBandIndex(primary.Points);
                int bandS = cfg.GetTierBandIndex(secondary.Points);
                if (bandP != bandS)
                {
                    string primaryTitle = cfg.FormatRankedTitle(primary.Path, primary.Points);
                    string secondaryTitle = cfg.FormatRankedTitle(secondary.Path, secondary.Points);
                    return primaryTitle + ClassPresentationConfig.DefaultHybridJoiner + secondaryTitle;
                }
            }

            return cfg.FormatRankedTitle(primary.Path, primary.Points);
        }

        public string GetFullNameWithQualifier(string characterName, CharacterStats? stats)
        {
            var cfg = Pres;
            string displayClass = AttributeClassNameComposer.ComposeDisplayClass(this, cfg);
            int primaryClassPoints = GetPrimaryWeaponPathPoints();
            var wt = GetPrimaryClassWeaponType();
            string qualifier = FlavorText.GetClassQualifier(wt, primaryClassPoints, displayClass);
            return $"{characterName} {qualifier}";
        }

        /// <summary>Points on the highest weapon path (0 if no path has points).</summary>
        public int GetPrimaryWeaponPathPoints()
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
            Console.WriteLine($"Weapon-path class (action unlocks): {GetWeaponPointsClassTitle()}");
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

    }
}
