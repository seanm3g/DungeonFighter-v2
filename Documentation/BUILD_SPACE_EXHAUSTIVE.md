# Build Space — Exhaustive (pre-condense)

Status: **brainstorm, divergent pass** (2026-11-11). Every action, item, skill and combo below is hypothetical unless
marked `(exists)`. Nothing here is balanced or implemented. The next pass collapses this list into a small set of
shared primitives (see the end of the doc for the seeds of that pass).

How to read a build entry:

- **Fantasy**: what the player feels they are doing.
- **Actions**: things in the strip (active, cost a slot and time, visible every turn).
- **Items**: things worn (passive, loot-rolled, swappable, counted by sets).
- **Skills / Class**: things learned (permanent per character, ranked, can rewrite rules).
- **Combo**: one concrete strip, in order, with why the order matters.
- **Fun / Risk**: the decision the build asks for and what makes it fail.

Mechanic ids (`M01`…) are used by the pair section.

---

## Part 0 — What each category is *for* (affordances)

The point of having four systems is that each can say something the others can't. If a piece could live in any
category, it probably belongs in the one whose affordance it uses most. Below is what each category literally is.

### Actions = verbs

| Affordance | What it means for design |
|---|---|
| Active, chosen | The player put it in the strip, so it can carry a cost (a slot, time, HP, a debuff on self). |
| Positional | It has a slot index, a neighbour before and after, and can be opener / middle / finisher. Only actions can care about *where* they are. |
| Timed | Action length / speed. Only actions spend time, so only actions can trade time for power. |
| Per-hit | Multihit, per-hit triggers. Actions are the only source of "how many times did X happen". |
| Visible and readable | The log narrates them. Big, rare, dramatic effects belong here. |
| Consumable state | Actions are where stored stuff gets *spent* (mints, marks, banked rolls, debt). |
| Scarce | The strip has few slots, so every action competes with every other. That is the main build tension. |

Literal: **an action is a sentence's verb**: it does one thing, at one point in time, and can spend what others built.

### Items = nouns and conditions

| Affordance | What it means for design |
|---|---|
| Passive, always on | "While equipped" rules. Good for conditions ("while Harden is up…") rather than events. |
| Slot-bound | Head / Body / Legs / Feet / Weapon: each slot can own a kind of effect (weapons → hit shape, feet → speed, body → defense). |
| Counted | Material / set / tag counts. Only items naturally stack into "how many do you wear" thresholds (2/3/5). |
| Rolled | Affixes, rarity, condition (Broken → Perfect). Variance and loot excitement live here; the same build differs run to run. |
| Swappable mid-run | Items are the cheapest way to pivot. Good for "situational" effects (anti-Undead, anti-Burning). |
| Can grant | An item can add an action to the pool or a tag to all actions: the bridge into the strip. |
| Has a body | Weight, condition, durability: items can degrade, break, be sacrificed, be fed. |

Literal: **an item is an adjective on the hero**: it changes what the hero *is* while worn, and answers "how many".

### Skills / Class = grammar rules

| Affordance | What it means for design |
|---|---|
| Permanent | Survives gear swaps; it's the run's commitment. |
| Ranked | Scales smoothly; the right place for "+X per rank" numbers. |
| Rule-changing | "Harden counts as Bronze", "repeating a tag cashes debt": only skills should change how *other* systems are read. |
| Gated by class points | Identity: a hybrid has to pay to reach both sides. |
| Stat-coupled | "Every 20 INT adds…": the place where primary stats turn into mechanics. |
| Unlocks | Can unlock actions (`unlockActionName`) and loot bias (Blood Market). |

Literal: **a skill is a rule of grammar**: it changes what sentences (combos) mean, not one word.

### Combo / Strip = sentences

| Affordance | What it means for design |
|---|---|
| Order | A→B differs from B→A. Setup before payoff. |
| Adjacency | "The next action", "the previous action". |
| Length | Long strips (Wizard) vs short loops. Threshold to continue. |
| Repetition | Loops, retriggers, repeated tags. |
| Breakage | A miss ends or resets the chain, so the strip has a failure state to play around. |
| Structure ops | Jump / skip / loop / shuffle / replace: the strip can rewrite itself. |

Literal: **a combo is a sentence**: the build's meaning comes from the order of the words.

### The glue and the context

- **Triggers (WHEN)**: ONCONNECT, ONCRITICAL, ONFIRSTHIT, ONCOMBO, ONMISS, ONTAKEHIT, ONAFTERMISS, ONCOMBOEND,
  ONKILL, ONROOMSCLEARED, ONCRITICALMISS, ONEVEN/ONODD, ONNATURALROLL:n, ONROLLVALUE:n, WHILE_EQUIPPED. Any category can
  own a trigger; *which* category owns it decides whether it's chosen (action), worn (item) or permanent (skill).
- **Scopes**: ACTION / TURN / FIGHT / DUNGEON: how long a payoff lasts. Builds differ a lot by which scope they
  invest in (burst vs snowball).
- **Mints / currencies** (exist): RAGE, CRISIS, TRIUMPH, BEAT, DRAG, STREAK, GRAZE, SWING, TROPHY, CHAIN, LENS, CONTACT.
  Generic shape: *event mints a token → token feeds a convert action*.
- **Context**: enemy prefixes (Undead, Giant… tag-only today), room hazards (EnvironmentalActions), regions (Air band
  harder), dungeon length (room-clear triggers).

---

## Part 1 — Mechanic list

| Id | Mechanic | Family |
|---|---|---|
| M01 | Heavy Hit (raw damage, STR, big slow swings) | Output |
| M02 | Tempo (speed, action length, extra turns) | Output |
| M03 | Multihit (hit count, per-hit procs) | Output |
| M04 | AMP (combo amplification, later slots hit harder) | Output |
| M05 | Crit (crit threshold, crit faces, crit effects) | Output |
| M06 | Precision (accuracy, hit threshold, never miss) | Output |
| M07 | Exact Numbers (natural roll values, odd/even, 7s, 21) | Dice |
| M08 | Fate Control (reroll, replace roll, exploding dice, loaded) | Dice |
| M09 | Failure Engine (misses, crit misses, salvage) | Dice |
| M10 | Chain Length (combo threshold, long combos) | Combo |
| M11 | Opener (first hit, first slot) | Combo |
| M12 | Finisher (last slot, combo end) | Combo |
| M13 | Strip Manipulation (loop, jump, skip, shuffle, replace) | Combo |
| M14 | Echo / Retrigger (repeat actions or slots) | Combo |
| M15 | Tag Grammar (repeat-a-tag vs vary-tags) | Combo |
| M16 | Poison | Status offense |
| M17 | Bleed | Status offense |
| M18 | Burn | Status offense |
| M19 | Stun / Hard Control | Status offense |
| M20 | Slow | Status offense |
| M21 | Weaken | Status offense |
| M22 | Expose / Vulnerability / Armor Break | Status offense |
| M23 | Pierce (ignore armor) | Status offense |
| M24 | Mark (delayed, detonated payoff) | Status offense |
| M25 | Disrupt (confuse, silence, enemy accuracy) | Status offense |
| M26 | Bulwark (Harden, Fortify, armor) | Defense |
| M27 | Sustain (heal, lifesteal, regen) | Defense |
| M28 | Shield (temp HP, absorb, overheal) | Defense |
| M29 | Retaliation (on take hit, reflect, RAGE) | Defense |
| M30 | Brink (low-HP power, risk) | Defense |
| M31 | Snowball (permanent per-dungeon gains, kill / room clear) | Scaling |
| M32 | Material Count (set stacking, tag counts) | Scaling |
| M33 | Condition & Rarity (Broken…Perfect, rarity tiers) | Scaling |
| M34 | Mint & Spend (currencies, banking) | Scaling |
| M35 | Status Conversion (one status becomes another) | Scaling |
| M36 | Context (enemy prefix, room hazard, region) | Context |
| M37 | Swarm (summons, extra bodies) | Context |
| M38 | Self-Cost (self-debuff, HP payment, sacrifice) | Context |
| M39 | Long Strip (slot count, INT slots) | Combo |
| M40 | Cadence (every Nth turn, beats, parity of turn) | Combo |

---

## Part 2 — Single-mechanic builds

### M01 · Heavy Hit — "Siegebreaker"

- **Fantasy**: one swing per turn and every swing could end the fight.
- **Actions**
  - `WIND UP` (exists): next action +damage. The wind-up is the investment.
  - `CATACLYSM`: 2.5× length, 400% damage, can't be the opener. Pays off long tempo.
  - `HEFT`: no damage; your next action's damage scales with its own length ("slow is heavy").
  - `FOLLOW THROUGH`: after a hit over 30% enemy max HP, the next action costs half its length.
- **Items**
  - *Maul of Gravity* (weapon): +1% damage per 1% slower the weapon is vs base.
  - *Anvil Greaves* (feet): −20% speed, +STR equal to Legs+Feet armor.
  - Affix *Ponderous*: +damage, −speed (the classic trade, made explicit).
  - Set *Iron ×3*: hits that deal over 25% of enemy HP mint CRISIS.
- **Skills / Class** (Barbarian)
  - Passive *Momentum of Mass*: damage cap +X per STR (pairs with Titan, exists).
  - Rule *Unhurried*: actions longer than 1.5 can't be interrupted by stun, and ignore slow.
  - Mastery *Earthshaker*: overkill damage carries to the next enemy in the room / next fight's opener.
- **Combo**: `HEFT → WIND UP → CATACLYSM`: two cheap setups multiply one finisher; the strip is a run-up.
- **Fun / Risk**: big numbers; misses are devastating. Answer: Precision items, or accept variance.

### M02 · Tempo — "Metronome"

- **Fantasy**: you act two or three times for every enemy swing.
- **Actions**
  - `QUICKSTEP`: 0.4 length, small hit, next action +30% speed.
  - `DOUBLE TIME`: next 2 actions have half length but −25% damage.
  - `STEAL TURN`: if your action ended before the enemy's next one, take an extra action immediately (once/fight).
  - `CADENCE BREAK`: damage scales with how many actions you've taken since the enemy last acted.
