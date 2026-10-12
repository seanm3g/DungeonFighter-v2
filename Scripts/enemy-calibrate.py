#!/usr/bin/env python3
"""Solve enemySystem.identity.calibration so every enemy costs the hero the same HP at every level.

Runs FUNDMATRIX (typical gear) for the whole Enemies.json roster at each calibration level and weapon in
parallel, compares each enemy's mean hero HP lost with the level mean, and scales that enemy's calibration
anchor by (mean / enemy) ** damping. Repeats for --iterations passes and writes the patch after each pass.

Win rate is not used: it sits at 100% for most enemies at low levels and carries no signal there.
Re-run WRSOLVE afterwards to put the overall damage curve back on the win-rate targets.

  python Scripts/enemy-calibrate.py --patch overnight-v1 --iterations 3
"""
import argparse
import json
import statistics
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WEAPONS = ["Mace", "Sword", "Dagger", "Wand"]


def run_matrix(dll, patch, level, weapon, enemies, encounters, label):
    cmd = ["dotnet", str(dll), "FUNDMATRIX", "--patch", patch, "--gear", "typical", "--level", str(level),
           "--weapons", weapon, "--encounters", str(encounters), "--seed", "1", "--enemies", ",".join(enemies),
           "--label", label]
    subprocess.run(cmd, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=True)
    runs = sorted((ROOT / "GameData" / "TuningRuns").glob(f"fundmatrix-*-{label}.json"))
    return json.loads(runs[-1].read_text(encoding="utf-8-sig"))


def measure(dll, patch, levels, enemies, encounters, tag, workers):
    jobs = [(lv, w) for lv in levels for w in WEAPONS]
    with ThreadPoolExecutor(max_workers=workers) as pool:
        results = list(pool.map(lambda j: run_matrix(dll, patch, j[0], j[1], enemies, encounters,
                                                     f"cal-{tag}-L{j[0]}-{j[1]}"), jobs))
    hp = {}
    for r in results:
        for c in r["Cells"]:
            hp.setdefault((r["Level"], c["Enemy"]), []).append(c["HeroHpLostFraction"])
    return {k: statistics.mean(v) for k, v in hp.items()}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--patch", default="overnight-v1")
    ap.add_argument("--levels", default="1,5,10,20,35,50,75")
    ap.add_argument("--iterations", type=int, default=3)
    ap.add_argument("--encounters", type=int, default=40)
    ap.add_argument("--damping", type=float, default=0.8)
    ap.add_argument("--max-step", type=float, default=2.0, help="largest change to one anchor in one pass (×)")
    ap.add_argument("--workers", type=int, default=28)
    ap.add_argument("--dll", default=str(ROOT / "Code" / "bin" / "Debug" / "net8.0" / "DF.dll"))
    args = ap.parse_args()

    levels = [int(x) for x in args.levels.split(",")]
    patch_path = ROOT / "GameData" / "Patches" / "Balance" / f"{args.patch}.json"
    enemies = [e["name"] for e in json.loads((ROOT / "GameData" / "Enemies.json").read_text(encoding="utf-8-sig"))]

    for it in range(1, args.iterations + 1):
        hp = measure(args.dll, args.patch, levels, enemies, args.encounters, f"it{it}", args.workers)
        patch = json.loads(patch_path.read_text(encoding="utf-8-sig"))
        identity = patch["enemySystem"].setdefault("identity", {})
        if not identity.get("enabled"):
            sys.exit("enemySystem.identity.enabled is false in the patch; calibration only applies in identity mode.")
        cal = identity.setdefault("calibration", {})
        print(f"\nPass {it}: level  mean HP lost  spread p10-p90 (before this pass's change)")
        for lv in levels:
            vals = sorted(hp[(lv, e)] for e in enemies if (lv, e) in hp)
            mean = statistics.mean(vals)
            print(f"  L{lv:<3}  {mean * 100:5.1f}%   {vals[len(vals) // 10] * 100:4.0f}% - {vals[9 * len(vals) // 10] * 100:4.0f}%")
            for e in enemies:
                if (lv, e) not in hp:
                    continue
                factor = (mean / max(hp[(lv, e)], 0.01)) ** args.damping
                factor = min(args.max_step, max(1.0 / args.max_step, factor))
                anchors = cal.setdefault(e, [])
                anchor = next((a for a in anchors if a["level"] == lv), None)
                if anchor is None:
                    anchor = {"level": lv, "multiplier": 1.0}
                    anchors.append(anchor)
                anchor["multiplier"] = round(anchor["multiplier"] * factor, 3)
                anchors.sort(key=lambda a: a["level"])
        patch_path.write_text(json.dumps(patch, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"\nWrote calibration for {len(enemies)} enemies to {patch_path}")


if __name__ == "__main__":
    main()
