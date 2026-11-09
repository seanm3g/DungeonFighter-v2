#!/usr/bin/env python3
"""Build-diversity and fight-dynamics metrics from DUNGEONSIM raw shards (dungeon-sim.py --keep DIR).

Answers "is the game varied and lively?" rather than "is it the right difficulty?". Every metric is computed per hero
level and per class (and 'all'), from real dungeon fights played by the calibration bot.

  python Scripts/diversity-report.py report RAW_DIR [RAW_DIR...] [--json out.json]
  python Scripts/diversity-report.py grid NAME=sim.json ... [--json out.json]     forced-material / forced-affix clear grid
  python Scripts/diversity-report.py compare A.json B.json                        two report JSONs side by side

Metric families (see Documentation/FUN_METRICS.md):
  build   material_eff, offclass_share, classless_share, set_rate, set4_rate, set_eff, affix_eff, stat_eff, mod_eff,
          strip_eff, skill_eff, build_eff, build_distance
  adopt   per material / affix: offered item-passes, worn share (worn / offered), from the bot's gear passes
  choice  slot_contest (runner-up / best item value), contest_share (runner-up within 10%), upgrades per hero
  fight   close_call, lead_changes, length_cv, p90_ratio, one_shot, miss_streak, action_eff, dot_mix, status_eff,
          enemy_move_eff, combo_share, crit_share, convert_share, strip_completion
"""
import argparse, glob, json, math, os, random, statistics, sys
from collections import Counter, defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']


def material_classes():
    try:
        rows = json.load(open(os.path.join(ROOT, 'GameData', 'MaterialBuilds.json'), encoding='utf-8'))
    except Exception:
        return {}
    out = {}
    for r in rows:
        m = r.get('material') or r.get('Material')
        c = r.get('class') or r.get('Class')
        if m:
            out[m.lower()] = (c or '').title()
    return out


MATCLASS = material_classes()


def eff(counter):
    """Effective number of kinds (inverse Simpson): 1 = one thing everywhere, N = N things evenly."""
    tot = sum(counter.values())
    if tot <= 0:
        return 0.0
    return 1.0 / sum((v / tot) ** 2 for v in counter.values())


def gear_material(g):
    parts = (g or '').split('|')
    return parts[1].strip() if len(parts) > 1 and parts[1].strip() else 'none'


def load_raw(dirs):
    """Fights and gear passes by level from every shard JSON in the folders."""
    fights, passes, outcomes = defaultdict(list), defaultdict(list), defaultdict(list)
    for d in dirs:
        for path in sorted(glob.glob(os.path.join(d, '**', '*.json'), recursive=True)):
            try:
                doc = json.load(open(path, encoding='utf-8'))
            except Exception:
                continue
            if not isinstance(doc, dict) or 'Fights' not in doc:
                continue
            L = doc.get('Level')
            hero_tag = os.path.basename(path)
            for f in doc.get('Fights') or []:
                f['_shard'] = hero_tag
                fights[L].append(f)
            for p in doc.get('GearPasses') or []:
                passes[L].append(p)
            for o in doc.get('Outcomes') or []:
                outcomes[L].append(o)
    return fights, passes, outcomes


def fingerprint(f):
    """Coarse build identity: dominant set, top-2 stat suffix types, skill branch roots, strip as a set."""
    sets = f.get('Sets') or {}
    dom = max(sets.items(), key=lambda kv: kv[1])[0] if sets else 'noset'
    stats = Counter(a for lst in (f.get('Affixes') or {}).values() for a in lst if a.startswith('stat:'))
    top = tuple(sorted(k for k, _ in stats.most_common(2)))
    skills = tuple(sorted((f.get('Skills') or {}).keys()))
    return (dom, top, skills, tuple(sorted(f.get('Strip') or [])))


def feature_set(f):
    feats = set()
    for g in (f.get('Gear') or {}).values():
        feats.add('m:' + gear_material(g).lower())
    for lst in (f.get('Affixes') or {}).values():
        feats.update(lst)
    feats.update('k:' + k for k in (f.get('Skills') or {}))
    feats.update('a:' + a for a in (f.get('Strip') or []))
    return feats