- **Items**
  - *Winged Sandals* (feet): +speed, actions under 0.7 length gain +1 multihit.
  - *Featherweight* affix (exists) generalised: −weight, +speed.
  - Set *Bronze ×3*: BEAT on every turn (exists); BEAT feeds speed.
  - *Pendulum Charm*: alternating fast/slow actions grant Focus (Cadence Keeper as an item).
- **Skills / Class** (Warrior)
  - Passive *First Formation* (exists).
  - Rule *Initiative*: the hero always acts first in a fight if their first action is under 1.0 length.
  - Mastery *Overclock*: every 3rd consecutive action before an enemy act is free (0 length).
- **Combo**: `QUICKSTEP → QUICKSTEP → CADENCE BREAK`: stack actions between enemy swings, cash in the count.
- **Fun / Risk**: low damage per action; weak against armor (needs Pierce) and enemies with reflect.

### M03 · Multihit — "Thousand Cuts"

- **Fantasy**: a flurry; every hit procs something.
- **Actions**
  - `FLURRY`: 6 hits at 20% each.
  - `RAKE`: each hit applies the hero's on-hit effects twice but deals no base damage.
  - `CRESCENDO`: hit N deals N×10% (rewards long flurries).
  - `SPLINTER`: converts a single-hit action's damage into 3 hits of 40%.
- **Items**
  - *Twin Daggers* (weapon): +1 multihit on all actions, −15% damage per hit.
  - *Serrated* affix (exists): each hit adds Bleed.
  - *Gauntlets of Echoes*: the last hit of a multihit action repeats at 50%.
  - Set *Shadow ×3*: SWING per attack (exists): feed per hit instead of per action.
- **Skills / Class** (Rogue)
  - Mastery *Precision* (exists): Rogue actions +1 multihit.
  - Passive *Dagger Mastery* (exists): extra hits add Bleed.
  - Rule *Rhythm Strikes*: every 5th hit in a fight is an automatic crit.
- **Combo**: `SPLINTER → FLURRY → CRESCENDO`: converts the strip to many hits, then a long ramping flurry.
- **Fun / Risk**: big logs, proc soup. Weak against flat damage reduction (Harden-type enemies); needs Pierce or Expose.

### M04 · AMP — "Crescendo Mage"

- **Fantasy**: each slot is louder than the last; the 6th action is a nuke.
- **Actions**
  - `PRELUDE`: tiny hit; +AMP to every later slot this combo.
  - `SUSTAIN`: does nothing but carry and grow AMP by 50% (a "rest" in the score).
  - `CODA`: finisher; consumes all AMP for damage; AMP doesn't reset if it kills.
  - `KEY CHANGE`: doubles current AMP but the next action has a 25% miss penalty.
- **Items**
  - *Tuning Fork Wand*: AMP decays half as fast on miss.
  - *Score Robe* (body): +1 strip slot (Long Strip support).
  - Affix *Resonant*: +AMP per Wizard item.
  - Set *Willow ×3*: CHAIN mint per consecutive spell.
- **Skills / Class** (Wizard)
  - *Concentrated Chaos* (exists), *Slot Mind* (exists), *Archmage* (exists).
  - Rule *Crescendo Law*: AMP bonus is multiplied by slot index instead of added.
- **Combo**: `PRELUDE → SUSTAIN → SUSTAIN → KEY CHANGE → CODA`.
- **Fun / Risk**: a long sentence whose end is huge; one miss and nothing pays. Needs Precision or a shield.

### M05 · Crit — "Glass Edge"

- **Fantasy**: fishing for 19s and 20s; every crit is an event.
- **Actions**
  - `CRIT FACES` (exists): 18+ crits.
  - `HONE THE EDGE`: next 3 rolls: crit threshold −2.
  - `EXECUTION LINE`: crit is guaranteed if the target is below 30% HP.
  - `SHATTERPOINT`: on crit, deals the crit's damage again as true damage; on non-crit, half damage.
- **Items**
  - *Glass Dagger*: crit multiplier +100%, item loses condition each crit and shatters at Broken (re-rolls into a new Glass item).
  - *Keen* affix (exists).
  - *Lens Monocle* (head): crit threshold −1 per Silver item.
  - Set *Glass ×3*: GRAZE on crit (exists).
- **Skills / Class** (Rogue)
  - *Perfect Aim* (exists), *Concussive Crit* (exists, Barbarian).
  - Rule *Cascade*: a crit lowers the next action's crit threshold by 2 (resets on non-crit).
- **Combo**: `HONE THE EDGE → STAB → STAB → SHATTERPOINT`: three cheap crit chances while honed, then a crit-or-bust payoff.
- **Fun / Risk**: spiky; pairs with Fate Control to smooth.

### M06 · Precision — "Surgeon"

- **Fantasy**: never miss; reliability is the build.
- **Actions**
  - `MEASURED CUT` (exists).
  - `STEADY` : next 3 actions can't miss but can't crit.
  - `CALIBRATE`: hit threshold −1 permanently this fight per consecutive hit (max 5).
  - `SURE STRIKE`: damage +5% per point your roll exceeded the hit threshold.
- **Items**
  - *Plumb Line* (weapon): misses become grazes at 40% damage.
  - *Precise* affix (exists).
  - Set *Mithril ×3*: STREAK per consecutive hit (exists).
- **Skills / Class** (TEC / Rogue)
  - *Steady Hand* (exists): TEC per consecutive non-miss.
  - Rule *Margin*: overflow over the hit threshold converts to damage at 2% per point.
- **Combo**: `STEADY → SURE STRIKE → SURE STRIKE → CALIBRATE`.
- **Fun / Risk**: low variance, lower ceiling; fun comes from margin math. Counters: enemy evasion prefixes.

### M07 · Exact Numbers — "Numerologist"

- **Fantasy**: specific numbers are magic. 7s, odd/even, totals of 21.
- **Actions**
  - `NATURAL SEVEN` (exists), `ROLL FIFTEEN` (exists).
  - `CALL THE NUMBER`: name a number 1–20 (randomly assigned per fight shown in log); rolling it triples damage and sets the next called number.
  - `PARITY BLADE`: odd roll = poison, even roll = bleed.
  - `SUM`: tracks running roll total in a combo; landing exactly on 21 retriggers the opener (Grand Equation, exists, as an action).
- **Items**
  - *Seven-Sided Charm*: 7s count as crits.
  - *Odd Gloves / Even Boots*: each gives a bonus on one parity; wearing both lets you choose per fight.
  - *Abacus* (head): show running SUM; +1 to it per Silver piece.
- **Skills / Class** (Wizard / Rogue)
  - *Grease Palm* (exists, ≤7), *Loaded Odds* (exists, 15+), *Snake Eyes* (exists).
  - Rule *Lucky Number*: choose a number at dungeon start; it counts as a natural 20.
- **Combo**: `PARITY BLADE → PARITY BLADE → SUM`: each roll applies a status and moves the total toward 21.
- **Fun / Risk**: puzzle-like; low baseline. Needs Fate Control to hit targets.

### M08 · Fate Control — "Loaded Hand"

- **Fantasy**: you cheat the dice.
- **Actions**
  - `REPLACE ROLL` (exists), `EXPLODING DICE` (exists), `REWRITE FATE` (exists), `LOADED DICE` (exists).
  - `PALM`: store your current roll; use it later instead of rolling.
  - `SWAP FATE`: swap your next roll with the enemy's next roll.
- **Items**
  - *Weighted Die* (charm): +1 reroll per fight.
  - *Gambler's Coat*: rerolls also roll a bonus d6 added to damage.
  - Affix *Fated*: the first roll of each fight is 15+.
- **Skills / Class**
  - *Flow State* (exists): reroll + exploding dice after three distinct connects.
  - Rule *Fate Debt*: you may reroll any time; each reroll adds −1 to a roll at the end of the fight (or next fight).
- **Combo**: `PALM → (anything) → SHATTERPOINT`: bank a 20, spend it on the big crit payoff.
- **Fun / Risk**: control fantasy; Fate Debt makes it a real loan.

### M09 · Failure Engine — "Lucky Fool"

- **Fantasy**: you miss on purpose; misses charge you.
- **Actions**
  - `SALVAGE MISS` (exists), `AFTER MISS HEAL` (exists).
  - `RECKLESS SWING`: −5 to hit, +100% damage; on miss, mints 2 GRAZE.
  - `PRATFALL`: auto-miss; next action is a guaranteed crit.
  - `SECOND WIND STRIKE`: deals bonus damage per miss this fight.
- **Items**
  - *Jester's Bells*: every miss grants +2 TEC for the fight.
  - *Cracked Mirror*: crit misses count as crits for triggers.
  - Set *Shadow ×2*: misses don't end the combo.
- **Skills / Class** (Rogue)
  - *Second Chance* (exists).
  - Rule *Fail Forward*: the combo never resets from misses; misses advance the strip but deal 0.
- **Combo**: `RECKLESS SWING → PRATFALL → SECOND WIND STRIKE`.
- **Fun / Risk**: inverts normal feelings about misses; risky against hard-hitting enemies.

### M10 · Chain Length — "Endless Chain"

- **Fantasy**: keep the combo alive forever.
- **Actions**
  - `LINK`: lowers the combo threshold for the next action by 3.
  - `CHAIN SNAP`: deals damage = combo step × 20%; ends the combo.
  - `HOLD THE LINE`: if this misses, the combo doesn't break (but it doesn't advance).
- **Items**
  - *Chain Shirt* (body): +1 Combo (lower threshold) per 2 links in this combo.
  - Set *Heavy / Drill / Soft Cadence* (exists, skills) as items.
- **Skills / Class**
  - Rule *Cadence* rules (exist).
  - Mastery *Infinite Form*: after step 8, every action is +1 multihit.
- **Combo**: `LINK → STRIKE → LINK → STRIKE → CHAIN SNAP`.
- **Fun / Risk**: ramp; vulnerable to stun/disrupt enemies.

### M11 · Opener — "Ambusher"

- **Fantasy**: the first hit decides the fight.
- **Actions**
  - `OPENER` (exists), `RETRIGGER OPENER` (exists).
  - `FROM THE SHADOWS`: opener only; 300% damage; Expose 2.
  - `FIRST BLOOD`: the first hit each fight applies every status your items can apply.
- **Items**
  - *Assassin's Hood*: ONFIRSTHIT effects doubled.
  - *Starting Pistol charm*: the opener is free (0 length).
