# Affix pass: purpose, item-level anchoring, level targets (proposal, 2026-11-15)

Status: **section 4b built 2026-11-15** (stat affix vocabulary, no-repeat split, hybrids, Pack); sections 3 (animal procs
retired by 4b instead), 4 (level targets) and the on/off-family values are still proposals. Follows the item generation redesign
([ITEM_GENERATION_REDESIGN_2026-11-14.md](ITEM_GENERATION_REDESIGN_2026-11-14.md), handoff
[ITEM_GENERATION_HANDOFF_2026-11-14.md](ITEM_GENERATION_HANDOFF_2026-11-14.md)). Numbers are from the seed-1
10k roll at 584aa1ac (ITEMSTATS --dump, levels 1-100, 100 per level).

## 1. What the affixes are today

| Group | Families | Tiers | Item-level anchors | Value grows with tier? | Pool weight share |
|---|---|---|---|---|---|
| Stat suffixes (of the Ox…) | 13 | 7 + 7 Uncanny | 1 / 6 / 14 / 25 / 40 / 60 / 80 | yes (×0.5 … ×3.4 of base) | stat pool |
| Stat prefixes (Sharp…) | 10 | 7 + 7 Uncanny | same | yes | stat pool |
| Quality (Worn … Heirloom) | 1 | 8 | 1 … 80 | yes | stat pool |
| DoT prefixes (Burning, Venom, Bleeding) | 3 | 5 | 3 / 12 / 25 / 45 / 70 | yes | 3% of mechanic |
| Rule prefixes (Vampiric, Thorned…) | 28 | 3 | 8 / 30 / 60 | yes ({n} in the rule) | 6% of mechanic |
| Material pool (Listening, Attuned, Doubled, Charged-Up, 18 cross-wires) | 22 | 3 | 1 / 25 / 55 | **only Charged-Up**; the rest are on/off, so their tiers change nothing | material pool |
| Animal suffixes | 120 | **1** | 5 | **no** (a heal 2 at L90 is still 2) | 91% of mechanic |
| Extra action slot | 1 | 1 | 20 | no | — |

What the roll shows by level:

| Item level | Lines / item | Stat | Mechanic | Material | Action | Avg stat tier |
|---|---|---|---|---|---|---|
| 1-20 | 2.32 | 0.76 | 0.24 | 0.20 | 1.12 | 2.5 |
| 21-40 | 2.40 | 0.73 | 0.28 | 0.22 | 1.17 | 4.7 |
| 41-60 | 2.42 | 0.74 | 0.28 | 0.19 | 1.21 | 6.2 |
| 61-80 | 2.47 | 0.71 | 0.29 | 0.21 | 1.25 | 7.5 |
| 81-100 | 2.51 | 0.73 | 0.28 | 0.20 | 1.30 | 8.6 |

So an item at L90 has the same kinds and counts of lines as one at L5; only stat tiers, base damage / defense and
implicits grow. Nothing was ever set as a target for these numbers; they fall out of the weights.

**Measurement gap:** the sim bot's gear score (`ItemValueModel.Score`) counts stat lines, actions and adjective
prefixes, but animal suffixes score 0 (their `StatType` is empty), and `RulesSynergy` only prices the affix
templates. So DUNGEONSIM can't tell us whether animals matter until the bot can value them.

## 2. What affixes are for (proposed roles)

Every line should do one of four jobs, and the pool it lives in should say which:

1. **Power: "number goes up".** Stat lines. Scale with item level, are the bulk of lines, and are what makes a
   new drop better than the old one.
2. **Set glue.** Material lines (mint, tune, double count, cross-wire): make a material set stronger or let two
   sets talk. Worth something only next to the right set, which is the point: they make drops "talk" to the build.
3. **Identity.** Rule prefixes, DoTs, animals: a visible effect that changes how a fight plays. One per item at
   most is plenty.
4. **Content.** Actions: new things for the combo strip.

Rules that follow from this:

- **Every family is anchored on item level.** A family is a value curve `value(ilvl)` plus keyword bands, not a
  hand ladder. A family that is on/off (Doubled, a cross-wire) either gets a real number that grows (cross-wire
  50% → 75% → 100%, Listening mints 1 → 2) or collapses to a single tier so its tiers don't pretend.
- **A line that talks to a build names what it talks to** (material, class, animal group, action tag). The
  influence tilt already does this for rolling; the measure in §4 checks it.
- **Identity lines stay rare and readable.** If a player can't tell what an effect does from one line, it's too
  much information for how often it shows up.

