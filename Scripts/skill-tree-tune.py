#!/usr/bin/env python3
"""Apply skill-tree templates and solve a per-class power scale with SKILLVALUE.

GameData/TuningProfiles/skill-tree-templates.json holds, per node: design power, effect text with base amounts in
{braces} (the game shows amount x power) and whether the class scale may touch it. Final power = design power x
classScale[class] for scalable nodes.

  python Scripts/skill-tree-tune.py --apply                          # write templates into SkillTrees.json
  python Scripts/skill-tree-tune.py --solve --level 60 --target 160  # measure whole-tree value, rescale, repeat
"""
import argparse, glob, json, math, os, subprocess
from concurrent.futures import ThreadPoolExecutor

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TEMPLATES = os.path.join(ROOT, 'GameData', 'TuningProfiles', 'skill-tree-templates.json')
TREES = os.path.join(ROOT, 'GameData', 'SkillTrees.json')
DLL = os.path.join(ROOT, 'Code', 'bin', 'Debug', 'net8.0', 'DF.dll')


def load_json(path):
    raw = open(path, 'rb').read()
    return json.loads(raw.decode('utf-8-sig')), raw


def apply(templates):
    data, raw = load_json(TREES)
    scale = templates['classScale']
    for tree in data['trees']:
        for node in tree['nodes']:
            t = templates['nodes'].get(node['id'])
            if not t:
                continue
            design, text, scalable = t
            node['power'] = round(design * (scale[tree['classKey']] if scalable else 1.0), 5)
            node['effect'] = text
    out = json.dumps(data, indent=2, ensure_ascii=True)
    if b'\r\n' in raw:
        out = out.replace('\n', '\r\n')
    bom = raw[:3] == b'\xef\xbb\xbf'
    with open(TREES, 'w', encoding='utf-8-sig' if bom else 'utf-8', newline='') as f:
        f.write(out + ('\r\n' if raw.endswith(b'\r\n') else ''))


def measure(level, classes, encounters, patch):
    def run(cls):
        out = os.path.join(ROOT, 'GameData', 'TuningRuns', f'skilltune-{cls}-L{level}.json')
        subprocess.run(['dotnet', DLL, 'SKILLVALUE', '--patch', patch, '--level', str(level), '--encounters',
                        str(encounters), '--classes', cls, '--json', out], cwd=ROOT,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=True)
        rows = json.load(open(out))
        return cls, next((r['ValueIndex'] for r in rows if r['Node'] == 'ALL'), 100.0)
    with ThreadPoolExecutor(max_workers=len(classes)) as pool:
        return dict(pool.map(run, classes))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--apply', action='store_true')
    ap.add_argument('--solve', action='store_true')
    ap.add_argument('--level', type=int, default=60)
    ap.add_argument('--target', type=float, default=160.0, help='whole tree value index (100 = no change)')
    ap.add_argument('--iterations', type=int, default=3)
    ap.add_argument('--encounters', type=int, default=30)
    ap.add_argument('--patch', default='overnight-v1')
    args = ap.parse_args()
    templates, _ = load_json(TEMPLATES)
    if args.apply or args.solve:
        apply(templates)
    if not args.solve:
        return
    classes = list(templates['classScale'])
    for it in range(1, args.iterations + 1):
        values = measure(args.level, classes, args.encounters, args.patch)
        print(f'pass {it}: ' + '  '.join(f'{c} {values[c]:.0f} (scale {templates["classScale"][c]:.3f})' for c in classes))
        for c in classes:
            v = max(values[c], 101.0)
            ratio = math.log(args.target / 100.0) / math.log(v / 100.0)
            templates['classScale'][c] = round(templates['classScale'][c] * max(0.4, min(1.6, ratio)) ** 0.8, 4)
        with open(TEMPLATES, 'w', encoding='utf-8') as f:
            json.dump(templates, f, indent=2, ensure_ascii=False)
        apply(templates)
    values = measure(args.level, classes, args.encounters, args.patch)
    print('final: ' + '  '.join(f'{c} {values[c]:.0f} (scale {templates["classScale"][c]:.3f})' for c in classes))


if __name__ == '__main__':
    main()
