#!/usr/bin/env python3
"""
DEMON FIGHTER — Location ID Validator (v3)
============================================
v1 shipped with a real bug: it treated every markdown table row starting
with an ID as a "definition," including Gameplay-Detail rows that
INTENTIONALLY reuse an Identity row's ID (that's the documented two-table-
per-entry format this project uses). That produced ~60 false-positive
"duplicate ID" errors on the very first real run. Fixed in v2 by parsing
table HEADERS and only comparing "Name" columns against each other.

v2 still produced two categories of recurring false positive at the
"read every warning" stage: (a) dangling-reference warnings for IDs that
were deliberately retired/renumbered, which re-fire every run forever
unless registered somewhere, and (b) retired-terminology hits inside the
script's own kind of self-referential audit notes. v3 fixes (a) with an
explicit RETIRED_IDS registry (see below — add an entry the moment you
retire an ID, and it never warns again, but still surfaces as a visible
INFO line so retirement stays auditable). (b) is NOT fixed by adding more
regex — it's fixed by a writing-convention change: audit notes should
reference "the retired-term list" rather than re-typing the actual banned
words, which stops the false positive from being generated at all. The
self-reference filter stays as a safety net for cases where that
convention slips, not as the primary fix.

WHAT THIS CHECKS (v2)
----------------------
1. Duplicate ID definitions — same ID given two different NAMES across
   Identity-style tables (tables with a "Name" header column). Gameplay-
   detail tables (Architecture/Enemy Niche/etc., no "Name" column) are
   correctly treated as detail-of, not redefinition-of.
2. Ad-hoc suffixes — any ID like CX-306b. Locked convention: sequential
   numbers only.
3. Zone-block mismatches — ONLY checked where a row has an explicit
   Zone/Parent column value naming a different zone than the ID's numeric
   block predicts. No fuzzy whole-row keyword scanning (v1's version of
   this was too noisy to trust and has been removed).
4. Dangling references — an ID mentioned anywhere in the text that is
   never defined as an Identity-table row. This is the check that has
   caught real problems twice now (undefined Regions referenced only in
   parent-pointers) — keep taking its output seriously.
5. Duplicate names under different IDs — same location name attached to
   2+ different IDs (the pre-fix three-Emberfall shape).
6. Retired-vocabulary leaks — flagged as WARNING, not auto-ERROR, when
   the surrounding line looks like a self-referential audit note (contains
   words like "retired", "grep", "flagged", "clean", "checked") rather
   than live content. Still shown either way — context decides, script
   flags.
7. Retired-root leaks on [NEW]-marked names only.

USAGE
-----
    python3 dfvalidate.py file1.md file2.md ...

Run against every current location file together — cross-file duplicate
detection is the main point and is invisible file-by-file.

Exit 0 = no ERROR-level findings. Exit 1 = at least one.
WARNINGs never block — they're judgment calls, always worth reading anyway.
"""

import re
import sys
from collections import defaultdict

ZONE_BLOCKS = {
    "CX-000": "Antarkeia (standalone)",
    "CX-1": "Forest (Thorne)",
    "CX-2": "Water (Marlow)",
    "CX-3": "Desert (Dray)",
    "CX-4": "Fire (Colehurst)",
    "CX-5": "Ecotone borderlands",
    "CX-6": "Deep-Time / unzoned",
    "CX-7": "Offstage landmasses",
    "CX-999": "Cross-zone / unspecified territory (reserved)",
}
ZONE_NAME_HINTS = {
    "CX-1": ["forest", "thorne"],
    "CX-2": ["water", "marlow"],
    "CX-3": ["desert", "dray"],
    "CX-4": ["fire", "colehurst"],
    "CX-5": ["ecotone"],
    "CX-6": ["deep-time", "deep time", "unzoned"],
    "CX-7": ["offstage"],
}

RETIRED_TERMS = [
    r"\bCindergate\b",
    r"\bVhennic\b",
    r"\bReachborn\b",
    r"\bBattle of Antarkeia\b",
]
SELF_REFERENCE_MARKERS = re.compile(
    r"retired|grep|flagged|checked|re-ran|re-verified|clean\b|leak|audit|scrape", re.IGNORECASE
)

RETIRED_ROOT_PREFIXES = ["Ash-", "Oss-", "Cor"]
ROOT_WHITELIST = [
    "Ashen Land", "Ashen Reach", "Ashen Expanse", "Ashwing Harpy",
    "Ossian Vale", "Ossmoor",
    "Corah", "Corvane", "Corwin",
]