def jaccard_distance(a, b):
    u = a | b
    return 1 - len(a & b) / len(u) if u else 0.0


def fight_metrics(fs):
    if not fs:
        return {}
    won = [f for f in fs if not f.get('HeroDied')]
    lengths = [f.get('HeroActions', 0) for f in fs if f.get('HeroActions', 0) > 0]
    med = statistics.median(lengths) if lengths else 0
    p90 = sorted(lengths)[int(0.9 * (len(lengths) - 1))] if lengths else 0
    src_eff, status_eff, enemy_eff, action_eff = [], [], [], []
    combo = crit = conv = total = 0
    completions = []
    for f in fs:
        src = Counter({k: v for k, v in (f.get('DamageBySource') or {}).items() if v > 0})
        if src:
            src_eff.append(eff(src))
        uses = Counter({k: v for k, v in (f.get('ActionUses') or {}).items() if v > 0})
        if uses:
            action_eff.append(eff(uses))
        st = Counter(f.get('StatusApplied') or {})
        status_eff.append(eff(st) if st else 0)
        ea = Counter(f.get('EnemyActionUses') or {})
        if ea:
            enemy_eff.append(eff(ea))
        dmg = sum(src.values())
        total += dmg
        combo += f.get('ComboDamage', 0)
        crit += f.get('CritDamage', 0)
        conv += f.get('ConvertDamage', 0)
        if f.get('HeroActions'):
            completions.append(f.get('StripCompletions', 0) / max(1, f['HeroActions']))
    mean = lambda xs: round(statistics.mean(xs), 3) if xs else 0
    return {
        'fights': len(fs),
        'close_call': round(sum(1 for f in won if f.get('HpMinPct', 1) < 0.25) / max(1, len(won)), 3),
        'lead_changes': mean([f.get('LeadChanges', 0) for f in fs]),
        'length_cv': round(statistics.pstdev(lengths) / statistics.mean(lengths), 3) if len(lengths) > 1 else 0,
        'p90_ratio': round(p90 / med, 2) if med else 0,
        'one_shot': round(sum(1 for f in won if 0 < f.get('HeroActions', 0) <= 2) / max(1, len(won)), 3),
        'miss_streak': round(sum(1 for f in fs if f.get('LongestMissStreak', 0) >= 4) / len(fs), 3),
        'dot_mix': mean(src_eff),
        'action_eff': mean(action_eff),
        'status_eff': mean(status_eff),
        'enemy_move_eff': mean(enemy_eff),
        'combo_share': round(combo / total, 3) if total else 0,
        'crit_share': round(crit / total, 3) if total else 0,
        'convert_share': round(conv / total, 3) if total else 0,
        'strip_completion': mean(completions),
    }


def build_metrics(fs, cls):
    if not fs:
        return {}
    mats, sets, affix, stat, mod, act, strips, skills, prints = (Counter() for _ in range(9))
    off = classless = pieces = 0
    set2 = set4 = 0
    for f in fs:
        for g in (f.get('Gear') or {}).values():
            m = gear_material(g)
            mats[m] += 1
            pieces += 1
            owner = MATCLASS.get(m.lower())
            if owner == '':
                classless += 1
            elif owner and cls != 'all' and owner != cls:
                off += 1
        s = f.get('Sets') or {}
        if s:
            set2 += 1
            if max(s.values()) >= 4:
                set4 += 1
            sets[max(s.items(), key=lambda kv: kv[1])[0]] += 1
        else:
            sets['noset'] += 1
        for lst in (f.get('Affixes') or {}).values():
            for a in lst:
                affix[a] += 1
                (stat if a.startswith('stat:') else mod if a.startswith(('mod:', 'qual:')) else act)[a] += 1
        strips[tuple(f.get('Strip') or [])] += 1
        skills[tuple(sorted((f.get('Skills') or {}).keys()))] += 1
        prints[fingerprint(f)] += 1
    # Build distance: mean Jaccard distance between random pairs of fights from different shards (different heroes).
    rnd = random.Random(7)
    feats = [(f['_shard'], feature_set(f)) for f in fs]
    dists = []
    for _ in range(min(600, len(feats) * 2)):
        a, b = rnd.choice(feats), rnd.choice(feats)
        if a[0] != b[0]:
            dists.append(jaccard_distance(a[1], b[1]))
    n = len(fs)
    return {
        'material_eff': round(eff(mats), 2),
        'offclass_share': round(off / pieces, 3) if pieces and cls != 'all' else None,
        'classless_share': round(classless / pieces, 3) if pieces else 0,
        'set_rate': round(set2 / n, 3),
        'set4_rate': round(set4 / n, 3),
        'set_eff': round(eff(sets), 2),
        'top_sets': sets.most_common(4),
        'affix_eff': round(eff(affix), 2),
        'stat_eff': round(eff(stat), 2),
        'mod_eff': round(eff(mod), 2),
        'act_eff': round(eff(act), 2),
        'affixes_per_piece': round(sum(affix.values()) / pieces, 2) if pieces else 0,
        'strip_eff': round(eff(strips), 2),
        'skill_eff': round(eff(skills), 2),
        'build_eff': round(eff(prints), 2),
        'build_distance': round(statistics.mean(dists), 3) if dists else 0,
    }


