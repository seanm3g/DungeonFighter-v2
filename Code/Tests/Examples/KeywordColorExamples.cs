using System;
using System.Linq;

namespace RPGGame.UI
{
    /// <summary>
    /// Examples demonstrating the KeywordColorSystem
    /// </summary>
    public static class KeywordColorExamples
    {
        /// <summary>
        /// Demonstrates automatic keyword coloring
        /// </summary>
        public static void DemoBasicKeywordColoring()
        {
            Console.WriteLine("\n=== BASIC KEYWORD COLORING ===\n");
            Console.WriteLine("Keywords are automatically colored based on their group:\n");

            var testMessages = new[]
            {
                "You hit the goblin for 25 damage!",
                "CRITICAL HIT! You deal 50 damage to the orc!",
                "You heal for 15 health.",
                "The enemy is poisoned and takes 5 damage per turn.",
                "You found a legendary sword with fire damage!",
                "The wizard casts a spell and you are stunned!",
                "You defeat the dragon and gain 1000 gold!",
                "The holy light purges the shadow from the demon.",
                "You gained experience and leveled up!"
            };

            foreach (var message in testMessages)
            {
                var colored = KeywordColorSystem.Colorize(message);
                ColoredConsoleWriter.WriteLine(colored);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates specific group coloring
        /// </summary>
        public static void DemoGroupSpecificColoring()
        {
            Console.WriteLine("\n=== GROUP-SPECIFIC COLORING ===\n");

            string message = "The warrior hits the goblin with fire damage for 30 health!";

            Console.WriteLine("Original: " + message);
            Console.WriteLine();

            Console.Write("Damage only: ");
            ColoredConsoleWriter.WriteLine(KeywordColorSystem.ColorizeWithGroups(message, "damage"));

            Console.Write("Enemy only: ");
            ColoredConsoleWriter.WriteLine(KeywordColorSystem.ColorizeWithGroups(message, "enemy"));

            Console.Write("Fire only: ");
            ColoredConsoleWriter.WriteLine(KeywordColorSystem.ColorizeWithGroups(message, "fire"));

            Console.Write("Class only: ");
            ColoredConsoleWriter.WriteLine(KeywordColorSystem.ColorizeWithGroups(message, "class"));

            Console.Write("All groups: ");
            ColoredConsoleWriter.WriteLine(KeywordColorSystem.Colorize(message));

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates combat scenarios with keywords
        /// </summary>
        public static void DemoCombatScenarios()
        {
            Console.WriteLine("\n=== COMBAT SCENARIOS WITH KEYWORDS ===\n");

            var combatLog = new[]
            {
                "--- COMBAT START ---",
                "You encounter a fierce orc warrior!",
                "",
                "Round 1:",
                "You attack the orc for 15 damage.",
                "The orc strikes back for 12 damage!",
                "",
                "Round 2:",
                "CRITICAL HIT! You deal 35 damage to the orc!",
                "The orc is wounded and bleeds for 3 damage.",
                "",
                "Round 3:",
                "You cast a fire spell! The orc burns for 20 damage!",
                "The orc dies. You gain 50 experience and 25 gold.",
                "",
                "--- VICTORY ---"
            };

            foreach (var line in combatLog)
            {
                if (string.IsNullOrEmpty(line))
                {
                    Console.WriteLine();
                }
                else
                {
                    var colored = KeywordColorSystem.Colorize(line);
                    ColoredConsoleWriter.WriteLine(colored);
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates item descriptions with rarity keywords
        /// </summary>
        public static void DemoItemDescriptions()
        {
            Console.WriteLine("\n=== ITEM DESCRIPTIONS ===\n");

            var items = new[]
            {
                "Common Iron Sword - Basic weapon. Deals 10 damage.",
                "Uncommon Steel Axe - Improved weapon. Deals 15 damage.",
                "Rare Mithril Blade - Enchanted weapon. Deals 25 damage and has magic properties.",
                "Epic Dragon Slayer - Powerful weapon. Deals 40 fire damage. Burns enemies.",
                "Legendary Excalibur - Mythic artifact. Deals 60 holy damage. Blessed by divine light."
            };

            foreach (var item in items)
            {
                var colored = KeywordColorSystem.Colorize(item);
                ColoredConsoleWriter.WriteLine(colored);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates environmental descriptions
        /// </summary>
        public static void DemoEnvironmentalDescriptions()
        {
            Console.WriteLine("\n=== ENVIRONMENTAL DESCRIPTIONS ===\n");

            var descriptions = new[]
            {
                "You enter a dark cave filled with shadows.",
                "The forest is alive with natural magic.",
                "Lava bubbles and releases toxic fumes. You take 5 fire damage!",
                "The frozen wasteland chills you to the bone. You take 3 ice damage.",
                "Lightning crackles overhead. Thunder shakes the ground.",
                "Holy light emanates from the sacred temple.",
                "The corrupted swamp reeks of death and poison.",
                "You find treasure! 500 gold coins!"
            };

            foreach (var desc in descriptions)
            {
                var colored = KeywordColorSystem.Colorize(desc);
                ColoredConsoleWriter.WriteLine(colored);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates combat actions and character keywords
        /// </summary>
        public static void DemoCombatActionsAndCharacter()
        {
            Console.WriteLine("\n=== COMBAT ACTIONS & CHARACTER KEYWORDS ===\n");

            var actionMessages = new[]
            {
                "You use jab to quickly strike the goblin!",
                "Your flurry of attacks overwhelms the enemy!",
                "You taunt the orc and draw its attention.",
                "The hero performs a shield bash, stunning the skeleton!",
                "Your precision strike hits a weak point for critical damage!",
                "You execute a brutal strike against the wounded enemy!",
                "The champion unleashes blood frenzy, attacking wildly!",
                "Your backstab deals massive damage from behind!",
                "You swing for the fences with all your might!",
                "The adventurer uses second wind to recover stamina!"
            };

            foreach (var message in actionMessages)
            {
                var colored = KeywordColorSystem.Colorize(message);
                ColoredConsoleWriter.WriteLine(colored);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates room and environment theme keywords
        /// </summary>
        public static void DemoRoomsAndThemes()
        {
            Console.WriteLine("\n=== ROOMS & ENVIRONMENT THEMES ===\n");

            var roomDescriptions = new[]
            {
                "You enter the dungeon entrance. The air is thick with anticipation.",
                "This chamber is filled with ancient relics and shadows.",
                "The forest grove is peaceful, with sunlight filtering through trees.",
                "You arrive at the lava cavern. Heat radiates from molten rock.",
                "The frozen hall is covered in ice and frost.",
                "Inside the crypt, you see rows of ancient tombs.",
                "The crystal vault sparkles with prismatic light.",
                "You reach the boss chamber. This will be a tough fight!",
                "The underground passage winds through solid rock.",
                "The temple sanctuary glows with holy light.",
                "You cross into the astral realm. Reality bends around you.",
                "The swamp is murky and toxic. Watch your step!",
                "This steampunk laboratory is filled with mechanical wonders.",
                "The storm outside rages with thunder and lightning.",
                "The shadow void consumes all light around you."
            };

            foreach (var description in roomDescriptions)
            {
                var colored = KeywordColorSystem.Colorize(description);
                ColoredConsoleWriter.WriteLine(colored);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates extended enemy roster
        /// </summary>
        public static void DemoExpandedEnemies()
        {
            Console.WriteLine("\n=== EXPANDED ENEMY ROSTER ===\n");

            var enemyEncounters = new[]
            {
                "You encounter a goblin scout in the tunnel!",
                "A massive bear charges at you from the forest!",
                "The treant awakens and attacks with its branches!",
                "A fire elemental emerges from the lava pool!",
                "The golem's stone fists smash the ground!",
                "A salamander slithers toward you with burning eyes!",
                "The lich raises its skeletal hand and casts a spell!",
                "The temple warden blocks your path with its guardian shield!",
                "A kobold throws a bomb at your feet!",
                "The wyrm unfurls its crystalline wings!",
                "A yeti roars and charges through the snow!",
                "The vampire emerges from the shadows, fangs bared!",
                "A werewolf howls under the full moon!",
                "The hydra's multiple heads snap at you viciously!"
            };

            foreach (var encounter in enemyEncounters)
            {
                var colored = KeywordColorSystem.Colorize(encounter);
                ColoredConsoleWriter.WriteLine(colored);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates status effects
        /// </summary>
        public static void DemoStatusEffects()
        {
            Console.WriteLine("\n=== STATUS EFFECTS ===\n");

            var statusMessages = new[]
            {
                "You are poisoned! Taking 5 toxic damage per turn.",
                "You are stunned and cannot move!",
                "You are burning! Fire deals 8 damage.",
                "You are frozen and move slowly.",
                "You are bleeding! Losing 3 health per turn.",
                "You are blessed with holy protection!",
                "You are strengthened! Your damage increases.",
                "You are cursed! Your attacks weaken.",
                "The shadow corrupts your mind!"
            };

            foreach (var message in statusMessages)
            {
                var colored = KeywordColorSystem.Colorize(message);
                ColoredConsoleWriter.WriteLine(colored);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates custom keyword groups
        /// </summary>
        public static void DemoCustomKeywordGroups()
        {
            Console.WriteLine("\n=== CUSTOM KEYWORD GROUPS ===\n");

            // Create a custom group for player actions
            KeywordColorSystem.CreateGroup("player-actions", "cyan", false, 
                "defend", "dodge", "parry", "block", "evade");

            // Create a custom group for treasure
            KeywordColorSystem.CreateGroup("treasure", "golden", false,
                "chest", "loot", "treasure", "gems", "jewels");

            var messages = new[]
            {
                "You dodge the attack and counter!",
                "You block the strike with your shield.",
                "You found a treasure chest filled with gems and jewels!",
                "You defend against the damage and parry the next attack."
            };

            foreach (var message in messages)
            {
                var colored = KeywordColorSystem.Colorize(message);
                ColoredConsoleWriter.WriteLine(colored);
            }

            Console.WriteLine();

            // Clean up custom groups
            KeywordColorSystem.RemoveGroup("player-actions");
            KeywordColorSystem.RemoveGroup("treasure");
        }

        /// <summary>
        /// Demonstrates mixing manual markup with keyword coloring
        /// </summary>
        public static void DemoMixedMarkup()
        {
            Console.WriteLine("\n=== MIXED MANUAL + KEYWORD MARKUP ===\n");

            var messages = new[]
            {
                "&Y[BOSS FIGHT]&y You encounter a legendary dragon!",
                "{{critical|DEVASTATING BLOW!}} You hit for 100 damage!",
                "&RHealth: 50/100&y | &BMana: 75/100&y | &WGold: 1,234",
                "The {{holy|Divine Warrior}} defeats the {{shadow|Shadow Demon}}!"
            };

            Console.WriteLine("Manual markup is preserved, keywords are colored:\n");

            foreach (var message in messages)
            {
                // First apply keyword coloring, then parse all markup
                var withKeywords = KeywordColorSystem.Colorize(message);
                ColoredConsoleWriter.WriteLine(withKeywords);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Shows all registered keyword groups
        /// </summary>
        public static void ShowAllKeywordGroups()
        {
            Console.WriteLine("\n=== REGISTERED KEYWORD GROUPS ===\n");

            var groups = KeywordColorSystem.GetAllGroupNames().OrderBy(n => n);

            foreach (var groupName in groups)
            {
                var group = KeywordColorSystem.GetKeywordGroup(groupName);
                if (group != null)
                {
                    Console.WriteLine($"Group: {group.Name}");
                    Console.WriteLine($"  Color Pattern: {group.ColorPattern}");
                    Console.WriteLine($"  Keywords: {string.Join(", ", group.Keywords.Take(10))}");
                    if (group.Keywords.Count > 10)
                    {
                        Console.WriteLine($"  ... and {group.Keywords.Count - 10} more");
                    }
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Runs all demonstrations
        /// </summary>
        public static void RunAllDemos()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         KEYWORD COLOR SYSTEM DEMONSTRATION                 ║");
            Console.WriteLine("║    Automatic Coloring Based on Keyword Groups              ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

            DemoBasicKeywordColoring();
            DemoGroupSpecificColoring();
            DemoCombatScenarios();
            DemoItemDescriptions();
            DemoEnvironmentalDescriptions();
            DemoCombatActionsAndCharacter();
            DemoRoomsAndThemes();
            DemoExpandedEnemies();
            DemoStatusEffects();
            DemoCustomKeywordGroups();
            DemoMixedMarkup();
            ShowAllKeywordGroups();

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}