## 3. Animals, simplified

Today: 120 one-tier animals, each with its own proc, and a 3-rung set ladder (any 2 animals +3% speed; 2 of one
group get that group's bonus; 3 of the same animal ×1.5; a charm layer amplifies it). The third rung needs ~1 in
500 drops three times over, and the ladder is a lot to read for what it gives.

**Proposal: one rule, "Pack".** Wearing 2+ pieces whose animals share a group (Beast, Bird, Bug, Shell,
Reptile, Fish, Mythic) **doubles those animals' own effects**: numbers ×2, statuses apply 2 stacks, strip moves
happen twice. No separate group bonus numbers, no same-animal rung, no general speed rung. The tooltip says
`Bird pack ×2`. A 2-piece Beast pack is about 1 drop in 11 per piece today, so it is reachable.

Also:
- Animal procs scale with item level the same way implicits now do (`ItemImplicitScaling`): percent and flat values
  grow, discrete ones don't.
- Animals get **their own pool** with their own weight, so their share stops coming out of DoTs and rules; the
  family weight goes back to 2.
- The bot learns to value animal lines (and packs), or sims can't measure any of this.
- Open: keep the charm's animal layer (e.g. a charm makes the pack ×3), or drop it.

## 4. Distribution by level: set targets, then measure

Same loop as the balance work (`balance-targets.json` → measure → solve), for items:

1. **Targets file** `GameData/TuningProfiles/item-targets.json`: per level band (the 5-level region bands, or
   1-20 / 21-40 / …), a target and tolerance for each metric:
   - lines per item by rarity, and the pool mix (stat / material / mechanic / animal / action);
   - share of drops with at least one line that talks to the hero's class or worn material (build relevance);
   - drops needed, on average, to complete a 2-piece material set and a 2-piece animal pack;
   - item power by band vs the hero-level curve (how much better a fresh drop is than gear 5 levels old);
   - Uncanny share of stat lines; identity lines per item;
   - rarity mix (flat: static all game).
2. **Measure**: `ITEMSTATS --dump` → `Scripts/item-stats.py` already makes most of these tables; add the missing
   ones (build relevance, drops-to-set, power vs curve) and an `item-check` step that prints pass / off per metric
   per band, like `balance-loop.py measure`.
3. **Dashboard**: the Item Roller 10k page shows each metric against its target band.
4. **Solve**: the knobs are pool weights, family weights, rarity line counts, tier weights and the per-level value
   curves. Item rolls take under a second for 10k, so a plain search over the knobs against the targets is cheap
   (no sims needed for this layer). Fight impact is still judged later by DUNGEONSIM.

## 4b. Reining in the vocabulary (user direction 2026-11-15)

User: animals are a theme, their number doesn't matter; what matters is a set of affixes that map correctly and read
as one cohesive name. Today the system has **198 families, 616 tier rows and 508 distinct names**, and 8 levers are
rolled by two families each (one prefix, one suffix):

| Lever | Prefix family | Suffix family |
|---|---|---|
| Damage | Brutality (Sharp…) | Damage (of Thorns…) |
| Defense | Toughness (Padded…) | Defense (of Hide…) |
| Health | Vitality (Hale…) | Life (of the Sprout…) |
| Hit | Aim (Steady…) | Accuracy (of Aim…) |
| Crit | Menace (Long…) | Critical (of Edges…) |
| Combo | Poise (Balanced…) | Combo (of Rhythm…) |
| Speed | Swiftness (Light…) | Attack Speed (of Haste…) |
| Magic find | Fortune (Enchanted…) | Magic Find (of Coins…) |

**Proposed grammar:** `[Uncanny] [Prefix] [Material] [Base] of the [Tier] [Animal]`, e.g.
"Keen Iron Sword of the Dire Bear".

- **One lever = one family = one word.** 16 stat levers, each in exactly one slot:
  - **Prefix = how it fights** (adjective): Damage, Hit, Crit, Combo, Speed, Multi-hit, Amp, Quality (8). Identity
    prefixes (rules, DoTs) and material lines are prefixes too, one word each.
  - **Suffix = what it's made of** (animal): STR, AGI, TEC, INT, Health, Defense, Regen, Magic find (8 animals,
    e.g. Bear = STR, Fox = AGI, Spider = TEC, Owl = INT, Ox = Health, Tortoise = Defense, Salamander = Regen,
    Magpie = Magic find).
