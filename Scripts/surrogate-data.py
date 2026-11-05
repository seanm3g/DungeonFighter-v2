#!/usr/bin/env python3
"""Training data for the balance surrogate (Scripts/surrogate.py): DUNGEONSIM shards under randomized balance knobs.

Each shard is one DUNGEONSIM process: one class, one hero, a random level, and a patch copy whose knobs (KNOBS below) are
scaled by random factors (each knob moves with probability --move-chance, so most shards change a few knobs and the
model can tell them apart). One JSON line per shard goes to the dataset: knob scales, level, class, seed, the dungeon
outcome (cleared / retreated / died of --dungeons) and the compact fight rows. Appends, so reruns grow the same file;
--seed plus the shard index makes every shard replayable.

  python Scripts/surrogate-data.py --shards 400                       (active patch, ~2-3 min per shard)
  python Scripts/surrogate-data.py --shards 200 --levels 15-30 --out GameData/TuningRuns/surrogate/mid.jsonl
  python Scripts/surrogate-data.py --shards 50 --move-chance 0         (baseline-only shards: the patch as it is)
  python Scripts/surrogate-data.py --shards 2000 --spark 18           (also run 18 shards at once on the DGX Spark)
"""
import argparse, json, math, os, random, shutil, subprocess, sys, tempfile, time
from concurrent.futures import ThreadPoolExecutor, as_completed

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from simpool import SIM_SKIP, SimPool, add_args as add_pool_args

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']

# name -> (patch path, low scale, high scale). Scales are drawn log-uniform; "{class}" is the shard's class, "[]" scales
# every element of a list (curve anchors).
KNOBS = {
    'enemyDamage':     ('enemySystem.damageCurve.anchors[].multiplier', 0.6, 1.6),
    'enemyHealth':     ('enemySystem.globalMultipliers.healthMultiplier', 0.6, 1.6),
    # (globalMultipliers.armorMultiplier is left out: enemy baseline armor is 0, so it scales nothing)
    'enemySpeed':      ('enemySystem.globalMultipliers.speedMultiplier', 0.8, 1.25),   # identity reference AGI
    'heroHpPerLevel':  ('character.healthPerLevel', 0.7, 1.4),
    'classHealth':     ('classBalance.{class}.healthMultiplier', 0.75, 1.33),
    'classDamage':     ('classBalance.{class}.damageMultiplier', 0.75, 1.33),
    'armorReduction':  ('combat.armorReductionFactor', 0.6, 1.6),
    'gearArmor':       ('equipmentScaling.armorMultiplier', 0.6, 1.6),
    'critDamage':      ('combatBalance.criticalHitDamageMultiplier', 0.8, 1.3),
    'comboRollDamage': ('combatBalance.rollDamageMultipliers.comboRollDamageMultiplier', 0.8, 1.3),
    'basicRollDamage': ('combatBalance.rollDamageMultipliers.basicRollDamageMultiplier', 0.8, 1.3),
    'comboAmpMax':     ('comboSystem.comboAmplifierMax', 0.7, 1.4),
    'intAmp':          ('combat.intelligenceAmpPerPoint', 0.5, 2.0),
    'strDamage':       ('combat.strengthDamagePerPoint', 0.6, 1.6),
    'agiSoftCap':      ('combat.heroAgilitySpeedSoftCap', 0.7, 1.4),
    'bossDamage':      ('dungeonScaling.bossDamageMultiplier', 0.6, 1.5),
    'enrageDamage':    ('combat.enemyEnrageDamageMult', 0.8, 1.3),
}
# Knobs whose C# config field is an int: the scaled value is rounded and the recorded scale snapped to it. (JSON alone
# cannot tell: enemySystem.globalMultipliers holds 1 for double fields.)
INT_KNOBS = {'heroHpPerLevel', 'agiSoftCap'}
# Fight row fields kept in the dataset (DUNGEONSIM writes many more).
FIGHT_FIELDS = ['Enemy', 'EnemyLevel', 'EnemyPrefix', 'BossFight', 'FightInDungeon', 'HeroActions', 'EnemyActions', 'HeroDied',
                'HpLost', 'HpMinPct', 'SlotMax', 'HeroStrength', 'HeroAgility', 'HeroTechnique', 'HeroIntelligence', 'HeroMaxHp',
                'Sets', 'Skills', 'Strip']

