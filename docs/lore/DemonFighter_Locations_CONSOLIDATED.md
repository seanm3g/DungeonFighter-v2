# DEMON FIGHTER — Locations Reference (Consolidated)
### Region / Complex / Node — Corrected End State — 2026-09-17

*This document replaces `DemonFighter_Locations_Batch1_Complexes.md`, `DemonFighter_Locations_Batch1_Addendum.md`, and `DemonFighter_Gazetteer_v1.md` as the single working reference for Faerlan's locations. It is the corrected end state — every fix the Addendum made to the base file is already applied here; nothing below needs to be read "through" a correction layer. The three source files are not deleted or marked superseded by this document itself — that decision is left to whoever runs this project next (see Status).*

**Hierarchy (three tiers):**
- **Region** — unbounded, open-sky terrain. Not enterable as a single room. Contains multiple Complexes scattered within it, and can host open-terrain encounter Nodes directly (a specific ridge, a specific pass-stretch) that don't belong to any one Complex.
- **Complex** — bounded, enterable, room-based (city / castle / crypt-complex / temple-ruin / battlefield-tract-with-defined-edges — shape matches `Dungeons.json`: name, theme, level band, enemy pool). Sits inside a Region where one is defined, or stands zone-general where no Region has been formalized around it yet.
- **Node** — the actual room a player walks into (shape matches `ENVIRONMENTS` rows), nested inside a Complex. **Not generated in this pass.** The tier is formally defined here and left empty; see the Node Tier section near the end.

**Schema key:** `ID` · `Name` · `Parent Region` · `Cat` (category) · `Rarity` · `Aff` (affordance class: **A** = room/dungeon-candidate, **B** = world-texture only, no physical room planned) · `Era` (origin era) · `Faction` (association + active window) · `KT` (knowledge tier, 1–4: how much a present-era Faerl could plausibly know) · `Architecture` · `Enemy Niche` · `Mechanic` (environmental phenomenon, Madlib-§13-ready) · `Sediment` (source anchor, per the Sediment Principle — every location traces back to a specific document) · `Tags` (closed vocabulary: `earth, air, water, fire, flooded, overgrown, scorched, exposed, dilapidated, elegant, dormant, active, cycling`) · `Notes`.

**Standing rules applied throughout:**
- Nothing in a `Faction`/`Era` field predates its own house's founding (~1,000 BY for all four paramounts) unless the complex is explicitly Antem-era or Deep-Time.
- Nothing at `KT` 1–2 gets named as "Antem" in its own eventual flavor text — only in this internal sheet, for internal use.
- A location only stays at Complex tier if it's actually bounded/enterable (a specific castle, cave, tomb, ford, ruin). Anything unbounded (an open desert, a mountain range, a forest tract, a flat expanse) belongs at Region tier instead, containing the bounded Complexes within it.
- No ID is reused for two different things, and no ID carries an ad-hoc letter suffix — clean sequential numbers only, within the correct zone's numeric block: Forest in the one-hundreds (e.g. CX-101), Water in the two-hundreds (e.g. CX-201), Desert in the three-hundreds (e.g. CX-303), Fire in the four-hundreds (e.g. CX-402), Ecotones in the five-hundreds (e.g. CX-501), Deep-Time/unzoned in the six-hundreds (e.g. CX-601), Offstage in the seven-hundreds (e.g. CX-701), CX-999 reserved cross-zone, and sequential numbers for Regions (e.g. RG-01).
- Retired/banned project vocabulary never appears in this document as live content — including this reference document itself, not only player-facing text. Where a source event's *original* language used a now-retired term, this document describes the fact of that usage and directs any future writer to the retired-term list rather than repeating the term itself.

---

## RARITY TIER LEGEND (locations, not loot — same six-tier scale, different meaning)
*Carried forward from the Gazetteer — nothing else currently holds this table.*

| Tier | What it means for a *place* |
|---|---|
| **Common** | Unremarkable holds, fords, or waystations. No individually documented event — background texture, safe to reuse freely, low narrative weight if found. |
| **Uncommon** | One documented incident of local significance — a skirmish, a truce, a custom's origin. Worth a passing NPC reference. |
| **Rare** | Tied to a named historical figure or event that shaped a House's culture, doctrine, or a still-used idiom. |
| **Epic** | Tied to a pivotal, multi-House event — a war's end, an institution's founding, a House's fortune turning. |
| **Legendary** | Central to the world's deep-time spine. Antarkeia-tier. Every faction in the game has an opinion about it. |
| **Mythic** | Deliberately unlocatable or effectively unfindable in the fiction itself — single-witness accounts, vanished on return visits, never confirmed reachable twice. Built for the "found it once in ten thousand runs" design — most players should never see these. |

---

# REGION TIER (RG-01 – RG-14)

Fourteen Regions, formalizing every unbounded feature previously mis-tagged as a flat Complex, plus the continent-scale features and ecotone borderlands the v1 Gazetteer flagged as open naming work.

## Continental / Deep-Time Regions

| ID | Name | Zone | Rarity | Tags | Character | Nested Complexes / Notable Open-Terrain Features | Sediment | Notes |
|---|---|---|---|---|---|---|---|---|
| RG-11 | The Highspine | Continental mountain spine | Uncommon | earth, exposed | The unnamed central highland spine Faerlan's four biome influences radiate outward from; rivers originate here and cut irregular paths to the coast. | RG-01 (the Dray Highlands) is its Desert-zone segment, not a separate range. | Antarkeia Addendum Pt. Two (unnamed range) | Antarkeia sits in the lowland basin where the Highspine finally levels out. |
| RG-12 | The Vael | Continental river system | Uncommon | water | The principal river descending the Highspine into Antarkeia's marsh basin — named for Vaelume, the old Antem name for the whole continent ("the Unforgetting Ground"), a root most Faerl who use the river's name today don't know they're invoking. | None nested; connective geography. | Deep History §0 (Vaelume, locked); river network unnamed | A quiet, ready-made Sediment hook. |
| RG-10 | The Salt Flats (Iowen-Ranu Coast) | Deep-Time / unzoned | Uncommon | earth, exposed | Coastal salt flats, unclaimed and unworked. | None currently split out — no specifically-named bounded feature has been carved from this Region yet; that's a legitimate open state, not a gap. | Deep History BYT 5100/2040 | Fought over for a dozen seasons in the Salt Line War, then left unclaimed ever after — "Iowen-Ranu manners," the reflex to abandon anything two parties bled over equally. Ambient hazard: risen Skeletons/Wights from the unburied dead; otherwise ordinary open ground. **Formerly the flat Complex `CX-603`; that ID is retired — see the change log.** |

## Ecotone Borderland Regions
*Per the v1 Gazetteer's Section VI: the four Faerlan ecotone zones were explicitly flagged as open naming work. Each gets one static/architectural Complex and one history-anchored Complex, so the borderlands don't read as pure filler between the "real" zones.*

| ID | Name | Ecotone | Rarity | Tags | Character | Nested Complexes | Sediment |
|---|---|---|---|---|---|---|---|
| RG-06 | The Rimewood | Ice–Forest | Common | earth, overgrown | Frost-touched boreal pine forest and taiga where Ossmoor's ice-influence bleeds into Thorne's forest; biome influences blend rather than snap to a hard line here, per locked geography. | CX-501 (the Frostbriar Stand), CX-502 (the Long Cold Shrine) | Antarkeia Addendum Pt. Two |
| RG-07 | The Sere Margin | Forest–Desert | Common | earth, overgrown, exposed | Dry woodland thinning into savanna/scrubland where Thorne's forest gives way to Dray's desert. | CX-503 (the Dry Marches Waystation), CX-504 (the Grain Winter Camp) | Antarkeia Addendum Pt. Two |
| RG-08 | The Blackglass Flats | Desert–Fire | Common | earth, fire, scorched | Cracked badlands and dark volcanic sand where Dray's desert meets Colehurst's fire territory — named for the fused-glass patches this ground shares in character with the Glass Plains, though the two are not the same site. | CX-505 (the Fused Causeway), CX-506 (the Toll of the Green Road) | Antarkeia Addendum Pt. Two |
| RG-09 | The Steaming Marches | Fire–Ice | Common | fire, water, scorched | An Iceland-flavored geothermal zone — black volcanic rock steaming under snow, where Colehurst's fire and Ossmoor's northern ice-influence meet furthest from either House's core territory. | CX-507 (the Vent-Warded Hollow), CX-508 (the Reaving Shore) | Antarkeia Addendum Pt. Two |
| RG-13 | Widow's Blush | Named forest trade route (Forest zone) | Uncommon | overgrown, water, elegant | An old cross-territory trade corridor. Not a room — its own toll-markers run its whole length rather than sitting in one place. **LOCKED**, not a proposal. | CX-117 (Widow's Rest — the corridor's one specific spring-clearing stop) | Faerl Age Events #17 (LOCKED); Modern-Era Seeds Location Mapping |

