# Demon Fighter — Project File Precedence
## Determined August 11, 2026; updated September 11, 2026; updated September 16, 2026; cleanup note October 14, 2026 (see notes below)

This document exists because file upload dates aren't reliably visible and files can't be
renamed in place. When any file below conflicts with another, **this document's ranking
wins.** If this document itself is superseded later, a newer one with a later date should
replace it as the authority, and should say so explicitly at the top the way this one does.

**2026-10-14 cleanup:** removed from the project: `DemonFighter_File_Precedence_2026-09-11_REVISED.md` (superseded by this file) and one of two word-for-word identical copies of `DemonFighter_25_Narrative_Treatments_FINAL.md`. Both copies of the game data workbook were deliberately **kept** — see Game Data Workbooks below for how to tell them apart.

**2026-09-16 update:** the "Faerl Age" work session (house genealogies, 52 new historical events, 25 full narrative treatments) produced three new reference documents, corrected two existing files in place, and confirmed one existing supersession that had never actually been acted on. All changes below.

**2026-09-11 update:** during a full-project scrape as part of the Antem-Extinction Overhaul, all four files named in the original "Reference / Core Lore" entry below were found to still describe Antem, Kin, and Empties as living present-day populations — none of them had been touched by the overhaul, because none were on its original file list. They've been merged into one new, fixed, and genuinely complete document (see below) rather than patched separately, since patching four overlapping files independently is exactly what let them drift out of sync with each other and with current canon in the first place.

---

## Reference / Core Lore

**Authoritative: `DemonFighter_Reference_CONSOLIDATED.md`**
Supersedes: `DemonFighter_Reference.docx`, `DemonFighter_Reference_REVISED.docx`, `DemonFighter_ReferencewithMadLibtemplate.docx`, and `DemonFighter_Reference_Marten_Timeline.md` — all four are retired. The consolidated file merges everything genuinely unique from all four (the bar vignettes and character roster from Reference_REVISED; the Marten vignette, locked timeline, tech-as-magic rules, and dev-approved Madlib Template from the MadLibTemplate version; nothing unique in the Marten_Timeline file, confirmed a strict subset) into a single document, with the species section fixed to match current canon (Antem extinct, Kin cut entirely, Vessels extinct, Empty folded into the Bestiary) and pointed at Species Canon as the actual source of truth for species detail, rather than duplicating it here again.

---

## The Faerl Age (Ark 1's descendants, ~5,150 BY to Year Zero)

**New, 2026-09-16. No prior document covered this era in structured form — the old flat "Age of Man" label and its sparse entries are superseded by the era structure below.**

- **`DemonFighter_HouseGenealogies_FINAL.md`** — authoritative for the Dray/Colehurst/Thorne/Marlow paramount succession lines, founder to fragmentation. Cross-checked against the Chronicle of Antarkeia's already-locked Breach-era paramounts (Torvin Dray, Harroc Colehurst, Maren Thorne, Denna Marlow) — an earlier draft of this table used four different invented names for that generation and one outright wrong role assignment (Pell Dunmarrow, who is actually a locked deserter, not a paramount); both are corrected in this final version.
- **`DemonFighter_FaerlAge_HistoricalEvents_FINAL.md`** — the 52 new dated events (45 original + 7 fantasy/sci-fi-crossover-inspired additions) plus this session's full locked-decisions log, for future-session onboarding the same way the Antem-Extinction Overhaul decision log works for that thread.
- **`DemonFighter_25_Narrative_Treatments_FINAL.md`** — full prose treatment of 25 events spanning the entire timeline (Genesis through post-Breach), not just the Faerl Age.

## Two existing files corrected in place, 2026-09-16

