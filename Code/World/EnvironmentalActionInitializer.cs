using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Responsible for loading and initializing environmental actions from JSON and default configurations.
    /// Handles all action loading logic including theme-based and room-type-specific actions.
    /// </summary>
    public class EnvironmentalActionInitializer
    {
        private readonly string theme;
        private readonly string roomType;
        private readonly Random random;

        public EnvironmentalActionInitializer(string theme, string roomType)
        {
            this.theme = theme;
            this.roomType = roomType;
            this.random = new Random();
        }

        /// <summary>
        /// Initializes environmental actions, attempting JSON loading first, then falling back to defaults.
        /// </summary>
        public List<(Action action, double probability)> InitializeActions()
        {
            var actions = new List<(Action action, double probability)>();
            
            // Try to load from JSON first
            var jsonActions = LoadEnvironmentalActionsFromJson();
            if (jsonActions.Count > 0)
            {
                foreach (var (action, prob) in jsonActions)
                {
                    actions.Add((action, prob));
                }
                return actions;
            }

            // Fallback to default room-specific actions
            var roomActions = GetRoomSpecificActions();
            foreach (var action in roomActions)
            {
                actions.Add((action, 0.7)); // 70% probability for environmental actions
            }

            return actions;
        }

        private List<(Action action, double probability)> LoadEnvironmentalActionsFromJson()
        {
            var loadedActions = new List<(Action, double)>();
            
            try
            {
                string jsonPath = Path.Combine("GameData", "Actions.json");
                if (!File.Exists(jsonPath))
                    return loadedActions;

                string jsonContent = File.ReadAllText(jsonPath);
                var allActions = JsonSerializer.Deserialize<List<ActionData>>(jsonContent);

                if (allActions == null)
                    return loadedActions;

                // Filter actions by environment tag and theme
                var environmentalActions = allActions.Where(action =>
                    action.Tags != null &&
                    action.Tags.Contains("environment") &&
                    (action.Tags.Contains(theme.ToLower()) || action.Tags.Contains("generic"))
                ).ToList();

                foreach (var actionData in environmentalActions)
                {
                    var action = CreateActionFromData(actionData);
                    loadedActions.Add((action, 0.7));
                }
            }
            catch (Exception ex)
            {
                BlockDisplayManager.DisplaySystemBlock(ColoredTextParser.Parse($"Error loading environmental actions from JSON: {ex.Message}"));
            }

            return loadedActions;
        }

        private Action CreateActionFromData(ActionData data)
        {
            var actionType = Enum.TryParse<ActionType>(data.Type, true, out var parsedType) ? parsedType : ActionType.Attack;
            var targetType = TargetType.AreaOfEffect; // Environmental actions are always area of effect

            string enhancedDescription = EnhanceActionDescription(data);

            var action = new Action(
                name: data.Name,
                type: actionType,
                targetType: targetType,
                cooldown: data.Cooldown,
                description: enhancedDescription,
                comboOrder: -1,
                damageMultiplier: data.DamageMultiplier,
                length: data.Length,
                causesBleed: data.CausesBleed,
                causesWeaken: data.CausesWeaken,
                isComboAction: false,
                comboBonusAmount: data.ComboBonusAmount,
                comboBonusDuration: data.ComboBonusDuration
            );

            action.CausesSlow = data.CausesSlow;
            action.CausesPoison = data.CausesPoison;

            return action;
        }

        private string EnhanceActionDescription(ActionData data)
        {
            var modifiers = new List<string>();

            if (data.RollBonus != 0)
            {
                string rollText = data.RollBonus > 0 ? $"+{data.RollBonus}" : data.RollBonus.ToString();
                modifiers.Add($"Roll: {rollText}");
            }

            if (data.DamageMultiplier != 1.0)
                modifiers.Add($"Damage: {data.DamageMultiplier:F1}x");

            if (data.ComboBonusAmount > 0 && data.ComboBonusDuration > 0)
                modifiers.Add($"Combo: +{data.ComboBonusAmount} for {data.ComboBonusDuration} turns");

            if (data.CausesBleed)
                modifiers.Add("Causes Bleed");

            if (data.CausesWeaken)
                modifiers.Add("Causes Weaken");

            if (data.MultiHitCount > 1)
                modifiers.Add($"Multi-hit: {data.MultiHitCount} attacks");

            if (data.SelfDamagePercent > 0)
                modifiers.Add($"Self-damage: {data.SelfDamagePercent}%");

            if (data.StatBonus > 0 && !string.IsNullOrEmpty(data.StatBonusType))
            {
                string durationText = data.StatBonusDuration == -1 ? "dungeon" : $"{data.StatBonusDuration} turns";
                modifiers.Add($"+{data.StatBonus} {data.StatBonusType} ({durationText})");
            }

            if (data.SkipNextTurn)
                modifiers.Add("Skips next turn");

            if (data.RepeatLastAction)
                modifiers.Add("Repeats last action");

            string result = data.Description;
            if (modifiers.Count > 0)
                result += $" | {string.Join(", ", modifiers)}";

            return result;
        }

        private List<Action> GetRoomSpecificActions()
        {
            var actions = new List<Action>();

            var themeActions = GetThemeBasedActions();
            var roomActions = GetRoomTypeActions();

            actions.AddRange(roomActions);
            if (themeActions.Count > 0)
                actions.AddRange(themeActions);

            return actions;
        }

        private List<Action> GetThemeBasedActions()
        {
            var actions = new List<Action>();

            switch (theme.ToLower())
            {
                case "forest":
                    actions.Add(new Action(name: "Falling Branch", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "A heavy branch falls from the forest canopy, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Thorn Vines", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Thorny vines entangle and slow all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "lava":
                    actions.Add(new Action(name: "Lava Splash", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Molten lava splashes from nearby flows, burning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Steam Burst", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Scalding steam erupts from the ground, weakening all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "crypt":
                    actions.Add(new Action(name: "Necrotic Aura", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Dark energy from the crypt saps vitality, causing poison and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Bone Shards", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Flying bone fragments from the crypt cause bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "cavern":
                    actions.Add(new Action(name: "Cave In", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Rocks fall from the cavern ceiling, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Stalactite Drop", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Sharp stalactites fall from above, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "swamp":
                    actions.Add(new Action(name: "Miasma Cloud", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Thick miasma from the swamp poisons and weakens all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Bog Sink", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The swampy ground causes all combatants to sink and slow down", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "desert":
                    actions.Add(new Action(name: "Sandstorm", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Blinding sandstorm from the desert slows and weakens all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Heat Wave", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Intense desert heat causes exhaustion and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "ice":
                    actions.Add(new Action(name: "Ice Shards", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Sharp ice shards explode from the frozen environment, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Blizzard", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Fierce blizzard from the frozen wastes slows and weakens all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "ruins":
                    actions.Add(new Action(name: "Ancient Curse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient magic from the ruins weakens all who disturb them", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Collapsing Pillar", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient pillars from the ruins collapse, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "castle":
                    actions.Add(new Action(name: "Portcullis Drop", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy portcullis from the castle falls, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Arrow Slits", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Hidden arrow slits in the castle fire, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "graveyard":
                    actions.Add(new Action(name: "Grave Mist", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Thick mist from the graveyard poisons and weakens all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Rising Dead", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Undead hands from the graveyard grab and slow all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
            }

            return actions;
        }

        private List<Action> GetRoomTypeActions()
        {
            var actions = new List<Action>();

            switch (roomType.ToLower())
            {
                case "treasure":
                    actions.Add(new Action(name: "Treasure Trap", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient treasure protection mechanisms activate, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Gold Dust", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Shimmering gold dust blinds and weakens all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Vault Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The treasure vault begins to collapse, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "guard":
                    actions.Add(new Action(name: "Alarm Bell", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The alarm bell rings, disorienting and slowing all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Guard Post", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Defensive mechanisms activate, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Barricade", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy barricades fall, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "trap":
                    actions.Add(new Action(name: "Spike Trap", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Hidden spike traps activate, causing severe bleeding", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Poison Dart", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Poison darts fire from hidden mechanisms, poisoning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Pit Trap", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The floor gives way, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Net Trap", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy nets drop from above, slowing all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "puzzle":
                    actions.Add(new Action(name: "Puzzle Failure", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The puzzle mechanism malfunctions, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Ancient Runes", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient runes glow with power, weakening all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Mechanical Failure", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient mechanisms break down, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "rest":
                    actions.Add(new Action(name: "Peaceful Aura", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The peaceful atmosphere weakens aggressive combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Healing Mist", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Mystical healing mist causes confusion and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "storage":
                    actions.Add(new Action(name: "Falling Crates", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy storage crates fall, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Dust Cloud", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Thick dust from storage causes coughing and reduces accuracy", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Shelf Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Storage shelves collapse, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "library":
                    actions.Add(new Action(name: "Falling Books", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy tomes fall from shelves, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Ancient Knowledge", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient knowledge overwhelms minds, causing weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Dust of Ages", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient dust from old books causes coughing and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Magical Tome", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "A magical tome explodes, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "armory":
                    actions.Add(new Action(name: "Falling Weapons", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Weapons fall from racks, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Weapon Rack", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy weapon racks collapse, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Sharp Debris", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Sharp metal debris flies everywhere, causing bleeding", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Armor Clatter", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Falling armor creates deafening noise, slowing all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "kitchen":
                    actions.Add(new Action(name: "Boiling Pot", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Boiling pots spill, causing burns and bleeding", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Kitchen Fire", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Kitchen fires spread, causing burns and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Falling Pots", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy cooking pots fall, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Steam Cloud", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Thick steam clouds cause confusion and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "dining":
                    actions.Add(new Action(name: "Falling Chandelier", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The chandelier falls, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Table Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy dining tables collapse, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Food Poisoning", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Spoiled food releases toxic fumes, poisoning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Wine Spill", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Acidic wine spills cause burns and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "chamber":
                    actions.Add(new Action(name: "Chamber Echo", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Deafening echoes in the chamber slow all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Ancient Curse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient chamber curse weakens all who enter", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Chamber Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The chamber ceiling begins to collapse, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "hall":
                    actions.Add(new Action(name: "Hallway Echo", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Echoes in the long hallway disorient all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Falling Tapestry", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy tapestries fall, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Dust Storm", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient dust from the hallway causes coughing and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "vault":
                    actions.Add(new Action(name: "Vault Lock", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient vault mechanisms activate, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Vault Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The vault begins to collapse, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Protective Ward", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Protective wards weaken all intruders", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "sanctum":
                    actions.Add(new Action(name: "Divine Wrath", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Divine power weakens the unworthy", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Sacred Flames", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Sacred flames burn all combatants, causing bleeding", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Holy Light", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Blinding holy light disorients all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "grotto":
                    actions.Add(new Action(name: "Cave In", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Rocks fall from the grotto ceiling, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Stalactite Drop", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Sharp stalactites fall, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Underground Pool", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Toxic underground water poisons all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "catacomb":
                    actions.Add(new Action(name: "Bone Shards", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Flying bone fragments cause bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Necrotic Aura", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Dark energy saps vitality, causing poison and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Tomb Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient tombs collapse, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "shrine":
                    actions.Add(new Action(name: "Sacred Ground", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Sacred ground weakens those who fight upon it", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Divine Retribution", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Divine retribution causes bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Holy Water", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Blessed water causes burns and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "laboratory":
                    actions.Add(new Action(name: "Chemical Spill", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Toxic chemicals spill, poisoning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Alchemical Explosion", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Alchemical experiments explode, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Falling Equipment", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy laboratory equipment falls, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Toxic Fumes", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Toxic laboratory fumes cause weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "observatory":
                    actions.Add(new Action(name: "Telescope Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Heavy telescopes fall, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Stellar Energy", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Stellar energy weakens all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Cosmic Dust", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Cosmic dust causes confusion and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "throne":
                    actions.Add(new Action(name: "Throne Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The royal throne collapses, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Royal Curse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient royal curse weakens all who disturb the throne", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Crown Jewels", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Crown jewels explode with magical energy, causing bleeding", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Royal Guard", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient royal guard mechanisms activate, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                case "boss":
                    actions.Add(new Action(name: "Chamber Spikes", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Deadly spikes emerge from the chamber walls, causing bleeding wounds", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: true, causesWeaken: false, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Final Curse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The final curse of the dungeon weakens all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 3.0, causesBleed: false, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Chamber Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The boss chamber begins to collapse, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
                default:
                    actions.Add(new Action(name: "Room Collapse", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "The room structure begins to collapse, stunning all combatants", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: false, causesStun: true, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Dust Cloud", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Thick dust clouds cause coughing and reduce accuracy", comboOrder: -1, damageMultiplier: 0.0, length: 2.0, causesBleed: false, causesWeaken: false, causesPoison: true, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    actions.Add(new Action(name: "Ancient Trap", type: ActionType.Debuff, targetType: TargetType.AreaOfEffect, cooldown: 0, description: "Ancient mechanisms activate, causing bleeding and weakness", comboOrder: -1, damageMultiplier: 0.0, length: 2.5, causesBleed: true, causesWeaken: true, causesPoison: false, causesStun: false, isComboAction: false, comboBonusAmount: 0, comboBonusDuration: 0));
                    break;
            }

            return actions;
        }
    }
}

