using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Provides narrative text generation and retrieval from FlavorText.json.
    /// Encapsulates text formatting, randomization, and fallback logic for battle narratives.
    /// </summary>
    public class NarrativeTextProvider
    {
        private readonly Random narrativeRandom;

        public NarrativeTextProvider()
        {
            narrativeRandom = new Random();
        }

        /// <summary>
        /// Gets a random narrative text from FlavorText.json for the specified event type
        /// </summary>
        /// <param name="eventType">The type of narrative event (firstBlood, criticalHit, etc.)</param>
        /// <returns>A random narrative string for the event type</returns>
        public string GetRandomNarrative(string eventType)
        {
            try
            {
                var flavorData = FlavorText.GetData();

                // Use reflection to get the combat narratives
                var combatNarrativesProperty = flavorData.GetType().GetProperty("CombatNarratives");
                if (combatNarrativesProperty == null)
                {
                    return GetFallbackNarrative(eventType);
                }

                var combatNarratives = combatNarrativesProperty.GetValue(flavorData);
                if (combatNarratives == null)
                {
                    return GetFallbackNarrative(eventType);
                }

                // Get the specific event type array
                var eventProperty = combatNarratives.GetType().GetProperty(eventType);
                if (eventProperty == null)
                {
                    return GetFallbackNarrative(eventType);
                }

                var narratives = eventProperty.GetValue(combatNarratives) as string[];
                if (narratives == null || narratives.Length == 0)
                {
                    return GetFallbackNarrative(eventType);
                }

                // Return a random narrative
                return narratives[narrativeRandom.Next(narratives.Length)];
            }
            catch (Exception)
            {
                return GetFallbackNarrative(eventType);
            }
        }

        /// <summary>
        /// Provides fallback narrative text when FlavorText.json is not available
        /// </summary>
        /// <param name="eventType">The type of narrative event</param>
        /// <returns>A fallback narrative string</returns>
        public string GetFallbackNarrative(string eventType)
        {
            return eventType switch
            {
                "firstBlood" => "The first drop of blood is drawn! The battle has truly begun.",
                "criticalHit" => "A devastating blow strikes true! The impact is felt throughout the battlefield.",
                "criticalMiss" => "A wild swing misses completely! The attack goes wide of its target.",
                "environmentalAction" => "The environment itself joins the fray! {effect}",
                "healthLeadChange" => "The tide of battle shifts! {name} now holds the advantage!",
                "escalatingTension" => "The battle grows more desperate with each passing moment!",
                "healthRecovery" => "{name} feels renewed strength flowing through their veins.",
                "below50Percent" => "{name} staggers under the weight of their injuries, but refuses to yield!",
                "below10Percent" => "{name} is on the brink of collapse, but their will to fight remains unbroken!",
                "playerDefeated" => "You collapse to the ground, your strength finally exhausted.",
                "enemyDefeated" => "{name} falls to the ground, defeated at last!",
                "playerTaunt" => "\"{enemy}, you're no match for me!\" {name} declares confidently.",
                "enemyTaunt" => "\"You cannot defeat me, {player}!\" {name} growls menacingly.",
                "intenseBattle" => "The battle reaches a fever pitch as both {player} and {enemy} stand bloodied but unbroken!",
                // Library taunts
                "playerTaunt_library" => "\"Shh! We're in a library!\" {name} whispers fiercely to {enemy}.",
                "enemyTaunt_library" => "\"Silence! This sacred place demands respect!\" {name} hisses at {player}.",
                // Underwater taunts
                "playerTaunt_underwater" => "*Bubbles escape {name}'s mouth as they gesture threateningly at {enemy}.*",
                "enemyTaunt_underwater" => "*{name} makes aggressive gestures, bubbles streaming from their mouth.*",
                // Lava taunts
                "playerTaunt_lava" => "\"The heat won't save you, {enemy}!\" {name} shouts over the roaring flames.",
                "enemyTaunt_lava" => "\"You'll burn before you defeat me, {player}!\" {name} roars through the inferno.",
                // Crypt taunts
                "playerTaunt_crypt" => "\"You belong here with the dead, {enemy}!\" {name} declares in the echoing tomb.",
                "enemyTaunt_crypt" => "\"Join the others in eternal rest, {player}!\" {name} intones ominously.",
                // Crystal taunts
                "playerTaunt_crystal" => "\"Your fate is crystal clear, {enemy}!\" {name} shouts, voice echoing off the gems.",
                "enemyTaunt_crystal" => "\"You'll shatter like glass, {player}!\" {name} bellows in the crystalline chamber.",
                // Temple taunts
                "playerTaunt_temple" => "\"The gods favor me, not you, {enemy}!\" {name} proclaims in the sacred hall.",
                "enemyTaunt_temple" => "\"Your blasphemy ends here, {player}!\" {name} thunders in the holy sanctuary.",
                // Forest taunts
                "playerTaunt_forest" => "\"The forest itself will aid me against you, {enemy}!\" {name} calls to the trees.",
                "enemyTaunt_forest" => "\"Nature's wrath will consume you, {player}!\" {name} growls among the ancient oaks.",
                _ => "A significant event occurs in the battle."
            };
        }

        /// <summary>
        /// Replaces placeholders in narrative text with actual values
        /// </summary>
        public string ReplacePlaceholders(string narrative, Dictionary<string, string> replacements)
        {
            string result = narrative;
            foreach (var kvp in replacements)
            {
                result = result.Replace("{" + kvp.Key + "}", kvp.Value);
            }
            return result;
        }
    }
}