- **Skills / Class**
  - Rule *Every Fight Is New*: each new combo after a miss counts as an opener (ONFIRSTHIT re-arms).
- **Combo**: `FROM THE SHADOWS → RETRIGGER OPENER`.
- **Fun / Risk**: burst; weak in long boss fights.

### M12 · Finisher — "Executioner"

- **Fantasy**: build everything into the final slot.
- **Actions**
  - `FINISHER` (exists), `RETRIGGER FINISHER` (exists), `AVENGE` (exists).
  - `GUILLOTINE`: finisher; kills outright below X% HP (X = combo steps × 3).
  - `ENCORE`: finisher; if the combo had no misses, restart the strip immediately.
- **Items**
  - *Headsman's Axe*: finisher damage +50%, other slots −10%.
  - Set *Steel ×3*: TRIUMPH on combo end (exists).
- **Skills / Class**
  - *Second Wind* (exists): no-miss combo end heals.
  - Rule *Last Word*: the finisher inherits every on-hit effect from the earlier slots.
- **Combo**: `JAB → JAB → WIND UP → GUILLOTINE`.

### M13 · Strip Manipulation — "Clockmaker"

- **Fantasy**: you reprogram your own strip mid-fight.
- **Actions**
  - `STRIP JUMP / SKIP / LOOP / SHUFFLE / REPLACE / STOP` (exist).
  - `GEAR SWAP`: swap the next two slots.
  - `RECURSE`: loop the previous 2 slots once; can't recurse a RECURSE.
- **Items**
  - *Clockwork Gauntlet*: the first strip op each combo also deals damage.
  - *Escapement* (charm): loops don't count as repeats for Spell Memory / Debt.
- **Skills / Class**
  - Rule *Program*: strip ops cost 0 length.
  - Mastery *Debugger*: when a strip op lands on a miss, re-roll it.
- **Combo**: `HEAVY → RECURSE → FINISHER`: heavy hit plays twice before the finisher.

### M14 · Echo / Retrigger — "Echo Chamber"

- **Fantasy**: actions happen again.
- **Actions**
  - `ECHO SPELL` (exists), `RETRIGGER NEXT / SLOT` (exist), `PHANTOM FOLLOW-UP` (exists).
  - `REVERB`: every action this combo echoes at 25%.
- **Items**
  - *Mirror Shield*: echoes inherit statuses.
  - *Bell* (head): echoes trigger ONCONNECT.
- **Skills / Class**
  - *Living Alloy* (exists): crit consumes Harden to repeat the slot.
  - Rule *Harmonics*: echo damage scales with AMP.
- **Combo**: `BOLT → ECHO SPELL → REVERB → FIREBALL`.

### M15 · Tag Grammar — "Linguist"

- **Fantasy**: the strip is a word you spell with tags; repeat for stacks, vary for payouts.
- **Actions**
  - `CONJUGATE`: copies the previous action's tags onto the next action.
  - `NEUTRAL`: tagless filler; doesn't break repetition streaks.
- **Items**
  - *Lexicon* (charm): +1 tag of your choice on every action.
- **Skills / Class**
  - *Spell Memory* (exists, repeat), *Technical Debt* (exists, vary).
  - Rule *Etymology*: material tags on gear count as action tags for grammar rules.
- **Combo**: `FIRE → FIRE → FIRE → ICE`: build Spell Memory then cash by switching.

### M16 · Poison — "Plague Doctor"

- **Fantasy**: slow inevitability; the numbers pile up while you defend.
- **Actions**
  - `VENOM COAT`: no damage; your next 3 connects apply +2 Poison each.
  - `INCUBATE`: Poison doesn't tick this turn; next turn it ticks twice.
  - `PURGE`: consume all Poison; instant damage = stacks × ticks it would have dealt (cash-out).
  - `MIASMA`: Poison also ticks on the enemy's *misses*.
- **Items**
  - *Poisonous* affix (exists): on-hit Poison.
  - *Plague Mask* (head): Poison ticks heal you 0.5% max HP.
  - *Vial Bandolier* (body): Poison cap +5; Poison never decays while you're above 50% HP.
  - Set *Iron ×2*: Venom Mace (exists): Mace hits resolve one tick early and convert a stack into Acid.
- **Skills / Class** (Rogue / Barbarian Iron)
  - *Toxin Pressure*, *Envenomed Edge*, *Toxic Insight* (exist).
  - Rule *Virulence*: on kill, half of the Poison stacks jump to the next enemy of the dungeon.
  - Mastery *Potency*: each Poison stack above 10 adds +1% to every tick.
- **Combo**: `VENOM COAT → STAB → STAB → INCUBATE → PURGE`: load the stacks, skip a tick to double it, then cash in.
- **Fun / Risk**: a delayed big number with a timing decision (purge now or let it tick). Weak vs Undead (poison-immune idea) and in short fights.

### M17 · Bleed — "Hemomancer"

- **Fantasy**: the enemy bleeds every time *it* acts, so its own aggression kills it.
- **Actions**
  - `LEECHING CUT` (exists).
  - `OPEN VEIN`: applies Bleed equal to the action's hit count × 2.
  - `TOURNIQUET`: the enemy's next 2 actions trigger Bleed twice each.
  - `PROVOKE BLOOD`: taunt; the enemy's next action is faster (Bleed ticks sooner) and deals −20%.
- **Items**
  - *Serrated* affix (exists): Bleed per hit.
  - *Blood Chalice* (charm): heal 1% per Bleed tick.
  - *Butcher's Apron* (body): Bleeding enemies take +1 damage per Bleed stack from all sources.
  - Set *Obsidian ×3*: TROPHY / Headhunt pieces (exist).
- **Skills / Class** (Rogue Obsidian / Barbarian Bone)
  - *Deep Cut*, *Hemorrhage*, *Blood Draught*, *Dagger Mastery*, *Blood Frenzy* (exist).
  - Rule *Arterial*: Bleed ticks can crit using your crit threshold.
- **Combo**: `OPEN VEIN (multihit dagger) → PROVOKE BLOOD → TOURNIQUET`: stack Bleed, then make the enemy act fast and often.
- **Fun / Risk**: flips "the enemy acting is bad". Risky against heavy hitters; pairs with defense.

### M18 · Burn — "Pyromancer"

- **Fantasy**: fire that grows, then detonates.
- **Actions**
  - `KINDLE` (exists).
  - `STOKE`: +1 Burn per Burn already on the target (exponential, capped).
  - `FLASHPOINT`: consume all Burn; damage = stacks² × 2% of your AMP.
  - `CAUTERIZE`: consume Burn on *yourself* (from self-cost actions) to heal.
- **Items**
  - *Flaming* affix (exists).
  - *Ember Robe* (body): each Burn tick adds +2% AMP to your next action.
  - *Cinder Wand*: Burn duration +1 per Crystal piece.
  - Set *Crystal ×3*: CONTACT mint on status apply.
- **Skills / Class** (Wizard Crystal)
  - *Sigil Burn*, *Kindled Mind*, *Thermal Shock* (exist).
  - Rule *Wildfire*: Burn on a dying enemy spreads to the next room's enemy at half stacks.
- **Combo**: `KINDLE → STOKE → STOKE → FLASHPOINT`.
- **Fun / Risk**: a visible fuse with a clear detonate moment. Weak vs elemental / fire-resistant prefixes.

### M19 · Stun / Hard Control — "Concussion"

- **Fantasy**: the enemy never gets to act.
- **Actions**
  - `BLUDGEON`, `SKULL CRACK` (exist).
  - `DAZE`: builds Daze; at 3 Daze the enemy is stunned for one action.
  - `PUNISH THE HELPLESS`: +100% damage and +1 multihit against stunned targets.
  - `RING THE BELL`: if the target is stunned, extend the stun by 1, but your next action is 50% slower.
- **Items**
  - *Concussion Maul* (weapon): crits add 1 Daze.
  - *Heavy* affix (exists): stun duration +1 on crit.
  - *Earplugs* (head): you're immune to stun while the enemy is stunned.
- **Skills / Class** (Barbarian)
  - *Concussive Crit* (exists).
  - Rule *Diminishing Returns*: each stun on the same enemy needs +1 Daze, but each resisted stun becomes 2 Weaken (fair, still pays).
- **Combo**: `DAZE → DAZE → DAZE → PUNISH THE HELPLESS`.
- **Fun / Risk**: lock-down feels great; boss stun resistance is the counter.

### M20 · Slow — "Tide Anchor"

- **Fantasy**: drag the enemy to a crawl; its time becomes yours.
- **Actions**
  - `SLOW`, `GOLD ANCHOR` (exist).
  - `MOLASSES`: Slow that grows +1 each time the enemy acts.
  - `UNDERTOW`: damage scales with how much slower the enemy is than you.
- **Items**
  - Set *Gold ×3*: DRAG on slow (exists).
  - *Lead Chain* (weapon): Slowed enemies take +X% per Slow stack.
  - *Hourglass* (charm): each Slow stack you apply also gives you +1% speed.
- **Skills / Class** (Warrior Gold)
  - *Pinning Drill* (exists).
  - Rule *Time Debt*: when Slow expires on an enemy, you gain an immediate extra action.
- **Combo**: `SLOW → MOLASSES → UNDERTOW`.
- **Fun / Risk**: a tempo build that works against fast enemies; low burst.

### M21 · Weaken — "Hollow Out"

- **Fantasy**: by turn 3 the enemy is pathetic, and you're bigger.
- **Actions**
  - `WEAKEN` (exists).
  - `SAP`: transfer the target's Weaken amount to your STR for the fight.
  - `CRUSH SPIRIT`: Weaken doubled if the enemy missed its last action.
- **Items**
  - *Leeching Grip*: Weaken also lowers enemy armor.
  - *Brutal* affix (exists): Weaken on crit.
- **Skills / Class** (Warrior Gold)
  - *Pinning Drill* (exists).
  - Rule *Hollowing*: each re-application of Weaken raises the Weaken cap by 1.
- **Combo**: `WEAKEN → SAP → HAYMAKER`: drain, then spend the stolen STR.
- **Fun / Risk**: defensive and offensive at once; weak in burst fights.

### M22 · Expose / Vulnerability / Armor Break — "Breaker"