# IDs that were deliberately retired/renumbered during a fix, and are
# expected to keep appearing in prose explaining that history (e.g. "the
# original draft called this CX-306b, renumbered to CX-313"). Without this
# registry, every such explanation re-triggers a dangling-reference warning
# forever, which trains whoever's triaging it to stop reading warnings
# closely — the opposite of what this script is for. Add an entry here at
# the moment you retire an ID; never leave it to the warning to keep
# reappearing. Format: retired_id -> (replacement_id, short reason).
RETIRED_IDS = {
    "CX-306b": ("CX-313", "ad-hoc suffix eliminated, Contested Pass"),
    "CX-415": ("CX-312", "wrong zone-block number fixed, Cracked Ascension"),
    "CX-305": ("RG-01", "demoted from flat Complex to Region (Mountains -> the Dray Highlands), Locations consolidation"),
    "CX-301": ("RG-02", "demoted from flat Complex to Region (Dune -> the Dunefields), Locations consolidation"),
    "CX-302": ("RG-03", "demoted from flat Complex to Region (Glass Plains), Locations consolidation"),
    "CX-103": ("RG-04", "demoted from flat Complex to Region (Dense Brush -> the Farthicket), Locations consolidation"),
    "CX-401": ("RG-05", "demoted from flat Complex to Region (Ashen Land -> the Ashen Expanse), Locations consolidation"),
    "CX-603": ("RG-10", "demoted from flat Complex to Region (the Salt Flats), Locations consolidation"),
    "CX-304": ("RG-14", "demoted from flat Complex to Region (Pyramids -> the Pyramid Fields), Locations consolidation"),
}

ID_TOKEN = re.compile(r"\b(CX|RG)-(\d+)([a-zA-Z]*)\b")
SEP_ROW = re.compile(r"^:?-+:?$")


def normalize_id(raw_cell):
    m = ID_TOKEN.search(raw_cell.replace("*", ""))
    if not m:
        return None
    return f"{m.group(1)}-{m.group(2)}{m.group(3)}"


def parse_tables(path):
    with open(path, encoding="utf-8") as f:
        lines = f.readlines()
    tables = []
    current = None
    for i, raw in enumerate(lines, 1):
        line = raw.rstrip("\n")
        stripped = line.strip()
        is_row = stripped.startswith("|") and stripped.count("|") >= 2
        if is_row:
            cells = [c.strip() for c in stripped.strip("|").split("|")]
            if current is None:
                current = {"header": cells, "rows": [], "header_line": i}
            elif all(SEP_ROW.fullmatch(c) for c in cells if c != ""):
                continue
            else:
                current["rows"].append((i, cells))
        else:
            if current is not None:
                tables.append(current)
                current = None
    if current is not None:
        tables.append(current)
    return tables, "".join(lines)


def col_index(header, *names):
    for i, h in enumerate(header):
        h_clean = h.strip().lower()
        for n in names:
            if n in h_clean:
                return i
    return None


def collect(paths):
    identity_defs = defaultdict(list)
    all_text = {}
    all_ids_mentioned = defaultdict(set)

    for path in paths:
        tables, text = parse_tables(path)
        all_text[path] = text
        for m in ID_TOKEN.finditer(text):
            all_ids_mentioned[f"{m.group(1)}-{m.group(2)}{m.group(3)}"].add(path)

        for table in tables:
            header = table["header"]
            id_col = col_index(header, "id")
            name_col = col_index(header, "name")
            zone_col = col_index(header, "zone", "parent")
            if id_col is None or name_col is None:
                continue
            for line_no, cells in table["rows"]:
                if id_col >= len(cells) or name_col >= len(cells):
                    continue
                id_ = normalize_id(cells[id_col])
                if not id_:
                    continue
                name = cells[name_col].strip()
                zone_val = cells[zone_col].strip() if zone_col is not None and zone_col < len(cells) else None
                identity_defs[id_].append((path, line_no, name, zone_val))

    return identity_defs, all_text, all_ids_mentioned


def check_duplicate_ids(identity_defs):
    errors = []
    for id_, occ in identity_defs.items():
        names = set(n.strip().lower().rstrip(".") for _, _, n, _ in occ if n and "see part" not in n.lower())
        if len(names) > 1:
            detail = "; ".join(f"{p}:{ln} -> '{n}'" for p, ln, n, _ in occ)
            errors.append(f"[ERROR] Duplicate ID '{id_}' has different Names across Identity tables: {detail}")
    return errors


def check_suffixes(identity_defs):
    errors = []
    for id_ in identity_defs:
        m = ID_TOKEN.fullmatch(id_)
        if m and m.group(3):
            errors.append(f"[ERROR] '{id_}' uses an ad-hoc letter suffix — convention is sequential numbers only.")
    return errors


def check_duplicate_names(identity_defs):
    errors = []
    name_to_ids = defaultdict(set)
    for id_, occ in identity_defs.items():
        for _, _, name, _ in occ:
            clean = name.strip().lower()
            if not clean or clean.startswith("(") or "see part" in clean:
                continue
            name_to_ids[clean].add(id_)
    for name, ids in name_to_ids.items():
        if len(ids) > 1:
            errors.append(f"[ERROR] Name '{name}' attached to multiple IDs {sorted(ids)} — likely unintentional collision.")
    return errors


