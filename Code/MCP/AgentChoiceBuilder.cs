using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.MCP.Models;

namespace RPGGame.MCP
{
    /// <summary>
    /// Builds labeled choices and input hints for MCP agent gameplay.
    /// </summary>
    public static class AgentChoiceBuilder
    {
        public const string ModeNormal = "Normal";
        public const string ModeCustomDungeonLevel = "CustomDungeonLevel";
        public const string ModeExitChoice = "ExitChoice";

        public static McpGameplayInputContext ResolveInputContext(GameCoordinator game)
        {
            var ctx = new McpGameplayInputContext { PendingInputMode = ModeNormal };

            var exitHandler = game.GetExitChoiceHandler();
            if (exitHandler?.IsWaitingForChoice == true)
            {
                ctx.PendingInputMode = ModeExitChoice;
                return ctx;
            }

            var dungeonHandler = game.GetDungeonSelectionHandler();
            if (dungeonHandler?.IsAwaitingCustomDungeonLevel == true)
            {
                ctx.PendingInputMode = ModeCustomDungeonLevel;
                ctx.CustomLevelBuffer = dungeonHandler.CustomLevelBuffer;
            }

            return ctx;
        }

        public static List<string> BuildHints(McpGameplayInputContext inputContext, GameState state)
        {
            var hints = new List<string>();

            if (inputContext.PendingInputMode == ModeCustomDungeonLevel)
            {
                hints.Add("Custom dungeon level mode is active — digit keys append to the level buffer.");
                hints.Add("Send 'enter' to start the dungeon, 'backspace' to edit, or '0' with empty buffer to cancel.");
                if (!string.IsNullOrEmpty(inputContext.CustomLevelBuffer))
                    hints.Add($"Current level buffer: \"{inputContext.CustomLevelBuffer}\"");
                return hints;
            }

            if (inputContext.PendingInputMode == ModeExitChoice)
            {
                hints.Add("Exit choice: continue the dungeon or leave safely.");
                return hints;
            }

            if (state == GameState.DungeonSelection)
            {
                hints.Add("Selecting a dungeon runs the entire dungeon in one handle_input call (auto-combat).");
                hints.Add("Avoid option 4 (custom level) unless you intend to type a level and press enter.");
            }

            return hints;
        }

        public static List<AgentChoice> BuildChoices(GameCoordinator game, McpGameplayInputContext inputContext)
        {
            if (inputContext.PendingInputMode == ModeCustomDungeonLevel)
                return BuildCustomLevelChoices();

            if (inputContext.PendingInputMode == ModeExitChoice)
            {
                return new List<AgentChoice>
                {
                    new() { Input = "1", Label = "Stay and continue through the dungeon", Recommended = true },
                    new() { Input = "2", Label = "Leave the dungeon safely (no rewards)" }
                };
            }

            return game.CurrentState switch
            {
                GameState.MainMenu => new List<AgentChoice>
                {
                    new() { Input = "1", Label = "New Game", Recommended = true },
                    new() { Input = "2", Label = "Load Game" },
                    new() { Input = "3", Label = "Settings" },
                    new() { Input = "0", Label = "Exit" }
                },
                GameState.TrainingGroundOffer => new List<AgentChoice>
                {
                    new() { Input = "1", Label = "Enter Training Ground tutorial dungeon" },
                    new() { Input = "2", Label = "Skip to weapon selection", Recommended = true }
                },
                GameState.PreWeaponPathIntro => new List<AgentChoice>
                {
                    new() { Input = "1", Label = "Continue to weapon selection", Recommended = true }
                },
                GameState.WeaponSelection => BuildNumberedChoices("Select starting weapon", 4, recommendedIndex: 1),
                GameState.CharacterCreation => new List<AgentChoice>
                {
                    new() { Input = "1", Label = "Confirm character and continue", Recommended = true }
                },
                GameState.GameLoop => new List<AgentChoice>
                {
                    new() { Input = "1", Label = "Enter dungeons", Recommended = true },
                    new() { Input = "2", Label = "Inventory" },
                    new() { Input = "0", Label = "Save and exit to main menu" }
                },
                GameState.DungeonSelection => BuildDungeonChoices(game),
                GameState.DungeonCompletion => new List<AgentChoice>
                {
                    new() { Input = "1", Label = "Continue to dungeon selection", Recommended = true },
                    new() { Input = "2", Label = "Inventory" },
                    new() { Input = "0", Label = "Save and exit to main menu" }
                },
                GameState.Death => new List<AgentChoice>
                {
                    new() { Input = "2", Label = "Resurrect with no penalty [Dev]", Recommended = true },
                    new() { Input = "1", Label = "Clone this hero (loses equipped gear)" },
                    new() { Input = "0", Label = "Return to main menu (tombstone)" }
                },
                GameState.Combat => BuildCombatChoices(game),
                GameState.Inventory => BuildNumberedChoices("Inventory action", 6, includeZero: true, zeroLabel: "Back"),
                _ => new List<AgentChoice>
                {
                    new() { Input = "1", Label = "Continue / default action", Recommended = true }
                }
            };
        }