def adoption(passes):
    off, worn = Counter(), Counter()
    contest = []
    upgrades = []
    for p in passes:
        off.update(p.get('GearOffered') or {})
        worn.update(p.get('GearWorn') or {})
        contest.extend(p.get('SlotContest') or [])
        upgrades.append(p.get('UpgradesEquipped', 0))
    rows = {k: {'offered': v, 'worn': worn.get(k, 0), 'worn_share': round(worn.get(k, 0) / v, 3)} for k, v in off.items()}
    return rows, {
        'slot_contest': round(statistics.mean(contest), 3) if contest else 0,
        'contest_share': round(sum(1 for c in contest if c >= 0.9) / len(contest), 3) if contest else 0,
        'upgrades_per_hero': round(statistics.mean(upgrades), 2) if upgrades else 0,
    }


def clear_rate(outs, cls):
    rows = [o for o in outs if cls == 'all' or o.get('Class') == cls]
    d = sum(o.get('Dungeons', 0) for o in rows if o.get('Class') != 'all')
    c = sum(o.get('Cleared', 0) for o in rows if o.get('Class') != 'all')
    return round(c / d, 3) if d else None


def cmd_report(a):
    fights, passes, outcomes = load_raw(a.dirs)
    if not fights:
        sys.exit('no shard JSON with Fights under ' + ', '.join(a.dirs))
    report = {'sources': a.dirs, 'levels': {}}
    for L in sorted(fights):
        lv = {}
        for cls in CLASSES + ['all']:
            fs = [f for f in fights[L] if cls == 'all' or f.get('Class') == cls]
            ps = [p for p in passes[L] if cls == 'all' or p.get('Class') == cls]
            adopt_rows, choice = adoption(ps)
            lv[cls] = {'clear': clear_rate(outcomes[L], cls), **build_metrics(fs, cls), **choice, **fight_metrics(fs)}
            if cls == 'all':
                lv['adoption'] = adopt_rows
        report['levels'][str(L)] = lv
    print_report(report, a.min_offered)
    if a.json:
        json.dump(report, open(a.json, 'w', encoding='utf-8'), indent=1)
        print('\nwrote', a.json)


KEYS = ['clear', 'material_eff', 'offclass_share', 'classless_share', 'set_rate', 'set4_rate', 'set_eff', 'affix_eff', 'stat_eff',
        'mod_eff', 'strip_eff', 'skill_eff', 'build_eff', 'build_distance', 'slot_contest', 'contest_share', 'upgrades_per_hero',
        'close_call', 'lead_changes', 'length_cv', 'p90_ratio', 'one_shot', 'miss_streak', 'action_eff', 'dot_mix', 'status_eff',
        'enemy_move_eff', 'combo_share', 'crit_share', 'convert_share', 'strip_completion']


def fmt(v):
    if v is None:
        return '-'
    return f'{v:.3g}' if isinstance(v, float) else str(v)


