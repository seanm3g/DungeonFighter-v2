using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RPGGame
{
    public enum PassiveEffectType
    {
        None,
        DamageMultiplier, // e.g., -10% damage
        SpeedMultiplier   // e.g., +25% attack speed
    }

    public class Environment : Entity
    {
        public string Description { get; private set; }
        public bool IsHostile { get; private set; }
        private List<Enemy> enemies;
        private Random random;
        public string Theme { get; private set; }
        public string RoomType { get; private set; }

        // Passive and active effect support
        public PassiveEffectType PassiveEffectType { get; private set; } = PassiveEffectType.None;
        public double PassiveEffectValue { get; private set; } = 1.0;
        public Action? ActiveEffectAction { get; private set; }

        public Environment(string name, string description, bool isHostile, string theme, string roomType = "")
            : base(name)
        {
            random = new Random();
            Description = description;
            IsHostile = isHostile;
            Theme = theme;
            RoomType = roomType;
            enemies = new List<Enemy>();
            InitializeActions();
        }

        private void InitializeActions()
        {
            LoadEnvironmentalActionsFromJson();
        }

        private void LoadEnvironmentalActionsFromJson()
        {
            try
            {
                string jsonPath = Path.Combine("GameData", "Actions.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var allActions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (allActions != null)
                    {
                        // Filter actions by environment tag and theme
                        var environmentalActions = allActions.Where(action => 
                            action.Tags != null && 
                            action.Tags.Contains("environment") &&
                            (action.Tags.Contains(Theme.ToLower()) || action.Tags.Contains("generic"))
                        ).ToList();
                        
                        if (environmentalActions.Any())
                        {
                            // Add all matching environmental actions
                            foreach (var actionData in environmentalActions)
                            {
                                var action = CreateActionFromData(actionData);
                                AddAction(action, 0.7); // 70% probability for environmental actions
                            }
                        }
                        else
                        {
                            // Fallback to default environmental action
                            AddDefaultEnvironmentalAction();
                        }
                    }
                    else
                    {
                        AddDefaultEnvironmentalAction();
                    }
                }
                else
                {
                    AddDefaultEnvironmentalAction();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading environmental actions from JSON: {ex.Message}");
                AddDefaultEnvironmentalAction();
            }
        }

        private void AddDefaultEnvironmentalAction()
        {
            // Create room-specific environmental actions instead of theme-based
            var roomActions = GetRoomSpecificActions();
            foreach (var action in roomActions)
            {
                AddAction(action, 0.7); // 70% probability for environmental actions
            }
        }

        private List<Action> GetRoomSpecificActions()
        {
            var actions = new List<Action>();
            
            // Get theme-appropriate actions based on both room type and dungeon theme
            var themeActions = GetThemeBasedActions();
            var roomActions = GetRoomTypeActions();
            
            // Combine theme and room actions, prioritizing room-specific actions
            actions.AddRange(roomActions);
            if (themeActions.Count > 0)
            {
                actions.AddRange(themeActions);
            }
            
            return actions;
        }
        
        private List<Action> GetThemeBasedActions()
        {
            var actions = new List<Action>();
            
            switch (Theme.ToLower())
            {
                case "forest":
                    actions.Add(new Action("Falling Branch", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "A heavy branch falls from the forest canopy, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Thorn Vines", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Thorny vines entangle and slow all combatants", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "lava":
                    actions.Add(new Action("Lava Splash", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Molten lava splashes from nearby flows, burning all combatants", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Steam Burst", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Scalding steam erupts from the ground, weakening all combatants", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "crypt":
                    actions.Add(new Action("Necrotic Aura", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Dark energy from the crypt saps vitality, causing poison and weakness", -1, 0.0, 2.5, false, true, true, false, false, 0, 0));
                    actions.Add(new Action("Bone Shards", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Flying bone fragments from the crypt cause bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    break;
                    
                case "cavern":
                    actions.Add(new Action("Cave In", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Rocks fall from the cavern ceiling, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Stalactite Drop", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Sharp stalactites fall from above, causing bleeding wounds", -1, 0.0, 2.5, true, false, false, false, false, 0, 0));
                    break;
                    
                case "swamp":
                    actions.Add(new Action("Miasma Cloud", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Thick miasma from the swamp poisons and weakens all combatants", -1, 0.0, 2.5, false, true, true, false, false, 0, 0));
                    actions.Add(new Action("Bog Sink", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The swampy ground causes all combatants to sink and slow down", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "desert":
                    actions.Add(new Action("Sandstorm", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Blinding sandstorm from the desert slows and weakens all combatants", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Heat Wave", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Intense desert heat causes exhaustion and weakness", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    break;
                    
                case "ice":
                    actions.Add(new Action("Ice Shards", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Sharp ice shards explode from the frozen environment, causing bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Blizzard", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Fierce blizzard from the frozen wastes slows and weakens all combatants", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "ruins":
                    actions.Add(new Action("Ancient Curse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient magic from the ruins weakens all who disturb them", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Collapsing Pillar", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient pillars from the ruins collapse, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    break;
                    
                case "castle":
                    actions.Add(new Action("Portcullis Drop", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy portcullis from the castle falls, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Arrow Slits", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Hidden arrow slits in the castle fire, causing bleeding wounds", -1, 0.0, 2.5, true, false, false, false, false, 0, 0));
                    break;
                    
                case "graveyard":
                    actions.Add(new Action("Grave Mist", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Thick mist from the graveyard poisons and weakens all combatants", -1, 0.0, 2.5, false, true, true, false, false, 0, 0));
                    actions.Add(new Action("Rising Dead", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Undead hands from the graveyard grab and slow all combatants", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    break;
            }
            
            return actions;
        }
        
        private List<Action> GetRoomTypeActions()
        {
            var actions = new List<Action>();
            
            switch (RoomType.ToLower())
            {
                case "treasure":
                    actions.Add(new Action("Treasure Trap", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient treasure protection mechanisms activate, causing bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Gold Dust", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Shimmering gold dust blinds and weakens all combatants", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Vault Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The treasure vault begins to collapse, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    break;
                    
                case "guard":
                    actions.Add(new Action("Alarm Bell", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The alarm bell rings, disorienting and slowing all combatants", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Guard Post", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Defensive mechanisms activate, causing bleeding wounds", -1, 0.0, 2.5, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Barricade", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy barricades fall, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    break;
                    
                case "trap":
                    actions.Add(new Action("Spike Trap", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Hidden spike traps activate, causing severe bleeding", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Poison Dart", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Poison darts fire from hidden mechanisms, poisoning all combatants", -1, 0.0, 2.0, false, false, true, false, false, 0, 0));
                    actions.Add(new Action("Pit Trap", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The floor gives way, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Net Trap", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy nets drop from above, slowing all combatants", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    break;
                    
                case "puzzle":
                    actions.Add(new Action("Puzzle Failure", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The puzzle mechanism malfunctions, causing bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Ancient Runes", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient runes glow with power, weakening all combatants", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Mechanical Failure", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient mechanisms break down, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    break;
                    
                case "rest":
                    actions.Add(new Action("Peaceful Aura", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The peaceful atmosphere weakens aggressive combatants", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Healing Mist", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Mystical healing mist causes confusion and weakness", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "storage":
                    actions.Add(new Action("Falling Crates", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy storage crates fall, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Dust Cloud", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Thick dust from storage causes coughing and reduces accuracy", -1, 0.0, 2.0, false, false, false, false, false, 0, 0));
                    actions.Add(new Action("Shelf Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Storage shelves collapse, causing bleeding wounds", -1, 0.0, 2.5, true, false, false, false, false, 0, 0));
                    break;
                    
                case "library":
                    actions.Add(new Action("Falling Books", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy tomes fall from shelves, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Ancient Knowledge", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient knowledge overwhelms minds, causing weakness", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Dust of Ages", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient dust from old books causes coughing and weakness", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Magical Tome", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "A magical tome explodes, causing bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    break;
                    
                case "armory":
                    actions.Add(new Action("Falling Weapons", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Weapons fall from racks, causing bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Weapon Rack", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy weapon racks collapse, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Sharp Debris", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Sharp metal debris flies everywhere, causing bleeding", -1, 0.0, 2.5, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Armor Clatter", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Falling armor creates deafening noise, slowing all combatants", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "kitchen":
                    actions.Add(new Action("Boiling Pot", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Boiling pots spill, causing burns and bleeding", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Kitchen Fire", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Kitchen fires spread, causing burns and weakness", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Falling Pots", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy cooking pots fall, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Steam Cloud", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Thick steam clouds cause confusion and weakness", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "dining":
                    actions.Add(new Action("Falling Chandelier", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The chandelier falls, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Table Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy dining tables collapse, causing bleeding wounds", -1, 0.0, 2.5, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Food Poisoning", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Spoiled food releases toxic fumes, poisoning all combatants", -1, 0.0, 2.0, false, false, true, false, false, 0, 0));
                    actions.Add(new Action("Wine Spill", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Acidic wine spills cause burns and weakness", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "chamber":
                    actions.Add(new Action("Chamber Echo", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Deafening echoes in the chamber slow all combatants", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Ancient Curse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient chamber curse weakens all who enter", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Chamber Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The chamber ceiling begins to collapse, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    break;
                    
                case "hall":
                    actions.Add(new Action("Hallway Echo", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Echoes in the long hallway disorient all combatants", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Falling Tapestry", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy tapestries fall, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Dust Storm", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient dust from the hallway causes coughing and weakness", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "vault":
                    actions.Add(new Action("Vault Lock", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient vault mechanisms activate, causing bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Vault Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The vault begins to collapse, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Protective Ward", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Protective wards weaken all intruders", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    break;
                    
                case "sanctum":
                    actions.Add(new Action("Divine Wrath", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Divine power weakens the unworthy", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Sacred Flames", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Sacred flames burn all combatants, causing bleeding", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Holy Light", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Blinding holy light disorients all combatants", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    break;
                    
                case "grotto":
                    actions.Add(new Action("Cave In", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Rocks fall from the grotto ceiling, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Stalactite Drop", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Sharp stalactites fall, causing bleeding wounds", -1, 0.0, 2.5, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Underground Pool", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Toxic underground water poisons all combatants", -1, 0.0, 2.0, false, false, true, false, false, 0, 0));
                    break;
                    
                case "catacomb":
                    actions.Add(new Action("Bone Shards", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Flying bone fragments cause bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Necrotic Aura", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Dark energy saps vitality, causing poison and weakness", -1, 0.0, 2.5, false, true, true, false, false, 0, 0));
                    actions.Add(new Action("Tomb Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient tombs collapse, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    break;
                    
                case "shrine":
                    actions.Add(new Action("Sacred Ground", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Sacred ground weakens those who fight upon it", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Divine Retribution", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Divine retribution causes bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Holy Water", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Blessed water causes burns and weakness", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "laboratory":
                    actions.Add(new Action("Chemical Spill", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Toxic chemicals spill, poisoning all combatants", -1, 0.0, 2.0, false, false, true, false, false, 0, 0));
                    actions.Add(new Action("Alchemical Explosion", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Alchemical experiments explode, causing bleeding wounds", -1, 0.0, 2.5, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Falling Equipment", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy laboratory equipment falls, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Toxic Fumes", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Toxic laboratory fumes cause weakness", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "observatory":
                    actions.Add(new Action("Telescope Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Heavy telescopes fall, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Stellar Energy", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Stellar energy weakens all combatants", -1, 0.0, 2.5, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Cosmic Dust", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Cosmic dust causes confusion and weakness", -1, 0.0, 2.0, false, true, false, false, false, 0, 0));
                    break;
                    
                case "throne":
                    actions.Add(new Action("Throne Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The royal throne collapses, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Royal Curse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient royal curse weakens all who disturb the throne", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Crown Jewels", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Crown jewels explode with magical energy, causing bleeding", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Royal Guard", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient royal guard mechanisms activate, causing bleeding wounds", -1, 0.0, 2.5, true, false, false, false, false, 0, 0));
                    break;
                    
                case "boss":
                    actions.Add(new Action("Chamber Spikes", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Deadly spikes emerge from the chamber walls, causing bleeding wounds", -1, 0.0, 2.0, true, false, false, false, false, 0, 0));
                    actions.Add(new Action("Final Curse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The final curse of the dungeon weakens all combatants", -1, 0.0, 3.0, false, true, false, false, false, 0, 0));
                    actions.Add(new Action("Chamber Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The boss chamber begins to collapse, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    break;
                    
                default: // Generic room
                    actions.Add(new Action("Room Collapse", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "The room structure begins to collapse, stunning all combatants", -1, 0.0, 2.0, false, false, false, true, false, 0, 0));
                    actions.Add(new Action("Dust Cloud", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Thick dust clouds cause coughing and reduce accuracy", -1, 0.0, 2.0, false, false, false, false, false, 0, 0));
                    actions.Add(new Action("Ancient Trap", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient mechanisms activate, causing bleeding and weakness", -1, 0.0, 2.5, true, true, false, false, false, 0, 0));
                    break;
            }
            
            return actions;
        }

        private Action CreateActionFromData(ActionData data)
        {
            var actionType = Enum.TryParse<ActionType>(data.Type, true, out var parsedType) ? parsedType : ActionType.Attack;
            var targetType = TargetType.AreaOfEffect; // Environmental actions are always area of effect
            
            // Enhance description with modifiers
            string enhancedDescription = EnhanceActionDescription(data);
            
            var action = new Action(
                name: data.Name,
                type: actionType,
                targetType: targetType,
                baseValue: data.BaseValue,
                range: data.Range,
                cooldown: data.Cooldown,
                description: enhancedDescription,
                comboOrder: -1, // Default combo order
                damageMultiplier: data.DamageMultiplier,
                length: data.Length,
                causesBleed: data.CausesBleed,
                causesWeaken: data.CausesWeaken,
                isComboAction: false, // Default to false
                comboBonusAmount: data.ComboBonusAmount,
                comboBonusDuration: data.ComboBonusDuration
            );
            
            // Set additional debuff properties
            action.CausesSlow = data.CausesSlow;
            action.CausesPoison = data.CausesPoison;
            
            return action;
        }

        private string EnhanceActionDescription(ActionData data)
        {
            var modifiers = new List<string>();
            
            // Add roll bonus information
            if (data.RollBonus != 0)
            {
                string rollText = data.RollBonus > 0 ? $"+{data.RollBonus}" : data.RollBonus.ToString();
                modifiers.Add($"Roll: {rollText}");
            }
            
            // Add damage multiplier information
            if (data.DamageMultiplier != 1.0)
            {
                modifiers.Add($"Damage: {data.DamageMultiplier:F1}x");
            }
            
            // Add combo bonus information
            if (data.ComboBonusAmount > 0 && data.ComboBonusDuration > 0)
            {
                modifiers.Add($"Combo: +{data.ComboBonusAmount} for {data.ComboBonusDuration} turns");
            }
            
            // Add status effect information
            if (data.CausesBleed)
            {
                modifiers.Add("Causes Bleed");
            }
            
            if (data.CausesWeaken)
            {
                modifiers.Add("Causes Weaken");
            }
            
            // Add multi-hit information
            if (data.MultiHitCount > 1)
            {
                modifiers.Add($"Multi-hit: {data.MultiHitCount} attacks");
            }
            
            // Add self-damage information
            if (data.SelfDamagePercent > 0)
            {
                modifiers.Add($"Self-damage: {data.SelfDamagePercent}%");
            }
            
            // Add stat bonus information
            if (data.StatBonus > 0 && !string.IsNullOrEmpty(data.StatBonusType))
            {
                string durationText = data.StatBonusDuration == -1 ? "dungeon" : $"{data.StatBonusDuration} turns";
                modifiers.Add($"+{data.StatBonus} {data.StatBonusType} ({durationText})");
            }
            
            // Add special effects
            if (data.SkipNextTurn)
            {
                modifiers.Add("Skips next turn");
            }
            
            if (data.RepeatLastAction)
            {
                modifiers.Add("Repeats last action");
            }
            
            // Combine base description with modifiers
            string result = data.Description;
            if (modifiers.Count > 0)
            {
                result += $" | {string.Join(", ", modifiers)}";
            }
            
            return result;
        }

        public void GenerateEnemies(int roomLevel)
        {
            if (!IsHostile) return;

            int enemyCount = Math.Max(1, (int)Math.Ceiling(roomLevel / 2.0));
            
            // Try to load enemy data from JSON first
            var jsonEnemies = LoadEnemyDataFromJson();
            if (jsonEnemies != null && jsonEnemies.Count > 0)
            {
                GenerateEnemiesFromJson(roomLevel, enemyCount, jsonEnemies);
                return;
            }
            
            // Fallback: Create basic enemies if JSON loading fails
            Console.WriteLine("Warning: Could not load enemy data from JSON, creating basic enemies");
            var tuning = TuningConfig.Instance;
            var basicEnemies = new[] { 
                new { Name = "Basic Enemy", BaseHealth = 80, BaseStrength = 8, BaseAgility = 6, BaseTechnique = 4, BaseIntelligence = 3, Primary = PrimaryAttribute.Strength }
            };
            
            for (int i = 0; i < enemyCount; i++)
            {
                int enemyLevel = Math.Max(1, roomLevel + random.Next(-tuning.EnemyScaling.EnemyLevelVariance, tuning.EnemyScaling.EnemyLevelVariance + 1));
                var enemyType = basicEnemies[random.Next(basicEnemies.Length)];
                
                // Apply difficulty multipliers from tuning config
                int adjustedHealth = (int)(enemyType.BaseHealth * tuning.EnemyScaling.EnemyHealthMultiplier);
                int adjustedStrength = (int)(enemyType.BaseStrength * tuning.EnemyScaling.EnemyDamageMultiplier);
                int adjustedAgility = (int)(enemyType.BaseAgility * tuning.EnemyScaling.EnemyDamageMultiplier);
                int adjustedTechnique = (int)(enemyType.BaseTechnique * tuning.EnemyScaling.EnemyDamageMultiplier);
                int adjustedIntelligence = (int)(enemyType.BaseIntelligence * tuning.EnemyScaling.EnemyDamageMultiplier);
                
                var enemy = new Enemy(
                    enemyType.Name, 
                    enemyLevel,
                    adjustedHealth,
                    adjustedStrength,
                    adjustedAgility,
                    adjustedTechnique,
                    adjustedIntelligence,
                    0, // Base armor - will be scaled by level in Enemy constructor
                    enemyType.Primary
                );
                enemies.Add(enemy);
            }
        }

        public bool HasLivingEnemies()
        {
            return enemies.Any(e => e.IsAlive);
        }

        public Enemy? GetNextLivingEnemy()
        {
            return enemies.FirstOrDefault(e => e.IsAlive);
        }

        private int environmentActionCount = 0;
        private int maxEnvironmentActions = 2; // Maximum 2 environmental actions per fight
        
        /// <summary>
        /// Resets the environment action count for a new fight
        /// </summary>
        public void ResetForNewFight()
        {
            environmentActionCount = 0;
        }
        
        /// <summary>
        /// Checks if the environment should act (called by action speed system)
        /// </summary>
        public bool ShouldEnvironmentAct()
        {
            // Don't act if we've already used our maximum actions for this fight
            if (environmentActionCount >= maxEnvironmentActions)
            {
                return false;
            }
            
            // 10% chance to act when it's the environment's turn (but limited to max 2 per fight)
            if (IsHostile && random.NextDouble() < 0.10)
            {
                environmentActionCount++;
                return true;
            }
            
            return false;
        }

        public override string GetDescription()
        {
            string enemyInfo = "";
            if (enemies.Any())
            {
                var livingEnemies = enemies.Count(e => e.IsAlive);
                enemyInfo = $"\nThere are {livingEnemies} enemies present.";
            }
            return $"{Description}{enemyInfo}";
        }

        public override string ToString()
        {
            return $"{Name}: {GetDescription()}";
        }

        // Methods to apply passive and active effects
        public double ApplyPassiveEffect(double value)
        {
            if (PassiveEffectType == PassiveEffectType.DamageMultiplier)
                return value * PassiveEffectValue;
            return value;
        }

        public void ApplyActiveEffect(Character player, Enemy enemy)
        {
            if (ActiveEffectAction != null)
            {
                int dmg = ActiveEffectAction.BaseValue;
                player.TakeDamage(dmg);
                enemy.TakeDamage(dmg);
            }
        }

        private List<EnemyData>? LoadEnemyDataFromJson()
        {
            try
            {
                string[] possiblePaths = {
                    Path.Combine("GameData", "Enemies.json"),
                    Path.Combine("..", "GameData", "Enemies.json"),
                    Path.Combine("..", "..", "GameData", "Enemies.json"),
                    Path.Combine("DF4 - CONSOLE", "GameData", "Enemies.json"),
                    Path.Combine("..", "DF4 - CONSOLE", "GameData", "Enemies.json")
                };

                string? foundPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    var enemies = System.Text.Json.JsonSerializer.Deserialize<List<EnemyData>>(jsonContent);
                    return enemies;
                }
                else
                {
                    Console.WriteLine($"Warning: Enemies.json not found. Tried paths: {string.Join(", ", possiblePaths)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading enemy data from JSON: {ex.Message}");
            }
            return null;
        }

        private void GenerateEnemiesFromJson(int roomLevel, int enemyCount, List<EnemyData> enemyData)
        {
            var tuning = TuningConfig.Instance;
            
            // Filter enemies by theme if possible
            var themeEnemies = GetThemeAppropriateEnemies(enemyData);
            var availableEnemies = themeEnemies.Count > 0 ? themeEnemies : enemyData;
            
            for (int i = 0; i < enemyCount; i++)
            {
                int enemyLevel = Math.Max(1, roomLevel + random.Next(-tuning.EnemyScaling.EnemyLevelVariance, tuning.EnemyScaling.EnemyLevelVariance + 1));
                var enemyTemplate = availableEnemies[random.Next(availableEnemies.Count)];
                
                // Use EnemyLoader to create the enemy with proper actions loaded
                var enemy = EnemyLoader.CreateEnemy(enemyTemplate.Name, enemyLevel);
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
                else
                {
                    // Fallback: Create basic enemy if EnemyLoader fails
                    Console.WriteLine($"Warning: Could not create enemy {enemyTemplate.Name} from EnemyLoader, creating basic enemy");
                    var basicEnemy = new Enemy(
                        enemyTemplate.Name, 
                        enemyLevel,
                        80 + (enemyLevel * tuning.Character.EnemyHealthPerLevel),
                        enemyTemplate.Strength,
                        enemyTemplate.Agility,
                        enemyTemplate.Technique,
                        enemyTemplate.Intelligence,
                        enemyTemplate.Armor,
                        PrimaryAttribute.Strength
                    );
                    enemies.Add(basicEnemy);
                }
            }
        }

        private List<EnemyData> GetThemeAppropriateEnemies(List<EnemyData> allEnemies)
        {
            // Map themes to appropriate enemies based on the dungeon enemy lists
            var themeEnemyMap = new Dictionary<string, string[]>
            {
                ["Forest"] = new[] { "Goblin", "Spider", "Wolf", "Bear", "Treant", "Dryad", "Ent" },
                ["Lava"] = new[] { "Wraith", "Slime", "Bat", "Fire Elemental", "Lava Golem", "Salamander", "Magma Beast" },
                ["Crypt"] = new[] { "Skeleton", "Zombie", "Wraith", "Lich", "Ghoul", "Wight", "Mummy", "Banshee" },
                ["Cavern"] = new[] { "Bat", "Spider", "Slime", "Cave Troll", "Rock Golem", "Cave Bear", "Giant Rat" },
                ["Swamp"] = new[] { "Slime", "Spider", "Swamp Monster", "Bog Witch", "Marsh Hag", "Will-o'-Wisp", "Swamp Dragon" },
                ["Desert"] = new[] { "Sand Worm", "Desert Nomad", "Scorpion", "Mummy", "Sand Elemental", "Desert Bandit", "Cactus Golem" },
                ["Ice"] = new[] { "Ice Elemental", "Frost Giant", "Ice Golem", "Winter Wolf", "Frost Wraith", "Ice Spider", "Yeti" },
                ["Ruins"] = new[] { "Skeleton", "Zombie", "Gargoyle", "Stone Guardian", "Ancient Construct", "Ruin Wraith", "Fallen Knight" },
                ["Castle"] = new[] { "Knight", "Guard", "Gargoyle", "Ghost", "Vampire", "Royal Guard", "Castle Wraith" },
                ["Graveyard"] = new[] { "Skeleton", "Zombie", "Ghost", "Wraith", "Banshee", "Grave Wight", "Necromancer" },
                ["Crystal"] = new[] { "Crystal Golem", "Prism Spider", "Shard Beast", "Crystal Sprite", "Geode Beast", "Crystal Wyrm" },
                ["Temple"] = new[] { "Stone Guardian", "Temple Warden", "Ancient Sentinel", "Temple Guard", "Priest", "Paladin" },
                ["Generic"] = new[] { "Bandit", "Orc", "Troll", "Kobold", "Goblin" }
            };

            if (themeEnemyMap.TryGetValue(Theme, out var themeEnemyNames))
            {
                return allEnemies.Where(e => themeEnemyNames.Contains(e.Name)).ToList();
            }

            // If theme not found, return all enemies
            return allEnemies;
        }
    }

} 