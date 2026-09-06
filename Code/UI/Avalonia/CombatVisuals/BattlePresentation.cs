using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.CombatVisuals;

public sealed record BattleStatus(string Key, string Text, string Color);
public sealed record BattlePresentation(string HeroAsset, string Gear, string Rank, string Arena,
    string EnemyRole, string[] Advice, BattleStatus[] HeroStatuses, BattleStatus[] EnemyStatuses, string Combo, string EquipmentRarity = "Common")
{
    public static BattlePresentation Capture(Character hero, Enemy enemy)
    {
        string weapon = hero.Weapon?.WeaponType.ToString().ToLowerInvariant() ?? "unarmed";
        string armor = hero.Body is ChestItem chest && chest.GetTotalArmor() > 0 ? "plate" : "cloth";
        string gear = $"LV {hero.Level} · {hero.Weapon?.Name ?? "Unarmed"} (T{hero.Weapon?.Tier ?? 0}) · {hero.Body?.Name ?? "No body armor"}";
        var advice = new List<string>();
        var combo = hero.GetComboActions();
        if (hero.CurrentHealth < hero.GetEffectiveMaxHealth() * .35) advice.Add("Low health: review healing and defensive combo slots.");
        if (combo.Count == 1) advice.Add("One-action build: as slots unlock, pair damage with control or protection.");
        if (enemy.Armor > 0) advice.Add($"Armor {enemy.Armor}: compare damage per hit, speed and armor-breaking effects.");
        if (!enemy.IsLiving) advice.Add("Nonliving: check effect immunities before relying on poison or bleed.");
        if (enemy.AttackSpeed < hero.GetTotalAttackSpeed()) advice.Add("Enemy acts faster: protection and control deserve priority.");
        advice.Insert(0, BuildIdentity.Describe(hero));
        int next = combo.Count == 0 ? 0 : Math.Max(0, hero.ComboStep) % combo.Count;
        string order = string.Join(" > ", combo.Take(4).Select((a, i) => i == next ? "[" + a.Name + "]" : a.Name));
        if (combo.Count > 4) order += $" > +{combo.Count - 4} slots";
        return new($"fighter-{weapon}-{armor}", gear, enemy.Rarity,
            EnemyVisualCatalog.Arena(enemy.Name), enemy.Archetype.ToString(), advice.ToArray(),
            Statuses(hero), Statuses(enemy), order, hero.Weapon?.Rarity ?? "Common");
    }

    public static string ArenaFor(string? family) => family switch
    {
        "firehound" or "lavagolem" or "flame" or "lizard" or "crab" or "tortoise" => "volcanic",
        "wolf" or "bear" or "boar" or "treant" or "vine" or "sprite" or "spider" => "forest",
        "snake" or "scorpion" or "beetle" or "hunter" => "desert",
        "fish" or "leech" or "toad" or "swarm" => "wetland",
        "golem" or "bird" => "mountain",
        "bat" or "spore" or "rat" => "cavern",
        _ => "crypt"
    };

    public static BattleStatus[] Statuses(Actor actor)
    {
        var result = new List<BattleStatus>();
        void Add(bool active, string key, string text, string color) { if (active) result.Add(new(key, text, color)); }
        Add(actor.PoisonPercentOfMaxHealth > 0, "poison", $"POISON {actor.PoisonPercentOfMaxHealth:0.#}%", "#BDE34B");
        Add(actor.BurnIntensity > 0, "burn", $"BURN {actor.BurnIntensity}", "#FF873D");
        Add(actor.IsBleeding, "bleed", $"BLEED {actor.BleedIntensity + actor.PendingBleedFromHits}", "#FF3455");
        Add(actor.AcidIntensity > 0, "acid", $"ACID {actor.AcidIntensity}", "#CAEF4C");
        Add(actor.IsStunned, "stun", $"STUN {actor.StunTurnsRemaining}t", "#EEE4A3");
        Add(actor.FortifyTurns > 0, "shield", $"FORTIFY {actor.FortifyTurns}t", "#8CBFE1");
        Add(actor.TemporaryHP > 0, "shield", $"SHIELD {actor.TemporaryHP} · {actor.TemporaryHPTurns}t", "#8CBFE1");
        Add(actor.ArmorBreakTurns > 0, "break", $"ARMOR BREAK {actor.ArmorBreakTurns}t", "#F5BA73");
        Add(actor.IsWeakened, "weaken", $"WEAKEN {actor.WeakenTurns}t", "#C2A6D8");
        Add(actor.IsSilenced, "silence", $"SILENCE {actor.SilenceTurns}t", "#C2A6D8");
        Add(actor.IsMarked, "mark", $"MARK {actor.MarkTurns}t", "#FF3455");
        Add(actor.ReflectTurns > 0, "reflect", $"REFLECT {actor.ReflectTurns}t", "#8CBFE1");
        Add(actor.FocusTurns > 0, "focus", $"FOCUS {actor.FocusTurns}t", "#EEE4A3");
        return result.ToArray();
    }
}

