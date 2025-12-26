using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages character leveling, XP, and class progression
    /// </summary>
    public class CharacterProgression
    {
        public int Level { get; set; }
        public int XP { get; set; }

        // Class points system
        public int BarbarianPoints { get; set; } = 0; // Mace weapon
        public int WarriorPoints { get; set; } = 0;   // Sword weapon
        public int RoguePoints { get; set; } = 0;     // Dagger weapon
        public int WizardPoints { get; set; } = 0;    // Wand weapon

        public CharacterProgression(int level = 1)
        {
            Level = level;
            XP = 0;
        }

        public void AddXP(int amount)
        {
            XP += amount;
            while (XP >= XPToNextLevel())
            {
                XP -= XPToNextLevel();
                LevelUp();
            }
        }

        public void LevelUp()
        {
            Level++;
        }

        private int XPToNextLevel()
        {
            var tuning = GameConfiguration.Instance;
            
            // Use new ExperienceSystem configuration if available
            if (tuning.ExperienceSystem != null)
            {
                var variables = new Dictionary<string, double>
                {
                    ["BaseXP"] = tuning.Progression.BaseXPToLevel2,
                    ["Level"] = Level,
                    ["ExponentFactor"] = tuning.ExperienceSystem.BaseXPFormula.Contains("^") ? 1.5 : 1.0
                };
                
                // For now, use the existing formula but make it configurable
                return (int)(tuning.Progression.BaseXPToLevel2 * Math.Pow(tuning.Progression.XPScalingFactor, Level - 1));
            }
            
            // Fallback to old system
            return (int)(tuning.Progression.BaseXPToLevel2 * Math.Pow(tuning.Progression.XPScalingFactor, Level - 1));
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

        public string GetCurrentClass()
        {
            var classes = new List<(string name, int points)>
            {
                ("Barbarian", BarbarianPoints),
                ("Warrior", WarriorPoints),
                ("Rogue", RoguePoints),
                ("Wizard", WizardPoints)
            };

            // Sort by points descending
            classes.Sort((a, b) => b.points.CompareTo(a.points));

            if (classes[0].points == 0)
                return "Fighter";

            string primaryClass = GetClassTier(classes[0].name, classes[0].points);
            
            // Check for hybrid classes
            if (classes[1].points >= 2)
            {
                string secondaryClass = GetClassTier(classes[1].name, classes[1].points);
                return $"{primaryClass}-{secondaryClass}";
            }

            return primaryClass;
        }

        public string GetFullNameWithQualifier(string characterName)
        {
            string currentClass = GetCurrentClass();
            int primaryClassPoints = GetPrimaryClassPoints();
            string qualifier = FlavorText.GetClassQualifier(currentClass, primaryClassPoints);
            return $"{characterName} {qualifier}";
        }

        private int GetPrimaryClassPoints()
        {
            var classes = new List<(string name, int points)>
            {
                ("Barbarian", BarbarianPoints),
                ("Warrior", WarriorPoints),
                ("Rogue", RoguePoints),
                ("Wizard", WizardPoints)
            };

            // Sort by points descending and return the highest
            classes.Sort((a, b) => b.points.CompareTo(a.points));
            return classes[0].points;
        }

        private string GetClassTier(string baseClass, int points)
        {
            if (points >= 60)
                return $"Master {baseClass}";  // Tier 3
            if (points >= 20)
                return $"Expert {baseClass}";   // Tier 2
            if (points >= 2)
                return $"Adept {baseClass}";    // Tier 1
            return "Novice";
        }

        public int GetNextClassThreshold(string className)
        {
            int currentPoints = className switch
            {
                "Barbarian" => BarbarianPoints,
                "Warrior" => WarriorPoints,
                "Rogue" => RoguePoints,
                "Wizard" => WizardPoints,
                _ => 0
            };

            // Define upgrade thresholds - three tiers
            int[] thresholds = { 2, 20, 60 };
            
            foreach (int threshold in thresholds)
            {
                if (currentPoints < threshold)
                {
                    return threshold;
                }
            }
            
            return -1; // Max level reached
        }

        public string GetClassUpgradeInfo()
        {
            var upgradeInfo = new List<string>();
            
            var classes = new[] { ("Barbarian", BarbarianPoints), ("Warrior", WarriorPoints), ("Rogue", RoguePoints), ("Wizard", WizardPoints) };
            
            // Filter to only show classes with at least 1 point, then sort by points (highest first) and take top 2
            var classesWithPoints = classes.Where(c => c.Item2 > 0);
            var sortedClasses = classesWithPoints.OrderByDescending(c => c.Item2).Take(2);
            
            foreach (var (className, points) in sortedClasses)
            {
                int nextThreshold = GetNextClassThreshold(className);
                if (nextThreshold > 0)
                {
                    int pointsNeeded = nextThreshold - points;
                    upgradeInfo.Add($"{className}: {pointsNeeded} to go");
                }
                else
                {
                    upgradeInfo.Add($"{className}: MAX");
                }
            }
            
            return string.Join(" | ", upgradeInfo);
        }

        public bool IsWizardClass(WeaponType? weaponType)
        {
            // Must have a wand equipped
            if (weaponType != WeaponType.Wand)
            {
                return false;
            }
            
            // Must have wizard points
            if (WizardPoints <= 0)
            {
                return false;
            }
            
            // Wizard must be the primary class (most points)
            var classes = new List<(string name, int points)>
            {
                ("Barbarian", BarbarianPoints),
                ("Warrior", WarriorPoints),
                ("Rogue", RoguePoints),
                ("Wizard", WizardPoints)
            };
            
            // Sort by points descending - wizard must be first (highest points)
            classes.Sort((a, b) => b.points.CompareTo(a.points));
            return classes[0].name == "Wizard";
        }

        public void DisplayProgressionInfo(string characterName)
        {
            Console.WriteLine($"=== CHARACTER INFORMATION ===");
            Console.WriteLine($"Name: {characterName}");
            Console.WriteLine($"Class: {GetCurrentClass()}");
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"XP: {XP}");
            Console.WriteLine();
            Console.WriteLine("=== CLASS POINTS ===");
            Console.WriteLine($"Barbarian (Mace): {BarbarianPoints}");
            Console.WriteLine($"Warrior (Sword): {WarriorPoints}");
            Console.WriteLine($"Rogue (Dagger): {RoguePoints}");
            Console.WriteLine($"Wizard (Wand): {WizardPoints}");
            Console.WriteLine();
        }
    }
}