- **Fantasy**: you take the enemy apart so everything else works.
- **Actions**
  - `EXPOSE`, `VULNERABILITY`, `REVEAL` (exist).
  - `SUNDER`: Armor Break 1 per hit.
  - `CRACK OPEN`: consume all Armor Break; damage = armor removed × 5.
- **Items**
  - *Crowbar* (weapon): +1 Armor Break per multihit.
  - Set *Glass ×5*: Shatter (exists): Vulnerability per GRAZE.
  - *Inspector's Lens* (head): Expose lasts +1 turn.
- **Skills / Class** (Rogue Obsidian / Shadow)
  - *Hemorrhage*, *Technical Debt* (exist).
  - Rule *Weak Point*: hits against Exposed targets have crit threshold −2.
- **Combo**: `SUNDER (multihit) → EXPOSE → HEAVY STRIKE`.
- **Fun / Risk**: support-shaped; strong vs armored bosses, wasted on glass enemies.

### M23 · Pierce — "Lance"

- **Fantasy**: armor doesn't exist for you.
- **Actions**
  - `PIERCE` (exists).
  - `SKEWER`: pierce scales with your speed bonus.
  - `RUN THROUGH`: pierce 100%; can't crit.
- **Items**
  - *Needle* (weapon): +pierce equal to your margin over the hit threshold.
  - *Long* affix (exists): +pierce.
- **Skills / Class**
  - Rule *Through and Through*: pierce overflow (armor already zero) becomes Bleed.
- **Combo**: `SKEWER → SKEWER → RUN THROUGH`.
- **Fun / Risk**: anti-armor specialist; a dead slot vs unarmored enemies unless Through and Through.

### M24 · Mark — "Bounty Hunter"

- **Fantasy**: you set up a detonation that pays off later.
- **Actions**
  - `MARK`: tag the target; the next finisher detonates it for the stored damage.
  - `HEADHUNT` (exists).
  - `DEEP MARK`: the mark stores 20% of all damage dealt while it's active.
  - `TRANSFER MARK`: on kill, move the mark (and its stored value) to the next enemy.
- **Items**
  - *Bounty Ledger* (charm): detonations give a loot bias.
  - *Hunter's Hood* (head): marked enemies get −2 to hit you.
- **Skills / Class** (Rogue)
  - Rule *Contract*: marks persist across fights on the same enemy type in a dungeon.
- **Combo**: `DEEP MARK → STAB → STAB → FINISHER (detonate)`.
- **Fun / Risk**: anticipation; a dead strip if the enemy dies early.

### M25 · Disrupt — "Trickster"

- **Fantasy**: the enemy fumbles and hits itself.
- **Actions**
  - `CONFUSE`, `MISDIRECT`, `TAUNT` (exist).
  - `HEX OF CLUMSINESS`: enemy accuracy −3 for 2 actions.
  - `SILENCE`: the enemy's next special action is replaced by a basic attack.
- **Items**
  - *Mirror Cloak* (body): when a confused enemy hits itself, your on-hit effects apply.
  - *Jester's Cap* (head): +1 Confuse duration.
- **Skills / Class**
  - Rule *Chaos Theory*: each enemy miss gives you +1 to your next roll.
- **Combo**: `HEX OF CLUMSINESS → CONFUSE → PARRY`.
- **Fun / Risk**: an avoidance fantasy; low damage, needs a payoff partner.

### M26 · Bulwark — "Fortress"

- **Fantasy**: you can't be hurt, and your defense becomes offense.
- **Actions**
  - `HARDEN`, `PARRY` (exist).
  - `SHIELD BASH`: damage = current armor × 2.
  - `BRACE`: Fortify 3; your next action deals +1% per point of Fortify.
- **Items**
  - *Hardened*, *Sturdy* affixes (exist).
  - *Tower Plate* (body): armor doubled, −30% speed.
  - Set *Bone ×2* / *Steel ×2*: Bone Skin, Steel Knuckle (exist).
- **Skills / Class** (Barbarian / Warrior Gold)
  - *Phalanx*, *Living Alloy*, *Level 2 Barbarian* (exist).
  - Rule *Bulwark Strike*: armor counts as STR for damage at 50%.
- **Combo**: `HARDEN → BRACE → SHIELD BASH`.
- **Fun / Risk**: safe and slow; burst enemies are the test.

### M27 · Sustain — "Leech"

- **Fantasy**: every hit heals; you outlast anything.
- **Actions**
  - `LEECHING CUT` (exists).
  - `TRANSFUSION`: heal for 30% of damage dealt this combo.
  - `BLOOD FEAST`: on kill, heal to 50% if below it.
- **Items**
  - *Vampire Fang* (weapon): +lifesteal.
  - *Crit Heal* / *Combo End Heal* (exist as actions) moved onto item affixes.
- **Skills / Class**
  - *Blood Price*, *Mana Bleed*, *Blood Draught* (exist): the per-hit heals that drive class parity today.
  - Rule *Overflow*: overheal becomes temp HP (up to 20% max HP).
- **Combo**: `LEECHING CUT → LEECHING CUT → TRANSFUSION`.
- **Fun / Risk**: grinds out wins; too strong when per-hit heals stack with multihit (a known balance hazard).

### M28 · Shield — "Bastion"

- **Fantasy**: a buffer you build and then spend.
- **Actions**
  - `BARRIER`: gain temp HP.
  - `SHIELD SLAM`: deal damage equal to temp HP, then lose it.
  - `ABSORB`: the next enemy hit is absorbed and stored as damage for your next action.
- **Items**
  - *Aegis Charm*: start each fight with temp HP.
  - *Capacitor Plate* (body): temp HP cap +50%.
- **Skills / Class**
  - Rule *Conservation*: temp HP lost converts to Fortify.
- **Combo**: `BARRIER → ABSORB → SHIELD SLAM`.
- **Fun / Risk**: a spend-or-save decision; weak to piercing enemies.

### M29 · Retaliation — "Thornback"

- **Fantasy**: hit me; it hurts you more.
- **Actions**
  - `TAUNT` (exists).
  - `RIPOSTE`: the next enemy hit is reflected at 100%.
  - `GRUDGE`: damage +10% per hit taken this fight.
- **Items**
  - *Thorn Mail* (body): reflect 10%.
  - Set *Bone ×3*: RAGE on receive hit (exists).
- **Skills / Class** (Barbarian Bone)
  - *Wrath Unbound* (exists).
  - Rule *Vengeance*: every hit taken is banked; your next connect adds it.
- **Combo**: `TAUNT → RIPOSTE → BONE WRATH`.
- **Fun / Risk**: wants to be hit; needs sustain or bulwark.

### M30 · Brink — "Berserker"

- **Fantasy**: low HP is your power zone.
- **Actions**
  - `BERSERKER`, `ENRAGE` (exist).
  - `LAST STAND`: damage × (1 + missing HP%).
  - `DEATH'S DOOR`: once per fight, survive a lethal hit at 1 HP.
- **Items**
  - *Bloodied Helm*: crit threshold −2 below 30% HP.
  - Set *Iron ×3*: CRISIS on enemy HP threshold (exists); a mirror version on hero HP.
- **Skills / Class**
  - Rule *Deathwish*: you can't heal above 50%; damage +50%.
- **Combo**: `ENRAGE → LAST STAND`.
- **Fun / Risk**: close calls (good for the dungeon close-call metric); death if mistimed.

### M31 · Snowball — "Collector"

- **Fantasy**: every kill and every room makes you permanently stronger for the rest of the dungeon; the boss meets a monster.
- **Actions**
  - `TROPHY CUT`: normal hit; if it kills, +1 primary stat for the dungeon (DUNGEON scope).
  - `DUNGEON ANALYSIS` (exists): bonus per room cleared.
  - `RECKONING`: damage +4% per enemy killed this dungeon; the build's payoff verb.
  - `HARVEST`: no damage; consume 3 dungeon stacks for a full heal (cash out the snowball to survive).
  - `TALLY`: shows and banks the fight's kill count; if the fight ended in under 3 hero actions, +1 extra stack.
- **Items**
  - *Trophy Necklace* (charm): +2% damage per room cleared; resets on leaving the dungeon.
  - *Skull Belt* (body): each kill adds +1 armor for the dungeon (snowball on the defense side).
  - *Hunter's Satchel* (feet): room-clear heal (exists as an action) moved onto an affix, scaled by stacks.
  - Affix *Hungry*: +X% damage per dungeon stack, −X% damage if you have none (bad in room 1, great by room 5).
- **Skills / Class**
  - *Blood Frenzy*, *Pinning Drill*, *Toxic Insight*, *Kindled Mind* (exist): status apply → dungeon stat. Each class already has one.
  - Rule *Momentum Carry*: 10% of the combo step and AMP carry into the next fight's opener.
  - Mastery *Apex*: past 10 dungeon stacks, each new stack also lowers crit threshold by 1 (cap 3).
  - Rule *Glutton*: dungeon stacks also count as material pieces for set thresholds (bridges to M32).
- **Combo**: `TROPHY CUT → TROPHY CUT → RECKONING`. Early in the dungeon the strip finishes weak enemies with trophy cuts; by the boss, RECKONING is the main damage source. HARVEST is a panic button kept in the pool, not the strip.
- **Fun / Risk**: a visible power arc inside one dungeon, and a real choice when HARVEST spends progress for survival. Weak in room 1–2; the boss room or an early death erases the investment. Measurement note: judge it by dungeon clear rate, never by single-fight win rate.

### M32 · Material Count — "Purist"

- **Fantasy**: you're a collector; five matching pieces turn into a set bonus far stronger than its parts.
- **Actions**
  - The 12 convert actions (exist): IRON CULL, STEEL CREST, BONE WRATH, DRUM CUT, GOLD ANCHOR, MITHRIL FLOW, GLASS NICK, UMBRA, HEADHUNT, WILLOW LASH, REFRACT, RESONATE.
  - `ALLOY`: for this combo, count one extra piece of every material you wear at least 2 of.
  - `ASSAY`: damage +10% per distinct material worn (the "rainbow" counter to purity).
  - `TEMPER`: spend this action to treat the next convert action as if you wore one more matching piece.