def check_zone_blocks(identity_defs):
    warnings = []
    for id_, occ in identity_defs.items():
        if not id_.startswith("CX-") or id_ in ("CX-000", "CX-999"):
            continue
        m = re.match(r"CX-(\d)", id_)
        if not m:
            continue
        block = f"CX-{m.group(1)}"
        hints = ZONE_NAME_HINTS.get(block)
        if not hints:
            continue
        for path, line_no, name, zone_val in occ:
            if not zone_val:
                continue
            zv = zone_val.lower()
            if any(h in zv for h in hints):
                continue
            for other_block, other_hints in ZONE_NAME_HINTS.items():
                if other_block != block and any(h in zv for h in other_hints):
                    warnings.append(
                        f"[WARNING] '{id_}' ({path}:{line_no}) numeric block = {ZONE_BLOCKS[block]}, "
                        f"but its own Zone/Parent column says '{zone_val}' (looks like {ZONE_BLOCKS[other_block]}). Verify."
                    )
                    break
    return warnings


def check_dangling_references(identity_defs, all_ids_mentioned):
    warnings = []
    retired_notes = []
    defined = set(identity_defs.keys())
    for id_, files in all_ids_mentioned.items():
        if id_ == "CX-000":
            continue
        if id_ in defined:
            continue
        if id_ in RETIRED_IDS:
            replacement, reason = RETIRED_IDS[id_]
            retired_notes.append(f"[INFO] '{id_}' is a registered retired ID (-> {replacement}, {reason}) — mentions in {sorted(files)} are expected historical references, not a gap.")
            continue
        warnings.append(
            f"[WARNING] '{id_}' is referenced in {sorted(files)} but has no Identity-table definition row anywhere provided. Add one, or fix the typo. If this ID was deliberately retired, add it to RETIRED_IDS instead of leaving this to fire every run."
        )
    return warnings, retired_notes


def check_retired_terms(all_text):
    findings = []
    patterns = RETIRED_TERMS + [r"\bMarrow\b"]
    for path, text in all_text.items():
        for pattern in patterns:
            label = "Marrow" if "Marrow" in pattern else None
            for m in re.finditer(pattern, text):
                line_start = text.rfind("\n", 0, m.start()) + 1
                line_end = text.find("\n", m.start())
                line = text[line_start:line_end]
                line_no = text[:m.start()].count("\n") + 1
                term = label or m.group(0)
                if SELF_REFERENCE_MARKERS.search(line):
                    findings.append(f"[WARNING] Retired term '{term}' at {path}:{line_no} — looks like a self-referential audit note, not live content: \"{line.strip()[:120]}\"")
                else:
                    findings.append(f"[ERROR] Retired term '{term}' at {path}:{line_no}: \"{line.strip()[:120]}\"")
    return findings


def check_retired_roots(all_text):
    warnings = []
    for path, text in all_text.items():
        for m in re.finditer(r"[^\n]*\[NEW\][^\n]*", text):
            segment = m.group(0)
            line_no = text[:m.start()].count("\n") + 1
            for word_m in re.finditer(r"\b([A-Z][a-z]+)", segment):
                word = word_m.group(1)
                if any(word.startswith(p.rstrip("-")) for p in RETIRED_ROOT_PREFIXES):
                    if any(word in w for w in ROOT_WHITELIST):
                        continue
                    warnings.append(f"[WARNING] Possible retired-root name '{word}' near a [NEW] marker in {path}:{line_no} — verify against Ash-/Oss-/Cor- lock.")
    return warnings


def main(paths):
    identity_defs, all_text, all_ids_mentioned = collect(paths)

    errors = []
    errors += check_duplicate_ids(identity_defs)
    errors += check_suffixes(identity_defs)
    errors += check_duplicate_names(identity_defs)

    retired = check_retired_terms(all_text)
    retired_errors = [r for r in retired if r.startswith("[ERROR]")]
    retired_warnings = [r for r in retired if r.startswith("[WARNING]")]
    errors += retired_errors

    warnings = []
    warnings += check_zone_blocks(identity_defs)
    dangling_warnings, retired_notes = check_dangling_references(identity_defs, all_ids_mentioned)
    warnings += dangling_warnings
    warnings += check_retired_roots(all_text)
    warnings += retired_warnings

    print("=" * 70)
    print("DEMON FIGHTER — Location ID Validator v3")
    print(f"Files checked: {len(paths)}")
    print(f"Unique IDs with an Identity-table definition: {len(identity_defs)}")
    print("=" * 70)

    if errors:
        print(f"\n{len(errors)} ERROR(S):\n")
        for e in errors:
            print(e)
    else:
        print("\nNo ERROR-level findings.")

    if warnings:
        print(f"\n{len(warnings)} WARNING(S) — judgment calls, read before dismissing:\n")
        for w in warnings:
            print(w)
    else:
        print("\nNo WARNING-level findings.")

    if retired_notes:
        print(f"\n{len(retired_notes)} registered-retirement note(s) — informational only, not warnings, not re-triaged each run:\n")
        for n in retired_notes:
            print(n)

    print("\n" + "=" * 70)
    if errors:
        print("RESULT: FAIL — fix ERROR items before treating this batch as final.")
        return 1
    print("RESULT: PASS on mechanical checks. Read the WARNINGs — they're the ones a script can't resolve by itself.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python3 dfvalidate.py file1.md file2.md ...")
        sys.exit(2)
    sys.exit(main(sys.argv[1:]))
