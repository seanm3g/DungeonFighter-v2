using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Generates theme-appropriate discovery messages for exploration
    /// Uses pattern similar to FlavorText system
    /// </summary>
    public class EnvironmentDiscoveryGenerator
    {
        private static readonly Random random = new Random();

        /// <summary>
        /// Generates a discovery message based on room theme and type
        /// </summary>
        public string GenerateDiscovery(Environment room)
        {
            var discoveries = GetDiscoveryMessages(room.Theme, room.RoomType);
            if (discoveries.Count > 0)
            {
                return discoveries[random.Next(discoveries.Count)];
            }
            
            // Fallback message
            return "You notice something interesting about the room.";
        }

        /// <summary>
        /// Gets discovery messages for a specific theme and room type
        /// </summary>
        private List<string> GetDiscoveryMessages(string theme, string roomType)
        {
            var messages = new List<string>();
            
            // Theme-based discovery messages
            switch (theme.ToLower())
            {
                case "forest":
                    messages.AddRange(new[]
                    {
                        "You notice the trees seem to watch you.",
                        "Animal sounds echo in the distance.",
                        "The air feels charged with natural magic.",
                        "Ancient markings are carved into the bark of nearby trees.",
                        "You spot animal tracks leading deeper into the clearing."
                    });
                    break;
                    
                case "crypt":
                    messages.AddRange(new[]
                    {
                        "Ancient runes cover the walls.",
                        "The air smells of decay and dust.",
                        "You sense the presence of the undead.",
                        "Cryptic symbols glow faintly in the dim light.",
                        "The walls are lined with burial niches, some still occupied."
                    });
                    break;
                    
                case "lava":
                case "volcano":
                    messages.AddRange(new[]
                    {
                        "The heat is oppressive, but you spot a safe path.",
                        "Molten rock bubbles ominously nearby.",
                        "Steam rises from cracks in the floor.",
                        "The air shimmers with intense heat.",
                        "You notice a pattern in the lava flows that might be useful."
                    });
                    break;
                    
                case "ice":
                    messages.AddRange(new[]
                    {
                        "Ice crystals reflect light in beautiful patterns.",
                        "Your breath forms clouds in the frigid air.",
                        "Frozen formations create natural cover.",
                        "The ice seems unnaturally cold here.",
                        "Strange shapes are visible beneath the frozen surface."
                    });
                    break;
                    
                case "swamp":
                    messages.AddRange(new[]
                    {
                        "Murky water hides unknown dangers.",
                        "Twisted trees create a maze of shadows.",
                        "The ground feels unstable beneath your feet.",
                        "Noxious fumes make your eyes water.",
                        "Ancient stone circles rise from the mire."
                    });
                    break;
                    
                case "desert":
                    messages.AddRange(new[]
                    {
                        "Sand shifts constantly, revealing hidden paths.",
                        "The heat creates mirages in the distance.",
                        "Ancient ruins peek through the dunes.",
                        "A small oasis offers brief respite.",
                        "Desert winds carry whispers of the past."
                    });
                    break;
                    
                case "ruins":
                    messages.AddRange(new[]
                    {
                        "Broken pillars hint at past glory.",
                        "Fallen stones create natural barriers.",
                        "Ancient symbols are barely visible on weathered walls.",
                        "The structure seems unstable in places.",
                        "Time has taken its toll, but secrets remain."
                    });
                    break;
                    
                case "castle":
                    messages.AddRange(new[]
                    {
                        "Tapestries line the walls, telling ancient stories.",
                        "Armor stands guard in silent vigil.",
                        "Torches cast dancing shadows.",
                        "The architecture suggests this was once a grand hall.",
                        "You notice hidden passages behind the walls."
                    });
                    break;
                    
                case "graveyard":
                    messages.AddRange(new[]
                    {
                        "Gravestones mark the final resting places.",
                        "The ground feels disturbed in places.",
                        "Mist swirls around ancient monuments.",
                        "You sense the weight of history here.",
                        "Some graves appear to have been recently opened."
                    });
                    break;
                    
                default:
                    // Generic messages for unknown themes
                    messages.AddRange(new[]
                    {
                        "You notice something unusual about the room.",
                        "The atmosphere feels charged with energy.",
                        "Ancient markings cover the walls.",
                        "Something doesn't feel quite right here.",
                        "You sense danger lurking nearby."
                    });
                    break;
            }
            
            return messages;
        }
    }
}

