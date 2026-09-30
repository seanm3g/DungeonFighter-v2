# Affix visual rules — painted dark fantasy

## Rendering reference — September 22, 2026

Use [STYLE-GUIDE.md](STYLE-GUIDE.md) and its [primary mace reference](References/painted-mace-style-target.png) for rendering. This supersedes older flat-only, smooth-surface, and no-shine wording below: use sculpted painted volume, restrained component gradients, contact shadows, selective highlights, and material-following brush texture. Print distress remains separate. Preserve the existing component ownership and affix mappings; the reference's colored motifs are not an automatic change to current crest colors or effect anchors. September 20 ownership/crest rules take precedence over the earlier emission examples and tables retained below.

## Ownership and crest correction — September 20, 2026

This revision supersedes independent grip/fitting choices and colored magical overlays below. Quality owns the grip palette and workmanship; material owns the working-body palette and compatible fitting palette; prefix owns construction changes and a small material-colored embossed crest near the handle joint; suffix owns the endcap motif/inset. Flaming, Poisonous, Charged, Enchanted and Blessed use restrained relief symbols drawn with the host material's shadow and highlight, not large colored emission. There is no separately randomized grip or fitting. The live preview documents these ownership mappings beside the selected item. Print distress is an independent alpha mask removing pigment from the finished artwork, with a visibly stronger slider range; it does not roughen physical item materials. Legacy recipes migrate to these ownership rules.

## Anchor correction — September 20, 2026

This correction supersedes tip-based placement in the original examples below. Prefix effects originate on the handle and cross its junction into the working body: lower blade for swords/daggers, socket into head for maces, grip into shaft for wands. Use a small shared grip source and an upward-growing painted effect. Keep the weapon tip clear; do not move emission to the wand's ornamental head. Position by the joint, not by total weapon length. Suffixes continue to occupy separate fitting/endcap anchors.

Proposed art contract for concept generation. Companion to STYLE-GUIDE.md and References/painted-mace-style-target.png. This defines appearance only; it does not change affix effects, generation odds, equipment legality, or game balance. No renderer changes are included.

Catalog basis: GameData/Modifications.json (23 adjectives), PrefixMaterialQuality.json (16 materials and 11 qualities), StatBonuses.json (stat, creature and narrative suffixes). Display order remains Quality → Adjective → Material → Base item → Suffixes, as defined by ItemPrefixHelper.cs. Render order differs from name order.

## Component ownership

Each base design has named regions and attachment points. Resolve semantic regions before generating art.

| Region | Sword example | Other equipment equivalents |
| --- | --- | --- |
| Working body | Blade | Axe/hammer/mace head; spearhead; bow limbs; crossbow prod; armor plates; shield face; cloak body |
| Support | Tang/guard structure | Haft; crossbow stock; armor straps; shield rim; garment seams |
| Contact | Grip wrap | Bow grip; handle; lining; boot cuff |
| Fittings | Guard and pommel | Socket, endcap, buckle, clasp, shield boss |
| Motif anchor | Pommel face | Socket face, stock medallion, chest emblem, clasp |
| Effect anchor | Blade edge or fuller | Striking face, spear tip, bow limb tips, plate border, garment hem |

Do not invent a blade on a bow or a hilt on armor. Each base asset records applicable anchors and a fallback fitting. Flail chain remains connected, bowstring stays connected and functional, and silhouette identity is preserved.

## Ownership and order

1. Base item owns proportions, function and recognizable silhouette.
2. Material owns working-body color and broad painted planes.
3. Quality owns workmanship cues and one small construction detail.
4. Adjective owns one localized construction change OR one primary magical effect, plus at most one supporting color accent.
5. Suffix owns one motif on fittings/contact regions, plus at most one small accent. It does not replace the whole item's material.
6. Resolve visual conflicts and apply consistent painted lighting.
7. Apply the separate worn-art finish across the completed painting.

The clean base stays recognizable through every combination. Prefix and suffix cues must be distinguishable by location and motif, not just color.

## Materials

Default: change the working body and a matching fitting; preserve contrasting grip/shaft materials. Material means visual identity, not a physics claim about every component. Cloth and leather equipment receive the material through fittings, trim or reinforcement, rather than becoming solid metal garments.

| Material | Paint treatment |
| --- | --- |
| IRON | Charcoal body, narrow dull ivory plane |
| Steel | Slate body, broad pale edge plane |
| Bronze | Muted brown-ochre body, umber shadow |
| Gold | Subdued golden ochre, brown shadows, selective ivory bevel highlights |
| Silver | Cool gray with pale ivory plane, dark blue-gray shadow |
| Mithril | Desaturated blue-gray with a thin cool pale accent |
| Bone | Warm ivory, umber shadow, smooth broad shape |
| Glass | Flat smoky gray-green, one pale stripe; no refraction simulation |
| Crystal | Two or three broad lavender-gray facets; no sparkle spray |
| Obsidian | Near-black violet plane, one sharp painted gray edge |
| Shadow | Near-black body with muted violet boundary; retain readable silhouette |
| Willow | Dark umber, restrained olive undertone; intact smooth silhouette |
| Stone | Carved warm gray planes, restrained brush mottling and dark recesses |
| Celestial | Pale gray with muted ochre inset; no automatic aura |
| Strange | Muted plum plane with one asymmetric color division |
| Unknown | Neutral slate and a small unidentified inlay; deterministic fallback |

