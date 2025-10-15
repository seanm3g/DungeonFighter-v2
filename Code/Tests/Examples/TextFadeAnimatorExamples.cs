using System;
using System.Collections.Generic;
using RPGGame.UI;

namespace RPGGame
{
    /// <summary>
    /// Demonstration examples for the TextFadeAnimator system
    /// Shows various fade patterns and configurations
    /// </summary>
    public static class TextFadeAnimatorExamples
    {
        /// <summary>
        /// Run all fade animation demonstrations
        /// </summary>
        public static void RunAllDemos()
        {
            Console.Clear();
            Console.WriteLine("=== Text Fade Animation System Demonstrations ===\n");
            
            DemoAlternatingFade();
            Console.WriteLine("\n---\n");
            
            DemoSequentialFade();
            Console.WriteLine("\n---\n");
            
            DemoCenterCollapseFade();
            Console.WriteLine("\n---\n");
            
            DemoCenterExpandFade();
            Console.WriteLine("\n---\n");
            
            DemoTemplateFade();
            Console.WriteLine("\n---\n");
            
            DemoCustomColorProgression();
            Console.WriteLine("\n---\n");
            
            DemoCombatUsage();
            Console.WriteLine("\n---\n");
            
            Console.WriteLine("\n=== Demonstrations Complete ===");
        }

        /// <summary>
        /// Demonstrates alternating fade pattern (every other letter)
        /// </summary>
        public static void DemoAlternatingFade()
        {
            Console.WriteLine("1. Alternating Fade Pattern (Every Other Letter)");
            Console.WriteLine("   Text fades with alternating letters dimming first\n");
            
            Console.Write("   ");
            TextFadeAnimator.FadeOutAlternating("The enemy has been defeated!", frames: 6, delayMs: 150);
            
            Console.Write("   ");
            TextFadeAnimator.FadeOut("Victory is yours!", new TextFadeAnimator.FadeConfig
            {
                Pattern = TextFadeAnimator.FadePattern.Alternating,
                Direction = TextFadeAnimator.FadeDirection.ToDark,
                TotalFrames = 5,
                FrameDelayMs = 120
            });
        }

        /// <summary>
        /// Demonstrates sequential fade pattern (left to right)
        /// </summary>
        public static void DemoSequentialFade()
        {
            Console.WriteLine("2. Sequential Fade Pattern (Left to Right)");
            Console.WriteLine("   Letters fade from left to right\n");
            
            Console.Write("   ");
            TextFadeAnimator.FadeOut("Experience gained: 150 XP", new TextFadeAnimator.FadeConfig
            {
                Pattern = TextFadeAnimator.FadePattern.Sequential,
                Direction = TextFadeAnimator.FadeDirection.ToDark,
                TotalFrames = 8,
                FrameDelayMs = 80
            });
            
            Console.Write("   ");
            TextFadeAnimator.FadeOut("Level Up!", new TextFadeAnimator.FadeConfig
            {
                Pattern = TextFadeAnimator.FadePattern.Sequential,
                Direction = TextFadeAnimator.FadeDirection.ToDark,
                TotalFrames = 4,
                FrameDelayMs = 100
            });
        }

        /// <summary>
        /// Demonstrates center collapse fade pattern (outside to center)
        /// </summary>
        public static void DemoCenterCollapseFade()
        {
            Console.WriteLine("3. Center Collapse Pattern (Outside to Center)");
            Console.WriteLine("   Letters fade from outside edges to center\n");
            
            Console.Write("   ");
            TextFadeAnimator.FadeOut("The portal closes before you", new TextFadeAnimator.FadeConfig
            {
                Pattern = TextFadeAnimator.FadePattern.CenterCollapse,
                Direction = TextFadeAnimator.FadeDirection.ToDark,
                TotalFrames = 7,
                FrameDelayMs = 100
            });
        }

        /// <summary>
        /// Demonstrates center expand fade pattern (center to outside)
        /// </summary>
        public static void DemoCenterExpandFade()
        {
            Console.WriteLine("4. Center Expand Pattern (Center to Outside)");
            Console.WriteLine("   Letters fade from center to outside edges\n");
            
            Console.Write("   ");
            TextFadeAnimator.FadeOut("The darkness spreads outward", new TextFadeAnimator.FadeConfig
            {
                Pattern = TextFadeAnimator.FadePattern.CenterExpand,
                Direction = TextFadeAnimator.FadeDirection.ToDark,
                TotalFrames = 7,
                FrameDelayMs = 100
            });
        }

        /// <summary>
        /// Demonstrates fade using color templates
        /// </summary>
        public static void DemoTemplateFade()
        {
            Console.WriteLine("5. Template-Based Fade Patterns");
            Console.WriteLine("   Text fades through color template patterns\n");
            
            Console.Write("   Fiery fade: ");
            TextFadeAnimator.FadeOutWithTemplate("Burning flames diminish", "fiery", TextFadeAnimator.FadePattern.Alternating);
            
            Console.Write("   Icy fade: ");
            TextFadeAnimator.FadeOutWithTemplate("Frozen solid melts away", "icy", TextFadeAnimator.FadePattern.Sequential);
            
            Console.Write("   Shadow fade: ");
            TextFadeAnimator.FadeOutWithTemplate("Fading into shadows", "shadow", TextFadeAnimator.FadePattern.CenterCollapse);
            
            Console.Write("   Ethereal fade: ");
            TextFadeAnimator.FadeOutWithTemplate("Disappearing ethereally", "ethereal", TextFadeAnimator.FadePattern.CenterExpand);
        }