- **Tier = one shared ladder**, not a word per tier per family: 7 words used by every animal (e.g. Young, —, Great,
  Dire, Elder, Ancient, Primal) → "of the Bear" … "of the Primal Bear". Prefix words stay fixed (an adjective
  ladder in front of a prefix reads badly); the tooltip shows the tier.
- **Uncanny** stays the one special marker (common-only tier above the normal one).
- **Tooltip carries the logic:** `Bear · Strength · tier 4/7 · 41 (36–51)`, so the name only has to say what and
  roughly how strong.
- **Animal sets become simple:** the separate 120 one-tier animal procs retire; the animal *is* the stat family.
  "Pack" = 2+ pieces with the same animal double that animal's lines (one rule, readable from the names).

### Suffixes never repeat a prefix (user, 2026-11-15)

A suffix may relate to or amplify a prefix lever, but must not land in the same combat number. What each one does
today (from the code):

| Today | Combat effect | Same number as |
|---|---|---|
| prefix ARMOR / suffix Armor | % of the piece's armor, one shared sum | each other (exact) |
| prefix HEALTH BONUS / suffix Health | % of the HP reference, one sum | each other (exact) |
| prefix HIT, COMBO, CRIT / suffix HIT, COMBO, CRIT | flat threshold shifts, one sum | each other (exact) |
| prefix MAGIC FIND / suffix MagicFind | loot rolls only | each other (one total) |
| prefix DAMAGE (flat) / suffix Damage (% of a reference) | base damage before the STR multiplier | each other |
| prefix SPEED (%) / suffix AttackSpeed (points) | swing time, two divisors that multiply | each other |
| suffix STR | whole-swing damage multiplier (amplifies flat damage) | — |
| suffix AGI | swing-time curve; past the cap, speed overflow turns into damage | — |
| suffix TEC | milestone steps: TEC 10/15/20/25/35 hit, 30-85 combo, 50-100 crit | — |
| suffix INT | AMP curve and a second AMP multiplier; INT 10+ makes the strip run in order | — |
| suffix HealthRegen, extra action slot | HP per turn; combo slots | — |
| prefix MULTI-HIT, AMP | extra hits; × the AMP base | — |

**Split:**

- **Prefix = the direct numbers of a swing:** Damage (flat), Hit, Crit, Combo (threshold shifts), Speed (%),
  Multi-hit, Amp, plus Quality and the one-word identity / material prefixes.
- **Suffix = the body that scales those numbers:** STR (multiplies damage), AGI (speed curve and its overflow into
  damage), TEC (threshold milestones), INT (AMP layers and ordered strip), Health, Defense, Regen, Magic find,
  extra action slot.
- Each suffix is *related* to a prefix (STR ↔ Damage, AGI ↔ Speed, TEC ↔ Hit/Crit/Combo, INT ↔ Amp) but works
  through its own formula, so the two multiply instead of adding into one total. Health and Defense move to
  suffix only; the suffix Damage / Speed / Hit / Combo / Crit / Magic-find families and the prefix Armor / Health /
  Magic-find families go away.

### Lines with more than one stat

