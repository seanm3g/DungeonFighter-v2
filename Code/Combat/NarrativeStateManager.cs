using System;

namespace RPGGame
{
    /// <summary>
    /// Manages all state flags and counters for battle narrative tracking.
    /// Encapsulates the state management logic previously scattered throughout BattleNarrative.
    /// </summary>
    public class NarrativeStateManager
    {
        // One-time narrative events (occur once per battle)
        private bool firstBloodOccurred;
        private bool goodComboOccurred;
        private bool intenseBattleTriggered;
        private bool environmentalActionOccurred;
        private bool playerDefeated;
        private bool enemyDefeated;

        // Health threshold events (per entity)
        private bool playerBelow50Percent;
        private bool enemyBelow50Percent;
        private bool playerBelow10Percent;
        private bool enemyBelow10Percent;

        // Health lead tracking
        private bool playerHadHealthLead;
        private bool enemyHadHealthLead;

        // Action and taunt counters
        private int playerActionCount;
        private int enemyActionCount;
        private int playerTauntCount;
        private int enemyTauntCount;

        // Narrative event counter
        private int narrativeEventCount;

        public NarrativeStateManager()
        {
            ResetAllStates();
        }

        /// <summary>
        /// Resets all narrative states for a new battle
        /// </summary>
        public void ResetAllStates()
        {
            firstBloodOccurred = false;
            goodComboOccurred = false;
            intenseBattleTriggered = false;
            environmentalActionOccurred = false;
            playerDefeated = false;
            enemyDefeated = false;

            playerBelow50Percent = false;
            enemyBelow50Percent = false;
            playerBelow10Percent = false;
            enemyBelow10Percent = false;

            playerHadHealthLead = false;
            enemyHadHealthLead = false;

            playerActionCount = 0;
            enemyActionCount = 0;
            playerTauntCount = 0;
            enemyTauntCount = 0;

            narrativeEventCount = 0;
        }

        // ===== First Blood State =====
        public bool HasFirstBloodOccurred => firstBloodOccurred;
        public void SetFirstBloodOccurred() => firstBloodOccurred = true;

        // ===== Good Combo State =====
        public bool HasGoodComboOccurred => goodComboOccurred;
        public void SetGoodComboOccurred() => goodComboOccurred = true;

        // ===== Intense Battle State =====
        public bool HasIntenseBattleTriggered => intenseBattleTriggered;
        public void SetIntenseBattleTriggered() => intenseBattleTriggered = true;

        // ===== Environmental Action State =====
        public bool HasEnvironmentalActionOccurred => environmentalActionOccurred;
        public void SetEnvironmentalActionOccurred() => environmentalActionOccurred = true;

        // ===== Defeat States =====
        public bool HasPlayerDefeated => playerDefeated;
        public void SetPlayerDefeated() => playerDefeated = true;

        public bool HasEnemyDefeated => enemyDefeated;
        public void SetEnemyDefeated() => enemyDefeated = true;

        // ===== Health Threshold States (50%) =====
        public bool HasPlayerBelow50Percent => playerBelow50Percent;
        public void SetPlayerBelow50Percent() => playerBelow50Percent = true;

        public bool HasEnemyBelow50Percent => enemyBelow50Percent;
        public void SetEnemyBelow50Percent() => enemyBelow50Percent = true;

        // ===== Health Threshold States (10%) =====
        public bool HasPlayerBelow10Percent => playerBelow10Percent;
        public void SetPlayerBelow10Percent() => playerBelow10Percent = true;

        public bool HasEnemyBelow10Percent => enemyBelow10Percent;
        public void SetEnemyBelow10Percent() => enemyBelow10Percent = true;

        // ===== Health Lead States =====
        public bool HasPlayerHealthLead => playerHadHealthLead;
        public void SetPlayerHealthLead()
        {
            playerHadHealthLead = true;
            enemyHadHealthLead = false;
        }

        public bool HasEnemyHealthLead => enemyHadHealthLead;
        public void SetEnemyHealthLead()
        {
            enemyHadHealthLead = true;
            playerHadHealthLead = false;
        }

        // ===== Action Counters =====
        public int PlayerActionCount => playerActionCount;
        public void IncrementPlayerActionCount() => playerActionCount++;

        public int EnemyActionCount => enemyActionCount;
        public void IncrementEnemyActionCount() => enemyActionCount++;

        // ===== Taunt Counters =====
        public int PlayerTauntCount => playerTauntCount;
        public void IncrementPlayerTauntCount() => playerTauntCount++;
        public bool CanPlayerTaunt => playerTauntCount < 2;

        public int EnemyTauntCount => enemyTauntCount;
        public void IncrementEnemyTauntCount() => enemyTauntCount++;
        public bool CanEnemyTaunt => enemyTauntCount < 2;

        // ===== Narrative Event Counter =====
        public int NarrativeEventCount => narrativeEventCount;
        public void IncrementNarrativeEventCount() => narrativeEventCount++;
    }
}