        /// <summary>
        /// Demonstrates custom color progression
        /// </summary>
        public static void DemoCustomColorProgression()
        {
            Console.WriteLine("6. Custom Color Progression");
            Console.WriteLine("   Define your own color fade sequence\n");
            
            // Bright red to dark red to black
            Console.Write("   Blood fade: ");
            TextFadeAnimator.FadeOutCustom(
                "Bloodstained weapon",
                new List<char> { 'R', 'r', 'K', 'k' },
                TextFadeAnimator.FadePattern.Alternating
            );
            
            // Gold to brown to dark
            Console.Write("   Gold fade: ");
            TextFadeAnimator.FadeOutCustom(
                "Golden treasure vanishes",
                new List<char> { 'W', 'O', 'w', 'K' },
                TextFadeAnimator.FadePattern.Sequential
            );
            
            // Rainbow fade
            Console.Write("   Rainbow fade: ");
            TextFadeAnimator.FadeOutCustom(
                "Magical rainbow effect",
                new List<char> { 'R', 'O', 'W', 'G', 'B', 'M', 'k' },
                TextFadeAnimator.FadePattern.CenterExpand
            );
        }

        /// <summary>
        /// Demonstrates practical combat usage scenarios
        /// </summary>
        public static void DemoCombatUsage()
        {
            Console.WriteLine("7. Practical Combat Usage Scenarios");
            Console.WriteLine("   How to use fades in actual gameplay\n");
            
            // Enemy defeat
            Console.Write("   Enemy defeat: ");
            var defeatedText = ColorParser.Colorize("Goblin", "enemy") + " has been " + 
                              ColorParser.Colorize("defeated", "death") + "!";
            TextFadeAnimator.FadeOut(defeatedText, new TextFadeAnimator.FadeConfig
            {
                Pattern = TextFadeAnimator.FadePattern.Alternating,
                Direction = TextFadeAnimator.FadeDirection.ToDark,
                TotalFrames = 5,
                FrameDelayMs = 120
            });
            
            // Critical hit
            Console.Write("   Critical hit: ");
            var critText = ColorParser.Colorize("CRITICAL HIT", "critical") + "!";
            TextFadeAnimator.FadeOutWithTemplate(critText, "fiery", TextFadeAnimator.FadePattern.CenterExpand);
            
            // Status effect ending
            Console.Write("   Buff expires: ");
            var buffText = "Your " + ColorParser.Colorize("holy", "holy") + " protection fades...";
            TextFadeAnimator.FadeOut(buffText, new TextFadeAnimator.FadeConfig
            {
                Pattern = TextFadeAnimator.FadePattern.Uniform,
                Direction = TextFadeAnimator.FadeDirection.ToDark,
                TotalFrames = 4,
                FrameDelayMs = 150
            });
            
            // Quest completion
            Console.Write("   Quest complete: ");
            var questText = ColorParser.Colorize("Quest Complete", "golden") + ": The Ancient Ruins";
            TextFadeAnimator.FadeOut(questText, new TextFadeAnimator.FadeConfig
            {
                Pattern = TextFadeAnimator.FadePattern.Sequential,
                Direction = TextFadeAnimator.FadeDirection.ToDark,
                TotalFrames = 6,
                FrameDelayMs = 100
            });
        }

        /// <summary>
        /// Interactive test allowing user to customize fade parameters
        /// </summary>
        public static void InteractiveDemo()
        {
            Console.Clear();
            Console.WriteLine("=== Interactive Fade Animation Test ===\n");
            
            Console.Write("Enter text to animate: ");
            string? text = Console.ReadLine();
            if (string.IsNullOrEmpty(text)) text = "Test animation text";
            
            Console.WriteLine("\nSelect fade pattern:");
            Console.WriteLine("1. Alternating (every other letter)");
            Console.WriteLine("2. Sequential (left to right)");
            Console.WriteLine("3. Center Collapse (outside to center)");
            Console.WriteLine("4. Center Expand (center to outside)");
            Console.WriteLine("5. Uniform (all at once)");
            Console.WriteLine("6. Random");
            Console.Write("\nChoice (1-6): ");
            string? patternChoice = Console.ReadLine();
            
            var pattern = patternChoice switch
            {
                "2" => TextFadeAnimator.FadePattern.Sequential,
                "3" => TextFadeAnimator.FadePattern.CenterCollapse,
                "4" => TextFadeAnimator.FadePattern.CenterExpand,
                "5" => TextFadeAnimator.FadePattern.Uniform,
                "6" => TextFadeAnimator.FadePattern.Random,
                _ => TextFadeAnimator.FadePattern.Alternating
            };
            
            Console.Write("\nEnter number of frames (1-10, default 5): ");
            string? framesInput = Console.ReadLine();
            int frames = int.TryParse(framesInput, out int f) ? Math.Clamp(f, 1, 10) : 5;
            
            Console.Write("Enter delay between frames in ms (50-500, default 100): ");
            string? delayInput = Console.ReadLine();
            int delay = int.TryParse(delayInput, out int d) ? Math.Clamp(d, 50, 500) : 100;
            
            Console.WriteLine("\nAnimating...\n");
            Console.Write("   ");
            
            TextFadeAnimator.FadeOut(text, new TextFadeAnimator.FadeConfig
            {
                Pattern = pattern,
                Direction = TextFadeAnimator.FadeDirection.ToDark,
                TotalFrames = frames,
                FrameDelayMs = delay
            });
            
            Console.WriteLine("\nAnimation complete!");
        }
    }
}