The old pipeline had 30 multi-stat "story" suffixes ("bound by shame": STR + Damage + Hit; "stitched by a
mother": Health + Regen); the redesign stopped rolling them, so no line gives two stats today. Proposal: hybrids
are allowed in **both** slots, but a hybrid only mixes levers from its own slot, so the no-repeat rule holds:

- **Suffix hybrids = hybrid creatures** using the same tier ladder: Griffin (STR + AGI), Minotaur (STR + Health),
  Sphinx (INT + TEC), Basilisk (TEC + Defense), Phoenix (Regen + INT), Kraken (Health + Defense)…
  "of the Dire Griffin".
- **Prefix hybrids = one adjective for two swing numbers:** Savage (Damage + Crit), Flowing (Combo + Speed),
  Precise (Hit + Crit), Resonant (Amp + Multi-hit)…
- Each part of a hybrid rolls about 60% of a single-stat line at the same tier, so a hybrid is broader, not
  stronger. A cross-slot mix (STR + Damage) is what an item with both a prefix and a suffix already is.
- Rough size: ~6 suffix hybrids and ~4 prefix hybrids, picked for builds we want, not every pair.

Found on the way: when Hit or Combo is stuck at its floor, the leftover spills into Crit 1:1, although the code
comment says half rate (`ActionExecutionFlow.Selection.cs:40-62`).

**Size after:** 16 stat families × 7 tiers (+ Uncanny) ≈ 224 tier rows but only ~32 words to learn (8 adjectives,
8 animals, 7 tier words, Uncanny, and Quality's own ladder), plus the identity / material prefixes (~50, one word
each). Roughly **80 names instead of 508**.

## 5. Suggested order

1. Agree the roles (§2) and the targets (§4): the targets are design calls, not something to solve.
2. Build the targets file + `item-check`, measure today's roll against it.
3. Animal pass (§3): own pool, Pack rule, proc scaling, bot valuation.
4. Item-level anchoring pass: give on/off families real values or one tier; per-family value curves.
5. Fit the knobs to the targets with the fast roll loop; then the DUNGEONSIM balance pass.

## 6. Decisions needed

- Pack rule as written (double effects at 2 of a group), or something else?
- Charm animal layer: keep (×3 pack) or drop?
- Animals in their own pool?
- Level bands for targets: 5-level region bands, or 20-level bands?
- First target values: e.g. "1 build-relevant line on 1 drop in 3", "a 2-piece material set within ~10 drops",
  "a fresh drop beats 5-levels-old gear by ~15%". Rough picks are fine; they can move.

## 7. As built (2026-11-15)

- `Scripts/build-item-mods.py` makes 80 families (was 198): 8 animal suffixes (Bear STR, Fox AGI, Spider TEC,
  Owl INT, Ox Health, Tortoise Defense, Salamander Regen, Magpie Magic find) on `TIER_WORDS` Young / — / Great /
  Dire / Elder / Ancient / Primal; 7 fixed-word prefixes (Brutal damage, True hit, Deadly crit, Poised combo, Swift
  speed, Twin multi-hit, Focused amp) + Quality; 6 suffix hybrids (Griffin, Minotaur, Sphinx, Basilisk, Phoenix,
  Kraken) and 4 prefix hybrids (Savage, Flowing, Precise, Echoing). The suffix Damage / Accuracy / Critical / Combo /
  Attack Speed families and the prefix Toughness / Vitality / Fortune families are gone; the 120 animal proc suffixes
  no longer roll (their StatBonuses rows stay for old saves; `AnimalSetController`'s old ladder only sees those).
- Hybrids: family `kind: "hybrid"` with `parts`; the roller rolls each part at the same tier index at
  `lootSystem.itemGeneration.hybridPartShare` (0.6) of its range, one line object per part with the hybrid's
  keyword; parts count as used, so a hybrid and its own single stat never share an item. ITEM MODS sheet has a
  `parts` column.
- Pack (`AnimalPacks`): `animalPackPieces` (2) equipped pieces with the same stat suffix family → that family's
  lines × `animalPackMultiplier` (2) in `EquipmentBonusCalculator`'s suffix sums. Not shown in tooltips or the HUD
  yet, and the sim bot's gear score doesn't know about it.
- 10k roll: 2.42 lines per item; stat 0.75, mechanic 0.27 (rules / DoTs / slot only now), material 0.20, action 1.21.

### Three suffix kinds (user, 2026-11-15, built)

- An item holds up to 3 suffixes, at most one of each kind, printed in this order after the base:
  **common = descriptor** ("of Might"; 8, one per body stat, values at 0.8x the animal's at the same tier),
  **uncommon = animal** ("of the Dire Bear", keeps the tier words; the 8 animals + 6 hybrid creatures; only these
  form Packs), **rare = poetic phrase** for the fanciest effects: the 28 rule / opener / finisher / blood-price
  families, the 3 DoTs and the extra action slot moved from prefix to suffix, one phrase each ("Bathed in Blood" =
  Vampiric, "Remembered by the Careless" = +1 strip slot; mostly the old story phrases).
- `lootSystem.itemGeneration.suffixKinds`: the kinds a rarity always gets (Common: common; Uncommon: uncommon; Rare /
  Epic: rare; Legendary: uncommon + rare; Mythic: all three). Each lower kind it lacks joins at `suffixExtraChance`
  (0.25). A kind with nothing eligible at that item level is skipped (rare phrases start at L3 / L8). Suffix lines
  count toward the rarity's line count; the weighted lines fill the rest with prefixes and actions (mechanic pool
  weight is now 0: every mechanic family is a rare suffix).
- One line per body stat per item ("of Might" and "of the Bear" never share an item).
- 10k roll: every Common has a descriptor; Uncommon 75% animal only, 25% descriptor + animal; Rare+ have a poetic
  phrase except at very low item levels; 18 items with all three.
