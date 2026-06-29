using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPGGame.MCP.Models;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    public static class AgentContextBuilder
    {
        public static AgentContextSnapshot Build(GameCoordinator game, OutputCapture? outputCapture, int recentEventCount = 15)
        {
            var inputContext = AgentChoiceBuilder.ResolveInputContext(game);
            var state = game.CurrentState;
            var choices = AgentChoiceBuilder.BuildChoices(game, inputContext);

            var snapshot = new AgentContextSnapshot
            {
                CurrentState = state.ToString(),
                Screen = new AgentScreenInfo
                {
                    State = state.ToString(),
                    Title = AgentChoiceBuilder.GetScreenTitle(state)
                },
                Summary = BuildSummary(game, state),
                SuggestedFocus = BuildSuggestedFocus(game, state, inputContext),
                PendingInputMode = inputContext.PendingInputMode,
                CustomLevelBuffer = inputContext.CustomLevelBuffer,
                Hints = AgentChoiceBuilder.BuildHints(inputContext, state),
                Choices = choices,
                Player = game.CurrentPlayer != null ? BuildPlayerBrief(game.CurrentPlayer) : null,
                Dungeons = state == GameState.DungeonSelection ? BuildDungeonList(game) : new List<DungeonOptionSnapshot>(),
                RecentEvents = outputCapture?.GetRecentOutput(recentEventCount) ?? new List<string>(),
                UserDirective = McpToolState.AgentDirective,
                LastRunSummary = McpToolState.LastRunSummary
            };

            return snapshot;
        }

        public static List<DungeonOptionSnapshot> BuildDungeonList(GameCoordinator game)
        {
            var list = new List<DungeonOptionSnapshot>();
            var dungeons = game.AvailableDungeons;
            if (dungeons == null)
                return list;

            for (int i = 0; i < dungeons.Count; i++)
            {
                var d = dungeons[i];
                list.Add(new DungeonOptionSnapshot
                {
                    Index = i + 1,
                    Name = d.Name,
                    MinLevel = d.MinLevel,
                    MaxLevel = d.MaxLevel,
                    Theme = d.Theme,
                    IsCustomLevelEntry = d.Name == GameConstants.DungeonCustomLevelMenuName
                });
            }

            return list;
        }

        private static AgentPlayerBrief BuildPlayerBrief(Character player)
        {
            return new AgentPlayerBrief
            {
                Name = player.Name,
                Level = player.Level,
                CurrentHealth = player.CurrentHealth,
                MaxHealth = player.MaxHealth,
                HealthPercentage = player.GetHealthPercentage(),
                XP = player.XP,
                Strength = player.Strength,
                Agility = player.Agility,
                Technique = player.Technique,
                Intelligence = player.Intelligence,
                WeaponName = player.Weapon?.Name,
                WeaponTier = player.Weapon?.Tier,
                HeadArmor = player.Head?.Name,
                BodyArmor = player.Body?.Name,
                FeetArmor = player.Feet?.Name,
                Inventory = player.Inventory.Select(i => new ItemSnapshot
                {
                    Name = i.Name,
                    Type = i.GetType().Name,
                    Tier = i.Tier,
                    Rarity = i.Rarity ?? "Common"
                }).ToList()
            };
        }

        private static string BuildSummary(GameCoordinator game, GameState state)
        {
            var sb = new StringBuilder();
            var player = game.CurrentPlayer;

            if (player != null)
            {
                sb.Append($"{player.Name} is level {player.Level} at {player.CurrentHealth}/{player.MaxHealth} HP");
                if (player.Weapon != null)
                    sb.Append($" wielding {player.Weapon.Name}");
                sb.Append(". ");
            }

            switch (state)
            {
                case GameState.DungeonSelection:
                    sb.Append("Choose a dungeon to enter.");
                    break;
                case GameState.DungeonCompletion:
                    sb.Append("Dungeon cleared — review rewards and continue.");
                    break;
                case GameState.Death:
                    sb.Append("Character died.");
                    if (game.CurrentDungeon != null && game.CurrentRoom != null)
                        sb.Append($" Fell in {game.CurrentDungeon.Name} at room {GetRoomNumber(game)}.");
                    break;
                case GameState.Combat:
                    sb.Append("In combat.");
                    break;
                case GameState.GameLoop:
                    sb.Append("At the hub — enter a dungeon or manage inventory.");
                    break;
                default:
                    sb.Append($"On screen: {AgentChoiceBuilder.GetScreenTitle(state)}.");
                    break;
            }

            return sb.ToString().Trim();
        }

        private static string BuildSuggestedFocus(GameCoordinator game, GameState state, McpGameplayInputContext inputContext)
        {
            if (inputContext.PendingInputMode == AgentChoiceBuilder.ModeCustomDungeonLevel)
                return "Finish or cancel custom level entry before picking a numbered dungeon.";

            if (inputContext.PendingInputMode == AgentChoiceBuilder.ModeExitChoice)
                return "Choose whether to continue the dungeon or leave safely.";

            return state switch
            {
                GameState.TrainingGroundOffer => "Skip training (2) for faster progression unless testing tutorial.",
                GameState.DungeonSelection => $"Pick a dungeon near level {game.CurrentPlayer?.Level ?? 1}; avoid custom level unless testing.",
                GameState.DungeonCompletion => "Continue to dungeon selection or check inventory for upgrades.",
                GameState.Death => "Return to main menu or reset_game for a fresh run.",
                GameState.Combat => "Use combo actions; prefer attacks when health is high.",
                _ => "Call handle_input with a choice input key from the choices list."
            };
        }

        private static int GetRoomNumber(GameCoordinator game)
        {
            if (game.CurrentDungeon?.Rooms == null || game.CurrentRoom == null)
                return 0;
            int idx = game.CurrentDungeon.Rooms.IndexOf(game.CurrentRoom);
            return idx >= 0 ? idx + 1 : 0;
        }
    }
}
