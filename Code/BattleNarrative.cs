using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public class BattleEvent
    {
        public string Actor { get; set; } = "";
        public string Target { get; set; } = "";
        public string Action { get; set; } = "";
        public int Damage { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsCombo { get; set; }
        public int ComboStep { get; set; }
        public bool CausesBleed { get; set; }
        public bool CausesWeaken { get; set; }
        public bool IsHeal { get; set; }
        public int HealAmount { get; set; }
        public double ComboAmplifier { get; set; }
        public string EnvironmentEffect { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public int ActorHealthBefore { get; set; }
        public int TargetHealthBefore { get; set; }
        public int ActorHealthAfter { get; set; }
        public int TargetHealthAfter { get; set; }
        public int Roll { get; set; }
        public int Difficulty { get; set; }

        public BattleEvent()
        {
            Timestamp = DateTime.Now;
        }
    }

    public class BattleNarrative
    {
        private List<BattleEvent> events;
        private string playerName;
        private string enemyName;
        private string environmentName;
        private int initialPlayerHealth;
        private int initialEnemyHealth;
        private int finalPlayerHealth;
        private int finalEnemyHealth;
        private bool battleEnded;
        private List<string> narrativeEvents;
        private bool firstBloodOccurred;
        private bool healthReversalOccurred;
        private bool nearDeathOccurred;
        private bool goodComboOccurred;

        public BattleNarrative(string playerName, string enemyName, string environmentName = "", int playerHealth = 0, int enemyHealth = 0)
        {
            this.playerName = playerName;
            this.enemyName = enemyName;
            this.environmentName = environmentName;
            this.initialPlayerHealth = playerHealth;
            this.initialEnemyHealth = enemyHealth;
            this.finalPlayerHealth = playerHealth;
            this.finalEnemyHealth = enemyHealth;
            this.events = new List<BattleEvent>();
            this.narrativeEvents = new List<string>();
            this.battleEnded = false;
            this.firstBloodOccurred = false;
            this.healthReversalOccurred = false;
            this.nearDeathOccurred = false;
            this.goodComboOccurred = false;
        }

        public void AddEvent(BattleEvent evt)
        {
            events.Add(evt);
            
            // Update final health based on damage/healing
            if (evt.Actor == playerName && evt.Target == enemyName && evt.Damage > 0)
            {
                finalEnemyHealth = Math.Max(0, finalEnemyHealth - evt.Damage);
            }
            else if (evt.Actor == enemyName && evt.Target == playerName && evt.Damage > 0)
            {
                finalPlayerHealth = Math.Max(0, finalPlayerHealth - evt.Damage);
            }
            else if (evt.IsHeal && evt.Target == playerName)
            {
                finalPlayerHealth += evt.HealAmount;
            }

            // Check for significant events that trigger narrative
            CheckForSignificantEvents(evt);
        }

        private void CheckForSignificantEvents(BattleEvent evt)
        {
            // First Blood - first significant damage dealt
            if (!firstBloodOccurred && evt.Damage > 10)
            {
                firstBloodOccurred = true;
                if (evt.Actor == playerName)
                {
                    narrativeEvents.Add($"{playerName} draws first blood with a {evt.Action} that deals {evt.Damage} damage to {enemyName}!");
                }
                else
                {
                    narrativeEvents.Add($"{enemyName} strikes first, their {evt.Action} dealing {evt.Damage} damage to {playerName}!");
                }
            }

            // Health Reversal - when someone goes from higher to lower health than opponent
            if (!healthReversalOccurred)
            {
                var playerHealthPercentage = (double)finalPlayerHealth / initialPlayerHealth;
                var enemyHealthPercentage = (double)finalEnemyHealth / initialEnemyHealth;
                
                // Check if there was a reversal in health advantage
                if (evt.Actor == playerName && evt.Damage > 0)
                {
                    if (playerHealthPercentage > enemyHealthPercentage && finalPlayerHealth < finalEnemyHealth)
                    {
                        healthReversalOccurred = true;
                        narrativeEvents.Add($"{playerName} turns the tide! Their relentless assault has brought {enemyName} to their knees!");
                    }
                }
                else if (evt.Actor == enemyName && evt.Damage > 0)
                {
                    if (enemyHealthPercentage > playerHealthPercentage && finalEnemyHealth < finalPlayerHealth)
                    {
                        healthReversalOccurred = true;
                        narrativeEvents.Add($"{enemyName} seizes control! Their brutal counterattack has {playerName} reeling!");
                    }
                }
            }

            // Near Death - when someone gets below 20% health
            if (!nearDeathOccurred)
            {
                var playerHealthPercentage = (double)finalPlayerHealth / initialPlayerHealth;
                var enemyHealthPercentage = (double)finalEnemyHealth / initialEnemyHealth;
                
                if (playerHealthPercentage < 0.2 && finalPlayerHealth > 0)
                {
                    nearDeathOccurred = true;
                    narrativeEvents.Add($"{playerName} staggers, bloodied and battered, but their spirit refuses to break!");
                }
                else if (enemyHealthPercentage < 0.2 && finalEnemyHealth > 0)
                {
                    nearDeathOccurred = true;
                    narrativeEvents.Add($"{enemyName} stumbles, their malice faltering as death's shadow looms!");
                }
            }

            // Good Combo - when someone gets a 3+ step combo
            if (!goodComboOccurred && evt.IsCombo && evt.ComboStep >= 2)
            {
                goodComboOccurred = true;
                if (evt.Actor == playerName)
                {
                    narrativeEvents.Add($"{playerName} unleashes a devastating combo sequence! Each strike flows into the next with deadly precision!");
                }
                else
                {
                    narrativeEvents.Add($"{enemyName} demonstrates masterful technique with a brutal combo that leaves {playerName} reeling!");
                }
            }
        }

        public void EndBattle()
        {
            battleEnded = true;
        }

        public string GenerateNarrative()
        {
            if (!battleEnded)
            {
                return "Battle is still ongoing...";
            }

            var settings = GameSettings.Instance;
            var playerEvents = events.Where(e => e.Actor == playerName && e.IsSuccess).ToList();
            var enemyEvents = events.Where(e => e.Actor == enemyName && e.IsSuccess).ToList();
            var totalPlayerDamage = playerEvents.Sum(e => e.Damage);
            var totalEnemyDamage = enemyEvents.Sum(e => e.Damage);
            var playerComboCount = playerEvents.Count(e => e.IsCombo);
            var enemyComboCount = enemyEvents.Count(e => e.IsCombo);

            if (!settings.EnableNarrativeEvents)
            {
                // Return only informational summary
                return GenerateInformationalSummary(totalPlayerDamage, totalEnemyDamage, playerComboCount, enemyComboCount);
            }

            string summary = GenerateInformationalSummary(totalPlayerDamage, totalEnemyDamage, playerComboCount, enemyComboCount);
            
            // Add narrative events based on balance setting
            if (narrativeEvents.Any())
            {
                // Apply narrative balance setting
                if (settings.NarrativeBalance >= 0.5)
                {
                    // More narrative-focused
                    summary += "\n\n" + string.Join("\n", narrativeEvents);
                }
                else
                {
                    // More event-focused - only show significant events
                    var significantEvents = narrativeEvents.Where((_, index) => index < narrativeEvents.Count / 2).ToList();
                    if (significantEvents.Any())
                    {
                        summary += "\n\n" + string.Join("\n", significantEvents);
                    }
                }
            }

            return summary;
        }

        private string GenerateInformationalSummary(int totalPlayerDamage, int totalEnemyDamage, int playerComboCount, int enemyComboCount)
        {
            bool playerWon = finalEnemyHealth <= 0;
            bool enemyWon = finalPlayerHealth <= 0;

            if (playerWon)
            {
                return $"{playerName} defeats {enemyName}! Total damage dealt: {totalPlayerDamage} vs {totalEnemyDamage} received. " +
                       $"Combos executed: {playerComboCount} vs {enemyComboCount}.";
            }
            else if (enemyWon)
            {
                return $"{enemyName} defeats {playerName}! Total damage dealt: {totalEnemyDamage} vs {totalPlayerDamage} received. " +
                       $"Combos executed: {enemyComboCount} vs {playerComboCount}.";
            }
            else
            {
                return $"Battle ends in a stalemate. {playerName} dealt {totalPlayerDamage} damage, {enemyName} dealt {totalEnemyDamage} damage. " +
                       $"Combos: {playerComboCount} vs {enemyComboCount}.";
            }
        }
    }
} 