ap = argparse.ArgumentParser()
ap.add_argument('--shards', type=int, default=200, help='shards to add')
ap.add_argument('--out', default='GameData/TuningRuns/surrogate/dataset.jsonl')
ap.add_argument('--patch', default='', help='base balance patch name or path (default: active patch)')
ap.add_argument('--tools', default='Code/bin/SurrogateTools', help='folder for the built DF.dll')
ap.add_argument('--skip-build', action='store_true')
ap.add_argument('--levels', default='1-75', help='hero level range lo-hi (uniform) or a comma list')
ap.add_argument('--classes', default=','.join(CLASSES))
ap.add_argument('--dungeons', type=int, default=8, help='dungeons per shard (hero setup ~1 min, a dungeon ~10 s)')
ap.add_argument('--retreat', type=int, default=50)
ap.add_argument('--move-chance', type=float, default=0.5, help='chance each knob moves off 1.0 in a shard')
ap.add_argument('--seed', type=int, default=7, help='base seed; shard i uses seed*1e6+i for both knobs and DUNGEONSIM')
ap.add_argument('--parallel', type=int, default=max(1, (os.cpu_count() or 8) // 2))
ap.add_argument('--shard-timeout', type=int, default=1800)
add_pool_args(ap)
A = ap.parse_args()

OUT = os.path.join(ROOT, A.out) if not os.path.isabs(A.out) else A.out
WORK = tempfile.mkdtemp(prefix='surr-')
DLL = os.path.join(ROOT, A.tools, 'DF.dll')
LEVELS = (list(range(int(A.levels.split('-')[0]), int(A.levels.split('-')[1]) + 1)) if '-' in A.levels
          else [int(x) for x in A.levels.split(',')])
SHARD_CLASSES = [c.strip() for c in A.classes.split(',') if c.strip()]
POOL = None


def base_patch_path():
    name = A.patch
    if not name:
        prof = os.path.join(ROOT, 'GameData', 'PatchProfile.json')
        name = json.load(open(prof, encoding='utf-8-sig')).get('activeBalancePatch', 'default') if os.path.exists(prof) else 'default'
    if os.path.exists(name):
        return os.path.abspath(name)
    return os.path.join(ROOT, 'GameData', 'Patches', 'Balance', name if name.endswith('.json') else name + '.json')


BASE = json.load(open(base_patch_path(), encoding='utf-8-sig'))
PATCH_NAME = os.path.splitext(os.path.basename(base_patch_path()))[0]


def get_path(d, path):
    """Value at path; a list of values under "[]"."""
    head, _, rest = path.partition('.')
    if head.endswith('[]'):
        return [get_path(x, rest) for x in d[head[:-2]]]
    return get_path(d[head], rest) if rest else d[head]


def scale_path(d, path, scale, as_int):
    """Multiply the value at path (every list element under "[]") by scale."""
    head, _, rest = path.partition('.')
    if head.endswith('[]'):
        for x in d[head[:-2]]:
            scale_path(x, rest, scale, as_int)
    elif rest:
        scale_path(d[head], rest, scale, as_int)
    else:
        d[head] = round(d[head] * scale) if as_int else d[head] * scale


def build():
    print(f'Building tools -> {A.tools}', flush=True)
    r = subprocess.run(['dotnet', 'build', os.path.join(ROOT, 'Code', 'Code.csproj'), '-c', 'Debug', '-o', os.path.join(ROOT, A.tools),
                        f'-p:BaseIntermediateOutputPath={os.path.join(WORK, "obj")}{os.sep}'],
                       cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    if r.returncode != 0:
        print(r.stdout[-3000:]); raise SystemExit('Build failed.')


def copy_gamedata():
    skip = SIM_SKIP
    dst = os.path.join(WORK, 'GameData')
    shutil.copytree(os.path.join(ROOT, 'GameData'), dst, ignore=lambda d, names: [
        n for n in names if (n in skip and os.path.isdir(os.path.join(d, n))) or n.startswith('character_')])
    return dst


def draw(index):
    """Shard index -> (seed, level, class, knob scales). Deterministic, so a shard can be replayed from its index."""
    seed = A.seed * 1_000_000 + index
    rng = random.Random(seed)
    level, cls = rng.choice(LEVELS), rng.choice(SHARD_CLASSES)
    scales = {}
    for name, (path, lo, hi) in KNOBS.items():
        moved = rng.random() < A.move_chance
        s = math.exp(rng.uniform(math.log(lo), math.log(hi))) if moved else 1.0
        if name in INT_KNOBS:   # snap to the scale the rounded value really applies
            base = get_path(BASE, path.replace('{class}', cls.lower()))
            s = round(base * s) / base
        scales[name] = round(s, 4)
    return seed, level, cls, scales


def write_patch(cls, scales, tag):
    d = json.loads(json.dumps(BASE))
    for name, (path, _, _) in KNOBS.items():
        scale_path(d, path.replace('{class}', cls.lower()), scales[name], name in INT_KNOBS)
    d['description'] = f'surrogate shard {tag} on {PATCH_NAME}: ' + ', '.join(f'{k} x{v}' for k, v in scales.items() if v != 1.0)
    path = os.path.join(WORK, f'patch-{tag}.json')
    json.dump(d, open(path, 'w', encoding='utf-8'), indent=1)
    return path


def shard(index):
    seed, level, cls, scales = draw(index)
    tag = f's{index}'
    patch = write_patch(cls, scales, tag)
    out = os.path.join(WORK, f'{tag}.json')
    log_path = os.path.join(WORK, f'{tag}.log')
    t0 = time.time()
    host = POOL.run(['DUNGEONSIM', '--patch', patch, '--level', str(level), '--runs', '1',
                     '--dungeons', str(A.dungeons), '--classes', cls, '--retreat-below', str(A.retreat),
                     '--json', out, '--bot', 'rules', '--seed', str(seed)], out, log_path, timeout=A.shard_timeout)
    try:
        doc = json.load(open(out, encoding='utf-8-sig'))
    except Exception as e:
        return index, None, f'{type(e).__name__} on {host}: see {log_path}'
    o = next(x for x in doc['Outcomes'] if x['Class'] == cls)
    row = {
        'index': index, 'seed': seed, 'basePatch': PATCH_NAME, 'provenance': doc.get('Provenance'),
        'level': level, 'class': cls, 'knobs': scales, 'dungeons': o['Dungeons'], 'cleared': o['Cleared'],
        'retreated': o['Retreated'], 'died': o['Died'], 'fights': o['Fights'], 'levelDrift': o['LevelDrift'],
        'seconds': round(time.time() - t0, 1), 'host': host,
        'fightRows': [{k: f.get(k) for k in FIGHT_FIELDS} for f in doc.get('Fights', [])],
    }
    return index, row, None


def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    start = 0
    if os.path.exists(OUT):
        with open(OUT, encoding='utf-8') as f:
            done = [json.loads(l) for l in f if l.strip()]
        start = max((r['index'] for r in done if r.get('seed', 0) // 1_000_000 == A.seed), default=-1) + 1
        if done and any(r['basePatch'] != PATCH_NAME for r in done):
            print(f'NOTE: {OUT} already holds shards from other base patches; surrogate.py trains per basePatch.')
    if not A.skip_build:
        build()
    global POOL
    POOL = SimPool(ROOT, WORK, copy_gamedata(), DLL, local_slots=A.parallel, spark_slots=A.spark, host=A.spark_host,
                   log=lambda m: print(m, flush=True))
    POOL.start()
    print(f'Base patch {PATCH_NAME} | shards {start}..{start + A.shards - 1} | levels {LEVELS[0]}-{LEVELS[-1]} | '
          f'{A.dungeons} dungeons/shard | {POOL.describe()} | work {WORK}', flush=True)
    ok = failed = 0
    t0 = time.time()
    with ThreadPoolExecutor(POOL.slots) as pool, open(OUT, 'a', encoding='utf-8') as sink:
        futures = [pool.submit(shard, i) for i in range(start, start + A.shards)]
        for fut in as_completed(futures):
            index, row, err = fut.result()
            if row is None:
                failed += 1
                print(f'  shard {index} failed ({err})', flush=True)
                continue
            sink.write(json.dumps(row) + '\n'); sink.flush()
            ok += 1
            el = time.time() - t0
            print(f'  [{ok + failed}/{A.shards}] shard {index} ({row["host"]}): L{row["level"]} {row["class"]} cleared {row["cleared"]}/{row["dungeons"]}'
                  f' died {row["died"]} fights {row["fights"]} ({row["seconds"]:.0f} s; eta {el / (ok + failed) * (A.shards - ok - failed) / 60:.0f} min)',
                  flush=True)
    print(f'Done: {ok} shards added, {failed} failed -> {OUT}' + (f' | by host {POOL.counts}' if A.spark else ''))
    POOL.close()
    shutil.rmtree(WORK, ignore_errors=True)


if __name__ == '__main__':
    main()
