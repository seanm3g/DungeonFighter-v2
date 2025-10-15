using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI
{
    /// <summary>
    /// Examples demonstrating the ColorLayerSystem for brightness/saturation and dungeon depth
    /// </summary>
    public static class ColorLayerExamples
    {
        /// <summary>
        /// Demonstrates event significance affecting text brightness and saturation
        /// </summary>
        public static void DemoEventSignificance()
        {
            Console.WriteLine("\n=== EVENT SIGNIFICANCE DEMONSTRATION ===\n");
            Console.WriteLine("Same text at different significance levels:\n");

            var baseColorNullable = ColorDefinitions.GetColor('R');
            if (!baseColorNullable.HasValue) return;
            var baseColor = baseColorNullable.Value; // Red
            string text = "CRITICAL HIT";

            // Show the same text at different significance levels
            var trivial = ColorLayerSystem.CreateSignificantSegment(text, baseColor, EventSignificance.Trivial);
            var minor = ColorLayerSystem.CreateSignificantSegment(text, baseColor, EventSignificance.Minor);
            var normal = ColorLayerSystem.CreateSignificantSegment(text, baseColor, EventSignificance.Normal);
            var important = ColorLayerSystem.CreateSignificantSegment(text, baseColor, EventSignificance.Important);
            var critical = ColorLayerSystem.CreateSignificantSegment(text, baseColor, EventSignificance.Critical);

            Console.Write("Trivial:   "); ColoredConsoleWriter.WriteSegments(new[] { trivial }.ToList());
            Console.WriteLine(" (dim, low saturation)");
            
            Console.Write("Minor:     "); ColoredConsoleWriter.WriteSegments(new[] { minor }.ToList());
            Console.WriteLine(" (reduced brightness)");
            
            Console.Write("Normal:    "); ColoredConsoleWriter.WriteSegments(new[] { normal }.ToList());
            Console.WriteLine(" (standard)");
            
            Console.Write("Important: "); ColoredConsoleWriter.WriteSegments(new[] { important }.ToList());
            Console.WriteLine(" (brighter)");
            
            Console.Write("Critical:  "); ColoredConsoleWriter.WriteSegments(new[] { critical }.ToList());
            Console.WriteLine(" (very bright!)");

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates dungeon depth affecting white text temperature
        /// </summary>
        public static void DemoDungeonDepthProgression()
        {
            Console.WriteLine("\n=== DUNGEON DEPTH PROGRESSION ===\n");
            Console.WriteLine("White text transitioning from warm to cool as you go deeper:\n");

            int totalRooms = 10;
            
            for (int room = 1; room <= totalRooms; room++)
            {
                var whiteSegment = ColorLayerSystem.CreateDepthWhiteSegment(
                    $"Room {room,2}: You explore deeper into the dungeon...",
                    room,
                    totalRooms
                );

                string desc = room <= 3 ? "(warm white)" : 
                             room >= 8 ? "(cool white)" : 
                             "(neutral)";

                ColoredConsoleWriter.WriteSegments(new[] { whiteSegment }.ToList());
                Console.WriteLine($" {desc}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates combining depth progression with significance
        /// </summary>
        public static void DemoCombinedEffects()
        {
            Console.WriteLine("\n=== COMBINED DEPTH + SIGNIFICANCE ===\n");
            Console.WriteLine("Events at different depths with varying significance:\n");

            int totalRooms = 5;

            for (int room = 1; room <= totalRooms; room++)
            {
                // Room description uses depth-based white
                var roomDesc = ColorLayerSystem.CreateDepthWhiteSegment(
                    $"Room {room}: ",
                    room,
                    totalRooms
                );

                // Critical event uses significance
                var redColorNullable = ColorDefinitions.GetColor('R');
                if (!redColorNullable.HasValue) continue;
                var redColor = redColorNullable.Value;
                EventSignificance sig = room == totalRooms ? EventSignificance.Critical : EventSignificance.Normal;
                
                var eventDesc = ColorLayerSystem.CreateSignificantSegment(
                    room == totalRooms ? "BOSS BATTLE!" : "Combat encounter",
                    redColor,
                    sig
                );

                ColoredConsoleWriter.WriteSegments(new[] { roomDesc }.ToList());
                ColoredConsoleWriter.WriteSegments(new[] { eventDesc }.ToList());
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates practical combat message coloring
        /// </summary>
        public static void DemoCombatWithSignificance()
        {
            Console.WriteLine("\n=== COMBAT MESSAGES WITH SIGNIFICANCE ===\n");

            var redColorNullable = ColorDefinitions.GetColor('R');
            var greenColorNullable = ColorDefinitions.GetColor('G');
            var yellowColorNullable = ColorDefinitions.GetColor('W');
            
            if (!redColorNullable.HasValue || !greenColorNullable.HasValue || !yellowColorNullable.HasValue) return;
            
            var redColor = redColorNullable.Value;
            var greenColor = greenColorNullable.Value;
            var yellowColor = yellowColorNullable.Value;

            // Regular hit
            Console.Write("Regular hit: ");
            var regularHit = ColorLayerSystem.CreateSignificantSegment("Hit for 15 damage", redColor, EventSignificance.Normal);
            ColoredConsoleWriter.WriteSegments(new[] { regularHit }.ToList());
            Console.WriteLine();

            // Critical hit
            Console.Write("Critical hit: ");
            var critHit = ColorLayerSystem.CreateSignificantSegment("CRITICAL! 45 damage", redColor, EventSignificance.Critical);
            ColoredConsoleWriter.WriteSegments(new[] { critHit }.ToList());
            Console.WriteLine();

            // Miss
            Console.Write("Miss: ");
            var miss = ColorLayerSystem.CreateSignificantSegment("Attack missed", yellowColor, EventSignificance.Trivial);
            ColoredConsoleWriter.WriteSegments(new[] { miss }.ToList());
            Console.WriteLine();

            // Heal
            Console.Write("Major heal: ");
            var heal = ColorLayerSystem.CreateSignificantSegment("Restored 50 HP!", greenColor, EventSignificance.Important);
            ColoredConsoleWriter.WriteSegments(new[] { heal }.ToList());
            Console.WriteLine();

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates white temperature variations
        /// </summary>
        public static void DemoWhiteTemperatures()
        {
            Console.WriteLine("\n=== WHITE TEMPERATURE VARIATIONS ===\n");

            var warmWhite = ColorLayerSystem.GetWhite(WhiteTemperature.Warm);
            var neutralWhite = ColorLayerSystem.GetWhite(WhiteTemperature.Neutral);
            var coolWhite = ColorLayerSystem.GetWhite(WhiteTemperature.Cool);

            Console.Write("Warm White:    ");
            ColoredConsoleWriter.WriteSegments(new[] { 
                new ColorDefinitions.ColoredSegment("This text has a warm, yellowish tint", warmWhite) 
            }.ToList());
            Console.WriteLine();

            Console.Write("Neutral White: ");
            ColoredConsoleWriter.WriteSegments(new[] { 
                new ColorDefinitions.ColoredSegment("This text is pure white", neutralWhite) 
            }.ToList());
            Console.WriteLine();

            Console.Write("Cool White:    ");
            ColoredConsoleWriter.WriteSegments(new[] { 
                new ColorDefinitions.ColoredSegment("This text has a cool, bluish tint", coolWhite) 
            }.ToList());
            Console.WriteLine();

            Console.WriteLine("\nWith varying brightness levels:\n");

            for (float brightness = 0.4f; brightness <= 1.5f; brightness += 0.2f)
            {
                var white = ColorLayerSystem.GetWhite(WhiteTemperature.Neutral, brightness);
                var segment = new ColorDefinitions.ColoredSegment($"Brightness: {brightness:F1}", white);
                ColoredConsoleWriter.WriteSegments(new[] { segment }.ToList());
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Runs all layer system demonstrations
        /// </summary>
        public static void RunAllDemos()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║      COLOR LAYER SYSTEM DEMONSTRATION                      ║");
            Console.WriteLine("║   Brightness/Saturation & Dungeon Depth Effects            ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

            DemoEventSignificance();
            DemoDungeonDepthProgression();
            DemoWhiteTemperatures();
            DemoCombinedEffects();
            DemoCombatWithSignificance();

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}

