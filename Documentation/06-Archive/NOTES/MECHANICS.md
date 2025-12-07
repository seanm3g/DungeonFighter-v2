Absolutely — here is your list **cleanly formatted**, consistently indented, and ready for copy/paste into a design document.

---

# **Roll-Interaction Actions**

## **1.1 Modify Future Rolls**

* Modify next roll (± value, multiplier, clamp min/max)
* Re-roll same action (conditional, % chance)
* Exploding dice (roll > X → add new roll)
* Roll multiple dice (e.g., 1d20 → 2d20):

  * Take lowest
  * Take highest
  * Take average

## **1.2 Conditional Triggers**

**Triggers occur only on:**

* Miss
* Normal hit
* Combo hit
* Critical hit
* Exact value (e.g., roll = 3)

**Triggers occur only if:**

* Same action was used previously
* Different action was used previously
* Action or Gear contains a specific tag

## **1.3 Combo Position Modifiers**

* Modify effect based on slot index
* Modify based on total number of actions in combo
* Modify based on distance from slot *X* to current

## **1.4 Adjust Action Trigger Thresholds**

* Critical hit threshold (e.g., 20)
* Combo threshold (e.g., 14)
* Hit threshold (e.g., 5)

---

# **2. Damage & Speed Actions**

## **2.1 Direct Damage**

* Flat damage (+ or %)
* Dice-based damage (roll × X, roll + stat, etc.)
* Damage scaled by:

  * speed differences
  * health differences

## **2.2 Self-Damage**

* Self-inflicted damage (+ or %)

## **2.3 Attack Speed Modifiers**

Increase or decrease:

* Action attack speed
* Weapon attack speed

## **2.4 Multi-Hit Variants**

* 3 × 50% damage
* 1 × 200% damage
* Any {count × damage%} pattern

---

# **3. Attribute / Stat Manipulation**

Stats include: STR, AGI, TECH, INT, secondary attributes, etc.

## **3.1 Direct Stat Change**

* Temporary stat increases/decreases (per dungeon or fight)
* Modify primary attributes
* Modify secondary attributes
* Scaling changes (stat × %)

## **3.2 Dynamic-Based Stat Changes**

Scale actions based on:

* HP thresholds (full / high / low)
* Exact HP values

## **3.3 Stat Resets**

Reset stats:

* Per item
* Per enemy
* Per room
* Per dungeon
* Every N turns
* When HP crosses threshold (full or low)

---

# **4. Combat Mechanics Actions**

## **4.1 Apply Status Effects**

### **Damage / DOT**

* Burn (Fire) → stacking extends duration
* Poison (Underworld) → stacking increases damage
* Bleed (Blood) → triggers damage when you take an action (AS-sensitive)

### **Damage Taken Modifiers**

* Vulnerability (take more damage)
* Harden (take less damage)
* Fortify (increase armor)

### **Damage Given Modifiers**

* Weaken (reduce outgoing damage)
* Focus (increase outgoing damage)
* Expose (reduce target armor)

### **Utility Effects**

* HP Regen
* Swift / Slow (attack speed changes)
* Stun (skip turn)
* Armor Break (reduce armor)
* Pierce (ignore armor)
* Reflect (return damage)
* Silence / Paralyze (combo disabled)
* Lifesteal
* Stat drain (steal stats)
* Absorb (store damage → release at threshold)
* Temporary HP (overheal)
* Confusion (chance to attack self/ally)
* Cleanse (reduce stacks)
* Mark (next hit = guaranteed crit)
* Disrupt (reset combo)

## **4.2 Conditional Status Application**

Apply status only on:

* Hit
* Miss
* Combo
* Crit
* Exact result (e.g., 1d20 = 4)

## **4.3 Status Effect Groups**

* Multiple statuses applied together
* Blessings / Curses
* Rage (speed ×2, defense ÷2)
* Stoneform (armor ×3, no damage dealt)
* Focus (next action crits, half attack speed)
* Shadow (dodge next 2 attacks, −roll penalty)

---

# **5. Tag-Based Mechanics**

## **5.1 Actions Modified by Tags**

(Examples: FIRE, WATER, WIZARD, CELESTIAL)

* Damage modified per tag
* Unlock effects if ≥ X matching tags
* Unlock effects if tag count > Y

## **5.2 Actions That Modify Tags**

* Add temporary tag (fight or dungeon)
* Replace a tag
* Remove a tag
* Colorless / wildcard tag

Tags may exist on: gear, rooms, dungeons, enemies, characters, actions.

---

# **6. Combo Logic Actions**

## **6.1 Action-Slot Routing**

* Jump to slot N
* Skip next action
* Repeat previous action
* Loop to slot 1
* Stop combo early
* Disable slot (temporal or dungeon-wide)
* Random next action
* Trigger only if in slot N

---

# **7. Outcome-Based Actions**

## **7.1 Conditional Outcomes**

Trigger when:

* Enemy dies this turn / this dungeon
* Enemy HP hits 50%, 25%, 10%
* Combo ends naturally

## **7.2 Scaling Outcomes**

* Based on number of times this action has been used

## **7.3 Risk / Reward Mechanics**

* Gambling outcomes (e.g., 30% = 3× damage; 70% = fail)

---

# **8. Economic & Meta-Progression Actions**

* Gain XP per criteria:

  * Defeating enemy
  * Opening chest
  * Clearing room
  * Clearing dungeon
  * On crit / any condition
* Increase loot rarity when action triggers

---

# **9. Environmental & Room-Based Actions**

Interactions with environments (Fire, Ice, Celestial, Undead):

* Gain bonuses in matching-tag rooms
* Change current room environment
* Gain bonuses based on:

  * Rooms cleared
  * Rooms of specific tag cleared
  * Enemies cleared
  * Enemies of specific tag defeated

---

