namespace RPGGame.Actions.Parsing
{
    /// <summary>
    /// Parses action descriptions to extract advanced mechanics properties
    /// Uses strategy pattern for different action types
    /// </summary>
    public static class ActionDescriptionParser
    {
        /// <summary>
        /// Parses action description and name to populate advanced mechanics properties
        /// </summary>
        public static void ParseDescriptionForProperties(Action action)
        {
            if (string.IsNullOrEmpty(action.Description)) return;

            string desc = action.Description.ToLower();
            string actionName = action.Name.ToUpper();

            // Multi-hit attacks (Cleave)
            if (actionName == "CLEAVE" && desc.Contains("3x35%"))
            {
                action.Advanced.MultiHitCount = 3;
            }

            // Self-damage (Deal with the Devil)
            if (actionName == "DEAL WITH THE DEVIL" && desc.Contains("5% damage to yourself"))
            {
                action.Advanced.SelfDamagePercent = 5;
            }

            // Roll bonuses/penalties
            if (actionName == "LUCKY STRIKE" && desc.Contains("+1 to next roll"))
            {
                action.Advanced.RollBonus = 1;
            }
            else if (actionName == "LAST GRASP" && desc.Contains("+10 to roll"))
            {
                action.Advanced.RollBonus = 10;
            }
            else if (actionName == "DRUNKEN BRAWLER" && desc.Contains("-5 to your next roll"))
            {
                action.Advanced.RollBonus = -5;
            }

            // Stat bonuses
            if (desc.Contains("gain 1 str"))
            {
                action.Advanced.StatBonus = 1;
                action.Advanced.StatBonusType = "STR";
                action.Advanced.StatBonusDuration = 999; // Duration of dungeon
            }

            // Turn skipping (True Strike)
            if (actionName == "TRUE STRIKE" && desc.Contains("skip turn"))
            {
                action.Advanced.SkipNextTurn = true;
                action.Advanced.GuaranteeNextSuccess = true;
            }

            // Healing (Second Wind)
            if (actionName == "SECOND WIND" && desc.Contains("heal for 5 health"))
            {
                action.Advanced.HealAmount = 5;
            }

            // Health thresholds
            if (desc.Contains("below 25% health") || desc.Contains("health is below 25%"))
            {
                action.Advanced.HealthThreshold = 0.25;
            }
            else if (desc.Contains("below 5%") || desc.Contains("health is below 5%"))
            {
                action.Advanced.HealthThreshold = 0.05;
            }
            else if (desc.Contains("full health") || desc.Contains("at full health"))
            {
                action.Advanced.HealthThreshold = 1.0;
            }
            else if (desc.Contains("1 health") || desc.Contains("at 1 health"))
            {
                action.Advanced.HealthThreshold = 0.01;
            }

            // Stat thresholds
            if (desc.Contains("str ≥ 10"))
            {
                action.Advanced.StatThreshold = 10.0;
                action.Advanced.StatThresholdType = "STR";
            }

            // Conditional damage multipliers
            if (desc.Contains("double damage"))
            {
                action.Advanced.ConditionalDamageMultiplier = 2.0;
            }
            else if (desc.Contains("quadrable damage"))
            {
                action.Advanced.ConditionalDamageMultiplier = 4.0;
            }
            else if (desc.Contains("add 50% damage"))
            {
                action.Advanced.ConditionalDamageMultiplier = 1.5;
            }

            // Action repetition (Deja Vu)
            if (actionName == "DEJA VU" && desc.Contains("repeat the previous action"))
            {
                action.Advanced.RepeatLastAction = true;
            }

            // Extra attacks
            if (desc.Contains("add 1 attack to next action") || desc.Contains("+1 attack to next action"))
            {
                action.Advanced.ExtraAttacks = 1;
            }

            // Combo amplifier multiplier (Pretty Boy Swag)
            if (actionName == "PRETTY BOY SWAG" && desc.Contains("double combo amp"))
            {
                action.Advanced.ComboAmplifierMultiplier = 2.0;
            }

            // Enemy roll penalties
            if (actionName == "QUICK REFLEXES" && desc.Contains("-5 to next enemies roll"))
            {
                action.Advanced.EnemyRollPenalty = 5;
            }
            else if (actionName == "DRUNKEN BRAWLER" && desc.Contains("-5 to enemies next roll"))
            {
                action.Advanced.EnemyRollPenalty = 5;
            }

            // Extra damage (Opening Volley)
            if (actionName == "OPENING VOLLEY" && desc.Contains("10 extra damage"))
            {
                action.Advanced.ExtraDamage = 10;
                action.Advanced.ExtraDamageDecay = 1;
            }

            // Damage reduction (Sharp Edge)
            if (actionName == "SHARP EDGE" && desc.Contains("reduce damage by 50%"))
            {
                action.Advanced.DamageReduction = 0.5;
                action.Advanced.DamageReductionDecay = 1;
            }

            // Self-attack chance (Swing for the Fences)
            if (actionName == "SWING FOR THE FENCES" && desc.Contains("50% chance to attack yourself"))
            {
                action.Advanced.SelfAttackChance = 0.5;
            }

            // Enemy combo reset (Jab)
            if (actionName == "JAB" && desc.Contains("reset enemy combo"))
            {
                action.Advanced.ResetEnemyCombo = true;
            }

            // Stun effects (Stun)
            if (actionName == "STUN" && desc.Contains("stuns the enemy"))
            {
                action.Advanced.StunEnemy = true;
                action.Advanced.StunDuration = 5;
            }

            // Length reduction (Taunt)
            if (actionName == "TAUNT" && desc.Contains("50% length for next 2 actions"))
            {
                action.Advanced.ReduceLengthNextActions = true;
                action.Advanced.LengthReduction = 0.5;
                action.Advanced.LengthReductionDuration = 2;
            }
        }
    }
}

