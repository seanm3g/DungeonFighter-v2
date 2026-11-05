#!/usr/bin/env python3
"""Balance surrogate: learned stand-in for DUNGEONSIM, trained on Scripts/surrogate-data.py shards.

Answers "what if I change knob X" in milliseconds: dungeon clear rate, death rate and fight shape (hero / enemy actions,
HP lost per fight) for a level, class and knob scales (KNOBS in surrogate-data.py; 1.0 = the base patch value).
Gradient-boosted trees (LightGBM), cross-validated by shard so a shard's own fights never grade it. Use it to screen
and aim; confirm the pick with Scripts/dungeon-sim.py before it goes in a patch.

  python Scripts/surrogate.py train                                    (fit + CV report + knob importance)
  python Scripts/surrogate.py predict --level 20 --set enemyDamage=1.1 --set classHealth=0.9 --class Rogue
  python Scripts/surrogate.py sweep --knob enemyHealth --levels 10,20,35,50
  python Scripts/surrogate.py solve --knob enemyDamage --level 20 --target 75     (scale that predicts 75% clear)
  python Scripts/surrogate.py check --level 20 --set enemyDamage=1.2   (prediction next to a real dungeon-sim.py run)
"""
import argparse, json, math, os, subprocess, sys
import numpy as np
import pandas as pd

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'Scripts'))
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']
DEFAULT_DATA = 'GameData/TuningRuns/surrogate/dataset.jsonl'
JOBS = 1   # LightGBM threads: more only slows it down while sims hold the cores


def data_script_constant(name):
    """A literal (KNOBS, INT_KNOBS) from surrogate-data.py, read without running its argparse."""
    import ast
    src = open(os.path.join(ROOT, 'Scripts', 'surrogate-data.py'), encoding='utf-8').read()
    for node in ast.parse(src).body:
        if isinstance(node, ast.Assign) and any(getattr(t, 'id', '') == name for t in node.targets):
            return ast.literal_eval(node.value)
    raise SystemExit(f'{name} not found in surrogate-data.py')


KNOB_PATHS = {k: v[0] for k, v in data_script_constant('KNOBS').items()}
INT_KNOBS = data_script_constant('INT_KNOBS')
KNOBS = {k: (v[1], v[2]) for k, v in data_script_constant('KNOBS').items()}
KNOB_NAMES = list(KNOBS)
# Directions the data must respect (+1 helps the hero, -1 hurts): keeps sweeps and solves monotone where the design is.
MONOTONE = {'enemyDamage': -1, 'enemyHealth': -1, 'heroHpPerLevel': 1, 'classHealth': 1,
            'classDamage': 1, 'bossDamage': -1, 'enrageDamage': -1}


