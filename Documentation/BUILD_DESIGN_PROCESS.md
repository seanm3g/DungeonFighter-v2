# Build Design Process

Canonical format for proposing and stress-testing class builds. Prefer this sticky schema over free-form guides.

The Engine × Carrier matrix (Actions / Items / Class) remains useful when **implementing** a build. The sticky card below is the **design card** you fill first.

---

## Sticky card (required)

Fill every teal slot. One primary currency. One closed loop.

```
synthesis:
alias:
convert:
feed:
criteria for feed trigger:
```

Optional joiner (only if it plugs into the same currency):

```
JOINER NAME: (what it is) → how it modifies Feed and/or Convert
```

---

## Slot contracts

| Slot | Job | Must answer | Fail if… |
|------|-----|-------------|----------|
| **synthesis** | What is the **currency**, and what raw inputs mint its *identity*? | `CURRENCY ← inputs` | You only name a material/item with no currency recipe |
| **alias** | What **counts as** a valid feeder / tribe token? | `TOKEN → CLASS/tag` (or attribute count-as) | Alias never touches Feed or Convert gates |
| **convert** | How does the currency become combat power? | `CURRENCY → payoff` (vector + spend/always-on) | Convert reads a quantity that Feed never banks |
| **feed** | How does play **grow** the currency? | `+N CURRENCY` (amount / scale) | Feed is a cost, a status, or the Convert itself |
| **criteria for feed trigger** | **When** does Feed fire? | A WHEN / gate (`ON CONNECT`, `ONCRITICAL`, …) | Trigger exists but nothing listens |

**Joiner** (e.g. NUMBSKULL): a second fantasy that **multiplies mint rate or Convert rate** of the same currency. If it needs its own bank, it is a second build — split cards.

---

## Hard rules

1. **One primary currency** per card (DRAG, NUMBSKULL, BODY, …).
2. **Feed ≠ Convert.** Feed only adds currency. Convert only spends or reads it.
3. **Alias must gate or amplify** Feed and/or Convert (not flavor-only).
4. **Synthesis names the currency**, not only a loot material. Materials belong in Alias (and optionally as mint gates).
5. **Closed circuit:** trigger → Feed → (optional Joiner) → Convert → player wants to trigger again.
6. Mark each line **Exists / Partial / Missing** against current DF systems when stress-testing.

---

## Validity check (quick)

- Remove **Actions** (or the Feed WHEN): does Feed die? → should mostly yes.
- Remove **Alias tokens** (material/class count-as): does minting weaken? → should yes.
- Remove **Convert**: is the currency useless flavor? → should yes.
- Do Feed and Convert share the **same named currency**? → must yes.
- Does a Joiner touch that currency? → must yes, or delete the joiner.

---

## Optional: Engine × Carrier (when implementing)

After the sticky passes, expand any Missing/Partial row across carriers:

| Role | Actions | Items | Class |
|------|---------|-------|-------|
| Synthesize | | | |
| Alias | | | |
| Feed | **primary** | assist | assist |
| Convert | active cashout (optional) | passive / scaleFrom | mastery |
| Feed trigger | WHEN on strip verbs | item WHEN listeners | path gates |

Alias is Class-led. Feed is Action-led. Convert splits passive + optional active cashout.

---

## Worked example (sticky passed)

**BIG HIT SLOW SPEED** — Damascus Barbarian (design target; not all shipped)

```
synthesis:   DRAG ← swing heaviness (action Length / weapon slow / −SPEED_MOD)
alias:       DAMASCUS → CLASS (Barbarian) so only tribal heavies mint DRAG cleanly
feed:        +N DRAG
criteria for feed trigger: ON CONNECT (Barbarian-tagged / Damascus-gated)
convert:     +% damage per DRAG (cash on next heavy, or always-on while DRAG > 0)
NUMBSKULL:   empty slots → +DRAG mint rate or +Convert rate (joins the engines)
```

| Slot | Shipped today |
|------|----------------|
| synthesis DRAG | **Missing** (no DRAG bank) |
| alias Damascus → Barbarian | **Exists** (Level 1 tribe count-as) |
| feed +N DRAG | **Missing** |
| ON CONNECT criteria | **Exists** as WHEN; nothing banks DRAG |
| convert % dmg / DRAG | **Missing** |
| NUMBSKULL | **Partial** — Empty Fury is empty→STR, not DRAG mint/Convert |

---

## Blank template (copy)

```
BUILD NAME:
FANTASY (one line):
HYBRID: MONO | name

synthesis:
alias:
convert:
feed:
criteria for feed trigger:

JOINER (optional):
  NAME:
  effect on Feed / Convert:

EXISTS / PARTIAL / MISSING:
  synthesis:
  alias:
  feed:
  criteria:
  convert:
  joiner:

PLAYER LOOP (1–3 turns):
SIBLINGS (same currency, flip Convert vector or Feed WHEN):
  1)
  2)
```

---

## How to prompt

> Using the **Build Design Process**, propose N builds for fantasy X.  
> Fill the sticky card (synthesis / alias / convert / feed / criteria).  
> One currency. Feed ≠ Convert. Alias must gate mint or payoff.  
> Optional Joiner only if it modifies that currency.  
> Mark Exists / Partial / Missing. Expand Engine × Carrier only for Missing rows you want to ship.

---

**Bottom line:** A five-line closed circuit plus optional joiners. If the sticky does not close, the build is not ready — fix the card before writing systems or long guides.