## Zone Regions (demoted from Complex tier — the boundedness-test corrections)

| ID | Name | Zone | Rarity | Tags | Character | Enemy Presence | Mechanic | Nested Complexes / Notable Open-Terrain Features | Sediment | Notes |
|---|---|---|---|---|---|---|---|---|---|---|
| RG-01 | The Dray Highlands | Desert (Dray) | Uncommon | earth, exposed | Cliff-edge, abandoned campsite, wind-scoured summit — vertical, exposed, altitude-driven highland expanse; no edges, not a structure. | Pure native fauna (Hawk, Rock Lobber, Edge Stalker); techno-echo (Summit Titan) at the peak. | Altitude/exposure hazard throughout; the Summit — an open-terrain feature of this Region, not a Complex of its own — carries the deliberately-uncharted-pass mechanic. | CX-313 (the Contested Pass). The Summit (Summit Titan's ground) is an open Region-level feature, not nested under any Complex. | Bestiary §II; Deep History YT117 | Is the Desert-zone segment of RG-11 (the Highspine), not a separate range. The Vista of the Shimmering Mound (Mythic — see appendix) is loosely zone-adjacent here but is **not** part of this Region's Summit feature — kept fully separate per the project's no-merge flag. **Formerly the flat Complex `CX-305` ("Mountains"); that ID is retired — see the change log.** |
| RG-02 | The Dunefields | Desert (Dray) | Common | earth, exposed | Open dune-sea, wind-scoured, half-buried metal debris. | Pure native fauna (Sandstorm Flanker); techno-echo (Sand Serpent, Dune Roller) at depth. | Sandstorm visibility/blind hazard. | CX-303 (Oasis), CX-314 (the Fast Site). Also hosts the Golem Trade Wars road as a Tier-B backdrop-only texture note (usable as backdrop only — not a Complex). | Bestiary §II | **Formerly the flat Complex `CX-301` ("Dune"); that ID is retired — see the change log.** |
| RG-03 | The Glass Plains | Desert (Dray) | Epic | earth, exposed | Fused-glass flat, unnaturally smooth and reflective, horizon-distorting. | Pure native fauna (Sun Lance Scorpion, Glare Stalker); Genesis-adjacent (Shadow Wraith). | Color/heat-haze distortion — a sourceless bone-white wind struck a refugee column here (Renna Chases the Mirage); the legacy is a standing environmental tell, not a repeatable event. "Walked through the White Wind" survives as an idiom. | None currently split out. | Calendar doc §4 | **Formerly the flat Complex `CX-302` ("Glass Plains"); that ID is retired — see the change log.** |
| RG-04 | The Farthicket | Forest (Thorne) | Common | earth, overgrown | Thicket undergrowth, wildflower meadow, overgrown ruin pockets — dense, low-visibility, reclaimed-by-nature. | Pure native fauna dominant, light Genesis-adjacent at the ruin pocket only. | Visibility/ambush hazard — dense growth blocks sightlines. | CX-116 (the Overgrown Ruin). | Bestiary §IV | **Formerly the flat Complex `CX-103` ("Dense Brush"); that ID is retired — see the change log.** |
| RG-05 | The Ashen Expanse | Fire (Colehurst) | Common | fire, scorched | Smoldering ridge and blackened forest — scorched, ash-drift terrain. | Feral Earth-stock (Cinder Hound); pure native fauna (Magma Creeper, Ashwing Harpy). | Ash-fall visibility reduction. | CX-412 (the Colehurst Miscast), CX-413 (Smokey Cave — the enclosed cave-and-tunnel piece of the original terrain, now its own bounded Complex, distinct from the Region's open ash-fall). CX-414 (Emberfall, the Colehurst vassal hold) sits adjacent to this Region without being formally nested in it. | Bestiary §III | **Formerly the flat Complex `CX-401` ("Ashen Land"); that ID is retired — see the change log.** |
| RG-14 | The Pyramid Fields | Desert (Dray) | Uncommon | earth, exposed | A field of individually-named pyramids — the plural was itself the tell that this needed Region treatment rather than one flat "Pyramids" Complex. | — (varies by nested Complex; see CX-311/CX-312) | — (varies by nested Complex) | CX-311 (the Tomb of Too Many Eyes), CX-312 (the Cracked Ascension). | Deep History YT71; Bestiary §II | **Formerly the flat Complex `CX-304` ("Pyramids"); that ID is retired — see the change log.** Two pyramids in the same field, one sealed-and-swarming, one open-and-collapsing, each with its own enemy signature, instead of one Complex trying to be both at once. |

---

# COMPLEX TIER

*Sixty-nine Complexes. Every entry below carries its full field set — architecture, enemy niche, and mechanic are deliberately kept mechanically distinct from every other entry (a prior round of feedback flagged near-duplicate "50 throne rooms" as a real problem; this batch's differentiation work is preserved, not re-flattened, by this consolidation).*

## 0. ANTARKEIA (standalone — not zone-bound)

| ID | Name | Parent Region | Cat | Rarity | Aff | Era | Faction | KT |
|---|---|---|---|---|---|---|---|---|
| CX-000 | Antarkeia | — (standalone hub; sits in the lowland basin where RG-11 the Highspine levels out, fed by RG-12 the Vael) | Vault-City | Legendary | A | Genesis→Deep Dream (built); Breach at YT0 | Antem (built, pre-ext.) / Faerl-4-House-coalition (YT0 breach) | 4 |

| ID | Architecture | Enemy Niche | Mechanic | Sediment | Tags | Notes |
|---|---|---|---|---|---|---|
| CX-000 | Sealed vault-city in a lowland marsh basin; monumental Antem stonework, ward-mechanisms that still answer, collapsed inner-vault wing. The ancient Antem capital, breached at Year Zero after roughly 5,000 years sealed. | Techno-echo tier: Cragwings (heights), wraiths (deep passages), automated ward-constructs | Ward-mechanisms trigger on deep-seal disturbance; structural collapse hazard in the inner-vault wing | Chronicle of Antarkeia; Hollowing/Rivening §VII–VIII | earth, dilapidated, dormant | **Two-era complex** — every Node built here must declare whether it's Antem-original (pre-ext., untouched) or YT0+-disturbed (coalition-era wreckage/graffiti/camp debris layered on top). Never both undifferentiated in one Node. Two named sub-sites exist in the record and are pending their own Node-tier entries in a future pass, not detailed here: **the Gathering-Grounds** (the open ceremonial ground at Antarkeia's heart where the four-house coalition camped during the eleven-day breach; Cragwings nest in the high broken stone above it) and **the Inner Vault** (the sealed innermost chamber cracked on the eleventh day, triggering the collapse that swallowed the deepest search parties and the recovered relics alike; never reopened). Sediment for both: Chronicle of Antarkeia. Antarkeia's invisible containment network (the Spoke Sites) is represented in this batch by its four physical instances — CX-113, CX-209, CX-310, CX-411 — kept uniformly `KT 1` and never described as "Antarkeia-connected" in player-facing text. |

---

## I. THE FOREST ZONE — House Thorne

| ID | Name | Parent Region | Cat | Rarity | Aff | Era | Faction | KT |
|---|---|---|---|---|---|---|---|---|
| CX-101 | Den | — (unzoned; Thorne core territory) | Wild-Complex | Common | A | Age of Man (locked, current dungeon) | Faerl-Thorne / none-dominant | 4 |
| CX-102 | Labyrinth | — | Ruin-Complex | Uncommon | A | Age of Man | Faerl-Thorne, minor noble line (unnamed) | 3 |
| CX-104 | Wrenfield | — | Vassal Hold-Town | Rare | A | Age of Man (hold); Antem tech beneath, ~5,000+ BY | Faerl-Thorne, House Dray tie (Kester Dray d. YT180) | 4 |
| CX-105 | Millstead | — | Sealed Town | Rare | A | Age Unwritten, YT33 | Faerl-Thorne | 3 |
| CX-106 | Duskmoor Wood | — | Dead Forest-Tract | Rare | A | Age Unwritten, YT14 | Faerl-Thorne | 3 |
| CX-107 | The Ashen Reach | — | Dead Forest-Tract | Rare | A | Age Unwritten, YT160 | Faerl-Thorne | 3 |
| CX-108 | Hallow's Bend | — | Emptied Village | Uncommon | A | Age of Man | Faerl-Thorne | 3 |
| CX-109 | Reed Hollow | — | Emptied Village | Rare | A | Age Unwritten, YT205 | Faerl-Thorne | 3 |
| CX-110 | Twin Hearth | — | Fortified Hamlet (ruin) | Uncommon | A | Hollowing era, ~5,070 BY | Antem + Faerl, mixed (deep prehistory) | 2 |
| CX-111 | Greymoot | — | Fallen Stronghold | Uncommon | A | Landfall Centuries, ~4,700 BY | Faerl (pre-House) | 2 |
| CX-112 | Greenreach | — | Living Forest-Tract | Uncommon | A | Age of Clans, BYT 1870 | Antem-adjacent origin / unclaimed since | 2 |
| CX-113 | The Root-Bound Spoke | — | Ruin (Antarkeia Spoke) | Epic | A | Genesis-built; dormant since ~5,000 BY | Antem (built) / none present | 1 |
| CX-114 | The Rookmantle Barrow | — | Crypt-Complex | Uncommon | A | Age of Man (House Centuries) | Faerl-Thorne (Rookmantle vassal) | 3 |
| CX-115 | The Ferncrest Lodge-Hold | — | Institution Complex | Rare | A | Age Unwritten, founded YT9 | Faerl-Thorne / the Bound | 4 |
| CX-116 | The Overgrown Ruin | RG-04 (the Farthicket) | Ruin (bounded) | Uncommon | A | Age of Man *(inherited from RG-04's pre-split Complex entry)* | Faerl-Thorne *(inherited from RG-04's pre-split Complex entry)* | 4 |
| CX-117 | Widow's Rest | RG-13 (Widow's Blush) | Basin-Complex (spring-stop) | Uncommon | A | Widow's War, 3,200–3,050 BY | Faerl (cross-territory) | 4 |

| ID | Architecture | Enemy Niche | Mechanic | Sediment | Tags | Notes |
|---|---|---|---|---|---|---|
| CX-101 | Packed-earth den mouths, offering mounds, moonlit clearings — low, rounded, organic dig-structures | Feral Earth-stock (wolves, hounds), light Genesis-adjacent (Bone Spreader) | Pack-coordination hazard — enemies call/respond across adjacent Nodes | Bestiary §IV (locked) | earth, overgrown | Already-locked in-game zone; formalized here as Complex parent |
| CX-102 | Hedge-maze corridors, mirrored chambers, false exits — geometric, disorienting, human-built garden architecture | Genesis-adjacent (mimics, loop-spirits, doppelgangers) | Path/exit unreliability — the room lies about its own layout | Bestiary §IV; Deep History YT87 | earth, dilapidated | Contains the Room of Two Mirrors (Mythic — see appendix) as a sub-site, not a standard Node |
| CX-104 | Timber-and-fieldstone hold town built around a river ford; the ford-bed itself hides buried Antem-era wreckage | Techno-echo: a Ford-Sentinel-class guardian (now dead — see Deep History YT175) once held this ground; currently unguarded | The ford's buried tech is a latent physical hazard (stumble/throw), not a combat one | Calendar doc §4; Deep History YT175/180; 25 Treatments #21 | earth, water, exposed | The **only** ford complex with an active tech-hazard mechanic — see Larkford (CX-206) for the deliberately different Water-zone counterpart |
| CX-105 | A trade town's ordinary timber-and-stone build, sealed rather than cleared — doors and shutters barred from outside, not in | Genesis-adjacent: wights, at least one Lich wearing a resident's face | The town "answers" as if inhabited — false signs of occupancy (smoke, light) that aren't | Deep History YT33 | earth, dilapidated | |
| CX-106 | Living understory greying gradually outward from a single point — patchy, still partly green at the edges, navigable | Genesis-adjacent, low-threat: Loop Shade / early-stage color-drain phenomena | **Color-drain**, slow and spreading — desaturation status effect that intensifies toward the tract's center | Deep History YT14 | overgrown, dormant | Part of the Forest color-drain cluster — see Notes on CX-107/CX-108 |
| CX-107 | Fully standing-dead forest — trees upright, bark intact, wood hollow and grey throughout; total, not partial | Genesis-adjacent, higher-threat: a cold-arrival phenomenon distinct from Duskmoor's slow spread | **Sudden-cold burst** — an environmental cold/silence effect that precedes any combat trigger by a beat, unlike Duskmoor's gradual creep | Deep History YT160 | overgrown, dormant | **Deliberately differentiated from CX-106:** Duskmoor = slow/partial/patchy; Ashen Reach = instant/total/silent-first. |
| CX-108 | Ordinary Thorne border-village timber construction, domestic interiors intact and undisturbed | Genesis-adjacent, ambient only — no confirmed combat encounter on record | Color-drain, instantaneous rather than spreading (contrast CX-106) | Deep History (Age of Man) | exposed, dormant | Third leg of the color-drain cluster — Chronicle-Keepers in-fiction could plausibly connect CX-106/107/108 as one pattern; a strong wiki-bait thread if surfaced carefully at KT3+ only |
| CX-109 | Modest farming-village architecture, unremarkable except for scale of loss (~90) | Genesis-adjacent, unclassified — doctrine explicitly disputes its signature matches any known type | Sound-based onset — screaming, sudden silence, scorched-copper smell (not visual/color-based, unlike the Duskmoor cluster) | Deep History YT205 | earth, exposed | Explicitly the odd one out in the emptied-village set — keep its onset sensory (sound/smell), never visual |
| CX-110 | Small fortified hamlet, still standing — Antem and Faerl building styles fused in the same wall line, unusual for its era | None confirmed on record — a rare "nothing happened here" complex, valuable for pure atmosphere/quiet Nodes | None — intentionally a non-combat-forward complex | Hollowing/Rivening §II | earth, dilapidated | Good candidate for Tier-A Nodes that are *not* combat-first (a genuine rarity at this rarity tier) |
| CX-111 | Larger, well-defended fortress architecture — gatehouse and a since-infamous side passage | Faerl-bandit/rival-camp descendant enemies plausible; otherwise unclaimed ruin fauna | The betrayal side-passage is a literal secondary, unguarded entrance — a Node-level stealth/flank affordance | Faerl Age Events #46 | earth, dilapidated, exposed | |
| CX-112 | Old-growth grove, self-defending — canopy and root architecture that visibly closes ranks against intruders | Genesis-adjacent: Treant-family, one confirmed original scorched-treant precedent | Active terrain — the grove itself blocks/redirects paths in response to logging-type actions | Deep History BYT 1870 | overgrown | |
| CX-113 | Antem containment stonework fused into and overgrown by root systems — architecture barely visible under centuries of growth | Techno-echo, dormant-tier: low-grade automated ward response only, no active guardian | Faint suppressive field — demons/undead within range are subtly weaker (creator-knowledge only, never stated in-world) | Hollowing/Rivening §VIII; Antarkeia Addendum Pt. Two | earth, overgrown, dormant | First of four Spoke complexes (see CX-209, CX-310, CX-411). Pattern: each Spoke reads as an ordinary zone feature until closely examined — never obviously "Antarkeia-connected" at KT1–2 |
| CX-114 | Earthwork burial mounds, root-intruded interior chambers, corvid (rook) motifs in surviving stonework | Genesis-adjacent: Skeleton/Wight tier, low-to-mid | Burial-niche density hazard — collapsing/shifting mound interiors | Calendar doc §3 (Rookmantle vassal, locked) | earth, dilapidated | |
| CX-115 | Fortified training hall and drill-yard — institutional rather than domestic or military-siege architecture | Faerl-trained (the Bound) NPCs and trainees populate this rather than hostile enemies by default | None — a rare non-hostile-default Complex, useful for a hub/quest-giver role | Deep History YT9 | earth, elegant | This is the Bound's actual root — a shared training hall built by Antarkeia veterans, predating the Reckoning Faith's formal doctrine by over a decade. |
| CX-116 | A specific collapsed structure reclaimed by the thicket, walls still tracing a floor plan under growth | Genesis-adjacent, low-threat | Overgrowth-concealment — sightline/detection hazard distinct from the open Region around it | Bestiary §IV | overgrown, dilapidated | The bounded piece carved out of RG-04 (the Farthicket). |
| CX-117 | A specific spring-clearing along the Widow's Blush trade corridor (RG-13), matching the locked "Sacred Grove / Natural Spring" room types. The corridor itself is a Region, not a room. | Pure native fauna along the route; none confirmed hostile at the Rest itself | None distinct — a rare non-hostile waypoint, useful as a hub/rest-node between combat-forward Complexes | Faerl Age Events #17 (LOCKED); Modern-Era Seeds Location Mapping | overgrown, water, elegant | Genuinely LOCKED, not a proposal — the toll-keeper role is stated as canon fact. |

---

## II. THE WATER ZONE — House Marlow

| ID | Name | Parent Region | Cat | Rarity | Aff | Era | Faction | KT |
|---|---|---|---|---|---|---|---|---|
| CX-201 | Water Temple (the Drowned Court) | — (unzoned; Marlow core territory) | Temple-Ruin Complex | Rare | A | Antem-built; Faerl-occupied since | Antem (built) / Faerl-Marlow (present use) | 3 |
| CX-202 | Mirror Pond | — | Wetland-Complex | Uncommon | A | Age of Man | Faerl-Marlow | 4 |
| CX-203 | Spring | — | Basin-Complex | Rare | A | Antem-adjacent origin, current Faerl use | Antem (Chronicle Basin sub-site) / Faerl-Marlow / Non (sighted) | 3 |
| CX-204 | Wierston | — | Vassal Hold / Founding Seat | Rare | A | Age of Man, founded ~1,000 BY | Faerl-Marlow | 4 |
| CX-205 | Saltmere | — | Free Trade Port | Uncommon | B | Age Unwritten | Faerl (guild-elected, no House) | 4 |
| CX-206 | Larkford Crossing | — | Ford | Uncommon | A | Age of Man | Faerl-Marlow | 3 |
| CX-207 | The Drowned Weir | — | River-Industrial Ruin | Uncommon | A | Age of Man | Faerl-Marlow | 3 |
| CX-208 | Hollowreach | — | Emptied Settlement | Uncommon | A | Age Unwritten | Faerl-Marlow | 3 |
| CX-209 | The Weir-Spoke | — | Ruin (Antarkeia Spoke) | Epic | A | Genesis-built; dormant | Antem (built) / none present | 1 |
| CX-210 | The Tallowfen Vigil | — | Shrine-Manor | Common | A | Age of Man | Faerl-Marlow (Tallowfen vassal) | 3 |

| ID | Architecture | Enemy Niche | Mechanic | Sediment | Tags | Notes |
|---|---|---|---|---|---|---|
| CX-201 | Flooded forecourt, aqueduct nave, sunken throne room — monumental water-channel architecture, older than the temple built around it | Techno-echo: Forecourt Guard, Tide Sentinel, Aqueduct Warden, Throne Revenant | Rising/falling water level as an active hazard per-Node | Bestiary §V | water, flooded, dilapidated | Formalizes locked bestiary sub-areas (Flooded Forecourt / Aqueduct Nave / Drowned Throne) as Nodes |
| CX-202 | Reed shallows, tidal hollow, fog shelf — low, wet, visibility-poor marshland | Pure native fauna dominant; Genesis-adjacent (Hollow Drifter, Fog Mimic, Shelf Specter) at the hollow/shelf only | Fog/mist visibility reduction, tidal timing windows | Bestiary §V | water, overgrown | |
| CX-203 | Grotto, submerged basin-ruin, geothermal vent-basin — vertical cave-to-basin descent | Techno-echo at the Basin specifically: Chronicle Watcher; native fauna elsewhere | The Basin sub-site: archive-guardian behavior, non-random — same Watcher every visit | Bestiary §V; Deep History YT266 | water, dilapidated | Chronicle Basin is correctly a **Node** here, not its own Complex, matching its actual locked position in the Bestiary. A still, unnaturally deep basin with regular, non-natural structures at the bottom; at least two divers never returned; a lone Non has been sighted on its shore. |
| CX-204 | Marlow's ancestral moated manor-hall, river-adjacent, generations of additions visible in the stonework | Faerl-internal (rival-branch descendants, guard NPCs) rather than monster-forward | None combat-specific — political/succession-flavored Nodes are the better fit here | House Genealogies; Faerl Age Events #50 | water, earth, elegant | House Marlow's actual founding seat — site of the Split at Wierston (~600 BY), a generations-removed succession war between two cadet branches, settled by exhaustion rather than victory. |
| CX-205 | Coastal guild-hall trade-port town, wharves and warehouses | — | — | Modern-Era Seeds | water, elegant | **Tier B** — world-texture only per existing precedent; surfaces via dialogue/item lore, no physical room planned this pass. The Free Port of Saltmere bloodlessly deposed its hereditary lord for a guild-elected magistrate council. |
| CX-206 | Ancient worn-stone ford, pre-dating House Marlow itself — no tech buried beneath, just old, storied stone | Pure native fauna (river predators) | Strong-current/undertow hazard — mechanically distinct from Wrenfield's buried-tech stumble hazard | Calendar doc §3 (Larkford vassal, locked) | water | **Deliberately different mechanic from Wrenfield (CX-104)** — two fords, same category, no redundant affordance |
| CX-207 | Collapsed lock-and-weir system, industrial river architecture — gears, sluices, stone channels | Genesis-adjacent: Undertow Shade | Water-pressure/flow hazard tied to the broken lock mechanism | Deep History (River's Quiet Refusal) | water, dilapidated | |
| CX-208 | Ordinary riverside domestic architecture, quietly and completely emptied | Genesis-adjacent: standard zombie (per locked Empty-fold rule — never named as anything else) | None distinct — a plain infestation site, intentionally the "baseline" emptied-settlement entry against which Forest's stranger ones read as anomalies | Modern-Era Seeds | water, dormant | |
| CX-209 | Sunken Antem mechanism fused into a defunct weir, mostly submerged | Techno-echo, dormant-tier | Faint suppressive field, tidal-timed (only detectable at low water) | Hollowing/Rivening §VIII | water, dormant | Second Spoke complex — see CX-113 pattern note |
| CX-210 | Small candlelit manor-shrine, a single trimmed unlit candle at its center | None — ambient/quiet complex | None — atmosphere-only | Calendar doc §3 (Tallowfen vassal motto, locked) | water, elegant | |

---

## III. THE DESERT ZONE — House Dray (Air/Desert)

| ID | Name | Parent Region | Cat | Rarity | Aff | Era | Faction | KT |
|---|---|---|---|---|---|---|---|---|
| CX-303 | Oasis | RG-02 (the Dunefields) | Wild-Complex | Common | A | Age of Man | Faerl-Dray | 4 |
| CX-306 | Solvane | RG-02 / RG-14 (Desert region network) | Vassal Hold | Rare | A | Age of Man, ~785 BY event | Faerl-Dray | 4 |
| CX-307 | Yveth's Ruin | RG-02 / RG-14 (Desert region network) | Sealed Ruin | Rare | A | Antem-adjacent origin | Antem (built) / Faerl scholar (YT151) | 2 |
| CX-308 | The Ninefinger Hold | RG-02 / RG-14 (Desert region network) | Vassal Castle | Uncommon | A | Warlord Centuries founding | Faerl-Dray | 3 |
| CX-309 | The Sahrukk Waystation | RG-02 / RG-14 (Desert region network) | Trade Waystation | Common | A | Age of Man | Faerl-Dray | 4 |
| CX-310 | The Buried Vault-Spoke | RG-02 / RG-14 (Desert region network) | Ruin (Antarkeia Spoke) | Epic | A | Genesis-built; dormant | Antem (built) / none present | 1 |
| CX-311 | The Tomb of Too Many Eyes | RG-14 (the Pyramid Fields) | Sealed Tomb (bounded) | Uncommon | A | Antem-adjacent origin *(inherited from RG-14's pre-split Complex entry)* | Antem (built) / Faerl-Dray (present use) *(inherited from RG-14's pre-split Complex entry)* | 2 |
| CX-312 | The Cracked Ascension | RG-14 (the Pyramid Fields) | Collapsed Step-Pyramid (bounded) | Uncommon | A | Antem-adjacent origin *(inherited from RG-14's pre-split Complex entry)* | Antem (built) / Faerl-Dray (present use) *(inherited from RG-14's pre-split Complex entry)* | 2 |
| CX-313 | The Contested Pass | RG-01 (the Dray Highlands) | Fortified Pass-Remnant | Rare | A | Age of Man *(inherited from RG-01's pre-split Complex entry)* | Faerl-internal, opportunistic squatters/bandits — no standing garrison *(zone context inherited from RG-01's pre-split Complex entry)* | 3 |
| CX-314 | The Fast Site | RG-02 (the Dunefields) | Sacred/Ascetic Site | Uncommon | A | ~BYT 1400 | Faerl (Long Fast tradition) — **possible duplicate, see Notes** | 2 |

| ID | Architecture | Enemy Niche | Mechanic | Sediment | Tags | Notes |
|---|---|---|---|---|---|---|
| CX-303 | Water-source clearing amid dunes, false-shape heat-haze at the margins | Pure native fauna (Mirage Hunter) | Mirage/false-target visual hazard | Bestiary §II | earth, water | Stays Complex per its own "Oasis of ___" naming flexibility — a single sufficiently-specific site, not reclassified. |
| CX-306 | Rammed-earth/sandstone vassal hold, feast-hall centered | Faerl-internal (betrayal-era NPCs); otherwise none distinct | None combat-specific — a political/ambush-flavored complex | Faerl Age Events #48 | earth, exposed, elegant | Site of the Solvane Truce (~785 BY) — a false-flag truce-feast that turned to slaughter mid-toast. "A Solvane welcome" survives in both Houses' speech for hospitality too generous to trust. |
| CX-307 | Sealed, fused-glass ruin structure, faceted and geometric | Techno-echo: Glass Shard Golem (unique, responsive rather than hostile-on-sight) | Non-standard aggro logic — the guardian responds to patience/observation, not proximity | Deep History YT151 | earth, dilapidated | Studied for two decades by the scholar Yveth Marrelle until it finally let her in, and kept her; has never let anyone else close since. A genuinely different combat-trigger mechanic from every other guardian complex in this batch — worth protecting in any future Node pass |
| CX-308 | Desert fortress-keep, rammed-earth walls | Faerl-Dray military NPCs | None distinct | Deep History (Warlord Centuries) | earth, exposed | |
| CX-309 | Caravanserai/watchtower waystation, lit for night arrivals | Pure native fauna, opportunistic (Scavenger Rat) | None distinct — low-threat trade hub | Calendar doc §3 (Sahrukk vassal motto, locked) | earth | |
| CX-310 | Antem vault mostly buried beneath a dune, only a sealed hatch surfacing | Techno-echo, dormant-tier | Dynamic terrain — sand shift periodically reveals/reseals the entrance (visit-dependent access) | Hollowing/Rivening §VIII | earth, dormant | Third Spoke complex |
| CX-311 | Grand corridor leading to a sealed burial chamber — dark, enclosed, swarm-infested | **Guardian Scarab** swarm exclusively (narrowed to this tomb's specific signature, not shared with CX-312) | Swarm-density hazard; the one survivor's line — "something with too many eyes counted us in" — is the tomb's actual name-origin, not an invented title | Deep History YT71; Bestiary §II (Grand Corridor/Tomb Entrance/Burial Chamber Nodes formally nest here) | earth, dilapidated | |
| CX-312 | A second, structurally distinct pyramid — upper tiers collapsed open to the sky, unlike the Tomb's sealed darkness | Stone Colossus, Poison Dart Sentinel (kept deliberately not overlapping with the Tomb's Guardian Scarab) | Fall/height hazard through the collapsed tiers — a different physical danger from the Tomb's swarm-in-the-dark, on purpose | Bestiary §II | earth, exposed, dilapidated | Two pyramids in the same field, one sealed-and-swarming, one open-and-collapsing, each with its own enemy signature. |
| CX-313 | Abandoned fortification remnants from three generations-apart wars, layered construction styles visible per era | Faerl-internal, opportunistic squatters/bandits rather than a standing garrison | None distinct — the point is what's *not* here: no army, just inherited grievance | 25 Treatments #14; Modern-Era Seeds ("The Hundred Years' Contest pass — Ready") | earth, dilapidated, exposed | **Placement note:** nesting this in RG-01 is a geographic call only — the source text deliberately never names either warring House ("the mountain claimant," "the river claimant"), and that ambiguity is intentional, not a gap. Sitting inside Dray's territory today does not confirm Dray was the mountain claimant historically; any future Node or dialogue text built on this entry should preserve the same silence the source does. |
| CX-314 | Modest open-air ascetic site — a marked fasting-ground, not a structure | None confirmed | None | Calendar doc §Faith table ("the desert Prophet," ~BYT 1400); Modern-Era Seeds ("Prophet of the Long Fast") | earth, exposed | **Unresolved duplicate, flagged, not decided:** the project's own decision log already notes these two Prophet references likely describe the same tradition from two different sessions, never confirmed. This entry represents both possibilities under one site rather than forcing a merge or a split — a call for whoever picks this back up. |

---

## IV. THE FIRE ZONE — House Colehurst

| ID | Name | Parent Region | Cat | Rarity | Aff | Era | Faction | KT |
|---|---|---|---|---|---|---|---|---|
| CX-402 | Fire Temple | — (unzoned; Colehurst core territory) | Temple-Ruin Complex | Uncommon | A | Antem-adjacent origin | Antem (built) / Faerl-Colehurst (present use) | 3 |
| CX-403 | Lava Cave | — | Cave-Complex | Uncommon | A | Age of Man / Antem-adjacent (Amber City) | Faerl-Colehurst / Antem (Amber City) | 3 |
| CX-404 | Emberfall (the Siege) | — | Coastal City-Ruin | Uncommon | A | Age of Man | Faerl (unaffiliated coastal city) | 3 |
| CX-405 | Emberfall (the Refusal) | — | Ancient Meeting-Ground | Epic | A | 0 YOC (pre-Breach) | Faerl-Thorne/Dray/Colehurst/Marlow (all four) | 4 |
| CX-406 | Cresthollow | — | Mining Town | Uncommon | A | Age Unwritten | Faerl-Colehurst | 3 |
| CX-407 | Aldergate | — | Battlefield-Tract | Rare | A | Age Unwritten, YT97 | Faerl-Colehurst vs. rebel vassals | 3 |
| CX-408 | Drossglen | — | Crater | Rare | A | Warlord Centuries, ~3,600 BY | Faerl (pre-House warlord) | 2 |
| CX-409 | The Sundered Spire | — | Ruined Tower | Uncommon | A | Age of Man | Faerl (petty-king, unaffiliated) | 3 |
| CX-410 | The Colehurst Forge-Hold | — | Paramount Seat/Capital | Epic | A | Age of Man founding; burned YT~90 | Faerl-Colehurst | 4 |
| CX-411 | The Cinder-Spoke | — | Ruin (Antarkeia Spoke) | Epic | A | Genesis-built; dormant | Antem (built) / none present | 1 |
| CX-412 | The Colehurst Miscast | RG-05 (the Ashen Expanse) | Cursed Site (bounded) | Rare | A | Age Unwritten (post-Breach descendant) | Faerl-Colehurst | 3 |
| CX-413 | Smokey Cave | RG-05 (the Ashen Expanse) | Cave (bounded) | Common | A | Age of Man *(inherited from RG-05's pre-split Complex entry)* | Faerl-Colehurst *(inherited from RG-05's pre-split Complex entry)* | 4 |
| CX-414 | Emberfall (Colehurst vassal hold) | — (RG-05 adjacent, not formally nested) | Vassal Hold | Common | A | Age of Man | Faerl-Colehurst | 3 |

| ID | Architecture | Enemy Niche | Mechanic | Sediment | Tags | Notes |
|---|---|---|---|---|---|---|
| CX-402 | Lava-moat bridge, torched great hall, molten throne room — grand, ceremonial, fire-cult architecture | Pure native fauna (Bridge Warden [now renamed Ford Sentinel elsewhere — do not confuse], Lava Spitter); techno-echo (Pillar Watcher, Throne Effigy) | Lava-moat crossing hazard; effigy "vote" flavor at the throne room | Bestiary §III | fire, active | **Also hosts the Sun Priest order** — the order's doctrine descends from Colehurst's battle-wizard tradition (Harn Colehurst, LOCKED) and matches this Complex's architecture and fire-cult framing. Any future Node text for CX-402 should incorporate Sun Priest NPCs/doctrine alongside the enemy niche listed here, per Faerl Age Events #42 (Harn Colehurst, LOCKED). No separate Complex needed for the Sun Priest temples. |
| CX-403 | Lava spring, slag archipelago, amber-preserved ruin — molten-to-solid gradient across one cave system | Pure native fauna (Blood Feeder, Vent Spore); techno-echo (Crystal Leech, Amber Sentinel) | Amber-preservation field at the Amber City Node specifically — unique among Fire-zone hazards | Bestiary §III; Deep History YT245 | fire, active, dilapidated | Amber City and Slag Archipelago are correctly **Nodes** of this Complex, not standalone. |
| CX-404 | Fire-adjacent coastal city ruins, siege-scarred walls | Faerl-internal siege remnants; otherwise none distinct | None combat-specific | Modern-Era Seeds | fire, water, dilapidated | **Distinct Emberfall #1.** A coastal city besieged nearly a year over a stolen betrothal-relic nobody now remembers; every surviving version of the song about it ends differently. |
| CX-405 | Open ancient meeting-ground, standing-stone negotiation circle — unwalled, ceremonial rather than defensive | None on record — a historical/political site, not a combat site | None | Chronicle of Antarkeia | fire, exposed | **Distinct Emberfall #2.** Where representatives of Thorne, Dray, and Colehurst refused Marlow's pre-march proposal to preserve rather than loot Antem relics — the last attempt at a negotiated approach before the Breach. CX-404 and CX-405 are unrelated sites sharing a name by historical coincidence. Any Node generated under either must reference its own CX-ID, never the bare name "Emberfall" alone. |
| CX-406 | Mining-shaft/tunnel-network town | Pure native fauna (subterranean) | The Slag Tortoise's periodic surfacing — a scheduled rather than random world-event | Modern-Era Seeds | earth, fire | Site of the Waking of the Slag Tortoise — something enormous surfaced, sat nineteen days doing nothing, and vanished underground again. The town still keeps a shrine to it. |
| CX-407 | Scorched open battlefield, ruined relief-column camp remains | Genesis-adjacent, unclassified (a demon moved through, unrecorded type); techno-echo (Magma Creeper colony since) | Scorched-ground hazard, residual heat | Deep History YT97/101 | fire, exposed | Smoke Year War battle site — something moved through the field and both commanders retreated in the same direction without a word. |
| CX-408 | Crater and blast radius, low audible hum | None confirmed hostile — taboo/avoidance site, not a monster den | "Don't wake what's sleeping" — proximity-triggered escalation if disturbed, otherwise inert | Faerl Age Events #47 | fire, scorched | **Differentiated from Embers' Crater (CX-601) and Latchpit's Seat (Mythic — see appendix):** Drossglen is warband-scale, still faintly warm/active; Embers' Crater is ancient/cold-case with an established salamander colony; Latchpit's Seat is a total-loss single-event site with no ongoing phenomenon at all. Three craters, three different states. |
| CX-409 | Cracked-but-standing record tower, vertical ruin | None distinct | Structural instability — the crack itself is a traversal hazard | Modern-Era Seeds | fire, elegant, dilapidated | Raised by golem-labor; sundered the winter it finished but still standing. |
| CX-410 | Massive forge-citadel, industrial/foundry capital architecture | Faerl-Colehurst military and forge-worker NPCs | Forge/heat-industrial hazards, active-foundry mechanics | House Genealogies; Deep History YT85–90 (Smoke Year War/Forgehand Rising) | fire, active, elegant | Colehurst's actual capital, previously referenced only as burning in the Smoke Year War, formally named here. High sediment value; recommend prioritizing for Node generation. |
| CX-411 | Antem mechanism fused into active magma flow, heat-warped | Techno-echo, dormant-tier | Periodic pressure venting — timed environmental hazard | Hollowing/Rivening §VIII | fire, active, dormant | Fourth Spoke complex |
| CX-412 | A site where fire behaves backward — cold flame, smoke that sinks, heat drawn inward instead of radiating out | Genesis-adjacent, unique signature (inverted-fire behavior, unlike any other Fire-zone enemy niche in this batch) | **Inverted-fire status effect** — the one genuinely novel environmental mechanic in the Fire zone so far; do not reuse this specific inversion elsewhere without deliberately meaning to echo it | Modern-Era Seeds ("The Colehurst Miscast") | fire, active | High affordance value — flagged as a priority Node for the Fire-zone pass. |
| CX-413 | An actual enclosed cave mouth and tunnel, distinct from the open ridge/forest around it | Feral Earth-stock (Cinder Hound den) | Smoke-inhalation/visibility hazard, cave-specific (contrast the Region's open ash-fall) | Bestiary §III | fire, scorched | The bounded piece carved out of RG-05 (the Ashen Expanse). |
| CX-414 | Ordinary forge-industrial vassal hold, same tier as the other Colehurst vassals in the reference table | — | — | House Genealogies (vassal list, locked) | fire | No individual narrative yet, open hook. **Corrected cross-reference:** the v1 Gazetteer's vassal table incorrectly linked this Colehurst vassal hold to CX-404/CX-405 — it is unrelated to either; see the change log. Three Emberfalls now exist as fully distinct, permanently separated IDs: CX-404 (siege city), CX-405 (ancient meeting-ground), CX-414 (vassal hold). Any future Node or dialogue text must reference by ID. |

---

## V. THE FOUR ECOTONE BORDERLANDS

| ID | Name | Parent Region | Cat | Rarity | Aff | Era | Faction | KT |
|---|---|---|---|---|---|---|---|---|
| CX-501 | The Frostbriar Stand | RG-06 (the Rimewood) | Frozen Forest-Tract | Common | A | Age of Man | Faerl-Thorne (border) | 4 |
| CX-502 | The Long Cold Shrine | RG-06 (the Rimewood) | Memorial Site | Uncommon | A | ~BYT 1000 (Long Cold migration) | Faerl (Ossmoor-descended) | 3 |
| CX-503 | The Dry Marches Waystation | RG-07 (the Sere Margin) | Trade Waystation | Common | A | Age of Man | Faerl-Thorne/Dray (border) | 4 |
| CX-504 | The Grain Winter Camp | RG-07 (the Sere Margin) | Shared-Memory Ruin | Uncommon | A | Sparse Contact Centuries | Antem + Faerl, mixed (deep prehistory) | 2 |
| CX-505 | The Fused Causeway | RG-08 (the Blackglass Flats) | Natural Glass Road | Uncommon | A | Age of Man | Faerl-Dray/Colehurst (border) | 3 |
| CX-506 | The Toll of the Green Road | RG-08 (the Blackglass Flats) | Frontier Ruin | Common | A | Sparse Contact Centuries | Faerl (pre-House) | 3 |
| CX-507 | The Vent-Warded Hollow | RG-09 (the Steaming Marches) | Geothermal Basin | Uncommon | A | Age of Man | Faerl-Colehurst (border) | 4 |
| CX-508 | The Reaving Shore | RG-09 (the Steaming Marches) | Raider-Camp Ruin | Uncommon | A | House Centuries, ~900 BY | Vantry-descended raiders | 3 |

| ID | Architecture | Enemy Niche | Mechanic | Sediment | Tags | Notes |
|---|---|---|---|---|---|---|
| CX-501 | Frost-crusted boreal pine, static and ice-locked (not animate — contrast Greenreach's moving trees) | Feral Earth-stock, cold-adapted variants | Ice-crust footing hazard | Antarkeia Addendum Pt. Two | earth, overgrown | |
| CX-502 | Small stone cairn-memorial marking the first southbound Faerling landfall | None — pure atmosphere site | None | Deep History §0 | earth, water | First physical anchor for the Faerl origin story anywhere in the gazetteer |
| CX-503 | Thornscrub caravan corrals, savanna-adjacent | Pure native fauna, scrubland-adapted | None distinct | Antarkeia Addendum Pt. Two | earth, exposed | |
| CX-504 | Low joint-camp ruins, Antem and Faerl construction side by side | None on record — friendliest documented Antem/Faerl moment | None | Deep History (Sparse Contact) | earth, exposed | Rare cross-species-cooperation sediment hook — protect this tone in any future Node pass |
| CX-505 | Natural obsidian-glass causeway through cracked badlands | Pure native fauna, heat-adapted | Reflective-glare visibility hazard (distinct from the Glass Plains' heat-haze — this is a road, not a flat) | Antarkeia Addendum Pt. Two | earth, fire, exposed | |
| CX-506 | Old logging-camp frontier, toll-offering still left at the treeline by custom | Genesis-adjacent (Goblin raiding parties) | Toll-based aggro reduction — a rare mechanic where an offering measurably lowers hostile encounter rate | Deep History (Sparse Contact) | overgrown, exposed | |
| CX-507 | Black volcanic rock steaming under snow — genuinely alien-looking terrain, Iceland-flavored | Techno-echo (Fire Elemental-adjacent, per the Great Vantry Rift kinship theory) | Simultaneous fire/ice status exposure — a status-effect design not available anywhere else in this batch | Hollowing/Rivening §IX | fire, water, scorched | |
| CX-508 | Coastal raider-camp ruins, Vantry-style shipwright architecture | Faerl-adjacent raiders (Reaving Years descendants) | None distinct | Modern-Era Seeds | water, fire, exposed | |

---

## VI. DEEP-TIME & ANTEM-ERA SITES (pre-Faerl / unzoned)

| ID | Name | Parent Region | Cat | Rarity | Aff | Era | Faction | KT |
|---|---|---|---|---|---|---|---|---|
| CX-601 | The Embers' Crater | — (unzoned; Deep-Time) | Crater | Rare | A | Deep Dream, BYT 2980 | Antem (an unnamed minor lineage — see Notes) | 1 |
| CX-602 | The Ammerin Hillside | — | Warded Ruin | Rare | A | Deep Dream, BYT 4700 | Antem | 1 |
| CX-604 | Sallowmere | — | Dissolved Council Seat | Uncommon | A | Age of Clans, BYT 1150 | Antem | 1 |
| CX-605 | The Old Council Seat | — | Gathering-Circle Ruin | Common | A | Deep Dream, BYT 3620 | Antem | 1 |
| CX-606 | Vann's Rest | — | Farhand Waystation (vanished) | Uncommon | A | Deep Dream, BYT 2750 | Farhand (deep prehistory) | 2 |

| ID | Architecture | Enemy Niche | Mechanic | Sediment | Tags | Notes |
|---|---|---|---|---|---|---|
| CX-601 | Ancient crater, warm ash-fine soil, established salamander colony | Pure native fauna (Salamander colony, thriving) | Residual-heat environment, stable/cold-case | Deep History BYT 2980 | fire, scorched | The source event's original language for this vanished lineage uses now-retired species terminology (see the project's retired-term list) — never repeat that term in Node text or anywhere else. Frame the vanished lineage as an unnamed minor Antem family only, exactly as the Faction field above already does. |
| CX-602 | A "too-regular" sealed structure on a hillside, bruised-colored ground around it, unbuilt-on since | Pure native fauna (aggressive pale spiders, thick-bodied) | Ground-discoloration zone — a passive visual tell, no active hazard confirmed | Deep History BYT 4700 | earth, dilapidated | Where a clan found a seamless, warm structure and guarded it as a threshold for three generations before the fourth broke it open. |
| CX-604 | Crumbling assembly-hall, built architecture (not a stone circle — contrast CX-605) | None confirmed | None | Deep History BYT 1150 | earth, dilapidated, elegant | Seat of one of the last true Antem confederated councils, deposed by its own upstart clan leaders rather than outside conquest. |
| CX-605 | Ring of standing stones / gathering circle — unbuilt, natural-feeling despite being deliberate | None confirmed | None | Deep History BYT 3620 | earth, exposed | **Deliberately un-architected vs. CX-604's built hall** — same "dissolved council" category, different physical identity. Several dozen scattered Antem clans loosely federated here for roughly a century before the confederation simply stopped being attended — no collapse, no villain, just erosion. |
| CX-606 | Waystation, arranged-in-a-ring belongings frozen in place since the vanishing | Genesis-adjacent: wraiths behaving as sentries rather than ambient haunting — a documented behavioral anomaly | Guard-like wraith behavior — passive unless approached, unlike standard wraith ambient-haunt logic | Deep History BYT 2750 | earth, dormant | Where a five-person hunting party vanished, belongings found arranged in a perfect ring around blackened ground — one of the oldest attested "clean vanishings." |

*(The Salt Flats, previously the flat Complex CX-603 in this section, is now RG-10 — see the Region tier above. Dossa's Field, Elder's Reach, Elmsworth Ford, Lake Ilmere, and Yalume Ford remain Gazetteer-only entries with no Complex ID assigned yet — flagged as available Complex-tier work for a future pass, same treatment as any other named-but-unbuilt gazetteer entry.)*

---

## VII. OFFSTAGE LANDMASSES

| ID | Name | Parent Region | Cat | Rarity | Aff | Era | Faction | KT |
|---|---|---|---|---|---|---|---|---|
| CX-701 | Faerling (former Ossmoor foothold) | — (offstage; not zone-bound) | Ice-Locked Ruin-Town | Rare | B | Great Forgetting→BYT 1000 | Faerl (pre-migration) | 2 |
| CX-702 | The Sunken Canopy Landing | — | Rainforest River-Mouth | Rare | B | Unconfirmed/legend | Antem (Stillwing-departure legend) | 1 |
| CX-703 | The Cracked Basin | — | Dry Riverbed Complex | Rare | B | Unconfirmed | Kessara-native (unspecified) | 1 |
| CX-704 | The Ember Coast | — | Volcanic Isle Settlement | Rare | B | House Centuries onward | Vantry-descended | 2 |
| CX-705 | The Great Vantry Rift | — | Sea-Floor Scar | Legendary | B | Unrecoverable (predates all record) | None confirmed | 1 |

*(No gameplay-detail table for this section — all five are Tier B by design. They exist for worldbuilding completeness and future hook value; none are current playable-zone territory, so architecture/enemy/mechanic fields are deferred rather than invented ahead of need. The Great Vantry Rift is the catastrophic event that split the Vantry Isles from a single continent into scattered islands; Faerlan's Fire-zone fauna are proposed descendants of whatever still smolders in its remnants.)*

---

## VIII. CROSS-ZONE / RESERVED

| ID | Name | Parent Region | Cat | Rarity | Aff | Era | Faction | KT |
|---|---|---|---|---|---|---|---|---|
| CX-999 | The Old Boneyard (outside Ossian Vale's Seat) | — (cross-zone / unspecified territory) | Burial Ground → Institution Site | Rare | A | Age Unwritten, ~30 YOC | Faerl (petty-king Ossian Vale) | 4 |

| ID | Architecture | Enemy Niche | Mechanic | Sediment | Tags | Notes |
|---|---|---|---|---|---|---|
| CX-999 | An old burial ground outside a petty-king's seat, unwanted land repurposed first as a graveyard, then as the earliest Boneyard order's quarters | Genesis-adjacent, low-threat (risen-dead, thematically fitting for a graveyard origin) | None distinct — the value here is pure sediment, the literal origin of "Boneyard-trained" as a phrase already in ambient use | Vignette Archive (Boneyard order founding passage) | earth, dilapidated | **High sediment value, zero prior gazetteer presence** — "Boneyard-trained" appears three separate times across the Bestiary and flavor sheet as ambient flavor language, but its origin location had never been entered anywhere before this. Ossian Vale's Seat itself (his petty-kingdom capital) is referenced only descriptively in the source — left unnamed here rather than inventing a proper name for it on top of an already-full pass; flagged as available naming work, same treatment as the ecotones originally were. |

---

# NODE TIER

**Not generated in this pass.** The tier is formally defined — the actual room a player walks into, nested inside a Complex, matching `ENVIRONMENTS` sheet rows — but no Node-tier content exists in the project yet outside what's already locked in the Bestiary (the sub-sites called out by name in the Complex notes above: the Flooded Forecourt / Aqueduct Nave / Drowned Throne under CX-201, Amber City / Slag Archipelago under CX-403, the Chronicle Basin under CX-203, the Grand Corridor / Tomb Entrance / Burial Chamber under CX-311, and the Summit under RG-01). Recommend prioritizing Node generation for CX-410 (Colehurst's capital) and the four Spoke complexes (CX-113, CX-209, CX-310, CX-411) first, since all five are high-sediment-value entries with no existing Node content to build outward from — everything else in this reference already has partial Node content locked in the Bestiary.

---

# MYTHIC / UNLOCATABLE SITES
*Carried forward from the Gazetteer's Section X — nothing else currently holds this content. These are canonically real but the fiction itself makes them effectively unfindable; strong candidates for an ultra-rare spawn/discovery mechanic rather than a fixed room. No Region/Complex/Node ID is assigned to any of these by design — that mirrors how the source material itself treats them, and inventing IDs for genuinely unlocatable sites would misrepresent what they are.*

| Name | Category | Rarity | Tags | Description | Sediment |
|---|---|---|---|---|---|
| **Kolek's Cavern** | Cavern (vanished) | Mythic | earth, dilapidated | An "iron-encrusted" cavern briefly opened by Chieftain Kolek Tymtrove near Antarkeia during the early Scattering Wars — held a bounty of preserved food and a manuscript in unreadable script. The entrance vanished before Kolek could lead anyone back to it and has never been found again. | Vignette Archive — Kolek's Manuscript |
| **The Vista of the Shimmering Mound** | Mountain summit (sacred) | Mythic | earth, exposed | A shorn-topped mountain — flat prairie roughly a mile across where a peak should be, with a shimmering lake at its center. Site of the Seventh Keeping, a once-a-generation trial; no champion who has ever claimed the summit has been confirmed to return. Antem Stillwing-riders were thrown from the sky if they tried to land on it. Loosely associated with RG-01 (the Dray Highlands) for zone-tagging purposes only — this is almost certainly not the same ground as the Summit (Summit Titan's location, an open-terrain feature of RG-01); do not merge the two. | Vignette Archive — The Seventh Keeping |
| **House Verrin's Hall** | Manor (destroyed, site lost) | Mythic | fire, dilapidated | Vanished overnight with all twelve occupants during the Hollowing's fourth winter — floor glassed smooth, walls folded outward. Only the word "verrin-glass" survives; the hall's actual location was never recorded by anyone who lived to write it down. | Hollowing/Rivening §II |
| **House Latchpit's Seat** | Crater (former vassal hold) | Mythic | fire, scorched | Formerly a Colehurst vassal hold (see the vassal table below); reduced to a crater in YT92 when a sealed relic was forced open against explicit warning, killing the entire household. "Locked, not lost" now survives only as regional irony. Differentiated from CX-408 (Drossglen) and CX-601 (the Embers' Crater) — see the note on CX-408. | Deep History YT92 |
| **The Room of Two Mirrors** | Manor chamber (Labyrinth sub-site) | Mythic | dilapidated | A sealed chamber in a minor noble estate where a doppelganger replaced a household's daughter. The family sealed it and scattered; its actual location has been lost to regional legend for generations. Already a locked in-game room within CX-102 (Labyrinth) — this entry is the lore anchor for it, not a separate Complex. | Deep History YT87; Bestiary §IV |

---

# VASSAL HOLD SEATS — reference table
*Carried forward from the Gazetteer's Section IX, corrected — nothing else currently holds a full list of the locked vassal names. Every vassal name below is a locked house-vassal name from the Calendar/Faiths/Houses doc; by the project's own naming convention, each implies a physical seat/hold bearing the same name in its House's zone. Most carry no individually authored event yet — draw from this list before inventing a new hold name, rather than the reverse. Entries already given a full write-up above are cross-referenced by ID, not repeated. Rarity for the whole table, unless noted: **Common** (implied castle-tier hold, no individual narrative yet — open hooks).*

| House Thorne (Forest) | House Dray (Desert) | House Colehurst (Fire) | House Marlow (Water) |
|---|---|---|---|
| Ferncrest *(→ CX-115, Rare)* | Ninefinger *(→ CX-308, Uncommon)* | Emberfall *(→ CX-414, Common — corrected; not CX-404/CX-405, see the change log)* | Wierston *(→ CX-204, Rare)* |
| Wrenfield *(→ CX-104, Rare)* | Sarrat | Slagh | Siltmoor |
| Vell | Karrow | Rusmer | Fenrow |
| Hallowmere | Dunsher | Blackpit | Reedholt |
| Sallow End *(→ the Empty Chair, Common — Gazetteer-only, no Complex ID assigned yet)* | Vantric | Sootmark | Slowmere |
| Wickmoor | Solvane *(→ CX-306, Rare)* | Forgast | Driftholt |
| Greyhollow | Yelgast | Cracklow | Tallowfen *(→ CX-210, Common)* |
| Nettlewood | Therrow | Grimstack | Rennow |
| Thistlebrake | Zerrow | Coalmark | Milbrow |
| Oakstill | Emrikk | Slagreach | Coldweir |
| Sparrowholt | Sahrukk *(→ CX-309, Common)* | Bellowscar | Larkford *(→ CX-206, Uncommon)* |
| Deepwane | Vothsand | Ironsoot | Palefen |
| Foxglen | Ammareth | Cragforge | Wrackholme |
| Bramblecote | Ghurrath | Furnrow | Sillowe |
| Palewood | Sennoth | Hammerdown | Rowanfen |
| Rookmantle *(→ CX-114, Uncommon)* | Vandrahl | Scaldmoor | Fenholt |
| Yewmark | Rashka | Pitcross | Vennow |
| Coppicewell | Barrask | Charrow | Oldweir |
| Mosshaven | Fennik | Smokerow | Brackenfen |
| Winterholt | Orrinth | Latchpit *(destroyed — see the Mythic-tier appendix above)* | Tidewrack |
| Colcross *(founding site unnamed — open item; see Notes)* | | | |

**Notes:** House Colcross's founding site (the Concord of the Four Hearths was held "on Thorne ground," and Colcross itself was founded by "a minor lord who took in refugees... under one roof" per the Faerl Age Events doc) has no named physical seat yet, despite being one of the more narratively active vassal houses — flagged as available naming work for a future session, same treatment as the ecotones originally were. The Ridgeway Party's pass, the Silence Above the Cloud Line's mountain pass, the Altitude Wraith's Pass, and the three unnamed Dray passes banned to solo travelers are all deliberately left unnamed in their source documents — inventing names for them would undo an intentional authorial choice, not fill a gap.

---

# WHAT CHANGED AND WHY (compact log)

This document stands on its own; the log below is for anyone auditing the consolidation, not required reading to use the reference above.

**Structural fix — the Region tier:** Seven Complexes originally mis-tagged as bounded were demoted to Region tier because they're actually unbounded, open-sky terrain: `CX-305` (Mountains) → **RG-01**, `CX-301` (Dune) → **RG-02**, `CX-302` (Glass Plains) → **RG-03**, `CX-103` (Dense Brush) → **RG-04**, `CX-401` (Ashen Land) → **RG-05**, `CX-603` (the Salt Flats) → **RG-10**, `CX-304` (Pyramids) → **RG-14**. All seven of those Complex IDs are now retired; none are reused. Their genuinely bounded sub-features were split out as their own new Complexes: CX-116 (from RG-04), CX-413 (from RG-05), CX-311/CX-312 (from RG-14), CX-313 (from RG-01). Dune, Glass Plains, and the Salt Flats have no specifically-named bounded feature to carve out yet — that's a legitimate open state for a Region, not a gap.

**ID-hygiene fixes:** two ad-hoc suffixed/misnumbered IDs from earlier drafts were retired and renumbered before this consolidation — the pass complex once called `CX-306b` is **CX-313**, and the Cracked Ascension once misnumbered `CX-415` (wrong zone block) is **CX-312**. Both retirements are registered in `dfvalidate.py`'s `RETIRED_IDS` and are expected to keep appearing in explanatory text like this paragraph.

**Content gaps filled:** six locations previously "Ready" in Modern-Era Seeds but never entered are now in the reference: CX-412 (the Colehurst Miscast), CX-117 (Widow's Rest, under the newly-formalized RG-13), CX-314 (the Fast Site), CX-999 (the Old Boneyard), and the two Pyramid Fields Complexes (CX-311, CX-312).

**Collision fixed — the Emberfall triple:** three unrelated locations shared the name "Emberfall." All three now have permanently separate IDs — CX-404 (the siege city), CX-405 (the ancient meeting-ground), CX-414 (the Colehurst vassal hold) — and the Gazetteer's old vassal-table cross-reference, which incorrectly linked the vassal hold to the other two, is corrected in the vassal table above.

**Retired vocabulary removed from live content:** the Gazetteer's Antarkeia entry referenced a retired origin-story term for the landing site; that phrasing is not carried into this document's CX-000 entry. CX-601's Faction field and Notes no longer spell out the retired species term for its vanished lineage — both now point to the project's retired-term list instead, per the same self-referential-audit-note convention the validator script's own documentation recommends, so the term isn't reintroduced by an audit note that quotes it in order to warn against it.

**What was deliberately not done:** the three source files are not deleted, retired, or marked superseded by this document — that's left to whoever runs this project next. Dossa's Field, Elder's Reach, Elmsworth Ford, Lake Ilmere, and Yalume Ford remain Gazetteer-only entries with no Complex ID; Sallow End's "Empty Chair" custom and House Colcross's founding site are likewise still open naming work, exactly as they were before this pass, flagged rather than filled in on top of an already-large consolidation. The Mythic-tier sites were given no Region/Complex/Node IDs, matching how the source material treats them as genuinely unlocatable rather than force-fitting a schema built for placeable rooms.

---

# STATUS

Starting point verified clean: `python3 dfvalidate.py` run against the three original source files exactly as they stood before this consolidation reported **zero errors, zero warnings** (two informational retired-ID notes only). This consolidated document was then built from that clean baseline and re-validated; see the validator result reported alongside this document's delivery.

Fourteen Regions (RG-01–RG-14) and sixty-nine Complexes are locked as the corrected skeleton, all IDs unique and zone-consistent, three tiers fully defined with the Node tier formally empty pending Batch 2. Ready for Node-tier generation — see the Node Tier section above for the recommended starting order.