def region(level):
    return ((level - 1) // 5) % 4   # regions cycle Forest/Water/Fire/Air every 5 levels


def context_frame(rows):
    df = pd.DataFrame({k: [r['knobs'].get(k, 1.0) for r in rows] for k in KNOB_NAMES})
    df['level'] = [r['level'] for r in rows]
    df['region'] = df['level'].map(region)
    df['class'] = pd.Categorical([r['class'] for r in rows], categories=CLASSES)
    return df


DUNGEON_FEATURES = KNOB_NAMES + ['level', 'region', 'class']
FIGHT_FEATURES = DUNGEON_FEATURES + ['boss', 'fightInDungeon']
BUILD_FEATURES = ['HeroStrength', 'HeroAgility', 'HeroTechnique', 'HeroIntelligence', 'HeroMaxHp', 'SlotMax', 'setPieces', 'skills', 'stripLen']
FIGHT_TARGETS = {'heroActions': 'HeroActions', 'enemyActions': 'EnemyActions', 'hpLost': 'HpLost'}


def load(path, base_patch):
    rows = [json.loads(l) for l in open(path, encoding='utf-8') if l.strip()]
    patches = pd.Series([r['basePatch'] for r in rows]).value_counts()
    base_patch = base_patch or patches.index[0]
    rows = [r for r in rows if r['basePatch'] == base_patch and r['dungeons'] > 0 and r.get('levelDrift', 0) == 0]
    if not rows:
        raise SystemExit(f'No usable shards for base patch {base_patch} in {path}')
    return rows, base_patch


def dungeon_frame(rows):
    """One row per shard: clear and death rates over its dungeons, weighted by the dungeon count. (A run ends at its
    death, so the hero died in at most one of them.)"""
    df = context_frame(rows)
    df['dungeons'] = [r['dungeons'] for r in rows]
    df['cleared'] = [r['cleared'] / r['dungeons'] for r in rows]
    df['died'] = [r['died'] / r['dungeons'] for r in rows]
    df['group'] = [r['index'] for r in rows]
    return df


def fight_frame(rows):
    out = []
    for r in rows:
        for f in r['fightRows']:
            d = {k: r['knobs'].get(k, 1.0) for k in KNOB_NAMES}
            d.update(level=r['level'], region=region(r['level']), **{'class': r['class']}, group=r['index'],
                     boss=int(bool(f.get('BossFight'))), fightInDungeon=f.get('FightInDungeon') or 0,
                     setPieces=sum((f.get('Sets') or {}).values()), skills=len(f.get('Skills') or {}),
                     stripLen=len(f.get('Strip') or []), died=int(bool(f.get('HeroDied'))))
            d.update({k: f.get(k) for k in BUILD_FEATURES if k in f})
            d.update({t: f.get(src) for t, src in FIGHT_TARGETS.items()})
            out.append(d)
    df = pd.DataFrame(out)
    df['class'] = pd.Categorical(df['class'], categories=CLASSES)
    return df


def lgb_params(features, objective):
    mono = [MONOTONE.get(f, 0) for f in features]
    return dict(objective=objective, n_estimators=400, learning_rate=0.03, num_leaves=15, min_child_samples=20,
                subsample=0.8, subsample_freq=1, colsample_bytree=0.8, reg_lambda=1.0, verbose=-1, n_jobs=JOBS,
                monotone_constraints=mono, monotone_constraints_method='intermediate')


def fit(df, features, target, objective, sign=1):
    import lightgbm as lgb
    params = lgb_params(features, objective)
    params['monotone_constraints'] = [m * sign for m in params['monotone_constraints']]
    model = lgb.LGBMRegressor(**params)   # cross_entropy (rates in 0..1) predicts a probability directly
    model.fit(df[features], df[target], sample_weight=df['dungeons'] if objective == 'cross_entropy' else None)
    return model


def cv(df, features, target, objective, sign=1, folds=5):
    """Shard-grouped CV predictions (out of fold)."""
    from sklearn.model_selection import GroupKFold
    pred = np.zeros(len(df))
    for tr, te in GroupKFold(n_splits=folds).split(df, groups=df['group']):
        m = fit(df.iloc[tr], features, target, objective, sign)
        pred[te] = m.predict(df.iloc[te][features])
    return pred


def shard_rate_report(df, pred, target, label):
    """Per-shard predicted vs real rate: the error that matters for a dungeon-sim style read."""
    y, n = df[target].values, df['dungeons'].values
    # Expected error from binomial noise alone (n dungeons per shard): the floor no model can beat.
    noise = math.sqrt(np.average(pred * (1 - pred) / n, weights=n))
    rmse = math.sqrt(np.average((y - pred) ** 2, weights=n))
    mae = np.average(np.abs(y - pred), weights=n)
    print(f'  {label}: per-shard RMSE {rmse * 100:.1f} pts (binomial floor ~{noise * 100:.1f}), MAE {mae * 100:.1f} pts')
    return rmse, noise


def level_band_report(df, pred, target):
    t = pd.DataFrame({'band': (df['level'] - 1) // 10 * 10 + 1, 'y': df[target] * df['dungeons'], 'p': pred * df['dungeons'],
                      'n': df['dungeons']}).groupby('band').sum()
    print('  clear by level band (out of fold):')
    for b, r in t.iterrows():
        print(f'    L{b:>2}-{b + 9:<3} real {r.y / r.n * 100:5.1f}%  predicted {r.p / r.n * 100:5.1f}%  ({int(r.n)} dungeons)')


def bundle_path(base_patch):
    return os.path.join(ROOT, 'GameData', 'TuningRuns', 'surrogate', f'model-{base_patch}.pkl')


def cmd_train(A):
    import joblib
    from sklearn.metrics import r2_score
    rows, base_patch = load(A.data, A.base_patch)
    dd = dungeon_frame(rows)
    ff = fight_frame(rows)
    print(f'Base patch {base_patch}: {len(rows)} shards, {int(dd.dungeons.sum())} dungeons, {len(ff)} fights')
    metrics = {'basePatch': base_patch, 'shards': len(rows), 'dungeons': int(dd.dungeons.sum()), 'fights': len(ff)}

    print('\nDungeon clear (cross-validated by shard):')
    p = cv(dd, DUNGEON_FEATURES, 'cleared', 'cross_entropy')
    p_lvl = cv(dd, ['level', 'region', 'class'], 'cleared', 'cross_entropy')
    rmse, noise = shard_rate_report(dd, p, 'cleared', 'with knobs ')
    rmse0, _ = shard_rate_report(dd, p_lvl, 'cleared', 'level+class')
    level_band_report(dd, p, 'cleared')
    metrics['clear'] = {'shardRmse': rmse, 'binomialFloor': noise, 'levelOnlyRmse': rmse0}

    print('\nDungeon death:')
    pdie = cv(dd, DUNGEON_FEATURES, 'died', 'cross_entropy', sign=-1)
    r, n = shard_rate_report(dd, pdie, 'died', 'with knobs ')
    metrics['death'] = {'shardRmse': r, 'binomialFloor': n}

    print('\nFight shape (R^2 out of fold, single fights and per-shard means; "+build" adds hero stats, sets, skills, strip):')
    metrics['fight'] = {}
    for t in FIGHT_TARGETS:
        sub = ff.dropna(subset=[t])
        # All three fight targets fall as the hero gets stronger, so the helps-hero directions flip (sign -1).
        pa = cv(sub, FIGHT_FEATURES, t, 'regression', -1)
        pb = cv(sub, FIGHT_FEATURES + BUILD_FEATURES, t, 'regression', -1)
        g = pd.DataFrame({'group': sub['group'].values, 'y': sub[t].values, 'p': pa}).groupby('group').mean()
        a, b, m = r2_score(sub[t], pa), r2_score(sub[t], pb), r2_score(g['y'], g['p'])
        print(f'  {t:<13} fight R^2 {a:.3f} (+build {b:.3f})   shard-mean R^2 {m:.3f}')
        metrics['fight'][t] = {'r2': a, 'r2Build': b, 'r2ShardMean': m}

    models = {'clear': fit(dd, DUNGEON_FEATURES, 'cleared', 'cross_entropy'),
              'died': fit(dd, DUNGEON_FEATURES, 'died', 'cross_entropy', sign=-1)}
    for t in FIGHT_TARGETS:
        sub = ff.dropna(subset=[t])
        models[t] = fit(sub, FIGHT_FEATURES, t, 'regression', sign=-1)

    print('\nKnob importance for clear rate (split gain share):')
    gain = pd.Series(models['clear'].booster_.feature_importance('gain'), index=DUNGEON_FEATURES)
    gain = gain / gain.sum()
    for k, v in gain.sort_values(ascending=False).items():
        print(f'  {k:<16} {v * 100:5.1f}%  {"#" * int(round(v * 100))}')
    metrics['clearImportance'] = gain.round(4).to_dict()

    path = bundle_path(base_patch)
    joblib.dump({'basePatch': base_patch, 'models': models, 'metrics': metrics,
                 'levels': sorted({r['level'] for r in rows})}, path)
    json.dump(metrics, open(path.replace('.pkl', '-metrics.json'), 'w'), indent=2)
    print(f'\nSaved {path}')


def load_bundle(A):
    import joblib
    if A.base_patch:
        return joblib.load(bundle_path(A.base_patch))
    folder = os.path.join(ROOT, 'GameData', 'TuningRuns', 'surrogate')
    pkls = sorted((p for p in os.listdir(folder) if p.startswith('model-') and p.endswith('.pkl')),
                  key=lambda p: os.path.getmtime(os.path.join(folder, p)))
    if not pkls:
        raise SystemExit('No trained model: run  python Scripts/surrogate.py train')
    return joblib.load(os.path.join(folder, pkls[-1]))


def parse_sets(sets):
    out = {}
    for s in sets:
        k, v = s.split('=')
        if k not in KNOBS:
            raise SystemExit(f'Unknown knob {k}; knobs: {", ".join(KNOB_NAMES)}')
        lo, hi = KNOBS[k]
        if not lo <= float(v) <= hi:
            print(f'WARNING: {k}={v} is outside the trained range {lo}-{hi}; trees flatten past the edge.')
        out[k] = float(v)
    return out


def predict(bundle, level, classes, knobs):
    """Mean over classes of each model's prediction (fight models at a normal room fight)."""
    rows = [{'knobs': knobs, 'level': level, 'class': c} for c in classes]
    X = context_frame(rows)
    out = {'clear': bundle['models']['clear'].predict(X[DUNGEON_FEATURES]).mean(),
           'died': bundle['models']['died'].predict(X[DUNGEON_FEATURES]).mean()}
    X['boss'], X['fightInDungeon'] = 0, 2
    for t in FIGHT_TARGETS:
        out[t] = float(bundle['models'][t].predict(X[FIGHT_FEATURES]).mean())
    return out


def fmt(p):
    return (f'clear {p["clear"] * 100:5.1f}%  death {p["died"] * 100:4.1f}%/dungeon  hero acts {p["heroActions"]:4.1f}  '
            f'enemy acts {p["enemyActions"]:4.1f}  HP lost {p["hpLost"] * 100:4.1f}%/fight')


def classes_of(A):
    return CLASSES if A.cls == 'all' else [A.cls]


def cmd_predict(A):
    b = load_bundle(A)
    knobs = parse_sets(A.set)
    for level in [int(x) for x in A.levels.split(',')]:
        base = predict(b, level, classes_of(A), {})
        new = predict(b, level, classes_of(A), knobs)
        print(f'L{level:<3} base  {fmt(base)}')
        if knobs:
            print(f'     what-if {fmt(new)}   (clear {(new["clear"] - base["clear"]) * 100:+.1f} pts)')
    if A.cls == 'all' and knobs:
        print('Per class at', A.levels.split(',')[0] + ':')
        level = int(A.levels.split(',')[0])
        for c in CLASSES:
            d = predict(b, level, [c], knobs)['clear'] - predict(b, level, [c], {})['clear']
            print(f'  {c:<10} {predict(b, level, [c], knobs)["clear"] * 100:5.1f}% ({d * 100:+.1f})')


def cmd_sweep(A):
    b = load_bundle(A)
    lo, hi = (float(x) for x in A.range.split(',')) if A.range else KNOBS[A.knob]
    scales = np.exp(np.linspace(math.log(lo), math.log(hi), A.steps))
    levels = [int(x) for x in A.levels.split(',')]
    fixed = parse_sets(A.set)
    print(f'Predicted {A.metric} vs {A.knob} scale ({A.cls}):')
    print('  scale  ' + ''.join(f'{"L" + str(l):>8}' for l in levels))
    for s in scales:
        vals = [predict(b, l, classes_of(A), {**fixed, A.knob: s})[A.metric] for l in levels]
        mult = 100 if A.metric in ('clear', 'died', 'hpLost') else 1
        print(f'  {s:5.2f}  ' + ''.join(f'{v * mult:8.1f}' for v in vals))


def cmd_solve(A):
    b = load_bundle(A)
    lo, hi = KNOBS[A.knob]
    fixed = parse_sets(A.set)
    f = lambda s: predict(b, A.level, classes_of(A), {**fixed, A.knob: s})['clear'] * 100 - A.target
    grid = np.exp(np.linspace(math.log(lo), math.log(hi), 61))
    vals = [f(s) for s in grid]
    best = min(range(len(grid)), key=lambda i: abs(vals[i]))
    print(f'{A.knob} scale {grid[best]:.3f} predicts {vals[best] + A.target:.1f}% clear at L{A.level} (target {A.target}%)')
    print(f'  across the trained range {lo}-{hi}: {vals[0] + A.target:.1f}% .. {vals[-1] + A.target:.1f}%')
    if abs(vals[best]) > 3:
        print('  WARNING: target not reachable with this knob alone inside the trained range.')
    path = KNOB_PATHS.get(A.knob, '?')
    print(f'Confirm: python Scripts/dungeon-sim.py --levels {A.level}  with {path} x{grid[best]:.3f} in a patch copy')


def scale_path(d, path, scale, as_int):
    """Same knob application as surrogate-data.py ("[]" = every list element)."""
    head, _, rest = path.partition('.')
    if head.endswith('[]'):
        for x in d[head[:-2]]:
            scale_path(x, rest, scale, as_int)
    elif rest:
        scale_path(d[head], rest, scale, as_int)
    else:
        d[head] = round(d[head] * scale) if as_int else d[head] * scale


def cmd_check(A):
    """Prediction next to a real dungeon-sim.py run of the same knobs (all classes)."""
    b = load_bundle(A)
    knobs = parse_sets(A.set)
    pred = predict(b, A.level, CLASSES, knobs)
    patch_name = b['basePatch']
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import balancepatch as bp
    d = bp.resolve(patch_name)
    for k, s in knobs.items():
        for c in (CLASSES if '{class}' in KNOB_PATHS[k] else ['']):
            scale_path(d, KNOB_PATHS[k].replace('{class}', c.lower()), s, k in INT_KNOBS)
    tmp = os.path.join(ROOT, 'GameData', 'TuningRuns', 'surrogate', f'check-{patch_name}.json')
    json.dump(d, open(tmp, 'w', encoding='utf-8'), indent=1)
    out = tmp.replace('.json', '-result.json')
    print(f'Surrogate L{A.level}: {fmt(pred)}')
    print(f'Running dungeon-sim.py L{A.level} ({A.shards} shards/class x {A.runs} heroes x {A.dungeons} dungeons) ...', flush=True)
    subprocess.run([sys.executable, os.path.join(ROOT, 'Scripts', 'dungeon-sim.py'), '--levels', str(A.level), '--patch', tmp,
                    '--tools', A.tools, '--shards', str(A.shards), '--runs', str(A.runs), '--dungeons', str(A.dungeons),
                    '--seed', str(A.seed), '--parallel', str(A.parallel), '--json', out], cwd=ROOT, check=True,
                   stdout=subprocess.DEVNULL)
    real = json.load(open(out, encoding='utf-8'))
    r = real[0] if isinstance(real, list) else real
    lo, hi = r['clearCI']
    inside = lo <= pred['clear'] <= hi
    print(f'Real      L{A.level}: clear {r["clear"] * 100:5.1f}% (95% CI {lo * 100:.1f}-{hi * 100:.1f})  death {r["death"] * 100:4.1f}%  '
          f'hero acts {r["heroActionsPerFight"]:4.1f}  HP lost {r["hpLostPerFight"] * 100:4.1f}%/fight')
    print(f'Surrogate clear is {"inside" if inside else "OUTSIDE"} the real interval ({(pred["clear"] - r["clear"]) * 100:+.1f} pts).')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('command', choices=['train', 'predict', 'sweep', 'solve', 'check'])
    ap.add_argument('--data', default=DEFAULT_DATA)
    ap.add_argument('--base-patch', default='', help='train / load this base patch (default: the most common one / newest model)')
    ap.add_argument('--level', type=int, default=20)
    ap.add_argument('--levels', default='', help='comma list (predict, sweep)')
    ap.add_argument('--class', dest='cls', default='all', choices=['all'] + CLASSES)
    ap.add_argument('--set', action='append', default=[], help='knob=scale (repeatable)')
    ap.add_argument('--knob', default='enemyDamage')
    ap.add_argument('--range', default='', help='sweep: lo,hi scale (default: trained range)')
    ap.add_argument('--steps', type=int, default=9)
    ap.add_argument('--metric', default='clear', choices=['clear', 'died', 'heroActions', 'enemyActions', 'hpLost'])
    ap.add_argument('--target', type=float, default=75)
    ap.add_argument('--tools', default='Code/bin/SurrogateTools', help='check: folder with the built DF.dll')
    ap.add_argument('--shards', type=int, default=4, help='check: dungeon-sim shards per class')
    ap.add_argument('--runs', type=int, default=2)
    ap.add_argument('--dungeons', type=int, default=6)
    ap.add_argument('--seed', type=int, default=303)
    ap.add_argument('--parallel', type=int, default=max(1, (os.cpu_count() or 8) // 2))
    A = ap.parse_args()
    A.levels = A.levels or str(A.level)
    if A.command == 'sweep' and A.levels == str(A.level):
        A.levels = '10,20,35,50'
    if not os.path.isabs(A.data):
        A.data = os.path.join(ROOT, A.data)
    {'train': cmd_train, 'predict': cmd_predict, 'sweep': cmd_sweep, 'solve': cmd_solve, 'check': cmd_check}[A.command](A)


if __name__ == '__main__':
    main()