def print_report(report, min_offered):
    for L, lv in report['levels'].items():
        print(f'\n=== L{L} ===')
        print(f'  {"metric":<18}' + ''.join(f'{c:>11}' for c in CLASSES + ['all']))
        for k in KEYS:
            print(f'  {k:<18}' + ''.join(f'{fmt(lv[c].get(k)):>11}' for c in CLASSES + ['all']))
        for c in CLASSES:
            print(f'  top sets {c:<10} ' + ', '.join(f'{m}:{n}' for m, n in lv[c].get('top_sets', [])))
        rows = [(k, r) for k, r in lv.get('adoption', {}).items() if r['offered'] >= min_offered]
        for prefix, title in (('mat:', 'materials'), ('stat:', 'stat suffixes'), ('mod:', 'adjective prefixes'), ('act:', 'action bonuses')):
            sel = sorted([x for x in rows if x[0].startswith(prefix)], key=lambda x: -x[1]['worn_share'])
            if sel:
                print(f'  adoption {title}: ' + ', '.join(f"{k[len(prefix):]} {r['worn_share']:.2f}({r['offered']})" for k, r in sel))


def cmd_grid(a):
    """NAME=sim.json pairs (dungeon-sim --json): clear per class and level, plus within-class spread and dead/dominant flags."""
    grid = {}
    for spec in a.pairs:
        name, path = spec.split('=', 1)
        for row in json.load(open(path, encoding='utf-8')):
            grid.setdefault(row['level'], {})[name] = {'all': row.get('clear'), **(row.get('perClassClear') or {}),
                                                        'acts': row.get('heroActionsPerFight')}
    out = {}
    for L, byname in sorted(grid.items()):
        print(f'\n=== L{L} clear by forced variant ===')
        print(f'  {"variant":<14}' + ''.join(f'{c:>10}' for c in CLASSES + ['all']) + f'{"acts":>8}')
        base = byname.get(a.baseline, {})
        for name, r in sorted(byname.items(), key=lambda kv: -(kv[1].get('all') or 0)):
            def cell(c):
                v = r.get(c)
                if v is None:
                    return f'{"-":>10}'
                d = '' if not base or base.get(c) is None or name == a.baseline else f'{(v - base[c]) * 100:+.0f}'
                return f'{v * 100:5.0f}{d:>5}'
            print(f'  {name:<14}' + ''.join(cell(c) for c in CLASSES + ['all']) + f'{fmt(r.get("acts")):>8}')
        spread = {}
        for c in CLASSES:
            vals = [r[c] for n, r in byname.items() if n != a.baseline and r.get(c) is not None]
            if vals:
                spread[c] = round((max(vals) - min(vals)) * 100, 1)
        print('  within-class spread (pts): ' + ', '.join(f'{c} {v}' for c, v in spread.items()))
        out[L] = {'variants': byname, 'spread': spread}
    if a.json:
        json.dump(out, open(a.json, 'w', encoding='utf-8'), indent=1)


def cmd_compare(a):
    A, B = json.load(open(a.a, encoding='utf-8')), json.load(open(a.b, encoding='utf-8'))
    for L in A['levels']:
        if L not in B['levels']:
            continue
        print(f'\n=== L{L} (all classes)  A -> B ===')
        for k in KEYS:
            va, vb = A['levels'][L]['all'].get(k), B['levels'][L]['all'].get(k)
            if isinstance(va, (int, float)) and isinstance(vb, (int, float)):
                print(f'  {k:<18}{fmt(va):>10}{fmt(vb):>10}{vb - va:>+10.3f}')


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    r = sub.add_parser('report')
    r.add_argument('dirs', nargs='+')
    r.add_argument('--json', default='')
    r.add_argument('--min-offered', type=int, default=30, help='hide adoption rows offered fewer item-passes than this')
    g = sub.add_parser('grid')
    g.add_argument('pairs', nargs='+')
    g.add_argument('--baseline', default='none')
    g.add_argument('--json', default='')
    c = sub.add_parser('compare')
    c.add_argument('a')
    c.add_argument('b')
    a = ap.parse_args()
    {'report': cmd_report, 'grid': cmd_grid, 'compare': cmd_compare}[a.cmd](a)


if __name__ == '__main__':
    main()
