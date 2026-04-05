using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Custom JSON converter for SpreadsheetActionJson that omits empty string properties
    /// This significantly reduces file size by not writing redundant empty fields
    /// </summary>
    public class SpreadsheetActionJsonConverter : JsonConverter<SpreadsheetActionJson>
    {
        public override SpreadsheetActionJson Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Use default deserialization, then fill in missing properties with empty strings
            // This maintains backward compatibility with existing JSON files
            var jsonDocument = JsonDocument.ParseValue(ref reader);
            var root = jsonDocument.RootElement;
            
            var action = new SpreadsheetActionJson();
            
            // Read all properties, defaulting to empty string if missing
            action.Action = GetStringValue(root, "action");
            action.Description = GetStringValue(root, "description");
            action.ColumnC = GetStringValue(root, "columnC");
            action.Rarity = GetStringValue(root, "rarity");
            action.Category = GetStringValue(root, "category");
            action.DPS = GetStringValue(root, "dps");
            action.NumberOfHits = GetStringValue(root, "numberOfHits");
            action.Damage = GetStringValue(root, "damage");
            action.Speed = GetStringValue(root, "speed");
            action.Duration = GetStringValue(root, "duration");
            action.Cadence = GetStringValue(root, "cadence");
            action.Opener = GetStringValue(root, "opener");
            action.Finisher = GetStringValue(root, "finisher");
            action.HeroAccuracy = GetStringValue(root, "heroAccuracy");
            action.HeroHit = GetStringValue(root, "heroHit");
            action.HeroCombo = GetStringValue(root, "heroCombo");
            action.HeroCrit = GetStringValue(root, "heroCrit");
            action.HeroCritMiss = GetStringValue(root, "heroCritMiss");
            action.EnemyAccuracy = GetStringValue(root, "enemyAccuracy");
            action.EnemyHit = GetStringValue(root, "enemyHit");
            action.EnemyCombo = GetStringValue(root, "enemyCombo");
            action.EnemyCrit = GetStringValue(root, "enemyCrit");
            action.HeroSTR = GetStringValue(root, "heroSTR");
            action.HeroAGI = GetStringValue(root, "heroAGI");
            action.HeroTECH = GetStringValue(root, "heroTECH");
            action.HeroINT = GetStringValue(root, "heroINT");
            action.EnemySTR = GetStringValue(root, "enemySTR");
            action.EnemyAGI = GetStringValue(root, "enemyAGI");
            action.EnemyTECH = GetStringValue(root, "enemyTECH");
            action.EnemyINT = GetStringValue(root, "enemyINT");
            action.SpeedMod = GetStringValue(root, "speedMod");
            action.DamageMod = GetStringValue(root, "damageMod");
            action.MultiHitMod = GetStringValue(root, "multiHitMod");
            action.AmpMod = GetStringValue(root, "ampMod");
            action.Stun = GetStringValue(root, "stun");
            action.Poison = GetStringValue(root, "poison");
            action.Burn = GetStringValue(root, "burn");
            action.Bleed = GetStringValue(root, "bleed");
            action.Weaken = GetStringValue(root, "weaken");
            action.Expose = GetStringValue(root, "expose");
            action.Slow = GetStringValue(root, "slow");
            action.Vulnerability = GetStringValue(root, "vulnerability");
            action.Harden = GetStringValue(root, "harden");
            action.Silence = GetStringValue(root, "silence");
            action.Pierce = GetStringValue(root, "pierce");
            action.StatDrain = GetStringValue(root, "statDrain");
            action.Fortify = GetStringValue(root, "fortify");
            action.Consume = GetStringValue(root, "consume");
            action.Focus = GetStringValue(root, "focus");
            action.Cleanse = GetStringValue(root, "cleanse");
            action.Lifesteal = GetStringValue(root, "lifesteal");
            action.Reflect = GetStringValue(root, "reflect");
            action.SelfDamage = GetStringValue(root, "selfDamage");
            action.HeroHeal = GetStringValue(root, "heroHeal");
            action.HeroHealMaxHealth = GetStringValue(root, "heroHealMaxHealth");
            action.ReplaceNextRoll = GetStringValue(root, "replaceNextRoll");
            action.HighestLowestRoll = GetStringValue(root, "highestLowestRoll");
            action.DiceRolls = GetStringValue(root, "diceRolls");
            action.ExplodingDiceThreshold = GetStringValue(root, "explodingDiceThreshold");
            action.Curse = GetStringValue(root, "curse");
            action.Skip = GetStringValue(root, "skip");
            action.Jump = GetStringValue(root, "jump");
            action.Disrupt = GetStringValue(root, "disrupt");
            action.Grace = GetStringValue(root, "grace");
            action.LoopChain = GetStringValue(root, "loopChain");
            action.Shuffle = GetStringValue(root, "shuffle");
            action.ReplaceAction = GetStringValue(root, "replaceAction");
            action.ChainLength = GetStringValue(root, "chainLength");
            action.ChainPosition = GetStringValue(root, "chainPosition");
            action.ModifyBasedOnChainPosition = GetStringValue(root, "modifyBasedOnChainPosition");
            action.DistanceFromXSlot = GetStringValue(root, "distanceFromXSlot");
            action.OnHit = GetStringValue(root, "onHit");
            action.OnMiss = GetStringValue(root, "onMiss");
            action.OnCrit = GetStringValue(root, "onCrit");
            action.OnKill = GetStringValue(root, "onKill");
            action.OnRoomsCleared = GetStringValue(root, "onRoomsCleared");
            action.OnRollValue = GetStringValue(root, "onRollValue");
            action.Target = GetStringValue(root, "target");
            action.StatBonusesJson = GetStringValue(root, "statBonusesJson");
            action.ThresholdsJson = GetStringValue(root, "thresholdsJson");
            action.AccumulationsJson = GetStringValue(root, "accumulationsJson");
            action.ThresholdCategory = GetStringValue(root, "thresholdCategory");
            action.ThresholdAmount = GetStringValue(root, "thresholdAmount");
            action.Bonus = GetStringValue(root, "bonus");
            action.BonusAttribute = GetStringValue(root, "bonusAttribute");
            action.Value = GetStringValue(root, "value");
            action.Attribute = GetStringValue(root, "attribute");
            action.Reset = GetStringValue(root, "reset");
            action.ResetBlockerBuffer = GetStringValue(root, "resetBlockerBuffer");
            action.ModifyRoom = GetStringValue(root, "modifyRoom");
            action.Tags = GetStringValue(root, "tags");
            action.IsDefaultAction = GetStringValue(root, "isDefaultAction");
            action.WeaponTypes = GetStringValue(root, "weaponTypes");

            return action;
        }
        
        private static string GetStringValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString() ?? "";
            }
            return "";
        }
        
        public override void Write(Utf8JsonWriter writer, SpreadsheetActionJson value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            // Only write non-empty string properties
            WriteIfNotEmpty(writer, "action", value.Action);
            WriteIfNotEmpty(writer, "description", value.Description);
            WriteIfNotEmpty(writer, "columnC", value.ColumnC);
            WriteIfNotEmpty(writer, "rarity", value.Rarity);
            WriteIfNotEmpty(writer, "category", value.Category);
            WriteIfNotEmpty(writer, "dps", value.DPS);
            WriteIfNotEmpty(writer, "numberOfHits", value.NumberOfHits);
            WriteIfNotEmpty(writer, "damage", value.Damage);
            WriteIfNotEmpty(writer, "speed", value.Speed);
            WriteIfNotEmpty(writer, "duration", value.Duration);
            WriteIfNotEmpty(writer, "cadence", value.Cadence);
            WriteIfNotEmpty(writer, "opener", value.Opener);
            WriteIfNotEmpty(writer, "finisher", value.Finisher);
            WriteIfNotEmpty(writer, "heroAccuracy", value.HeroAccuracy);
            WriteIfNotEmpty(writer, "heroHit", value.HeroHit);
            WriteIfNotEmpty(writer, "heroCombo", value.HeroCombo);
            WriteIfNotEmpty(writer, "heroCrit", value.HeroCrit);
            WriteIfNotEmpty(writer, "heroCritMiss", value.HeroCritMiss);
            WriteIfNotEmpty(writer, "enemyAccuracy", value.EnemyAccuracy);
            WriteIfNotEmpty(writer, "enemyHit", value.EnemyHit);
            WriteIfNotEmpty(writer, "enemyCombo", value.EnemyCombo);
            WriteIfNotEmpty(writer, "enemyCrit", value.EnemyCrit);
            WriteIfNotEmpty(writer, "heroSTR", value.HeroSTR);
            WriteIfNotEmpty(writer, "heroAGI", value.HeroAGI);
            WriteIfNotEmpty(writer, "heroTECH", value.HeroTECH);
            WriteIfNotEmpty(writer, "heroINT", value.HeroINT);
            WriteIfNotEmpty(writer, "enemySTR", value.EnemySTR);
            WriteIfNotEmpty(writer, "enemyAGI", value.EnemyAGI);
            WriteIfNotEmpty(writer, "enemyTECH", value.EnemyTECH);
            WriteIfNotEmpty(writer, "enemyINT", value.EnemyINT);
            WriteIfNotEmpty(writer, "speedMod", value.SpeedMod);
            WriteIfNotEmpty(writer, "damageMod", value.DamageMod);
            WriteIfNotEmpty(writer, "multiHitMod", value.MultiHitMod);
            WriteIfNotEmpty(writer, "ampMod", value.AmpMod);
            WriteIfNotEmpty(writer, "stun", value.Stun);
            WriteIfNotEmpty(writer, "poison", value.Poison);
            WriteIfNotEmpty(writer, "burn", value.Burn);
            WriteIfNotEmpty(writer, "bleed", value.Bleed);
            WriteIfNotEmpty(writer, "weaken", value.Weaken);
            WriteIfNotEmpty(writer, "expose", value.Expose);
            WriteIfNotEmpty(writer, "slow", value.Slow);
            WriteIfNotEmpty(writer, "vulnerability", value.Vulnerability);
            WriteIfNotEmpty(writer, "harden", value.Harden);
            WriteIfNotEmpty(writer, "silence", value.Silence);
            WriteIfNotEmpty(writer, "pierce", value.Pierce);
            WriteIfNotEmpty(writer, "statDrain", value.StatDrain);
            WriteIfNotEmpty(writer, "fortify", value.Fortify);
            WriteIfNotEmpty(writer, "consume", value.Consume);
            WriteIfNotEmpty(writer, "focus", value.Focus);
            WriteIfNotEmpty(writer, "cleanse", value.Cleanse);
            WriteIfNotEmpty(writer, "lifesteal", value.Lifesteal);
            WriteIfNotEmpty(writer, "reflect", value.Reflect);
            WriteIfNotEmpty(writer, "selfDamage", value.SelfDamage);
            WriteIfNotEmpty(writer, "heroHeal", value.HeroHeal);
            WriteIfNotEmpty(writer, "heroHealMaxHealth", value.HeroHealMaxHealth);
            WriteIfNotEmpty(writer, "replaceNextRoll", value.ReplaceNextRoll);
            WriteIfNotEmpty(writer, "highestLowestRoll", value.HighestLowestRoll);
            WriteIfNotEmpty(writer, "diceRolls", value.DiceRolls);
            WriteIfNotEmpty(writer, "explodingDiceThreshold", value.ExplodingDiceThreshold);
            WriteIfNotEmpty(writer, "curse", value.Curse);
            WriteIfNotEmpty(writer, "skip", value.Skip);
            WriteIfNotEmpty(writer, "jump", value.Jump);
            WriteIfNotEmpty(writer, "disrupt", value.Disrupt);
            WriteIfNotEmpty(writer, "grace", value.Grace);
            WriteIfNotEmpty(writer, "loopChain", value.LoopChain);
            WriteIfNotEmpty(writer, "shuffle", value.Shuffle);
            WriteIfNotEmpty(writer, "replaceAction", value.ReplaceAction);
            WriteIfNotEmpty(writer, "chainLength", value.ChainLength);
            WriteIfNotEmpty(writer, "chainPosition", value.ChainPosition);
            WriteIfNotEmpty(writer, "modifyBasedOnChainPosition", value.ModifyBasedOnChainPosition);
            WriteIfNotEmpty(writer, "distanceFromXSlot", value.DistanceFromXSlot);
            WriteIfNotEmpty(writer, "onHit", value.OnHit);
            WriteIfNotEmpty(writer, "onMiss", value.OnMiss);
            WriteIfNotEmpty(writer, "onCrit", value.OnCrit);
            WriteIfNotEmpty(writer, "onKill", value.OnKill);
            WriteIfNotEmpty(writer, "onRoomsCleared", value.OnRoomsCleared);
            WriteIfNotEmpty(writer, "onRollValue", value.OnRollValue);
            WriteIfNotEmpty(writer, "target", value.Target);
            WriteIfNotEmpty(writer, "statBonusesJson", value.StatBonusesJson);
            WriteIfNotEmpty(writer, "thresholdsJson", value.ThresholdsJson);
            WriteIfNotEmpty(writer, "accumulationsJson", value.AccumulationsJson);
            WriteIfNotEmpty(writer, "thresholdCategory", value.ThresholdCategory);
            WriteIfNotEmpty(writer, "thresholdAmount", value.ThresholdAmount);
            WriteIfNotEmpty(writer, "bonus", value.Bonus);
            WriteIfNotEmpty(writer, "bonusAttribute", value.BonusAttribute);
            WriteIfNotEmpty(writer, "value", value.Value);
            WriteIfNotEmpty(writer, "attribute", value.Attribute);
            WriteIfNotEmpty(writer, "reset", value.Reset);
            WriteIfNotEmpty(writer, "resetBlockerBuffer", value.ResetBlockerBuffer);
            WriteIfNotEmpty(writer, "modifyRoom", value.ModifyRoom);
            WriteIfNotEmpty(writer, "tags", value.Tags);
            WriteIfNotEmpty(writer, "isDefaultAction", value.IsDefaultAction);
            WriteIfNotEmpty(writer, "weaponTypes", value.WeaponTypes);

            writer.WriteEndObject();
        }
        
        private static void WriteIfNotEmpty(Utf8JsonWriter writer, string propertyName, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteString(propertyName, value);
            }
        }
    }
}