Material alone does not emit light. Material variants use the same lighting and rendering complexity.

## Quality

Quality does not control the distressed-print overlay. By default keep physical surfaces smooth, including low-quality equipment.

| Quality | Local construction cue |
| --- | --- |
| Broken | One loose wrap or visibly misaligned fitting; do not break the functional silhouette |
| Battle Scarred | One restrained old repair band; no dense scratches |
| Worn | Slightly faded grip color |
| Second Hand / Preowned | Plain mismatched replacement binding / neutral base treatment |
| Like New / New | Complete simple wrap / clean aligned fittings |
| Perfect | Neat balanced fittings with a small pale accent |
| Masterwork | One deliberate crafted border or join |
| Heirloom | One old-fashioned fitting shape; suffix retains motif anchor |
| Cosmic | One restrained geometric inset; no mandatory glow |

These are art interpretations of existing labels, not changes to their mechanical meanings. If explicit physical damage is later desired, approve it as a separate treatment.

## All current adjective prefixes

Dimensions below are art tuning suggestions; small shifts must remain inside the base item's category and attachment constraints.

| Prefix | Primary visual change | Supporting cue |
| --- | --- | --- |
| Reinforced | One broad reinforcement collar/band at a load-bearing joint | Dark bronze fitting |
| Balanced | Matched guard/endcap proportions | Centered grip band |
| Precise | Straight narrow inset line on working body | Small aligned fitting |
| Swift | Slightly swept fitting shape | Single diagonal grip stripe |
| Featherweight | Slightly slimmer fitting | Pale contact-region binding |
| Hardened | Darker broad working-body shadow plane | Simple thick edge band |
| Light | Reduced fitting bulk | Narrow wrap; no glow despite the name |
| Heavy | Modestly broader working head or body | Wider collar |
| Short | Working length shortened about 10% where applicable | Proportions stay functional |
| Long | Working length extended about 10% where applicable | Preserve full silhouette padding |
| Blessed | Small ivory mark on working-body base | Muted ochre accent, no automatic halo |
| Enchanted | One short indigo rune group on working body | Optional low-intensity painted emission |
| Charged | One angular pale-ochre energy mark at effect anchor | At most one short detached spark |
| Acrobatic | Curved compact fitting | Paired angled grip bands |
| Ancient | Archaic guard/socket profile | Muted bronze binding; age without corrosion |
| Keen | One thin uninterrupted pale working-edge shape | No second effect |
| Flaming | Local oxblood/ochre flame shapes at effect anchor | Small warm reflected paint patch |
| Poisonous | Muted olive painted channel/inset | One droplet-shaped mark; no dripping cloud |
| serrated | Three to five broad shallow edge teeth where cutting edge exists | No random chips |
| refined | One elegant planar inset/border | Slim orderly fitting |
| Nimble | Compact fitting and short taper | One narrow grip accent |
| Brutal | Squared heavier working-head profile | Dark red grip band |
| Sturdy | Thick socket/support join | Broad dark binding |

On incompatible equipment, Short/Long affect a safe support element only if meaningful, otherwise a compact/elongated fitting. Serrated becomes a small sawtooth border motif, never teeth on a bowstring or cloth silhouette. Never infer a new mechanical effect from an art cue.

## Suffix grammar

Suffixes express identity on secondary parts. A serpent suffix can change a guard into a restrained coiled form, a wolf suffix can add a tooth-shaped pommel, and a protection suffix can put a shield motif on a fitting. None needs a whole-item recolor.

### Stat suffixes in the catalog

| Exact suffix | Motif / secondary component |
| --- | --- |
| of Accuracy | Single sight-line mark on guard/socket |
| of Agility | Swept chevron on grip or fitting |
| of ferosity | Small paired fang motif on endcap |
| of fleeting | Two trailing strokes on grip band |
| of Intelligence | Small eye or diamond inlay |
| of killing | Oxblood teardrop inset |
| of Power | Broad wedge emblem on fitting |
| of Precision | Nested narrow diamond mark |
| of Protection | Shield-shaped pommel/buckle inset |
| of Rejuvination | Small leaf-shaped inlay |
| of Strength | Squared knot or block-shaped endcap |
| of Technique | Interlocking angle mark |
| of Vitality | Small seed/heart-shaped red inset |

Keep source spelling for identity lookups; display-name cleanup is outside this art proposal.

### Creature suffix families

Map explicit suffix IDs to families before rendering. Family motifs are abstractions, not tiny literal animal portraits. Proposed examples:

| Family | Example catalog suffixes | Motif and location |
| --- | --- | --- |
| Canine | Wolf, Dire Wolf, Bloodhound | Angular fang guard/endcap |
| Feline | Panther, Lion, Tiger, Lynx | Curved claw fitting or split-eye inset |
| Serpent | Cobra, Viper, Python, Jade Serpent | One smooth coil around guard/socket, small eye inset |
| Bird | Raven, Owl, Hawk, Golden Eagle | Feather-shaped guard or quill mark |
| Horned beast | Ram, Elk, Auroch | Restrained curved horn fitting |
| Shell/armor | Armadillo, Tortoise, Crab, Pangolin | Two overlapping plate shapes on fitting |
| Insect/arachnid | Scarab Beetle, Spider, Scorpion | Compact geometric carapace or hooked endcap |
| Aquatic | Kraken, Octopus, Nautilus | One curled fitting or spiral inset |
| Mythic | Dragon, Phoenix, Thunderbird, Kirin | One recognizable wing/horn/feather motif; effect only if explicitly configured |

Individual mappings can distinguish related animals by motif geometry. Do not invent fire for Phoenix or poison for Cobra as a mechanical claim. Optional luminous cues require an explicit visual recipe tied to the real affix, not name guessing.

### Narrative suffixes

Use one small personal detail: a tied ribbon for `cherished by lovers`, a repaired binding for `mended by a sister`, a stitched contact wrap for `stitched by a mother`, a pale fitting inset for `dipped in moonlight`, or a dark-red binding for `bathed in blood`. Keep these subordinate and avoid literal scenes. `rusted by Dew` may use a muted green fitting tint rather than rough corrosion. The rest need explicit catalog mappings before production; this document does not claim all suffixes are individually mapped.

## Glow and effects

- Glow is flat painted emission: a small pale source shape, one colored surrounding shape, and optionally one reflected patch on an adjacent component.
- No blurred bloom, realistic volumetric light, particle showers or neon outlines around the whole item.
- One primary emission source per item. At most two additional small non-emissive accents.
- Aim for effect coverage below about 10% of the silhouette area; keep the base material dominant.
- Flame, spark, smoke and poison shapes remain a few deliberate painted marks. They must not obscure construction.
- Use a hue and a shape cue together so affixes remain distinct without color alone.

## Combining affixes deterministically

Budget: one construction edit, one dominant motif, one primary effect, and up to two small supporting accents. These are maxima, not requirements.

1. Use stable affix IDs and explicit semantic recipes. Do not roll appearance again every render.
2. Material controls working-body base paint. Prefix effects may overlay a bounded region but never replace that material wholesale.
3. Adjective has first claim on working-body/effect anchors. Suffix has first claim on motif/fitting anchors.
4. If a suffix wants an occupied fitting, use the next compatible fitting, then contact-region emblem. Never stack unreadable motifs.
5. If several suffixes compete, select the configured visual-priority winner, break ties by stable ID. A second may contribute one small non-emissive mark; remaining suffixes stay in item text/mechanics.
6. If emission conflicts, prefix emission wins; otherwise select highest visual priority then stable ID. Demote other cues to inlays or omit their visual effect. Gameplay remains unchanged.
7. Rarity does not override this budget or create mandatory glow. More affixes do not justify more noise.
8. Unknown affix: preserve the base and record it as unmapped; do not hallucinate an effect. Same recipe plus same seed yields the same treatment.

## Worked concept recipes

These are art examples, not promises that a particular combination is legal in the current loot tables.

| Combination | Resolved appearance |
| --- | --- |
| Flaming Steel Longsword of the Cobra | Slate blade remains; small ochre/oxblood flame at tip; bronze coil guard and dark grip; no added poison aura |
| Reinforced Bone Mace of Protection | Smooth ivory mace head; thick dark socket collar; small shield emblem on endcap; no glow |
| Charged Obsidian Spear of the Raven | Violet-black spearhead; one angular pale charge mark; feather-shaped socket fitting; umber shaft |
| Masterwork Mithril Bow of Accuracy | Blue-gray limb treatment; neat support join; sight-line mark at grip; connected string; no invented blade or aura |
| Poisonous Bronze Dagger of Vitality | Ochre blade, narrow olive channel; small dark red pommel inset; no competing large glow |
| Ancient Silver Breastplate of the Wolf | Cool gray plate; archaic bronze fastening; small fang-shaped clasp; smooth surfaces and flat painted highlights |

## Reusable prompt appendix

Start with STYLE-GUIDE.md generation brief, then add:

> Keep the supplied base item's silhouette, camera angle, light direction and painting style. Material [ID] changes [REGIONS] to [PALETTE]. Quality [ID] adds [LOCAL CUE]. Prefix [ID] changes only [REGION/SHAPE/EFFECT]. Suffix [ID] changes only [FITTING/MOTIF/ACCENT]. Preserve [UNCHANGED COMPONENTS]. Use the resolved effect budget: [ONE EFFECT OR NONE], [MOTIF], [SMALL ACCENTS]. Dimensional construction; high-contrast painted planes with restrained gradients, contact shadows and material-specific brush texture. Distressing is a separate worn-print finish across the finished artwork and stays constant across these variants.

For review, compare one base plus material-only, prefix-only, suffix-only and combined variants of the SAME item. Confirm each modifier is readable, the combination remains uncluttered, and the approved rendering style survives.