        public static List<string> GetInputKeys(IEnumerable<AgentChoice> choices)
        {
            return choices.Select(c => c.Input).ToList();
        }

        private static List<AgentChoice> BuildCustomLevelChoices()
        {
            var choices = new List<AgentChoice>();
            for (int d = 0; d <= 9; d++)
                choices.Add(new AgentChoice { Input = d.ToString(), Label = $"Append digit {d} to level" });
            choices.Add(new AgentChoice { Input = "enter", Label = "Confirm level and start dungeon", Recommended = true });
            choices.Add(new AgentChoice { Input = "backspace", Label = "Delete last digit" });
            choices.Add(new AgentChoice { Input = "0", Label = "Cancel custom level (when buffer empty)" });
            return choices;
        }

        private static List<AgentChoice> BuildDungeonChoices(GameCoordinator game)
        {
            var choices = new List<AgentChoice>();
            var dungeons = game.AvailableDungeons;
            if (dungeons == null || dungeons.Count == 0)
            {
                choices.Add(new AgentChoice { Input = "0", Label = "Back to game loop" });
                return choices;
            }

            int playerLevel = game.CurrentPlayer?.Level ?? 1;
            int bestIndex = -1;
            int bestDelta = int.MaxValue;

            for (int i = 0; i < dungeons.Count; i++)
            {
                var d = dungeons[i];
                bool isCustom = d.Name == GameConstants.DungeonCustomLevelMenuName;
                string label = isCustom
                    ? "Custom dungeon level (opens digit entry — avoid unless intentional)"
                    : $"{d.Name} (levels {d.MinLevel}-{d.MaxLevel}, {d.Theme})";

                choices.Add(new AgentChoice
                {
                    Input = (i + 1).ToString(),
                    Label = label
                });

                if (!isCustom)
                {
                    int delta = Math.Abs(d.MinLevel - playerLevel);
                    if (delta < bestDelta)
                    {
                        bestDelta = delta;
                        bestIndex = i + 1;
                    }
                }
            }

            if (bestIndex > 0)
            {
                var recommended = choices.FirstOrDefault(c => c.Input == bestIndex.ToString());
                if (recommended != null)
                    recommended.Recommended = true;
            }

            choices.Add(new AgentChoice { Input = "0", Label = "Back to game loop" });
            return choices;
        }

        public static List<AgentChoice> BuildCombatChoices(GameCoordinator game)
        {
            var choices = new List<AgentChoice>();
            if (game.CurrentPlayer == null)
                return choices;

            var comboActions = game.CurrentPlayer.GetComboActions();
            if (comboActions == null || comboActions.Count == 0)
            {
                choices.Add(new AgentChoice { Input = "1", Label = "Attack", Recommended = true });
                return choices;
            }

            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                choices.Add(new AgentChoice
                {
                    Input = (i + 1).ToString(),
                    Label = action.Name,
                    Recommended = i == 0
                });
            }

            return choices;
        }

        private static List<AgentChoice> BuildNumberedChoices(
            string prefix,
            int count,
            int recommendedIndex = 1,
            bool includeZero = false,
            string? zeroLabel = null)
        {
            var choices = new List<AgentChoice>();
            for (int i = 1; i <= count; i++)
            {
                choices.Add(new AgentChoice
                {
                    Input = i.ToString(),
                    Label = $"{prefix} {i}",
                    Recommended = i == recommendedIndex
                });
            }

            if (includeZero)
                choices.Add(new AgentChoice { Input = "0", Label = zeroLabel ?? "Back" });

            return choices;
        }

        public static string GetScreenTitle(GameState state) => state switch
        {
            GameState.MainMenu => "Main Menu",
            GameState.TrainingGroundOffer => "Training Ground Offer",
            GameState.PreWeaponPathIntro => "Path Introduction",
            GameState.WeaponSelection => "Weapon Selection",
            GameState.CharacterCreation => "Character Creation",
            GameState.GameLoop => "Game Hub",
            GameState.DungeonSelection => "Dungeon Selection",
            GameState.Dungeon => "Dungeon Exploration",
            GameState.Combat => "Combat",
            GameState.DungeonCompletion => "Dungeon Complete",
            GameState.Death => "Death",
            GameState.Inventory => "Inventory",
            _ => state.ToString()
        };
    }
}