- **Items**
  - Set thresholds 2/3/5 (exist): synthesis + convert unlock / +feed / ×feed.
  - *Mixed Ingot* (wildcard piece): counts as any material, but only toward the 3-piece threshold.
  - *Ancestral Weapon*: the weapon's material counts double for set count.
  - *Unmatched Ring*: +bonus only if no two worn pieces share a material (anti-set; supports the rainbow build).
- **Skills / Class**
  - *First Steel Age / Formation / Cut / Glyph*, *Kinship* rites, Level 1 "counts as class tag" rules (exist).
  - *Blood Market / Muster Hall / Black Market / Reagent Market* (exist): loot bias so purity is reachable.
  - Rule *Pure Blood*: at 5 of one material, the ×feed step also applies to the material's synthesis mint.
  - Rule *Patchwork*: each 2-piece set you wear gives +1 to every other 2-piece set (rewards a 2+2+1 build).
- **Combo**: depends on material. Pure Bone: `TAUNT → TEMPER → BONE WRATH`. Rainbow: `ASSAY → ALLOY → (any convert)`.
- **Fun / Risk**: the loot chase; two opposite fantasies (pure vs rainbow) in one system. Frustrating when drops won't cooperate; the market rules are the safety valve. Watch that set strength comes from set knobs, not stats (per the class-less set findings).

### M33 · Condition & Rarity — "Scrapper" / "Curator"

- **Fantasy**: two opposite fantasies. The Scrapper fights in falling-apart gear and gets stronger as it breaks; the Curator keeps pristine heirlooms and is rewarded for never letting them get damaged.
- **Actions**
  - `SACRIFICE GEAR`: destroy a worn item; one hit that scales with its rarity tier (Common ×1 … Legendary ×5).
  - `OVERSTRAIN`: +100% damage; one random worn item loses one condition step.
  - `MEND`: improve one item's condition by a step; the next action gets +damage per condition step on that item.
  - `POLISH`: no damage; while no worn item has lost condition this dungeon, +AMP to the rest of the combo.
- **Items**
  - Condition tiers (exist): Broken / Preowned / Worn / Like New / Perfect / Heirloom, etc.
  - *Patchwork Armor*: +5% damage per worn item at Broken.
  - *Display Case* (charm): Heirloom / Perfect items can't lose condition; +10% per such item worn.
  - *Glass Dagger* (from M05): loses condition on crit and shatters into a re-rolled item at Broken (gear as ammunition).
- **Skills / Class**
  - *Scrap Prefer*, *Parade Dress*, *Fragile Prefer*, *Pure Prefer* (exist): each class already prefers a condition band.
  - Rule *Salvage Rights*: destroyed items drop one material piece of the same material.
  - Rule *Provenance*: rarity tier adds to set count (a Legendary piece counts as 2).
- **Combo**: Scrapper: `OVERSTRAIN → HAYMAKER → SACRIFICE GEAR`. Curator: `POLISH → MEND → CODA`.
- **Fun / Risk**: makes every loot drop's condition and rarity a decision, not just a number. A sacrifice is a real loss; Salvage Rights softens it. Risk: inventory churn and UI noise.

### M34 · Mint & Spend — "Banker"

