using System;
using System.Collections.Generic;
using System.Threading;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Animations
{
    /// <summary>
    /// Opening animation for Dungeon Fighter with stylized ASCII art
    /// </summary>
    public static class OpeningAnimation
    {
        /// <summary>
        /// Displays the opening animation with ASCII art title - Skull with DF logo
        /// </summary>
        public static void ShowOpeningAnimation()
        {
            // Clear console if using console UI
            if (UIManager.GetCustomUIManager() == null)
            {
                Console.Clear();
            }
            
            // Use title art from AsciiArtAssets for consistency
            List<string> asciiArtList = new List<string> { "" };
            asciiArtList.AddRange(AsciiArtAssets.TitleArt.DungeonFighterTitle);
            asciiArtList.Add("");
            string[] asciiArt = asciiArtList.ToArray();
            
            // Display the ASCII art instantly (no animation delay)
            foreach (string line in asciiArt)
            {
                UIManager.WriteLine(line, UIMessageType.Title);
            }
            
            // Wait 2 seconds, then show prompt
            Thread.Sleep(2000);
            
            // Show press any key prompt
            if (UIManager.GetCustomUIManager() == null)
            {
                UIManager.WriteLine("", UIMessageType.Title);
                var promptBuilder = new ColoredTextBuilder();
                promptBuilder.Add("                    [Press any key to continue]", ColorPalette.Yellow);
                UIManager.WriteLine(ColoredTextRenderer.RenderAsMarkup(promptBuilder.Build()), UIMessageType.Title);
                Console.ReadKey(true);
                Console.Clear();
            }
            else
            {
                // For GUI, just display for total of 3 seconds
                Thread.Sleep(1000);
            }
        }
        
        /// <summary>
        /// Shows a simplified version of the opening animation (for faster startups)
        /// </summary>
        public static void ShowSimplifiedAnimation()
        {
            // Clear console if using console UI
            if (UIManager.GetCustomUIManager() == null)
            {
                Console.Clear();
            }
            
            // Simplified ASCII art
            string[] asciiArt = new string[]
            {
                "",
                "{{G|═══════════════════════════════════════════════════════════════════}}",
                "",
                "{{W|    ╔╗ ╦ ╦╔╗╔╔═╗╔═╗╔═╗╔╗╔  ╔═╗╦╔═╗╦ ╦╔╦╗╔═╗╦═╗}}",
                "{{W|    ║║ ║ ║║║║║ ╦║╣ ║ ║║║║  ╠╣ ║║ ╦╠═╣ ║ ║╣ ╠╦╝}}",
                "{{W|    ╚╝ ╚═╝╝╚╝╚═╝╚═╝╚═╝╝╚╝  ╚  ╩╚═╝╩ ╩ ╩ ╚═╝╩╚═}}",
                "",
                "{{G|═══════════════════════════════════════════════════════════════════}}",
                ""
            };
            
            foreach (string line in asciiArt)
            {
                UIManager.WriteLine(line, UIMessageType.Title);
                Thread.Sleep(30);
            }
            
            Thread.Sleep(1000);
            
            if (UIManager.GetCustomUIManager() == null)
            {
                Console.Clear();
            }
        }
        
        /// <summary>
        /// Shows an alternative ASCII art version with more detail
        /// </summary>
        public static void ShowDetailedAnimation()
        {
            // Clear console if using console UI
            if (UIManager.GetCustomUIManager() == null)
            {
                Console.Clear();
            }
            
            string[] asciiArt = new string[]
            {
                "",
                "{{G|╔═══════════════════════════════════════════════════════════════════════════════╗}}",
                "{{G|║                                                                               ║}}",
                "{{W|  ____    _   _   _   _    ____   _____    ___    _   _}}",
                "{{W| |  _ \\  | | | | | \\ | |  / ___| | ____|  / _ \\  | \\ | |}}",
                "{{W| | | | | | | | | |  \\| | | |  _  |  _|   | | | | |  \\| |}}",
                "{{W| | |_| | | |_| | | |\\  | | |_| | | |___  | |_| | | |\\  |}}",
                "{{W| |____/   \\___/  |_| \\_|  \\____| |_____|  \\___/  |_| \\_|}}",
                "{{G|║                                                                               ║}}",
                "{{R|  _____   ___    ____   _   _   _____   _____   ____  }}",
                "{{R| |  ___| |_ _|  / ___| | | | | |_   _| | ____| |  _ \\ }}",
                "{{R| | |_     | |  | |  _  | |_| |   | |   |  _|   | |_) |}}",
                "{{R| |  _|    | |  | |_| | |  _  |   | |   | |___  |  _ < }}",
                "{{R| |_|     |___|  \\____| |_| |_|   |_|   |_____| |_| \\_\\}}",
                "{{G|║                                                                               ║}}",
                "{{G|╚═══════════════════════════════════════════════════════════════════════════════╝}}",
                "",
                "{{Y|                  ⚔ Prepare yourself for adventure! ⚔}}",
                "",
                "{{Y|                    [Press any key to continue]}}",
                ""
            };
            
            foreach (string line in asciiArt)
            {
                UIManager.WriteLine(line, UIMessageType.Title);
                Thread.Sleep(60);
            }
            
            if (UIManager.GetCustomUIManager() == null)
            {
                Console.ReadKey(true);
                Console.Clear();
            }
            else
            {
                Thread.Sleep(2000);
            }
        }
        
        /// <summary>
        /// Shows a sword and shield themed opening
        /// </summary>
        public static void ShowSwordAndShieldAnimation()
        {
            // Clear console if using console UI
            if (UIManager.GetCustomUIManager() == null)
            {
                Console.Clear();
            }
            
            string[] asciiArt = new string[]
            {
                "",
                "{{G|         ╔═════════════════════════════════════════════════════════╗}}",
                "{{G|         ║                                                         ║}}",
                "{{W|              /\\                                  ___}}",
                "{{W|             /  \\              {{R|D U N G E O N}}         /   \\}}",
                "{{W|            /____\\                                  |  O  |}}",
                "{{W|           /  ||  \\           {{R|F I G H T E R}}        |     |}}",
                "{{W|          /   ||   \\                                |     |}}",
                "{{W|         /    ||    \\                               |     |}}",
                "{{W|        /     ||     \\                              |     |}}",
                "{{W|       /      ||      \\                             \\     /}}",
                "{{W|      /       ||       \\                             \\___/}}",
                "{{W|     /========||========\\}}",
                "{{W|            {{Y|[||||]}}                          {{G|⚔ Enter the Depths ⚔}}",
                "{{G|         ║                                                         ║}}",
                "{{G|         ╚═════════════════════════════════════════════════════════╝}}",
                "",
                "{{Y|                      [Press any key to begin]}}",
                ""
            };
            
            foreach (string line in asciiArt)
            {
                UIManager.WriteLine(line, UIMessageType.Title);
                Thread.Sleep(70);
            }
            
            if (UIManager.GetCustomUIManager() == null)
            {
                Console.ReadKey(true);
                Console.Clear();
            }
            else
            {
                Thread.Sleep(2000);
            }
        }
    }
}
