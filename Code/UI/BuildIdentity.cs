using System.Collections.Generic;
using System.Linq;
namespace RPGGame;

/// <summary>Explains mechanics present in the equipped combo; never grants hidden bonuses.</summary>
public static class BuildIdentity
{
    public static string Describe(Character hero)
    {
        var actions = hero.GetComboActions();
        var proc = hero.Weapon?.Modifications.FirstOrDefault(m => m.RolledValue > 0 &&
            m.Effect is "weaponBleed" or "weaponPoison" or "weaponBurn" or "weaponAcid");
        if (proc != null)
            return $"{proc.Effect.Replace("weapon", "").ToUpperInvariant()} WEAPON: {proc.RolledValue:0.#} on {(string.IsNullOrWhiteSpace(proc.TriggerWhen) ? "critical hits" : proc.TriggerWhen)}; pair with protection and check immunity.";
        if (actions.Any(a => a.CausesArmorBreak) && actions.Any(a => a.DamageMultiplier > 1))
            return "BREAKER: apply armor break before your heavy strikes; check effect duration.";
        if (actions.Any(a => a.CausesBleed))
            return "BLEED: stacks punish enemy actions; watch their intensity and immunity.";
        if (actions.Any(a => a.CausesTemporaryHP || a.CausesHarden || a.CausesAbsorb || a.CausesFortify))
            return "DEFENDER: protection buys time for your damage actions; compare uptime.";
        if (actions.Any(a => a.CausesWeaken || a.CausesSlow || a.CausesStun))
            return "CONTROL: weaken or delay enemy actions, then follow with damage.";
        if (actions.Any(a => a.CausesPoison || a.CausesBurn))
            return "ATTRITION: damage over time rewards surviving; pair it with protection.";
        if (actions.Any(a => (a.Advanced?.MultiHitCount ?? 1) > 1))
            return "FLURRY: compare damage per hit against armor, not just total damage.";
        return "BUILD: pair protection or control with your strongest attack.";
    }
}