- **Fantasy**: you build up currency during the fight and cash it out at exactly the right moment.
- **Actions**
  - Convert actions (exist): each spends its own material's mint.
  - `INVEST`: spend half your current mints; at combo end, they come back doubled (if the combo didn't miss).
  - `LIQUIDATE`: spend every currency you hold, of every kind, at 50% value each, in one hit.
  - `HEDGE`: lock in 25% of current mints as a floor that survives a miss or a fight end.
  - `LOAN`: gain 5 mints now; lose 8 at the end of the fight (a debt).
- **Items**
  - Mints from material sets (exist).
  - *Vault Belt*: mint cap +50%; mints don't decay between fights.
  - *Ledger* (charm): each time you spend 10+ mints at once, +1 Focus.
  - *Counterfeit Coin* (cursed charm): mints from one material also count as another, at −30%.
- **Skills / Class**
  - *Blood / Drill / Edge / Weave Synthesis* (exist): +1 to each mint per 20 primary stat.
  - Rule *Compound Interest*: unspent mints grow 10% at the start of each turn (cap).
  - Rule *Exchange*: once per combo, convert one currency into another 1:1.
- **Combo**: `(mint actions) → INVEST → (mint) → CONVERT`. A Barbarian with Bone and Iron might bank RAGE through a hard fight and only cash it out once CRISIS is minted.
- **Fun / Risk**: a planning build with a strong "now or later" choice; holding too long gets you killed, cashing too early wastes the multiplier. LOAN makes the risk explicit.

### M35 · Status Conversion — "Alchemist"

- **Fantasy**: you don't apply the status you want; you apply what's easy and turn it into what's strong.
- **Actions**
  - `TRANSMUTE`: convert all of one status on the target into another at 1:1 (the action names the pair).
  - `CATALYST`: the next conversion doubles stacks.
  - `REACTION`: consume two different statuses; deal damage = product of their stacks.
  - `DISTILL`: convert a status on the target into a buff on you (Poison → Focus, Burn → AMP, Bleed → lifesteal).
- **Items**
  - *Alchemist's Gloves*: conversions add +1 stack.
  - *Retort* (weapon): every 3rd hit converts the target's largest status into your weapon's element.
  - *Crucible* (body): statuses on *you* convert into armor at turn end.
- **Skills / Class**
  - *Envenomed Edge* (Poison → Bleed), *Thermal Shock* (Burn + Slow → Stun), *Venom Mace* (Poison → Acid) (exist).
  - Rule *Periodic Table*: a fixed conversion table; Hexblade (Rogue + Wizard) learns all of it.
  - Mastery *Transmutation*: conversion ratio improves from 1:1 to 1:1.5 per rank.
- **Combo**: `VENOM COAT → STAB → CATALYST → TRANSMUTE (Poison → Bleed) → OPEN VEIN`.
- **Fun / Risk**: a puzzle build with clear "aha" moments; weak alone and wants an applier partner. Strong candidate to become *the* shared conversion table in the condense pass.

### M36 · Context — "Hunter of Kinds"

- **Fantasy**: you're the specialist; you read the room (enemy prefix, region, hazard) and change shape to fit it.
- **Actions**
  - `SMITE`: +100% vs Undead / Demon; −25% otherwise.
  - `TRIP`: vs Giant, stun 1; vs others, Slow 1.
  - `TERRAIN`: copy the current room's hazard action (from EnvironmentalActions) as your next action.
  - `STUDY`: no damage; reveal the enemy's prefix mechanic and gain +10% vs that prefix for the dungeon.
- **Items**
  - *Silvered* affix: +damage vs Undead.
  - *Climber's Boots* (feet): immune to the current region's hazard; +speed in Air rooms.
  - *Giantslayer* (weapon): damage scales with enemy max HP ratio vs your own.
  - *Field Guide* (charm): each new prefix killed this dungeon gives +1% all damage.
- **Skills / Class**
  - Rule *Favored Enemy*: choose one prefix at dungeon start for +30% damage against it.
  - Rule *Adaptation*: after each room, gain resistance to the last room's hazard for the dungeon.
  - Mastery *Terrain Mastery*: TERRAIN copies at +50% and also applies to the enemy.
- **Combo**: `STUDY → SMITE → SMITE` against Undead rooms; a swap-in strip `TRIP → PUNISH` for Giants. This is the build that most wants mid-dungeon strip and item swaps.
- **Fun / Risk**: makes prefixes and regions matter; they're tag-only today, so this build needs prefix mechanics to exist first. Useless outside its niche; the swappable item slot is its answer.

### M37 · Swarm — "Swarmlord"

- **Fantasy**: you aren't the damage; your bats, rats and ants are. You command and protect them.
- **Actions**
  - `BAT SWARM`, `RAT SWARM`, `ANT SWARM` (exist).
  - `COMMAND`: all swarms act now at +25%.
  - `FEED THE SWARM`: pay 5% HP; swarms gain +1 body.
  - `SCATTER`: dismiss all swarms; each body becomes 1 stack of a status (bats → Confuse, rats → Poison, ants → Armor Break).
- **Items**
  - *Hive Charm*: swarm count +1.
  - *Pied Piper's Flute* (weapon): swarm hits count as your hits for on-hit affixes.
  - *Chitin Plate* (body): each live swarm body gives +1 armor.
- **Skills / Class**
  - Rule *Brood*: swarms persist across fights in a dungeon.
  - Rule *Queen*: your INT adds to swarm damage (Wizard-flavored summoner).
  - Mastery *Swarm Intelligence*: each kind of swarm active (bats/rats/ants) gives the others +10%.
- **Combo**: `RAT SWARM → BAT SWARM → ANT SWARM → COMMAND`, with `SCATTER` held for when the enemy is about to die (bodies become statuses that Brood can't save).
- **Fun / Risk**: an army fantasy, and per-body hits interact with every on-hit system (watch the per-hit heal hazard). Weak to area attacks if enemies ever get them.

### M38 · Self-Cost — "Martyr"

- **Fantasy**: you pay to win, and HP is a currency like any other.
- **Actions**
  - `SELF TARGET` (exists).
  - `BLOOD OATH`: pay 10% HP; +50% damage on the next action.
  - `SELF IMMOLATE`: apply 3 Burn to yourself; +3 Burn to the enemy on each of your hits while you burn.
  - `PENANCE`: Weaken yourself 2; your next 2 actions can't miss.
  - `TITHE`: pay 5% max HP; gain 3 of any mint.
- **Items**
  - *Martyr's Brand*: self-debuffs also apply to the enemy at 50%.
  - *Hair Shirt* (body): +2% damage per status effect on yourself.
  - *Scarred Gauntlets*: HP paid with self-cost actions counts as damage taken for Retaliation/RAGE.
- **Skills / Class**
  - Rule *Covenant*: HP paid returns as temp HP after the fight.
  - Rule *Penitent*: self-applied statuses last 1 turn less.
  - Mastery *Sacrament*: each self-cost action this fight lowers the next one's cost by 1% (min 2%).
- **Combo**: `BLOOD OATH → HAYMAKER → TITHE → BONE WRATH`. Pay HP twice, once for damage and once for mints, then spend the mints.
- **Fun / Risk**: makes HP a resource; dangerous at low HP. Natural pairs: Brink (M30), Sustain (M27), Retaliation (M29).

### M39 · Long Strip — "Scholar"

- **Fantasy**: more slots; each additional word in the sentence makes the whole stronger. The Wizard's natural home.
- **Actions**
  - `CONCENTRATED CHAOS` (exists): +AMP per slot beyond the first.
  - `FOOTNOTE`: 0.2 length, small hit; counts as a slot for every slot-scaling effect.
  - `INDEX`: damage +X per filled slot in the strip, whether or not they've executed yet.
  - `APPENDIX`: adds a temporary slot to the end of this combo (copy of the opener).
- **Items**
  - *Score Robe* (body): +1 strip slot.
  - *Bookmark* (charm): when a combo breaks, resume from the slot where it broke instead of slot 1.
  - *Tome* (weapon): +1 slot, −10% speed.
- **Skills / Class**
  - *Slot Mind* (exists): +INT per current action slot.
  - *Empty Fury* (exists, Barbarian): +STR per filled slot.
  - Rule *Margin Notes*: the first miss in a combo of 6+ slots is ignored.
- **Combo**: 8 slots: `PRELUDE → FOOTNOTE → FOOTNOTE → FOOTNOTE → APPENDIX → INDEX → KEY CHANGE → CODA`.
- **Fun / Risk**: long sentences with growing payoffs. Long strips are known to hurt at L75 (more chances to break). Bookmark and Margin Notes are the answers.

### M40 · Cadence — "Drummer"

- **Fantasy**: you play to a rhythm; every Nth turn lands a downbeat, and the build is about lining up big actions on it.
- **Actions**
  - `DRUM CUT` (exists): +speed per BEAT.
  - `DOWNBEAT`: triple damage on every 4th turn, half damage otherwise.
  - `SYNCOPATE`: shift the beat counter by +1 (so the downbeat lands on your next action).
  - `FERMATA`: pause; don't advance the beat this turn, and the next action gets +1 multihit.
  - `ROLL`: 4 quick hits; each counts as a beat.
- **Items**
  - Set *Bronze* (exists): BEAT per turn.
  - *Metronome Charm*: shows the beat count; every 4th turn your crit threshold drops by 2.
  - *Snare Gloves*: multihit actions on the downbeat get +1 hit.
- **Skills / Class**
  - *War Drum* (exists): DRUM CUT +speed per BEAT.
  - Rule *Polyrhythm*: track two beats (every 3rd and every 4th turn); the 12th turn fires both.
  - Mastery *Tempo Rubato*: each SYNCOPATE also grants +5% speed for the fight.
- **Combo**: `ROLL → SYNCOPATE → DOWNBEAT`: ROLL fills beats fast, SYNCOPATE lines up the downbeat, DOWNBEAT lands it.
- **Fun / Risk**: rhythm play; boring if it fires on its own, so the player (and bot) has to *line it up*. Natural pairs: Tempo (M02) and Exact Numbers (M07).

---

## Part 3 — Two-mechanic synergy builds

Each pair says *how* the two mechanics touch, using one of six verbs. The verb matters more than the pair: it's what
the condense pass will turn into shared rules.

| Verb | Meaning | Example |
|---|---|---|
| **Feeds** | A produces what B spends | Multihit → Bleed stacks |
| **Gates** | A unlocks / arms B | Stun → Punish |
| **Converts** | A's resource becomes B's | Poison → Bleed |
| **Covers** | A removes B's failure mode | Precision covers AMP's miss risk |
| **Multiplies** | A scales B's numbers | AMP × Echo |
| **Tension** | A and B pull opposite ways; the build lives in the balance | Heavy Hit vs Tempo |

Format: **Hook** (one line) / Action / Item / Skill / Combo. A dash means the category doesn't need a new piece.

### Output × Output

**P01 · M01 Heavy Hit × M02 Tempo — "Pendulum"** (Tension)
- Hook: alternate very slow and very fast actions; each swing of the pendulum charges the other.
- Action: `BACKSWING`: 0.3 length; the next action's length counts double for "heavy" damage scaling.
- Item: *Pendulum Weight* (weapon): fast actions store 10% of their speed bonus as damage for the next slow action, and vice versa.
- Skill: *Cadence Keeper* (exists) widened: alternating above/below base speed grants Focus **and** +5% damage.
- Combo: `BACKSWING → CATACLYSM → QUICKSTEP → CATACLYSM`.

**P02 · M01 Heavy Hit × M03 Multihit — "Avalanche"** (Converts)
- Hook: one huge hit shatters into many fragments.
- Action: `SHATTER BLOW`: one hit; overkill and 25% of damage split into 4 follow-up hits that proc on-hit effects.
- Item: *Fracturing Maul*: hits over 20% enemy HP add +1 multihit to the next action.
- Skill: Rule *Debris*: each extra hit inherits the stun / status chance of the original heavy hit.
- Combo: `WIND UP → SHATTER BLOW → FLURRY`.

**P03 · M02 Tempo × M04 AMP — "Accelerando"** (Multiplies)
- Hook: the faster the strip goes, the more AMP each slot carries.
- Action: `RUSH NOTE`: AMP gained this slot scales with how far below 1.0 length it is.
- Item: *Metronome Wand*: +2% AMP per action taken before the enemy acts again.
- Skill: *Metronome* (exists) on Wands too.
- Combo: `QUICKSTEP → RUSH NOTE → RUSH NOTE → CODA`.

**P04 · M03 Multihit × M05 Crit — "Spray and Pray"** (Feeds)
- Hook: more hits = more crit rolls; each crit lowers the threshold for the next hit.
- Action: `RICOCHET`: 5 hits; each crit adds a 6th, 7th… hit.
- Item: *Glass Shards* charm: each hit's crit threshold is 1 lower than the previous hit's (resets per action).
- Skill: Rule *Cascade* (from M05) applied per hit.
- Combo: `HONE THE EDGE → RICOCHET → RICOCHET`.

**P05 · M04 AMP × M14 Echo — "Hall of Mirrors"** (Multiplies)
- Hook: echoes carry the slot's AMP, so a late echo is enormous.
- Action: `REVERB` (M14) at slot 6.
- Item: *Mirror Robe*: echoes count as an extra slot for AMP.
- Skill: Rule *Harmonics* (M14).
- Combo: `PRELUDE → SUSTAIN → SUSTAIN → ECHO SPELL → BOLT`.

**P06 · M05 Crit × M06 Precision — "Perfect Form"** (Covers / Tension)
- Hook: precision builds "margin"; margin above 20 becomes crit chance.
- Action: `SURE STRIKE` (M06) that crits if margin ≥ 8.
- Item: *Plumb Edge*: every 3 points of margin lowers crit threshold by 1 this action.
- Skill: *Perfect Aim* (exists).
- Combo: `STEADY → SURE STRIKE → SURE STRIKE`.

### Dice × anything

**P07 · M07 Exact Numbers × M08 Fate Control — "Card Counter"** (Gates)
- Hook: control the dice to land the magic numbers on purpose.
- Action: `NUDGE`: change your next roll by ±2 (you choose — the bot picks the best).
- Item: *Abacus* (M07) + *Weighted Die* (M08).
- Skill: *Snake Eyes* (exists): 2s and 12s → Loaded → replace with 20.
- Combo: `PARITY BLADE → NUDGE → SUM (hit 21)`.

**P08 · M08 Fate Control × M09 Failure Engine — "Rigged Loss"** (Tension)
- Hook: deliberately take bad rolls to bank good ones.
- Action: `THROW THE MATCH`: auto-miss; bank this turn's natural roll + 5 for later.
- Item: *Loaded Gloves*: every miss adds +1 to your banked roll.
- Skill: Rule *Fate Debt* (M08) inverted: misses pay down the debt.
- Combo: `THROW THE MATCH → THROW THE MATCH → PALM (use banked 20+) → SHATTERPOINT`.

**P09 · M09 Failure Engine × M29 Retaliation — "Sore Loser"** (Feeds)
- Hook: missing and getting hit both build the same anger meter.
- Action: `TANTRUM`: damage scales with misses + hits taken this fight.
- Item: *Bone ×3* with RAGE also minted by misses.
- Skill: Rule *Spite*: ONAFTERMISS and ONTAKEHIT share one counter.
- Combo: `RECKLESS SWING → TAUNT → TANTRUM`.

**P10 · M07 Exact Numbers × M16 Poison — "Odd Venom"** (Gates)
- Hook: odd rolls apply poison, even rolls detonate it.
- Action: `PARITY FANG`: odd = +3 Poison; even = consume Poison as damage.
- Item: *Odd Gloves*.
- Skill: *Grease Palm* (exists) for low odds.
- Combo: `PARITY FANG ×4` (luck decides apply vs cash; fate control steers).

**P11 · M08 Fate Control × M05 Crit — "Loaded Edge"** (Covers)
- Hook: crit fishing with a safety net.
- Action: `LOADED DICE` (exists) + `SHATTERPOINT`.
- Item: *Weighted Die*.
- Skill: *Flow State* (exists).
- Combo: `STAB → STAB → LOADED DICE → SHATTERPOINT`.

### Combo structure × anything

**P12 · M10 Chain Length × M12 Finisher — "Long Fuse"** (Feeds)
- Hook: every link adds to the finisher; a long chain makes a guillotine.
- Action: `GUILLOTINE` (M12).
- Item: *Chain Shirt* (M10).
- Skill: Rule *Last Word* (M12).
- Combo: `LINK → JAB → LINK → JAB → GUILLOTINE`.

**P13 · M11 Opener × M14 Echo — "Second Ambush"** (Multiplies)
- Hook: retrigger the opener so it's a first hit twice.
- Action: `RETRIGGER OPENER` (exists).
- Item: *Assassin's Hood* (M11).
- Skill: Rule *Every Fight Is New* (M11).
- Combo: `FROM THE SHADOWS → JAB → RETRIGGER OPENER`.

**P14 · M13 Strip Manipulation × M12 Finisher — "Encore"** (Gates)
- Hook: jump straight to the finisher when the setup lands; loop it if it kills.
- Action: `STRIP JUMP` (exists) conditioned ONCRITICAL → finisher.
- Item: *Clockwork Gauntlet* (M13).
- Skill: Rule *Program* (M13).
- Combo: `STAB (crit → jump) → … → GUILLOTINE`.

**P15 · M15 Tag Grammar × M18 Burn — "Elemental Sentence"** (Converts)
- Hook: repeat Fire to stack Spell Memory; switch to Ice to cash it as Thermal Shock.
- Action: `FROST BOLT` (exists), `KINDLE` (exists).
- Item: *Lexicon* (M15).
- Skill: *Spell Memory* (exists) + *Thermal Shock* (exists).
- Combo: `KINDLE → KINDLE → KINDLE → FROST BOLT`.

**P16 · M39 Long Strip × M04 AMP — "Epic Poem"** (Multiplies)
- Hook: Wizard's extra slots exist to make AMP mean something.
- Action: `FOOTNOTE` (M39).
- Item: *Score Robe* (M04/M39).
- Skill: *Concentrated Chaos* + *Slot Mind* (exist).
- Combo: 8 slots: `PRELUDE → FOOTNOTE ×5 → KEY CHANGE → CODA`.

**P17 · M40 Cadence × M02 Tempo — "Drumline"** (Tension)
- Hook: beats come per turn, so faster actions mean more turns, more beats, smaller hits.
- Action: `DOWNBEAT` (M40).
- Item: *Bronze ×5*.
- Skill: *War Drum* (exists).
- Combo: `QUICKSTEP → QUICKSTEP → QUICKSTEP → DOWNBEAT`.

**P18 · M13 Strip Manipulation × M09 Failure — "Error Handler"** (Covers)
- Hook: a miss redirects the strip instead of breaking it.
- Action: `CATCH`: ONMISS → jump to this slot; this slot heals and resets.
- Item: *Escapement* (M13).
- Skill: Rule *Debugger* (M13).
- Combo: `RECKLESS SWING → RECKLESS SWING → CATCH → FINISHER`.

### Status × Status (conversion web)

**P19 · M16 Poison × M17 Bleed — "Toxic Ledger"** (Converts)
- Hook: Poison is the slow bank, Bleed is the spend.
- Action: `TRANSMUTE` (M35) fixed to Poison → Bleed.
- Item: *Envenomed Edge* as a dagger affix.
- Skill: *Envenomed Edge*, *Toxin Pressure* (exist).
- Combo: `VENOM COAT → STAB → OPEN VEIN → TOURNIQUET`.

**P20 · M18 Burn × M20 Slow — "Thermal Shock"** (Converts)
- Hook: exists as a skill; expand it into a full build.
- Action: `FROST BOLT` + `KINDLE` (exist).
- Item: *Cinder Wand* + *Lead Chain*.
- Skill: *Thermal Shock* (exists).
- Combo: `KINDLE → STOKE → FROST BOLT (→ Stun) → PUNISH THE HELPLESS`.

**P21 · M17 Bleed × M22 Expose — "Hemorrhage"** (Feeds)
- Hook: every Bleed tick exposes; exposure lets bleed crit.
- Action: `OPEN VEIN` + `CRACK OPEN`.
- Item: *Butcher's Apron*.
- Skill: *Hemorrhage* (exists) + *Arterial* (M17).
- Combo: `OPEN VEIN → TOURNIQUET → CRACK OPEN`.

**P22 · M19 Stun × M24 Mark — "Helpless Prey"** (Gates)
- Hook: a stunned target's mark stores double.
- Action: `DEEP MARK` + `DAZE`.
- Item: *Concussion Maul*.
- Skill: Rule: marks store 2× while the target can't act.
- Combo: `DEEP MARK → DAZE → DAZE → DAZE → FINISHER`.

**P23 · M20 Slow × M17 Bleed — "Drawn Out"** (Tension)
- Hook: Slow means fewer enemy actions, but Bleed ticks on enemy actions. Choose: slow for safety or haste them for bleed.
- Action: `PROVOKE BLOOD` (haste) vs `MOLASSES` (slow); a build might carry both and pick by HP.
- Item: *Hourglass*.
- Skill: Rule *Clotting*: Bleed on a slowed enemy ticks on its action **start and end**.
- Combo: `OPEN VEIN → MOLASSES → (Clotting doubles ticks)`.

**P24 · M21 Weaken × M29 Retaliation — "Whittle"** (Covers)
- Hook: weaken lets you safely invite hits that fuel RAGE.
- Action: `SAP` + `TAUNT`.
- Item: *Thorn Mail*.
- Skill: *Wrath Unbound* (exists).
- Combo: `WEAKEN → TAUNT → BONE WRATH`.

**P25 · M22 Expose × M23 Pierce — "Can Opener"** (Tension)
- Hook: both answer armor; together, overflow pierce converts to Bleed (Through and Through) and Expose turns it into crits.
- Action: `SUNDER` + `RUN THROUGH`.
- Item: *Crowbar*.
- Skill: *Through and Through* (M23).
- Combo: `SUNDER → SUNDER → RUN THROUGH`.

**P26 · M25 Disrupt × M35 Conversion — "Gaslight"** (Converts)
- Hook: Confused enemies' self-hits apply their own statuses to themselves, which you then convert.
- Action: `CONFUSE` + `TRANSMUTE`.
- Item: *Mirror Cloak* (M25).
- Skill: Rule *Chaos Theory* (M25).
- Combo: `CONFUSE → (enemy self-hits) → TRANSMUTE → PURGE`.

### Defense × Offense

**P27 · M26 Bulwark × M01 Heavy Hit — "Juggernaut"** (Converts)
- Hook: armor becomes damage.
- Action: `SHIELD BASH` (M26).
- Item: *Tower Plate*.
- Skill: *Steel Knuckle* (exists) + *Bulwark Strike* (M26).
- Combo: `HARDEN → BRACE → SHIELD BASH`.

**P28 · M27 Sustain × M30 Brink — "Edge Walker"** (Tension)
- Hook: heal back to the brink, never above it; lifesteal capped by Deathwish turns into temp HP.
- Action: `LAST STAND` + `LEECHING CUT`.
- Item: *Bloodied Helm*.
- Skill: *Deathwish* + *Overflow*.
- Combo: `LEECHING CUT → LAST STAND`.

**P29 · M28 Shield × M29 Retaliation — "Spiked Barrier"** (Feeds)
- Hook: damage absorbed by temp HP is reflected.
- Action: `ABSORB` (M28).
- Item: *Capacitor Plate* + *Thorn Mail*.
- Skill: Rule *Conservation* (M28).
- Combo: `BARRIER → TAUNT → SHIELD SLAM`.

**P30 · M38 Self-Cost × M27 Sustain — "Blood Bank"** (Feeds)
- Hook: pay HP for power; heal it back with lifesteal from the powered hit.
- Action: `BLOOD OATH` + `TRANSFUSION`.
- Item: *Vampire Fang*.
- Skill: *Covenant* (M38).
- Combo: `BLOOD OATH → HAYMAKER → TRANSFUSION`.

**P31 · M26 Bulwark × M14 Echo — "Living Alloy"** (Converts, exists as a rule)
- Hook: crits consume Harden to repeat the slot.
- Action: `HARDEN`.
- Item: *Bone ×2 / Steel ×2*.
- Skill: *Living Alloy* (exists).
- Combo: `HARDEN → HARDEN → CRIT FACES → SLAM`.

**P32 · M30 Brink × M05 Crit — "Desperado"** (Multiplies)
- Hook: below 30% HP, every 18+ is a crit and crits heal.
- Action: `ENRAGE` + `CRIT HEAL` (exists).
- Item: *Bloodied Helm*.
- Skill: *Perfect Aim* (exists).
- Combo: `ENRAGE → STAB → STAB → SHATTERPOINT`.

### Scaling × anything

**P33 · M31 Snowball × M24 Mark — "Bounty Board"** (Feeds)
- Hook: detonations grant permanent dungeon stats; a long dungeon is a payday.
- Action: `TRANSFER MARK` (M24).
- Item: *Bounty Ledger*.
- Skill: *Contract* (M24).
- Combo: `DEEP MARK → … → FINISHER`.

**P34 · M32 Material Count × M15 Tag Grammar — "Etymology"** (Converts)
- Hook: worn materials count as action tags; matching sets keep Spell Memory going.
- Action: `CONJUGATE` (M15).
- Item: *Willow ×5*.
- Skill: *Etymology* (M15).
- Combo: `WILLOW LASH → BOLT → WILLOW LASH`.

**P35 · M33 Condition × M38 Self-Cost — "Scrap Heap"** (Feeds)
- Hook: actions damage your own gear for power; broken gear powers Scrap Prefer.
- Action: `SACRIFICE GEAR`, `OVERSTRAIN`: −1 condition on a worn item, +100% damage.
- Item: *Patchwork Armor*.
- Skill: *Scrap Prefer* (exists).
- Combo: `OVERSTRAIN → HAYMAKER → SACRIFICE GEAR`.

**P36 · M34 Mint × M12 Finisher — "Payday"** (Gates)
- Hook: the finisher spends every mint you hold.
- Action: `LIQUIDATE` (M34).
- Item: *Vault Belt*.
- Skill: *Synthesis* masteries (exist).
- Combo: `(mint) → (mint) → LIQUIDATE`.

**P37 · M35 Conversion × M34 Mint — "Assayer"** (Converts)
- Hook: statuses become mints; mints become statuses.
- Action: `ASSAY`: convert stacks of a status into the matching material mint.
- Item: *Alchemist's Gloves*.
- Skill: *Venom Mace* (exists) generalised.
- Combo: `VENOM COAT → STAB → ASSAY → IRON CULL`.

**P38 · M36 Context × M31 Snowball — "Trophy Room"** (Feeds)
- Hook: each new enemy prefix you kill this dungeon gives a permanent bonus.
- Action: `SMITE` (M36).
- Item: *Trophy Necklace* (M31).
- Skill: *Favored Enemy* (M36).
- Combo: any.

**P39 · M37 Swarm × M03 Multihit — "Hive Mind"** (Multiplies)
- Hook: each swarm body counts as a hit for on-hit effects.
- Action: `COMMAND` (M37).
- Item: *Hive Charm* (M37).
- Skill: Rule *Brood* (M37).
- Combo: `RAT SWARM → BAT SWARM → COMMAND`.

**P40 · M37 Swarm × M16 Poison — "Plague Rats"** (Feeds)
- Hook: rats apply poison per bite.
- Action: `RAT SWARM` (exists) + `PURGE`.
- Item: *Plague Mask*.
- Skill: *Virulence* (M16).
- Combo: `RAT SWARM → RAT SWARM → PURGE`.

### Odd couples (looks like anti-synergy; the build is the bridge)

**P41 · M06 Precision × M09 Failure Engine — "Calculated Risk"**
- Hook: you choose *when* to miss: Precision makes hits reliable, so misses are deliberate (actions that auto-miss) and charge.
- Action: `FEINT`: auto-miss that doesn't break STREAK.
- Item: *Plumb Line* + *Jester's Bells*.
- Skill: *Steady Hand* exempts FEINT from resets.
- Combo: `SURE STRIKE → FEINT → SURE STRIKE → FEINT → SECOND WIND STRIKE`.

**P42 · M01 Heavy Hit × M39 Long Strip — "Slow Epic"**
- Hook: few long actions in a long strip; Empty Fury (exists) rewards filled slots while CATACLYSM rewards length.
- Action: `CATACLYSM`.
- Item: *Score Robe*.
- Skill: *Empty Fury* (exists).
- Combo: 6 slots of heavies.

**P43 · M02 Tempo × M26 Bulwark — "Hit and Run"**
- Hook: speed is defense; actions under 0.6 length grant Harden.
- Action: `SIDESTEP`: 0.3 length, +1 Harden.
- Item: *Winged Sandals*.
- Skill: Rule *Evasive Plate*: Harden from speed decays at turn end but stacks infinitely within a turn.
- Combo: `SIDESTEP → SIDESTEP → SHIELD BASH`.

**P44 · M27 Sustain × M29 Retaliation — "Bloodbath"**
- Hook: you want to be hit and heal from it; reflected damage heals.
- Action: `RIPOSTE`.
- Item: *Thorn Mail* + *Vampire Fang*.
- Skill: Rule: reflected damage counts as lifesteal-able.
- Combo: `TAUNT → RIPOSTE → TRANSFUSION`.

**P45 · M11 Opener × M12 Finisher — "Bookends"**
- Hook: the opener stores, the finisher cashes; middle slots are filler.
- Action: `CLASP`: opener; the finisher repeats the opener's statuses.
- Item: *Bookend Pair* (two-piece set): opener and finisher +50% if both are worn.
- Skill: Rule *Circle*: finisher counts as an opener for the next combo.
- Combo: `CLASP → JAB → JAB → GUILLOTINE`.

**P46 · M19 Stun × M02 Tempo — "Time Thief"**
- Hook: while the enemy is stunned, your actions are free.
- Action: `PUNISH THE HELPLESS`.
- Item: *Earplugs*.
- Skill: Rule *Stolen Time*: actions taken during an enemy stun cost half length.
- Combo: `DAZE ×3 → QUICKSTEP → QUICKSTEP → PUNISH`.

**P47 · M24 Mark × M14 Echo — "Echoed Bounty"**
- Hook: echoes add to the mark's stored damage; detonation itself echoes.
- Action: `REVERB` + `DEEP MARK`.
- Item: *Mirror Shield*.
- Skill: *Harmonics* (M14).
- Combo: `DEEP MARK → REVERB → BOLT → FINISHER`.

**P48 · M40 Cadence × M07 Exact Numbers — "Clockwork Numbers"**
- Hook: the turn counter is a number too; turn N + natural roll = 21 retriggers.
- Action: `SYNC`: adds the turn number to your next roll for exact-number checks.
- Item: *Abacus*.
- Skill: *Grand Equation* (exists) using turn instead of slot.
- Combo: `SYNC → PARITY BLADE → SUM`.

**P49 · M32 Material Count × M33 Condition — "Heirloom vs Scrapper"**
- Hook: matching sets get stronger at extreme conditions (all Broken or all Perfect), not in between.
- Item: *Broken Bone ×5* / *Perfect Silver ×5*.
- Skill: *Scrap Prefer* / *Pure Prefer* (exist).

**P50 · M36 Context × M25 Disrupt — "Haunt the Haunted"**
- Hook: Undead / Giant prefixes get special disrupts (Undead: turn, Giant: trip).
- Action: `TURN UNDEAD`, `TRIP`.
- Item: *Holy Symbol*.
- Skill: *Favored Enemy*.

---

## Part 4 — Pair shortlist (one-liners for the rest)

Pairs not built out above, by verb. Each is a seed for a condensed rule rather than a full build.

| Pair | Verb | One-liner |
|---|---|---|
| M03 × M17 | Feeds | Multihit applies Bleed per hit (Dagger Mastery exists). |
| M03 × M27 | Multiplies | Per-hit heals scale with multihit — the known balance hazard; cap per action. |
| M04 × M18 | Multiplies | AMP scales Burn applied; Burn ticks feed AMP (Ember Robe). |
| M05 × M22 | Gates | Crits Expose; Exposed lowers crit threshold (feedback loop, needs a cap). |
| M05 × M33 | Feeds | Glass items lose condition per crit and shatter into new loot. |
| M06 × M23 | Converts | Hit margin becomes pierce (Needle). |
| M06 × M10 | Covers | Precision keeps long chains alive. |
| M07 × M40 | Gates | Even turns / odd turns flip which parity bonus is active. |
| M08 × M12 | Covers | Bank a 20 for the finisher. |
| M09 × M28 | Feeds | Misses grant temp HP ("stumble behind your shield"). |
| M10 × M40 | Tension | Long chains vs per-turn beats. |
| M11 × M19 | Gates | The opener stuns; the fight's first enemy action is lost. |
| M11 × M31 | Feeds | First hit of each room grants a dungeon stack. |
| M12 × M28 | Converts | Finisher converts leftover temp HP into damage. |
| M13 × M15 | Covers | Loops don't count as tag repeats (Escapement). |
| M13 × M39 | Multiplies | Long strips give jump/skip more targets. |
| M14 × M16 | Multiplies | Echoes re-apply Poison. |
| M14 × M34 | Feeds | Echoes mint currency again. |
| M15 × M22 | Converts | Technical Debt (exists): varied tags cash into Expose. |
| M16 × M27 | Converts | Poison ticks heal (Plague Mask). |
| M16 × M31 | Feeds | Virulence: poison carries between fights. |
| M17 × M29 | Feeds | Enemy attacks trigger Bleed and Retaliation together. |
| M18 × M38 | Converts | Burn yourself for AMP; Cauterize heals. |
| M19 × M21 | Converts | Resisted stuns become Weaken. |
| M20 × M02 | Converts | Slow on the enemy becomes speed for you (Hourglass). |
| M20 × M40 | Multiplies | Slowed enemies give you more beats per enemy action. |
| M21 × M01 | Feeds | SAP steals STR for heavy hits. |
| M22 × M03 | Feeds | Multihit Armor Break (Crowbar). |
| M23 × M17 | Converts | Pierce overflow becomes Bleed. |
| M24 × M12 | Gates | Finisher detonates marks. |
| M25 × M06 | Tension | Lower enemy accuracy vs raising your own; a shared "accuracy war" stat. |
| M26 × M10 | Covers | Harden keeps combos from being reset by hits. |
| M27 × M31 | Feeds | Kill heals (Blood Feast) keep a snowball alive. |
| M28 × M01 | Converts | Shield Slam. |
| M29 × M34 | Feeds | RAGE (exists). |
| M30 × M34 | Feeds | CRISIS on hero HP thresholds. |
| M30 × M38 | Multiplies | Pay HP to enter Brink deliberately. |
| M31 × M33 | Feeds | Room clears repair one item's condition. |
| M32 × M39 | Feeds | Each set piece of a Wizard material adds a slot at 5. |
| M33 × M36 | Gates | Heirloom items do bonus damage to Demons. |
| M35 × M18 | Converts | Thermal Shock (exists). |
| M35 × M21 | Converts | Weaken on a Poisoned target turns into Vulnerability. |
| M36 × M18 | Gates | Burning in the Fire region spreads; Air region doubles Slow. |
| M37 × M24 | Feeds | Swarms mark targets; the hero detonates. |
| M37 × M26 | Covers | Swarms take hits for you (bodies as armor). |
| M38 × M33 | Converts | Sacrifice item condition instead of HP. |
| M39 × M09 | Tension | Long strips break more; Fail Forward lets them survive misses. |
| M40 × M29 | Feeds | Every 4th enemy hit grants a free RIPOSTE. |

---

## Part 5 — Seeds for the condense pass (observations only)

Things that show up again and again above, likely to become shared primitives:

1. **Charge → Spend**: VENOM COAT/PURGE, KINDLE/FLASHPOINT, MARK/detonate, BARRIER/SHIELD SLAM, mints/converts,
   PALM, banked RAGE. One generic "bank X, spend X" shape with different X would replace roughly 20 of the actions above.
2. **Next-action modifiers**: WIND UP, HEFT, QUICKSTEP, BACKSWING, BLOOD OATH, BRACE: all "the next action gets +Y".
   The `hero_next_action_*` trigger mechanics already cover this; actions could just be (cost, next-action effect).
3. **Per-X scaling**: per hit, per slot, per material, per miss, per hit taken, per room, per stack. A single
   "scales with counter C" field plus a counter registry would cover most payoff actions (GRUDGE, TANTRUM, CHAIN SNAP,
   CADENCE BREAK, SECOND WIND STRIKE, LAST STAND).
4. **Conversion table**: Poison→Bleed, Burn+Slow→Stun, Harden→repeat, Armor→damage, temp HP→Fortify,
   misses→currency. One data table of (from, to, ratio, trigger) instead of bespoke skills.
5. **Event unification**: ONMISS + ONTAKEHIT share a counter (Spite); ONCRITICAL arms strip ops; detonation on
   finisher. Many skills are just "trigger X now also fires on Y".
6. **Category placement**: conditions ("while Harden", "below 30% HP", "vs Undead") cluster naturally on items;
   rule rewrites (counts-as, shares-a-counter, doesn't-break-combo) cluster on skills; spends cluster on actions.
7. **Tension pairs make the best builds**: P01, P08, P17, P23, P28, P41: two mechanics pulling opposite ways with
   a bridge piece. Worth keeping a bridge item/skill per tension pair even after condensing.