- **`DemonFighter_Vignette_Archive_9_11_26.docx`** — three "Scattering Wars" era-label references corrected to match the new Faerl Age era names (the Widow's Accords → the Widow's War; the Charter of Salt and Silence and the Boy Who Counted Stars → Charter Centuries). One reference (Kolek's Manuscript) was deliberately left unchanged — nothing in it requires a Faerl presence, and it reads as legitimately Antem-era as written. The orphaned "Rensk Hollow" reference (confirmed, project-wide search, to appear nowhere else) was dropped from the Boy Who Counted Stars dating line rather than reconstructed.
- **`DemonFighter_Calendar_Faiths_Houses_History_Corrections_REVISED.md`** — the White Wind's era placement, previously flagged and unresolved, is now locked to Landfall Centuries; the corresponding "still open" note removed.

## One pre-existing supersession, confirmed still outstanding

- **`DEMON_FIGHTER__DATA.xlsx`** was already marked superseded by `DEMON_FIGHTER__DATA_1.xlsx` on 2026-09-11 (see Game Data Workbooks, below) but was never actually removed from the project. Still recommended for deletion — nothing depends on it.

---

## Game Data Workbooks

**Authoritative: `DEMON_FIGHTER__DATA_1.xlsx`**
Supersedes: `DEMON_FIGHTER__DATA.xlsx` — **confirmed still present in the project as of 2026-09-16 and safe to delete.**

Confirmed by direct comparison of the `flavor` sheet: `DATA_1.xlsx` contains the current,
rewritten room-description content; `DATA.xlsx` still holds old placeholder text (e.g. generic
"A massive stone fortress..." Castle descriptions vs. the current rewritten versions). Any
future spreadsheet work should read from and write to `DATA_1.xlsx` only.

**2026-10-14 — two copies, same display name.** The project holds two files both shown as `DEMON FIGHTER  DATA 1.xlsx`. Both are kept on purpose. Tell them apart this way:

- **AUTHORITATIVE — the larger copy (~1.5 MB, uploaded 2026-07-28).** Has the `flavor` tab (991 rows), `triggers`, `MECHANIC_LIST`, `ActionValueWeights`, `CADENCE_LIST`. Use this one for all flavor and data work.
- **REFERENCE ONLY — the smaller copy (~1.1 MB, uploaded 2026-07-02).** Older. No `flavor` tab. Kept only because it holds a `new Suffix` tab (209 rows, a reworked suffix table with Mechanic 1/2/3 columns) that the newer copy doesn't have. That tab is mechanics data in Sean/Max's lane; pending Sean's word on whether it matters. Do not read flavor or room content from this copy.

When asking Claude to work from "the workbook," it means the larger copy unless stated otherwise.

---

## Everything else in the project

- `DemonFighter_Bestiary_v2_ThreeTier.md` — unaffected, current.
- `DemonFighter_Antarkeia_Addendum_and_Geography_REVISED.md` — current.
- `DemonFighter_Cragwings_Addendum.md` — errata already applied to `DemonFighter_Chronicle_Antarkeia_REVISED.md`; superseded content only, kept for history.
- `DemonFighter_FlavorSheet_Content_Package.md` — current.
- `DemonFighter_FlavorText_Pipeline_Status.md` — current.
- `The_First_Child.pdf` (Genesis text) — current; re-reviewed in full 2026-09-16, still clean, no character names in this source conflict with anything drafted this session. **Keep out of Cursor's context** — it is the hidden sci-fi layer and must not leak into player-facing flavor work.
- `DemonFighter_Chronicle_Antarkeia_REVISED.md` — current. **Authoritative for all Breach-era paramount and "Ten Who Were There" names** — checked against this file, not assumed, when drafting anything touching the Breach.
- `DemonFighter_TheHollowing_and_TheRivening_REVISED_v2.md` — current.
- `DemonFighter_Archive_ModernEraSeeds_REVISED.md` — current.
- `DemonFighter_Madlib_Spectrum.pdf` — current, unaffected. Its slot definitions are carried into the Cursor format spec (`docs/flavor/format-spec.md` in the repo).
- `DemonFighter_DeepHistory_FiveEras_2026-08-12_CORRECTED_v4.md` — current.
- `DemonFighter_Species_Canon_2026-09-10_REVISED.md` — current, the single authority for species detail.
- `DemonFighter_SpeciesCanon_Addendum_ArkTwoScattering_2026-08-12.md` — current.
- `DemonFighter_AntemOverhaul_DecisionLog_2026-09-11_v2.md` — the living record of the Antem-Extinction Overhaul specifically; read first for anything touching Antem, species canon, or Antarkeia's rewrite.

---

## How to keep this current

Whenever a future session produces a document that supersedes something on this list,
update this file (or note the change here) rather than letting two documents silently
disagree. This file is the tiebreaker — not the newest upload, not the biggest file, not
whichever one gets read first.
